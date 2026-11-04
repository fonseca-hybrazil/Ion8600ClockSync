using System.Collections.Concurrent;

namespace Ion8600ClockSync.Services;

public record MedidorLiveStatus(DateTime HorarioMedidorUtc, DateTime CapturadoEmUtc, bool Sucesso, string? MensagemErro);

public class MedidorLiveStatusStore
{
    private readonly ConcurrentDictionary<int, MedidorLiveStatus> _status = new();

    public void Set(int medidorId, MedidorLiveStatus status) => _status[medidorId] = status;

    public MedidorLiveStatus? Get(int medidorId) => _status.TryGetValue(medidorId, out var status) ? status : null;
}
