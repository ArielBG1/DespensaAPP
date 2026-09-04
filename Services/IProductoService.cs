using DespensaApp.Models.Entities;
using DespensaApp.Models.ViewModels;

namespace DespensaApp.Services;

public interface IProductoService
{
    Task<ProductoIndexViewModel> GetListadoAsync(ProductoFiltroViewModel filtro);
    Task<Producto?> GetByIdAsync(int id);
    Task<ProductoFormViewModel> GetFormViewModelAsync(int? id = null);
    Task PopulateDropdownsAsync(ProductoFormViewModel vm);
    Task<(bool success, string? error)> CreateAsync(ProductoFormViewModel vm, int usuarioId);
    Task<(bool success, string? error)> UpdateAsync(ProductoFormViewModel vm, int usuarioId);
    Task<bool> DeleteLogicalAsync(int id);
    Task<(bool success, string? error)> AjustarStockAsync(AjusteStockViewModel vm, int usuarioId);
    Task<MovimientoFiltroViewModel?> GetMovimientosAsync(int productoId, DateTime? desde, DateTime? hasta);
    Task<object> CheckCodigoBarraAsync(string codigo, int idActual);
    Task<string> GenerarCodigoBarraInternoAsync();
    Task<EtiquetasViewModel> GetProductosParaEtiquetasAsync(int[] ids);
}
