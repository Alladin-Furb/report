using Microsoft.EntityFrameworkCore;
using TransporteEscolar.Relatorios.Infrastructure.Persistence.Context;

namespace TransporteEscolar.Relatorios.Api.Extensions;

public static class DatabaseMigrationExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseMigration");

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<RelatoriosDbContext>();
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Esquema do banco de relatórios garantido.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Não foi possível aplicar as migrations do banco de relatórios.");
            throw;
        }
    }
}
