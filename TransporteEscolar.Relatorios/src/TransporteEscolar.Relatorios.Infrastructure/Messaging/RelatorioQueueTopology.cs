using RabbitMQ.Client;

namespace TransporteEscolar.Relatorios.Infrastructure.Messaging;

internal static class RelatorioQueueTopology
{
    public const string Exchange = "relatorio.events";
    public const string RoutingKey = "relatorio.gerar";
    public const string Queue = "relatorio.gerar";
    public const string DeadLetterExchange = "relatorio.dlx";
    public const string DeadLetterQueue = "relatorio.gerar.dlq";

    public static async Task DeclararAsync(
        IChannel channel,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            DeadLetterExchange,
            ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            DeadLetterQueue,
            DeadLetterExchange,
            RoutingKey,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            Exchange,
            ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchange,
                ["x-dead-letter-routing-key"] = RoutingKey
            },
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            Queue,
            Exchange,
            RoutingKey,
            cancellationToken: cancellationToken);
    }
}
