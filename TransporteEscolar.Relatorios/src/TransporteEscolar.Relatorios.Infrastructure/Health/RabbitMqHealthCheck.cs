using Microsoft.Extensions.Diagnostics.HealthChecks;
using TransporteEscolar.Relatorios.Infrastructure.Messaging;

namespace TransporteEscolar.Relatorios.Infrastructure.Health;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly IRabbitMqConnectionProvider _connectionProvider;

    public RabbitMqHealthCheck(IRabbitMqConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection =
                await _connectionProvider.CriarConexaoAsync(cancellationToken);
            return connection.IsOpen
                ? HealthCheckResult.Healthy("RabbitMQ disponível.")
                : HealthCheckResult.Unhealthy("RabbitMQ sem conexão ativa.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Falha ao conectar ao RabbitMQ.", ex);
        }
    }
}
