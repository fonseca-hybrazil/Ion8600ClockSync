using Ion8600ClockSync.Services;

namespace Ion8600ClockSync.Models;

public class MedidorStatusDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Ip { get; set; } = "";
    public bool Ativo { get; set; }
    public bool DnpHabilitado { get; set; }
    public SincronismoHistorico? UltimoSync { get; set; }
    public MedidorLiveStatus? HorarioAtual { get; set; }
}
