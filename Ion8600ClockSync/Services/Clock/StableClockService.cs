using System.Diagnostics;

namespace Ion8600ClockSync.Services.Clock;

public class StableClockService
{
    private readonly Stopwatch _sw;
    private readonly object _lock = new();
    private readonly TaskCompletionSource _firstSync = new(TaskCreationOptions.RunContinuationsAsynchronously);
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

        _firstSync.TrySetResult();
    }

    public DateTime UtcNow()
    {
        lock (_lock)
        {
            return _baseUtc + _sw.Elapsed;
        }
    }

    /// <summary>
    /// Espera o primeiro sincronismo NTP bem-sucedido, para evitar usar o relógio de
    /// referência (ainda baseado só no DateTime.UtcNow local) antes dele ser corrigido.
    /// Se o NTP não responder dentro do timeout, segue em frente mesmo assim.
    /// </summary>
    public async Task WaitForFirstSyncAsync(TimeSpan timeout, CancellationToken ct)
    {
        var delay = Task.Delay(timeout, ct);
        await Task.WhenAny(_firstSync.Task, delay);
    }
}
