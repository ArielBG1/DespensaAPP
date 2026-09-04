using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DespensaApp.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DespensaDbContext>
{
    public DespensaDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = ConnectionStringHelper.Build(config);

        var optionsBuilder = new DbContextOptionsBuilder<DespensaDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new DespensaDbContext(optionsBuilder.Options);
    }
}
