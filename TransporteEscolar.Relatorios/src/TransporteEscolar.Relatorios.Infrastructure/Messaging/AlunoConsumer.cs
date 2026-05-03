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

public class AlunoConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AlunoConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public AlunoConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AlunoConsumer> logger)
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
            exchange: "aluno.events",
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);

        foreach (var (queue, routingKey) in new[]
        {
            ("relatorio.aluno.cadastrado", "aluno.cadastrado"),
            ("relatorio.aluno.atualizado", "aluno.atualizado")
        })
        {
            await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false,
                autoDelete: false, cancellationToken: cancellationToken);
            await _channel.QueueBindAsync(queue, "aluno.events", routingKey,
                cancellationToken: cancellationToken);
        }

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
                var dto = JsonSerializer.Deserialize<AlunoRecebidoDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (dto is null || dto.Id == 0)
                {
                    await _channel!.BasicAckAsync(ea.DeliveryTag, false);
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
                    _logger.LogInformation("AlunoSnapshot criado para ExternalId {Id} ({Nome})", dto.Id, dto.Nome);
                }
                else
                {
                    existente.Nome = dto.Nome;
                    existente.Ativo = true;
                    await repo.AtualizarAsync(existente, stoppingToken);
                    _logger.LogInformation("AlunoSnapshot atualizado para ExternalId {Id} ({Nome})", dto.Id, dto.Nome);
                }

                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar evento de aluno.");
                await _channel!.BasicNackAsync(ea.DeliveryTag, false, requeue: true);
            }
        };

        await _channel!.BasicConsumeAsync("relatorio.aluno.cadastrado", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        await _channel!.BasicConsumeAsync("relatorio.aluno.atualizado", autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}