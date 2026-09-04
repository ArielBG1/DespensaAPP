using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DespensaApp.Models.Entities;

public enum RolUsuario { Admin, Cajero }

[Table("usuarios")]
public class Usuario
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("nombre")]
    [Required, MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Column("email")]
    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("rol")]
    public RolUsuario Rol { get; set; }

    [Column("activo")]
    public bool Activo { get; set; } = true;

    [Column("ultimo_acceso")]
    public DateTime? UltimoAcceso { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Venta> Ventas { get; set; } = [];
    public ICollection<Compra> Compras { get; set; } = [];
    public ICollection<MovimientoStock> MovimientosStock { get; set; } = [];
}
