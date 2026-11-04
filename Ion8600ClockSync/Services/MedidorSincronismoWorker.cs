using Ion8600ClockSync.Models;
using Ion8600ClockSync.Services.Clock;
using Ion8600ClockSync.Services.Dnp3;
using Microsoft.EntityFrameworkCore;

namespace Ion8600ClockSync.Services;

public class MedidorSincronismoWorker(
    ILogger<MedidorSincronismoWorker> logger,
    IServiceScopeFactory scopeFactory,
    StableClockService clock,
    MedidorLiveStatusStore liveStatus,
    SincronismoLogWriter syncLog,
    IConfiguration configuration) : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(configuration.GetValue("AppSettings:CheckIntervalSeconds", 30));
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(configuration.GetValue("AppSettings:SyncIntervalMinutes", 360));
    private readonly ushort _masterAddress = (ushort)configuration.GetValue("AppSettings:MasterAddress", 1);
    private readonly TimeSpan _dnpTimeout = TimeSpan.FromSeconds(configuration.GetValue("AppSettings:DnpTimeoutSeconds", 10));
    private readonly Dictionary<int, DateTime> _ultimoSincronismoUtc = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Espera o NTP corrigir nosso relógio de referência antes do primeiro ciclo - sem isso,
        // o primeiro ciclo usaria DateTime.UtcNow local (potencialmente impreciso) pra decidir
        // o horário a escrever no medidor.
        await clock.WaitForFirstSyncAsync(TimeSpan.FromSeconds(10), stoppingToken);

        using var timer = new PeriodicTimer(_checkInterval);
        do
        {
            try
            {
                await VerificarTodosAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Erro no ciclo de verificação de sincronismo (ex: banco de dados indisponível). O site continua no ar.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task VerificarTodosAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Dnp3SyncDbContext>();

        var medidores = await db.Medidores
            .Where(m => m.Ativo && m.DnpHabilitado)
            .ToListAsync(stoppingToken);

        var historicos = new List<SincronismoHistorico>();
        foreach (var medidor in medidores)
        {
            var historico = await VerificarEsSincronizarSeNecessarioAsync(medidor, stoppingToken);
            if (historico is not null)
                historicos.Add(historico);
        }

        if (historicos.Count > 0)
        {
            db.SincronismoHistoricos.AddRange(historicos);
            await db.SaveChangesAsync(stoppingToken);
        }
    }

    private async Task<SincronismoHistorico?> VerificarEsSincronizarSeNecessarioAsync(Medidor medidor, CancellationToken stoppingToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeoutCts.CancelAfter(_dnpTimeout);

        await using var client = new Dnp3Client(medidor.Ip, medidor.Porta, (ushort)medidor.EnderecoDnp, _masterAddress);

        bool precisaSincronizar;
        string motivo = "";
        try
        {
            // Leitura leve pra exibir o horário atual do medidor na tela e checar os bits IIN
            // (NEED_TIME / DEVICE_RESTART).
            var horarioLido = await client.ReadTimeAsync(timeoutCts.Token);
            liveStatus.Set(medidor.Id, new MedidorLiveStatus(horarioLido.UtcDateTime, clock.UtcNow(), Sucesso: true, MensagemErro: null));

            bool passouIntervalo = !_ultimoSincronismoUtc.TryGetValue(medidor.Id, out var ultimo)
                || clock.UtcNow() - ultimo >= _syncInterval;

            var motivos = new List<string>(3);
            if (client.LastIndications.DeviceRestart)
                motivos.Add("DEVICE_RESTART");
            if (client.LastIndications.NeedTime)
                motivos.Add("NEED_TIME");
            if (passouIntervalo)
                motivos.Add("Tempo");

            if (client.LastIndications.DeviceRestart)
                logger.LogInformation("Medidor {Nome} sinalizou DEVICE_RESTART - sincronizando imediatamente.", medidor.Nome);
            else if (client.LastIndications.NeedTime)
                logger.LogInformation("Medidor {Nome} sinalizou NEED_TIME - sincronizando imediatamente.", medidor.Nome);

            precisaSincronizar = motivos.Count > 0;
            motivo = string.Join(" + ", motivos);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao verificar status do medidor {Nome} ({Ip})", medidor.Nome, medidor.Ip);
            liveStatus.Set(medidor.Id, new MedidorLiveStatus(default, clock.UtcNow(), Sucesso: false, MensagemErro: ex.Message));
            return new SincronismoHistorico
            {
                IdMedidor = medidor.Id,
                DataHoraUtc = clock.UtcNow(),
                Sucesso = false,
                MensagemErro = ex.Message,
            };
        }

        return precisaSincronizar
            ? await SincronizarMedidorAsync(medidor, client, motivo, timeoutCts.Token)
            : null;
    }

    private async Task<SincronismoHistorico> SincronizarMedidorAsync(Medidor medidor, Dnp3Client client, string motivo, CancellationToken ct)
    {
        var historico = new SincronismoHistorico
        {
            IdMedidor = medidor.Id,
            DataHoraUtc = clock.UtcNow(),
        };

        try
        {
            int delayMs = await client.MeasureDelayMsAsync(ct);
            var horarioEnviadoUtc = clock.UtcNow().AddMilliseconds(delayMs / 2.0);

            await client.WriteTimeAsync(new DateTimeOffset(horarioEnviadoUtc, TimeSpan.Zero), ct);
            var horarioApos = await client.ReadTimeAsync(ct);

            historico.Sucesso = true;
            historico.DelayMedidoMs = delayMs;
            historico.HorarioEnviado = horarioEnviadoUtc;
            historico.HorarioMedidorApos = horarioApos.UtcDateTime;
            historico.DesvioMs = (long)(horarioApos.UtcDateTime - clock.UtcNow()).TotalMilliseconds;

            _ultimoSincronismoUtc[medidor.Id] = clock.UtcNow();
            liveStatus.Set(medidor.Id, new MedidorLiveStatus(horarioApos.UtcDateTime, clock.UtcNow(), Sucesso: true, MensagemErro: null));

            logger.LogInformation(
                "Sincronismo OK no medidor {Nome} ({Ip}): delay={DelayMs}ms, desvio={DesvioMs}ms",
                medidor.Nome, medidor.Ip, delayMs, historico.DesvioMs);

            // O bit IIN1.7 (DEVICE_RESTART) não é limpo automaticamente pelo Write de Time and Date -
            // o medidor continua sinalizando restart (e forçando full sync a cada ciclo) até o master
            // confirmar explicitamente com um Write em Group80Var1. Sem isso o SyncIntervalMinutes é
            // ignorado na prática pra qualquer medidor que já tenha sinalizado DEVICE_RESTART uma vez.
            if (client.LastIndications.DeviceRestart)
            {
                try
                {
                    await client.ClearDeviceRestartAsync(ct);
                    logger.LogInformation("IIN1.7 (DEVICE_RESTART) confirmado/limpo no medidor {Nome}.", medidor.Nome);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Falha ao limpar IIN1.7 (DEVICE_RESTART) do medidor {Nome} - tentará novamente no próximo ciclo.", medidor.Nome);
                }
            }
        }
        catch (Exception ex)
        {
            historico.Sucesso = false;
            historico.MensagemErro = ex.Message;
            logger.LogWarning(ex, "Falha ao sincronizar medidor {Nome} ({Ip})", medidor.Nome, medidor.Ip);
        }

        await syncLog.LogAsync(medidor.Nome, motivo, historico.Sucesso, historico.MensagemErro, historico.DelayMedidoMs, historico.DesvioMs, historico.DataHoraUtc);

        return historico;
    }
}
