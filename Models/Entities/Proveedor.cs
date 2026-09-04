using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DespensaApp.Models.Entities;

[Table("proveedores")]
public class Proveedor
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    [Required, MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [Column("contacto")]
    [MaxLength(100)]
    public string? Contacto { get; set; }

    [Column("telefono")]
    [MaxLength(30)]
    public string? Telefono { get; set; }

    [Column("email")]
    [MaxLength(150)]
    public string? Email { get; set; }

    [Column("direccion")]
    [MaxLength(250)]
    public string? Direccion { get; set; }

    [Column("activo")]
    public bool Activo { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Producto> Productos { get; set; } = [];
    public ICollection<Compra> Compras { get; set; } = [];
}
