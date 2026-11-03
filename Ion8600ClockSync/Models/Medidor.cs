using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ion8600ClockSync.Models;

public class Medidor
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nome")]
    [StringLength(50)]
    public string Nome { get; set; } = "";

    [Column("ip")]
    [StringLength(45)]
    public string Ip { get; set; } = "";

    [Column("porta")]
    public int Porta { get; set; } = 20000;

    [Column("endereco_dnp")]
    public int EnderecoDnp { get; set; } = 100;

    [Column("ativo")]
    public bool Ativo { get; set; } = true;

    [Column("dnp_habilitado")]
    public bool DnpHabilitado { get; set; } = false;

    [InverseProperty("IdMedidorNavigation")]
    public virtual ICollection<SincronismoHistorico> SincronismoHistoricos { get; set; } = new List<SincronismoHistorico>();
}
