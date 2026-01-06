using RabbitMQ.Client;

namespace Backend.Infrastructure.Messaging;

public class RabbitMqConnection : IDisposable
{
    public IConnection Connection { get; }
    public IModel Channel { get; }

    public RabbitMqConnection(IConfiguration config)
    {
        var factory = new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost",
            Port = int.Parse(Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672"),
            UserName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "admin",
            Password = Environment.GetEnvironmentVariable("RABBITMQ_PASS") ?? "admin123"
        };

        Connection = factory.CreateConnection();
        Channel = Connection.CreateModel();
    }

    public void Dispose()
    {
        Channel?.Close();
        Connection?.Close();
    }
}
