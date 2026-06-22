using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TransporteEscolar.Relatorios.Infrastructure.Health;

namespace TransporteEscolar.Relatorios.Api.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddReportHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<MariaDbHealthCheck>(
                "mariadb",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "required"],
                timeout: TimeSpan.FromSeconds(5))
            .AddCheck<RabbitMqHealthCheck>(
                "rabbitmq",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "required"],
                timeout: TimeSpan.FromSeconds(5))
            .AddCheck<RedisHealthCheck>(
                "redis",
                failureStatus: HealthStatus.Degraded,
                tags: ["ready", "optional"],
                timeout: TimeSpan.FromSeconds(5));

        return services;
    }

    public static IEndpointRouteBuilder MapReportHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = EscreverRespostaAsync
        });

        var readinessOptions = new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready"),
            ResponseWriter = EscreverRespostaAsync,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        };

        endpoints.MapHealthChecks("/health/ready", readinessOptions);
        endpoints.MapHealthChecks("/health", readinessOptions);
        return endpoints;
    }

    private static Task EscreverRespostaAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var resposta = new
        {
            status = report.Status.ToString(),
            durationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 2),
                    data = entry.Value.Data
                })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(
            resposta,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
