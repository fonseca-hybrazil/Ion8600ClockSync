namespace Ion8600ClockSync.Services;

public sealed class SincronismoLogWriter
{
    private static readonly TimeZoneInfo FusoBrasil = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    private readonly ILogger<SincronismoLogWriter> _logger;

    public SincronismoLogWriter(IHostEnvironment env, ILogger<SincronismoLogWriter> logger)
    {
        _logger = logger;
        var logsDir = Path.Combine(env.ContentRootPath, "logs");
        Directory.CreateDirectory(logsDir);
        LogFilePath = Path.Combine(logsDir, "sincronismo.log");
    }

    public string LogFilePath { get; }

    public async Task LogAsync(string medidorNome, string motivo, bool sucesso, string? mensagemErro, int? delayMs, long? desvioMs, DateTime horarioUtc)
    {
        var horarioLocal = TimeZoneInfo.ConvertTimeFromUtc(horarioUtc, FusoBrasil);
        var resultado = sucesso ? "OK" : "FALHOU";
        var detalhe = sucesso ? $"delay={delayMs}ms desvio={desvioMs}ms" : mensagemErro ?? "";

        var linha = $"{horarioLocal:yyyy-MM-dd HH:mm:ss} -03  {medidorNome,-28} motivo={motivo,-24} {resultado,-6} {detalhe}";

        try
        {
            await File.AppendAllTextAsync(LogFilePath, linha + Environment.NewLine);
        }
        catch (Exception ex)
        {
            // Nunca deixa uma falha ao gravar o log (ex: disco cheio) derrubar o sincronismo em si.
            _logger.LogWarning(ex, "Falha ao gravar no arquivo de log de sincronismo ({Path})", LogFilePath);
        }
    }

    public async Task<IReadOnlyList<string>> ReadLastLinesAsync(int maxLinhas)
    {
        if (!File.Exists(LogFilePath))
            return [];

        var linhas = await File.ReadAllLinesAsync(LogFilePath);
        return linhas.TakeLast(maxLinhas).Reverse().ToList();
    }
}
