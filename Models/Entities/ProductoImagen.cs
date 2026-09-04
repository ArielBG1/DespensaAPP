using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DespensaApp.Models.Entities;

/// <summary>
/// Imagen del producto guardada en la base (bytea) y no en disco, para que sobreviva a los
/// reinicios del hosting. Va en una tabla aparte para que los listados de productos no
/// arrastren los bytes de la foto en cada consulta.
/// </summary>
[Table("producto_imagenes")]
public class ProductoImagen
{
    [Key]
    [Column("producto_id")]
    public int ProductoId { get; set; }

    [Column("datos")]
    public byte[] Datos { get; set; } = [];

    [Column("content_type")]
    [Required, MaxLength(100)]
    public string ContentType { get; set; } = "image/jpeg";

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(ProductoId))]
    public Producto? Producto { get; set; }
}
