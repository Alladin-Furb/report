using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using TransporteEscolar.Relatorios.Application.Abstractions;
using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.Infrastructure.Messaging;

public class RelatorioDispatcher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly ILogger<RelatorioDispatcher> _logger;

    public RelatorioDispatcher(
        IServiceScopeFactory scopeFactory,
        IRabbitMqConnectionProvider connectionProvider,
        ILogger<RelatorioDispatcher> logger)
    {
        _scopeFactory = scopeFactory;
        _connectionProvider = connectionProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection =
                    await _connectionProvider.CriarConexaoAsync(stoppingToken);
                await using var channel =
                    await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await RelatorioQueueTopology.DeclararAsync(channel, stoppingToken);

                while (!stoppingToken.IsCancellationRequested && connection.IsOpen)
                {
                    await PublicarPendentesAsync(channel, stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dispatcher perdeu a conexão com RabbitMQ. Nova tentativa em 5 segundos.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task PublicarPendentesAsync(
        IChannel channel,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISolicitacaoRelatorioRepository>();
        var pendentes = await repository.ObterParaEnfileirarAsync(
            limite: 50,
            reenfileirarAntesDe: DateTime.UtcNow.AddMinutes(-10),
            cancellationToken);

        foreach (var solicitacao in pendentes)
        {
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                relatorioId = solicitacao.Id
            }));
            var properties = new BasicProperties
            {
                Persistent = true,
                ContentType = "application/json",
                MessageId = solicitacao.Id.ToString()
            };

            await channel.BasicPublishAsync(
                RelatorioQueueTopology.Exchange,
                RelatorioQueueTopology.RoutingKey,
                mandatory: false,
                properties,
                body,
                cancellationToken);

            solicitacao.Status = StatusSolicitacaoRelatorio.Enfileirado;
            solicitacao.AtualizadoEm = DateTime.UtcNow;
            await repository.SalvarAlteracoesAsync(cancellationToken);
        }
    }
}
