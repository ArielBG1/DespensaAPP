using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DespensaApp.Models.Entities;

[Table("configuraciones")]
public class Configuracion
{
    [Key]
    [Column("clave")]
    [MaxLength(100)]
    public string Clave { get; set; } = string.Empty;

    [Column("valor")]
    [MaxLength(500)]
    public string Valor { get; set; } = string.Empty;

    [Column("descripcion")]
    [MaxLength(300)]
    public string? Descripcion { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
