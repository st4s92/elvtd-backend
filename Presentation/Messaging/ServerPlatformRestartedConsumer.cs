using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Backend.Infrastructure.Messaging;
using Backend.Presentation.Handlers;
using Backend.Model;

namespace Backend.Presentation.Messaging;

public class ServerPlatformRestartedConsumer : BackgroundService
{
    private readonly IModel _channel;
    private readonly IServiceScopeFactory _scopeFactory;

    public ServerPlatformRestartedConsumer(
        RabbitMqConnection rabbit,
        IServiceScopeFactory scopeFactory
    )
    {
        _channel = rabbit.Channel;
        _scopeFactory = scopeFactory;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel.QueueDeclare(
            "platform.restarted",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            TradePlatformCreatedEvent? payload;

            try
            {
                payload = JsonSerializer.Deserialize<TradePlatformCreatedEvent>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );
            }
            catch (Exception)
            {
                _channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            if (payload == null)
            {
                _channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var handler = scope.ServiceProvider
                .GetRequiredService<ServerPlatformCreatedHandler>();

            await handler.HandleAsync(payload);

            _channel.BasicAck(ea.DeliveryTag, false);
        };

        _channel.BasicConsume(
            "platform.restarted",
            autoAck: false,
            consumer
        );

        return Task.CompletedTask;
    }
}
