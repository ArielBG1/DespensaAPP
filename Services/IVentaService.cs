using DespensaApp.Models.ViewModels;
using DespensaApp.Models.Entities;

namespace DespensaApp.Services;

public interface IVentaService
{
    Task<(bool success, string? error, int ventaId, string numeroTicket)> ProcesarVentaAsync(
        ProcesarVentaDto dto, int usuarioId);

    Task<Venta?> GetByIdAsync(int id);

    Task<VentaIndexViewModel> GetListadoAsync(
        VentaFiltroViewModel filtro, int usuarioId, bool isAdmin);

    Task<(bool success, string? error)> AnularVentaAsync(int ventaId, int usuarioId);

    Task<ProductoBusquedaDto?> BuscarPorCodigoAsync(string codigo);

    Task<List<ProductoBusquedaDto>> BuscarPorNombreAsync(string q);

    Task<List<VentaListItemViewModel>> GetParaExportAsync(VentaFiltroViewModel filtro, int usuarioId, bool isAdmin);
}
