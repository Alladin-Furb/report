namespace TransporteEscolar.Relatorios.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public string? Url { get; set; }
    public string Host { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin123";
    public string VirtualHost { get; set; } = "/";
}
