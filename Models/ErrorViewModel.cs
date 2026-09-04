namespace DespensaApp.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    public int StatusCode { get; set; } = 500;
    public string StatusMessage => StatusCode switch
    {
        404 => "Página no encontrada",
        403 => "Acceso denegado",
        _   => "Error interno del servidor"
    };
}
