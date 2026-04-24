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
        _channel.ExchangeDeclare(
            exchange: "mt5.exchange",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null
        );
        _channel.ExchangeDeclare(
            exchange: "ctrader.exchange",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null
        );
    }

    public Task PublishCreateJob(TradePlatformCreateJob job)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(job);
        Console.WriteLine(Encoding.UTF8.GetString(body.ToArray()));

        // JOURNAL accounts → journal.sync queue (handled by go-journal-worker)
        var queue = job.Role == "JOURNAL" ? "journal.sync" : "platform.create";

        _channel.QueueDeclare(queue, true, false, false, null);

        _channel.BasicPublish(
            exchange: "",
            routingKey: queue,
            basicProperties: null,
            body: body
        );

        Console.WriteLine($"Published to {queue}: account {job.AccountNumber} role={job.Role}");
        return Task.CompletedTask;
    }

    public Task PublishDeleteJob(TradePlatformCreateJob job)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(job);
        Console.WriteLine(Encoding.UTF8.GetString(body.ToArray()));

        _channel.QueueDeclare("platform.delete", true, false, false, null);

        _channel.BasicPublish(
            exchange: "",
            routingKey: "platform.delete",
            basicProperties: null,
            body: body
        );

        return Task.CompletedTask;
    }

    public Task PublishRestartJob(TradePlatformCreateJob job)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(job);
        Console.WriteLine(Encoding.UTF8.GetString(body.ToArray()));

        _channel.QueueDeclare("platform.restart", true, false, false, null);

        _channel.BasicPublish(
            exchange: "",
            routingKey: "platform.restart",
            basicProperties: null,
            body: body
        );

        return Task.CompletedTask;
    }

    public Task PublishMt5Packet(
        string server,
        long account,
        string type,
        object payload
    )
    {
        var exchange = "mt5.exchange";
        if (string.IsNullOrEmpty(exchange))
            throw new Exception("MT5_EXCHANGE not set");

        var routingKey = $"mt5.receive.packet.{server.Replace(" ", "_")}.{account}";

        var envelope = new
        {
            type = type,
            data = payload
        };

        Console.WriteLine($"exchange: {exchange}");
        Console.WriteLine($"routingKey: {routingKey}");
        Console.WriteLine($"payload: {JsonSerializer.Serialize(envelope)}");

        var body = JsonSerializer.SerializeToUtf8Bytes(envelope);

        _channel.BasicPublish(
            exchange: exchange,
            routingKey: routingKey,
            basicProperties: null,
            body: body
        );

        return Task.CompletedTask;
    }

    public Task PublishMt5PacketBatch(
        string server,
        long account,
        IEnumerable<object> payloads
    )
    {
        var exchange = "mt5.exchange";
        if (string.IsNullOrEmpty(exchange))
            throw new Exception("MT5_EXCHANGE not set");

        var routingKey = $"mt5.receive.packet.{server.Replace(" ", "_")}.{account}";

        var bodyJson = JsonSerializer.Serialize(payloads);

        Console.WriteLine($"exchange: {exchange}");
        Console.WriteLine($"routingKey: {routingKey}");
        Console.WriteLine($"batch-count: {payloads.Count()}");
        Console.WriteLine($"payload: {bodyJson}");

        var body = JsonSerializer.SerializeToUtf8Bytes(payloads);

        _channel.BasicPublish(
            exchange: exchange,
            routingKey: routingKey,
            basicProperties: null,
            body: body
        );

        return Task.CompletedTask;
    }

    public Task PublishCtraderPacket(
        long ctraderId,
        string type,
        object payload
    )
    {
        var exchange = "ctrader.exchange";
        var routingKey = $"ctrader.receive.packet.{ctraderId}";

        var envelope = new
        {
            type = type,
            data = payload
        };

        Console.WriteLine($"exchange: {exchange}");
        Console.WriteLine($"routingKey: {routingKey}");
        Console.WriteLine($"payload: {JsonSerializer.Serialize(envelope)}");

        var body = JsonSerializer.SerializeToUtf8Bytes(envelope);

        _channel.BasicPublish(
            exchange: exchange,
            routingKey: routingKey,
            basicProperties: null,
            body: body
        );

        return Task.CompletedTask;
    }

    public Task PublishCtraderPacketBatch(
        long ctraderId,
        IEnumerable<object> payloads
    )
    {
        var exchange = "ctrader.exchange";
        var routingKey = $"ctrader.receive.packet.{ctraderId}";

        var bodyJson = JsonSerializer.Serialize(payloads);

        Console.WriteLine($"exchange: {exchange}");
        Console.WriteLine($"routingKey: {routingKey}");
        Console.WriteLine($"batch-count: {payloads.Count()}");
        Console.WriteLine($"payload: {bodyJson}");

        var body = JsonSerializer.SerializeToUtf8Bytes(payloads);

        _channel.BasicPublish(
            exchange: exchange,
            routingKey: routingKey,
            basicProperties: null,
            body: body
        );

        return Task.CompletedTask;
    }

    public Task PublishCtraderManageAccount(Account account)
    {
        var exchange = "ctrader.exchange";
        var routingKey = "ctrader.manage.account";

        var envelope = new
        {
            type = "MANAGE_ACCOUNT",
            data = new
            {
                id = account.Id,
                platform_name = account.PlatformName,
                account_number = account.AccountNumber,
                account_password = account.AccountPassword,
                broker_name = account.BrokerName,
                server_name = account.ServerName,
                user_id = account.UserId,
                role = account.Role,
                balance = account.Balance.ToString(),
                equity = account.Equity.ToString(),
                status = (int)account.Status,
            }
        };

        Console.WriteLine($"exchange: {exchange}");
        Console.WriteLine($"routingKey: {routingKey}");
        Console.WriteLine($"payload: {JsonSerializer.Serialize(envelope)}");

        var body = JsonSerializer.SerializeToUtf8Bytes(envelope);

        _channel.BasicPublish(
            exchange: exchange,
            routingKey: routingKey,
            basicProperties: null,
            body: body
        );

        return Task.CompletedTask;
    }

    public Task PublishBridgeKillTerminal(long accountNumber)
    {
        var payload = new
        {
            command = "kill_terminal",
            account_number = accountNumber,
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(payload);

        _channel.ExchangeDeclare("bridge.command", "fanout", true, false, null);
        _channel.BasicPublish(
            exchange: "bridge.command",
            routingKey: "",
            basicProperties: null,
            body: body
        );

        Console.WriteLine($"[BRIDGE_CMD] kill_terminal broadcast for account {accountNumber}");
        return Task.CompletedTask;
    }

}
