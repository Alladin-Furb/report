using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TransporteEscolar.Relatorios.Infrastructure.Persistence.Context;

namespace TransporteEscolar.Relatorios.Infrastructure.Health;

public sealed class MariaDbHealthCheck : IHealthCheck
{
    private readonly RelatoriosDbContext _context;

    public MariaDbHealthCheck(RelatoriosDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var conectado = await _context.Database.CanConnectAsync(cancellationToken);
            return conectado
                ? HealthCheckResult.Healthy("MariaDB disponível.")
                : HealthCheckResult.Unhealthy("MariaDB indisponível.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Falha ao conectar ao MariaDB.", ex);
        }
    }
}
