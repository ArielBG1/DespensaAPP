using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DespensaApp.Models.Entities;

public enum TipoMovimiento
{
    Entrada,
    Salida,
    AjustePositivo,
    AjusteNegativo,
    Venta,
    Compra,
    Devolucion
}

[Table("movimientos_stock")]
public class MovimientoStock
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("producto_id")]
    public int ProductoId { get; set; }

    [Column("tipo")]
    public TipoMovimiento Tipo { get; set; }

    [Column("cantidad", TypeName = "numeric(12,3)")]
    public decimal Cantidad { get; set; }

    [Column("stock_antes", TypeName = "numeric(12,3)")]
    public decimal StockAntes { get; set; }

    [Column("stock_despues", TypeName = "numeric(12,3)")]
    public decimal StockDespues { get; set; }

    [Column("motivo")]
    [MaxLength(300)]
    public string? Motivo { get; set; }

    [Column("referencia_id")]
    public int? ReferenciaId { get; set; }

    [Column("usuario_id")]
    public int UsuarioId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ProductoId")]
    public Producto? Producto { get; set; }

    [ForeignKey("UsuarioId")]
    public Usuario? Usuario { get; set; }
}
