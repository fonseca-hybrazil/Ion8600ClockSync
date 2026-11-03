namespace Ion8600ClockSync.Services.Clock;

public class NtpClockSyncWorker(ILogger<NtpClockSyncWorker> logger, StableClockService clock, NtpSyncService ntp) : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SyncOnceAsync();

        using var timer = new PeriodicTimer(SyncInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SyncOnceAsync();
        }
    }

    private async Task SyncOnceAsync()
    {
        try
        {
            var result = await ntp.GetTimeAsync();
            if (result.Success)
            {
                clock.Sync(result.UtcTime);
                logger.LogInformation("Sincronismo NTP OK via {Server}: {UtcTime:O}", result.Server, result.UtcTime);
            }
            else
            {
                logger.LogWarning("Falha ao sincronizar com todos os servidores NTP configurados.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro durante sincronismo NTP.");
        }
    }
}
