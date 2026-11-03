using System.Diagnostics;

namespace Ion8600ClockSync.Services.Clock;

public class StableClockService
{
    private readonly Stopwatch _sw;
    private readonly object _lock = new();
    private DateTime _baseUtc;

    public StableClockService()
    {
        _baseUtc = DateTime.UtcNow;
        _sw = Stopwatch.StartNew();
    }

    public void Sync(DateTime utc)
    {
        lock (_lock)
        {
            _baseUtc = utc - _sw.Elapsed;
        }
    }

    public DateTime UtcNow()
    {
        lock (_lock)
        {
            return _baseUtc + _sw.Elapsed;
        }
    }
}
