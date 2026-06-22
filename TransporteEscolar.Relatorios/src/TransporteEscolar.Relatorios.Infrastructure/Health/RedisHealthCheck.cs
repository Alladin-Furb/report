using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace TransporteEscolar.Relatorios.Infrastructure.Health;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public RedisHealthCheck(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Redis") ?? "redis:6379";
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = ConfigurationOptions.Parse(_connectionString);
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = Math.Min(options.ConnectTimeout, 5000);
            options.SyncTimeout = Math.Min(options.SyncTimeout, 5000);

            await using var connection = await ConnectionMultiplexer.ConnectAsync(options);
            var latency = await connection.GetDatabase().PingAsync();

            return HealthCheckResult.Healthy(
                "Redis disponível.",
                new Dictionary<string, object>
                {
                    ["latencyMs"] = Math.Round(latency.TotalMilliseconds, 2)
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded(
                "Redis indisponível; o Report continuará usando MariaDB.",
                ex);
        }
    }
}
