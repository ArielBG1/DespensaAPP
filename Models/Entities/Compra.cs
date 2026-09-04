using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DespensaApp.Models.Entities;

public enum EstadoCompra { Pendiente, Recibida, Anulada }

[Table("compras")]
public class Compra
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("numero_remito")]
    [MaxLength(50)]
    public string? NumeroRemito { get; set; }

    [Column("proveedor_id")]
    public int ProveedorId { get; set; }

    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("total", TypeName = "numeric(12,2)")]
    public decimal Total { get; set; }

    [Column("estado")]
    public EstadoCompra Estado { get; set; } = EstadoCompra.Pendiente;

    [Column("observacion")]
    [MaxLength(500)]
    public string? Observacion { get; set; }

    [Column("fecha_compra")]
    public DateTime FechaCompra { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ProveedorId")]
    public Proveedor? Proveedor { get; set; }

    [ForeignKey("UsuarioId")]
    public Usuario? Usuario { get; set; }

    public ICollection<DetalleCompra> Detalles { get; set; } = [];
}
