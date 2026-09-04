namespace DespensaApp.Services;

public interface IConfiguracionService
{
    Task<string> ObtenerValorAsync(string clave, string valorPorDefecto = "");
    Task<Dictionary<string, string>> ObtenerTodasAsync();
    Task SetValorAsync(string clave, string valor);
    Task SetVariosAsync(Dictionary<string, string> pares);
}
