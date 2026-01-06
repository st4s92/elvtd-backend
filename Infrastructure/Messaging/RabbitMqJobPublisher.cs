using System.Text;
using System.Text.Json;
using Backend.Application.Interfaces;
using Backend.Model;
using RabbitMQ.Client;

namespace Backend.Infrastructure.Messaging;

public class RabbitMqJobPublisher : IJobPublisher
{
    private readonly IModel _channel;

    public RabbitMqJobPublisher(RabbitMqConnection rabbit)
    {
        _channel = rabbit.Channel;
    }

    public Task PublishCreateJob(TradePlatformCreateJob job)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(job);
        Console.WriteLine(Encoding.UTF8.GetString(body.ToArray()));

        _channel.QueueDeclare("platform.create", true, false, false, null);

        _channel.BasicPublish(
            exchange: "",
            routingKey: "platform.create",
            basicProperties: null,
            body: body
        );

        return Task.CompletedTask;
    }
}
