using Microsoft.EntityFrameworkCore;
using System.Data;
using TransporteEscolar.Relatorios.Infrastructure.Persistence.Context;

namespace TransporteEscolar.Relatorios.Api.Extensions;

public static class DatabaseMigrationExtensions
{
    private const string InitialMariaDbMigration = "20260621182034_InitialMariaDb";

    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseMigration");

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<RelatoriosDbContext>();
            await BaselineLegacyMariaDbAsync(context, logger);
            await context.Database.MigrateAsync();
            logger.LogInformation("Migrations do banco de relatórios aplicadas.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Não foi possível aplicar as migrations do banco de relatórios.");
            throw;
        }
    }

    private static async Task BaselineLegacyMariaDbAsync(
        RelatoriosDbContext context,
        ILogger logger)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            const string countCoreTablesSql =
                """
                SELECT COUNT(*)
                FROM information_schema.tables
                WHERE table_schema = DATABASE()
                  AND table_name IN (
                      'alunos_snapshot',
                      'presencas_historicas',
                      'rotas_historicas',
                      'solicitacoes_relatorio'
                  );
                """;

            await using var countCommand = connection.CreateCommand();
            countCommand.CommandText = countCoreTablesSql;
            var existingCoreTables = Convert.ToInt32(await countCommand.ExecuteScalarAsync());

            // Banco novo: a migration inicial deve criar o esquema normalmente.
            if (existingCoreTables == 0)
            {
                return;
            }

            // Um esquema parcial não deve ser marcado como completo.
            if (existingCoreTables != 4)
            {
                throw new InvalidOperationException(
                    $"Esquema legado incompleto: foram encontradas {existingCoreTables} de 4 tabelas do Report.");
            }

            const string createHistorySql =
                """
                CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
                    `MigrationId` varchar(150) NOT NULL,
                    `ProductVersion` varchar(32) NOT NULL,
                    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
                ) CHARACTER SET=utf8mb4;
                """;

            await using var createHistoryCommand = connection.CreateCommand();
            createHistoryCommand.CommandText = createHistorySql;
            await createHistoryCommand.ExecuteNonQueryAsync();

            const string baselineSql =
                """
                INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
                VALUES (@migrationId, '9.0.0');
                """;

            await using var baselineCommand = connection.CreateCommand();
            baselineCommand.CommandText = baselineSql;

            var migrationParameter = baselineCommand.CreateParameter();
            migrationParameter.ParameterName = "@migrationId";
            migrationParameter.Value = InitialMariaDbMigration;
            baselineCommand.Parameters.Add(migrationParameter);

            var insertedRows = await baselineCommand.ExecuteNonQueryAsync();
            if (insertedRows > 0)
            {
                logger.LogWarning(
                    "Banco legado detectado. Migration base {MigrationId} registrada antes do reparo do esquema.",
                    InitialMariaDbMigration);
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
