using DespensaApp.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DespensaApp.Models.ViewModels;

public class ProductoFiltroViewModel
{
    public int? CategoriaId { get; set; }
    public TipoVenta? TipoVenta { get; set; }
    public bool? Activo { get; set; }
    public string? Busqueda { get; set; }
    public bool SoloStockBajo { get; set; }
    public int Pagina { get; set; } = 1;
    public const int ItemsPorPagina = 20;
}

public class ProductoListItemViewModel
{
    public int Id { get; set; }
    public string? CodigoBarra { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public TipoVenta TipoVenta { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal StockActual { get; set; }
    public decimal StockMinimo { get; set; }
    public bool Activo { get; set; }
    public string? ImagenUrl { get; set; }
    public DateTime? FechaVencimiento { get; set; }

    public const int DiasAlertaVencimiento = 7;

    public string? VencimientoClase
    {
        get
        {
            if (!FechaVencimiento.HasValue) return null;
            var dias = (FechaVencimiento.Value.Date - DateTime.UtcNow.Date).Days;
            if (dias < 0) return "danger";
            if (dias <= DiasAlertaVencimiento) return "warning";
            return null;
        }
    }

    public string? VencimientoTexto
    {
        get
        {
            if (!FechaVencimiento.HasValue) return null;
            var dias = (FechaVencimiento.Value.Date - DateTime.UtcNow.Date).Days;
            if (dias < 0) return $"Venció hace {-dias} día(s)";
            if (dias == 0) return "Vence hoy";
            if (dias <= DiasAlertaVencimiento) return $"Vence en {dias} día(s)";
            return null;
        }
    }

    public string StockClase
    {
        get
        {
            if (StockActual <= 0) return "danger";
            if (StockMinimo <= 0) return "success";
            if (StockActual <= StockMinimo) return "danger";
            if (StockActual <= StockMinimo * 1.5m) return "warning";
            return "success";
        }
    }

    public string StockTexto
    {
        get
        {
            if (StockActual <= 0) return "Sin stock";
            if (StockMinimo <= 0) return "OK";
            if (StockActual <= StockMinimo) return "Bajo mínimo";
            if (StockActual <= StockMinimo * 1.5m) return "Por agotar";
            return "OK";
        }
    }

    public string TipoVentaLabel => TipoVenta switch
    {
        TipoVenta.Unidad => "Unidad",
        TipoVenta.Kg => "Por Kg",
        TipoVenta.Gramo => "Por Gramo",
        TipoVenta.Cien_g => "Por 100g",
        _ => TipoVenta.ToString()
    };

    public string UnidadStock => TipoVenta switch
    {
        TipoVenta.Unidad => "u",
        TipoVenta.Kg => "kg",
        TipoVenta.Gramo => "g",
        TipoVenta.Cien_g => "×100g",
        _ => ""
    };
}

public class ProductoIndexViewModel
{
    public List<ProductoListItemViewModel> Productos { get; set; } = [];
    public ProductoFiltroViewModel Filtro { get; set; } = new();
    public int TotalItems { get; set; }
    public int TotalPaginas { get; set; }
    public List<SelectListItem> Categorias { get; set; } = [];
}
