using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TransporteEscolar.Relatorios.Application.Abstractions;
using TransporteEscolar.Relatorios.Application.DTOs;

namespace TransporteEscolar.Relatorios.Infrastructure.Caching;

public class RedisRelatorioCacheService : IRelatorioCacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisRelatorioCacheService> _logger;

    public RedisRelatorioCacheService(
        IDistributedCache cache,
        ILogger<RedisRelatorioCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<ConsultaRelatorioDto?> ObterAsync(
        Guid relatorioId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _cache.GetStringAsync(Chave(relatorioId), cancellationToken);
            return string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<ConsultaRelatorioDto>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis indisponível ao consultar o relatório {RelatorioId}.", relatorioId);
            return null;
        }
    }

    public async Task ArmazenarAsync(
        ConsultaRelatorioDto relatorio,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.SetStringAsync(
                Chave(relatorio.RelatorioId),
                JsonSerializer.Serialize(relatorio, JsonOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Redis indisponível ao armazenar o relatório {RelatorioId}.",
                relatorio.RelatorioId);
        }
    }

    private static string Chave(Guid relatorioId) => $"relatorio:v2:{relatorioId}";
}
