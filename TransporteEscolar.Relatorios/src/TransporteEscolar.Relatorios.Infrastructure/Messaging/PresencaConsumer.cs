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

public class PresencaConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRabbitMqConnectionProvider _connectionProvider;
    private readonly ILogger<PresencaConsumer> _logger;

    public PresencaConsumer(
        IServiceScopeFactory scopeFactory,
        IRabbitMqConnectionProvider connectionProvider,
        ILogger<PresencaConsumer> logger)
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
                _logger.LogError(ex, "Consumidor de presenças desconectado. Nova tentativa em 5 segundos.");
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
            "presenca.events",
            ExchangeType.Topic,
            durable: true,
            cancellationToken: stoppingToken);
        await channel.QueueDeclareAsync(
            "relatorio.presenca.registrada",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);
        await channel.QueueBindAsync(
            "relatorio.presenca.registrada",
            "presenca.events",
            "presenca.registrada",
            cancellationToken: stoppingToken);
        await channel.BasicQosAsync(0, 1, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.Span);
                var dto = JsonSerializer.Deserialize<PresencaRecebidaDto>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (dto is null || !DateOnly.TryParse(dto.DataPresenca, out var dataPresenca))
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var alunoRepo = scope.ServiceProvider.GetRequiredService<IAlunoSnapshotRepository>();
                var presencaRepo = scope.ServiceProvider.GetRequiredService<IPresencaHistoricaRepository>();
                var aluno = await alunoRepo.BuscarPorExternalIdAsync(dto.AlunoId, stoppingToken);

                if (aluno is null)
                {
                    _logger.LogWarning(
                        "AlunoSnapshot não encontrado para ExternalId {AlunoId}. Evento será reenfileirado.",
                        dto.AlunoId);
                    await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
                    return;
                }

                var jaExiste = await presencaRepo.ExistePorAlunoEDataAsync(
                    aluno.Id,
                    dataPresenca,
                    stoppingToken);

                if (!jaExiste)
                {
                    await presencaRepo.AdicionarAsync(new PresencaHistorica
                    {
                        Id = Guid.NewGuid(),
                        AlunoId = aluno.Id,
                        Data = dataPresenca,
                        ConfirmouPresenca = dto.Status == "PRESENTE",
                        CancelouPresenca = dto.Status is
                            "CANCELADO" or "FALTA_NAO_JUSTIFICADA" or "FALTA_JUSTIFICADA"
                    }, stoppingToken);
                }

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar evento de presença.");
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false);
            }
        };

        await channel.BasicConsumeAsync(
            "relatorio.presenca.registrada",
            autoAck: false,
            consumer,
            stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
