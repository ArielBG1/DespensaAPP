using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DespensaApp.Models.Entities;

public enum MetodoPago { Efectivo, Tarjeta, Transferencia, QR }
public enum EstadoVenta { EnProceso, Completada, Anulada }

[Table("ventas")]
public class Venta
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("numero_ticket")]
    [Required, MaxLength(30)]
    public string NumeroTicket { get; set; } = string.Empty;

    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("subtotal", TypeName = "numeric(12,2)")]
    public decimal Subtotal { get; set; }

    [Column("descuento", TypeName = "numeric(12,2)")]
    public decimal Descuento { get; set; }

    [Column("total", TypeName = "numeric(12,2)")]
    public decimal Total { get; set; }

    [Column("metodo_pago")]
    public MetodoPago MetodoPago { get; set; }

    [Column("monto_recibido", TypeName = "numeric(12,2)")]
    public decimal MontoRecibido { get; set; }

    [Column("vuelto", TypeName = "numeric(12,2)")]
    public decimal Vuelto { get; set; }

    [Column("estado")]
    public EstadoVenta Estado { get; set; } = EstadoVenta.EnProceso;

    [Column("observacion")]
    [MaxLength(500)]
    public string? Observacion { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UsuarioId")]
    public Usuario? Usuario { get; set; }

    public ICollection<DetalleVenta> Detalles { get; set; } = [];
}
