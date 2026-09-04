using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DespensaApp.Models.Entities;

[Table("codigos_barra_alternativos")]
public class CodigoBarraAlternativo
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("producto_id")]
    public int ProductoId { get; set; }

    [Column("codigo")]
    [Required, MaxLength(50)]
    public string Codigo { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ProductoId")]
    public Producto? Producto { get; set; }
}
