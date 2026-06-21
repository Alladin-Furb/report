using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TransporteEscolar.Relatorios.Application.Abstractions;

namespace TransporteEscolar.Relatorios.Infrastructure.Messaging;

public class RelatorioWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly ILogger<RelatorioWorker> _logger;

    public RelatorioWorker(
        IServiceScopeFactory scopeFactory,
        IRabbitMqConnectionProvider connectionProvider,
        ILogger<RelatorioWorker> logger)
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
                await ConsumirAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker perdeu a conexão com RabbitMQ. Nova tentativa em 5 segundos.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumirAsync(CancellationToken stoppingToken)
    {
        await using var connection =
            await _connectionProvider.CriarConexaoAsync(stoppingToken);
        await using var channel =
            await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await RelatorioQueueTopology.DeclararAsync(channel, stoppingToken);
        await channel.BasicQosAsync(0, 1, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var relatorioId = ObterRelatorioId(ea.Body.Span);
            if (relatorioId is null)
            {
                _logger.LogWarning("Mensagem de relatório inválida enviada para a DLQ.");
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var processador = scope.ServiceProvider.GetRequiredService<IProcessadorRelatorioService>();

            try
            {
                await processador.ProcessarAsync(relatorioId.Value, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                var repository = scope.ServiceProvider.GetRequiredService<ISolicitacaoRelatorioRepository>();
                var solicitacao = await repository.ObterPorIdAsync(relatorioId.Value, stoppingToken);
                var esgotouTentativas = solicitacao?.Tentativas >= 3;

                _logger.LogError(
                    ex,
                    "Falha ao gerar relatório {RelatorioId}. Tentativa {Tentativa}.",
                    relatorioId,
                    solicitacao?.Tentativas);

                await channel.BasicNackAsync(
                    ea.DeliveryTag,
                    multiple: false,
                    requeue: !esgotouTentativas);
            }
        };

        await channel.BasicConsumeAsync(
            RelatorioQueueTopology.Queue,
            autoAck: false,
            consumer,
            stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static Guid? ObterRelatorioId(ReadOnlySpan<byte> body)
    {
        try
        {
            using var json = JsonDocument.Parse(body.ToArray());
            var valor = json.RootElement.GetProperty("relatorioId").GetString();
            return Guid.TryParse(valor, out var id) ? id : null;
        }
        catch
        {
            return null;
        }
    }
}
