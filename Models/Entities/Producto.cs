using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DespensaApp.Models.Entities;

public enum TipoVenta { Unidad, Kg, Gramo, Cien_g }

[Table("productos")]
public class Producto
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("codigo_barra")]
    [MaxLength(50)]
    public string? CodigoBarra { get; set; }

    [Column("nombre")]
    [Required, MaxLength(200)]
    public string Nombre { get; set; } = string.Empty;

    [Column("descripcion")]
    [MaxLength(500)]
    public string? Descripcion { get; set; }

    [Column("categoria_id")]
    public int? CategoriaId { get; set; }

    [Column("proveedor_id")]
    public int? ProveedorId { get; set; }

    [Column("tipo_venta")]
    public TipoVenta TipoVenta { get; set; } = TipoVenta.Unidad;

    [Column("precio_compra", TypeName = "numeric(12,2)")]
    public decimal PrecioCompra { get; set; }

    [Column("precio_venta", TypeName = "numeric(12,2)")]
    public decimal PrecioVenta { get; set; }

    [Column("stock_actual", TypeName = "numeric(12,3)")]
    public decimal StockActual { get; set; }

    [Column("stock_minimo", TypeName = "numeric(12,3)")]
    public decimal StockMinimo { get; set; }

    [Column("imagen_url")]
    [MaxLength(500)]
    public string? ImagenUrl { get; set; }

    [Column("fecha_vencimiento", TypeName = "date")]
    public DateTime? FechaVencimiento { get; set; }

    [Column("activo")]
    public bool Activo { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("CategoriaId")]
    public Categoria? Categoria { get; set; }

    [ForeignKey("ProveedorId")]
    public Proveedor? Proveedor { get; set; }

    public ICollection<DetalleVenta> DetallesVenta { get; set; } = [];
    public ICollection<DetalleCompra> DetallesCompra { get; set; } = [];
    public ICollection<MovimientoStock> MovimientosStock { get; set; } = [];
    public ICollection<CodigoBarraAlternativo> CodigosAlternativos { get; set; } = [];

    public ProductoImagen? Imagen { get; set; }
}
