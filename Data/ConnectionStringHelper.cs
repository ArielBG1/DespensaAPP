using Microsoft.Extensions.Configuration;
using Npgsql;

namespace DespensaApp.Data;

public static class ConnectionStringHelper
{
    /// <summary>
    /// Arma el connection string final. En producción se usa DATABASE_URL (la URL que entregan
    /// Supabase/Render/Neon). En local se usa "ConnectionStrings:DefaultConnection" (sin contraseña
    /// en appsettings.json) más la contraseña de la variable de entorno DESPENSA_DB_PASSWORD,
    /// para no guardar credenciales en texto plano en el repo.
    /// </summary>
    public static string Build(IConfiguration config)
    {
        var databaseUrl = config["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
            return FromUrl(databaseUrl);

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:DefaultConnection en la configuración.");

        var password = config["DESPENSA_DB_PASSWORD"];
        if (string.IsNullOrEmpty(password))
            return connectionString;

        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Password = password };
        return builder.ConnectionString;
    }

    /// <summary>
    /// Convierte una URL tipo postgresql://usuario:clave@host:puerto/base al formato de Npgsql.
    /// </summary>
    private static string FromUrl(string databaseUrl)
    {
        var uri = new Uri(databaseUrl.Trim());
        var userInfo = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(userInfo[0]),
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }
}
