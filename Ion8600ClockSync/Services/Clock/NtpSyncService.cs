using GuerrillaNtp;

namespace Ion8600ClockSync.Services.Clock;

public class NtpSyncService
{
    private readonly string[] _servers =
    {
        "a.ntp.br",
        "b.ntp.br",
        "pool.ntp.org",
    };

    public async Task<NtpResult> GetTimeAsync()
    {
        foreach (var server in _servers)
        {
            try
            {
                var client = new NtpClient(server);
                NtpClock clock = await client.QueryAsync();
                return new NtpResult
                {
                    Success = true,
                    Server = server,
                    UtcTime = clock.UtcNow.UtcDateTime,
                    OffsetMs = clock.CorrectionOffset.TotalMilliseconds,
                };
            }
            catch
            {
            }
        }

        return new NtpResult { Success = false };
    }
}

public class NtpResult
{
    public bool Success { get; set; }
    public string? Server { get; set; }
    public DateTime UtcTime { get; set; }
    public double OffsetMs { get; set; }
}
