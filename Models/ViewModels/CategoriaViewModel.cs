namespace DespensaApp.Models.ViewModels;

public record CategoriaListItemVm(int Id, string Nombre, string? Descripcion, bool Activo, int NumProductos);
