using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Backend.Infrastructure.Messaging;
using Backend.Presentation.Handlers;
using Backend.Model;

public class ServerPlatformCreatedConsumer : BackgroundService
{
    private readonly IModel _channel;
    private readonly IServiceScopeFactory _scopeFactory;

    public ServerPlatformCreatedConsumer(
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
            "platform.created",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += async (_, ea) =>
        {
            Console.WriteLine("📩 Message RECEIVED");

            Console.WriteLine($"   DeliveryTag: {ea.DeliveryTag}");
            Console.WriteLine($"   Exchange   : {ea.Exchange}");
            Console.WriteLine($"   RoutingKey: {ea.RoutingKey}");

            var json = Encoding.UTF8.GetString(ea.Body.ToArray());

            Console.WriteLine("Raw payload:");
            Console.WriteLine(json);

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
            catch (Exception ex)
            {
                Console.WriteLine("❌ Deserialize error");
                Console.WriteLine(ex);
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
            "platform.created",
            autoAck: false,
            consumer
        );

        return Task.CompletedTask;
    }
}
