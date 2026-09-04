using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DespensaApp.Models.Entities;

[Table("detalle_ventas")]
public class DetalleVenta
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("venta_id")]
    public int VentaId { get; set; }

    [Column("producto_id")]
    public int ProductoId { get; set; }

    [Column("cantidad", TypeName = "numeric(12,3)")]
    public decimal Cantidad { get; set; }

    [Column("precio_unitario", TypeName = "numeric(12,2)")]
    public decimal PrecioUnitario { get; set; }

    [Column("descuento_item", TypeName = "numeric(12,2)")]
    public decimal DescuentoItem { get; set; }

    [Column("subtotal", TypeName = "numeric(12,2)")]
    public decimal Subtotal { get; set; }

    [Column("es_por_peso")]
    public bool EsPorPeso { get; set; }

    [Column("nombre_producto")]
    [MaxLength(200)]
    public string NombreProducto { get; set; } = string.Empty;

    [Column("codigo_barra")]
    [MaxLength(50)]
    public string? CodigoBarra { get; set; }

    [ForeignKey("VentaId")]
    public Venta? Venta { get; set; }

    [ForeignKey("ProductoId")]
    public Producto? Producto { get; set; }
}
