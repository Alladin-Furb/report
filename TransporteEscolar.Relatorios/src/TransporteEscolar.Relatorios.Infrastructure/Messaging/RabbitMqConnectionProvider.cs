using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace TransporteEscolar.Relatorios.Infrastructure.Messaging;

public interface IRabbitMqConnectionProvider
{
    Task<IConnection> CriarConexaoAsync(CancellationToken cancellationToken = default);
}

public class RabbitMqConnectionProvider : IRabbitMqConnectionProvider
{
    private readonly RabbitMqOptions _options;

    public RabbitMqConnectionProvider(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public Task<IConnection> CriarConexaoAsync(CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            ConsumerDispatchConcurrency = 1
        };

        if (!string.IsNullOrWhiteSpace(_options.Url))
        {
            factory.Uri = new Uri(_options.Url);
        }
        else
        {
            factory.HostName = _options.Host;
            factory.Port = _options.Port;
            factory.UserName = _options.Username;
            factory.Password = _options.Password;
            factory.VirtualHost = _options.VirtualHost;
        }

        return factory.CreateConnectionAsync(cancellationToken);
    }
}
