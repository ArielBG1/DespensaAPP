namespace DespensaApp.Models.ViewModels;

public class EtiquetaItemViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? CodigoBarra { get; set; }

    public bool EsEan13 => CodigoBarra is { Length: 13 } c && c.All(char.IsDigit);
}

public class EtiquetasViewModel
{
    public List<EtiquetaItemViewModel> Productos { get; set; } = [];
}
