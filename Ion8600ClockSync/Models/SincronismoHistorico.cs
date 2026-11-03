using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ion8600ClockSync.Models;

public class SincronismoHistorico
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("id_medidor")]
    public int IdMedidor { get; set; }

    [Column("data_hora_utc")]
    public DateTime DataHoraUtc { get; set; }

    [Column("sucesso")]
    public bool Sucesso { get; set; }

    [Column("delay_medido_ms")]
    public int? DelayMedidoMs { get; set; }

    [Column("horario_enviado")]
    public DateTime? HorarioEnviado { get; set; }

    [Column("horario_medidor_apos")]
    public DateTime? HorarioMedidorApos { get; set; }

    [Column("desvio_ms")]
    public long? DesvioMs { get; set; }

    [Column("mensagem_erro")]
    [StringLength(500)]
    public string? MensagemErro { get; set; }

    [ForeignKey("IdMedidor")]
    [InverseProperty("SincronismoHistoricos")]
    public virtual Medidor IdMedidorNavigation { get; set; } = null!;
}
