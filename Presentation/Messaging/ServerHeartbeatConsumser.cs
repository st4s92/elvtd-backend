using System.Text;
using System.Text.Json;
using Backend.Infrastructure.Messaging;
using Backend.Model;
using Backend.Presentation.Handlers;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Backend.Presentation.Messaging;

public class ServerHeartbeatConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IModel _channel;

    public ServerHeartbeatConsumer(
        RabbitMqConnection rabbit,
        IServiceScopeFactory scopeFactory
    )
    {
        Console.WriteLine("🐰 RabbitMQ channel injected");
        _channel = rabbit.Channel;
        _scopeFactory = scopeFactory;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.QueueDeclare(
            "worker.heartbeat",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var payload = JsonSerializer.Deserialize<ServerHeartbeatRequest>(json);

                if (payload == null)
                {
                    _channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider
                    .GetRequiredService<ServerHeartbeatHandler>();

                await handler.HandleAsync(payload);
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HeartbeatConsumer] error: {ex.Message}");
                try { _channel.BasicNack(ea.DeliveryTag, false, true); } catch { }
            }
        };

        _channel.BasicConsume(
            "worker.heartbeat",
            autoAck: false,
            consumer
        );

        return Task.CompletedTask;
    }
}
