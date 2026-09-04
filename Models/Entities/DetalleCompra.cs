using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DespensaApp.Models.Entities;

[Table("detalle_compras")]
public class DetalleCompra
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("compra_id")]
    public int CompraId { get; set; }

    [Column("producto_id")]
    public int ProductoId { get; set; }

    [Column("cantidad", TypeName = "numeric(12,3)")]
    public decimal Cantidad { get; set; }

    [Column("precio_unitario", TypeName = "numeric(12,2)")]
    public decimal PrecioUnitario { get; set; }

    [Column("subtotal", TypeName = "numeric(12,2)")]
    public decimal Subtotal { get; set; }

    [ForeignKey("CompraId")]
    public Compra? Compra { get; set; }

    [ForeignKey("ProductoId")]
    public Producto? Producto { get; set; }
}
