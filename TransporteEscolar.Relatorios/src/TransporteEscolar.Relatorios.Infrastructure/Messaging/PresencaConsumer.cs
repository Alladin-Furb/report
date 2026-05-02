using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
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
    private readonly IConfiguration _configuration;
    private readonly ILogger<PresencaConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public PresencaConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<PresencaConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:Host"] ?? "rabbitmq",
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672"),
            UserName = _configuration["RabbitMQ:Username"] ?? "admin",
            Password = _configuration["RabbitMQ:Password"] ?? "admin123"
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: "presenca.events",
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: "relatorio.presenca.registrada",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: "relatorio.presenca.registrada",
            exchange: "presenca.events",
            routingKey: "presenca.registrada",
            cancellationToken: cancellationToken);

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel!);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var dto = JsonSerializer.Deserialize<PresencaRecebidaDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (dto is null) return;

                using var scope = _scopeFactory.CreateScope();
                var alunoRepo = scope.ServiceProvider.GetRequiredService<IAlunoSnapshotRepository>();
                var presencaRepo = scope.ServiceProvider.GetRequiredService<IPresencaHistoricaRepository>();

                var aluno = await alunoRepo.BuscarPorExternalIdAsync(dto.AlunoId, stoppingToken);
                if (aluno is null)
                {
                    _logger.LogWarning("Aluno externo {ExternalId} não encontrado no snapshot.", dto.AlunoId);
                    await _channel!.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                var jaExiste = await presencaRepo.ExistePorAlunoEDataAsync(
                    aluno.Id, dto.DataPresenca, stoppingToken);

                if (!jaExiste)
                {
                    await presencaRepo.AdicionarAsync(new PresencaHistorica
                    {
                        Id = Guid.NewGuid(),
                        AlunoId = aluno.Id,
                        Data = dto.DataPresenca,
                        ConfirmouPresenca = dto.Status == "PRESENTE",
                        CancelouPresenca = dto.Status == "CANCELADO"
                    }, stoppingToken);

                    _logger.LogInformation("Presença do aluno {Nome} em {Data} salva.", aluno.Nome, dto.DataPresenca);
                }

                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem de presença.");
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
            }
        };

        await _channel!.BasicConsumeAsync(
            queue: "relatorio.presenca.registrada",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
}