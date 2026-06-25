using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TransporteEscolar.Relatorios.Application.Abstractions;
using TransporteEscolar.Relatorios.Domain.Entities;

namespace TransporteEscolar.Relatorios.Infrastructure.Messaging;

public class AlunoConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly ILogger<AlunoConsumer> _logger;

    public AlunoConsumer(
        IServiceScopeFactory scopeFactory,
        IRabbitMqConnectionProvider connectionProvider,
        ILogger<AlunoConsumer> logger)
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
                _logger.LogError(ex, "Consumidor de alunos desconectado. Nova tentativa em 5 segundos.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumirAsync(CancellationToken stoppingToken)
    {
        await using var connection = await _connectionProvider.CriarConexaoAsync(stoppingToken);
        await using var channel =
            await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.ExchangeDeclareAsync(
            "aluno.events",
            ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken);

        foreach (var (queue, routingKey) in Filas)
        {
            await channel.QueueDeclareAsync(
                queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: stoppingToken);
            await channel.QueueBindAsync(
                queue,
                "aluno.events",
                routingKey,
                cancellationToken: stoppingToken);
        }

        await channel.BasicQosAsync(0, 1, global: false, stoppingToken);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);
                var dto = JsonSerializer.Deserialize<AlunoRecebidoDto>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (dto is null || dto.Id == Guid.Empty)
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<IAlunoSnapshotRepository>();
                var existente = await repo.BuscarPorExternalIdAsync(dto.Id, stoppingToken);

                if (existente is null)
                {
                    await repo.AdicionarAsync(new AlunoSnapshot
                    {
                        Id = Guid.NewGuid(),
                        ExternalId = dto.Id,
                        Nome = dto.Nome,
                        Ativo = true
                    }, stoppingToken);
                }
                else
                {
                    existente.Nome = dto.Nome;
                    existente.Ativo = true;
                    await repo.AtualizarAsync(existente, stoppingToken);
                }

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar evento de aluno.");
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
            }
        };

        foreach (var (queue, _) in Filas)
        {
            await channel.BasicConsumeAsync(
                queue,
                autoAck: false,
                consumer,
                stoppingToken);
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static readonly (string Queue, string RoutingKey)[] Filas =
    [
        ("relatorio.aluno.cadastrado", "aluno.cadastrado"),
        ("relatorio.aluno.atualizado", "aluno.atualizado")
    ];
}
