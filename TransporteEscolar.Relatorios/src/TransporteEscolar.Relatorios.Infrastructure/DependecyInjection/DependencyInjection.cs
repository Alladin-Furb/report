using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TransporteEscolar.Relatorios.Application.Abstractions;
using TransporteEscolar.Relatorios.Infrastructure.Caching;
using TransporteEscolar.Relatorios.Infrastructure.Exporting;
using TransporteEscolar.Relatorios.Infrastructure.Messaging;
using TransporteEscolar.Relatorios.Infrastructure.Persistence.Context;
using TransporteEscolar.Relatorios.Infrastructure.Persistence.Repositories;

namespace TransporteEscolar.Relatorios.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RelatoriosDb");

        services.AddDbContext<RelatoriosDbContext>(options =>
            options
                .UseMySql(
                    connectionString,
                    new MariaDbServerVersion(new Version(11, 2)),
                    mysql => mysql
                        .EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorNumbersToAdd: null)
                        .CommandTimeout(30))
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddScoped<IAlunoSnapshotRepository, AlunoSnapshotRepository>();
        services.AddScoped<IPresencaHistoricaRepository, PresencaHistoricaRepository>();
        services.AddScoped<IRotaHistoricaRepository, RotaHistoricaRepository>();
        services.AddScoped<ISolicitacaoRelatorioRepository, SolicitacaoRelatorioRepository>();
        services.AddSingleton<IExportadorRelatorioService, ExportadorRelatorioService>();

        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));
        services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis") ?? "redis:6379";
            options.InstanceName = "transporte-escolar:";
        });
        services.AddSingleton<IRelatorioCacheService, RedisRelatorioCacheService>();

        var presencaBaseUrl = configuration["PresencaService:BaseUrl"];

        services.AddHttpClient("presenca-service", client =>
        {
            client.BaseAddress = new Uri(presencaBaseUrl!);
            client.DefaultRequestHeaders.Add("X-User-Role", "ROLE_ADMIN");
        });

        return services;
    }
}
