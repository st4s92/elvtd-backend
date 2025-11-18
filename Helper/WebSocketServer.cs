using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;

namespace Backend.Helper;

public class WebSocketServer
{
    // Use a string key like "FinexBisnisSolusi-Demo:60059278"
    private readonly ConcurrentDictionary<string, WebSocket> _clients = new();

    public void RegisterClient(string clientKey, WebSocket socket)
    {
        _clients[clientKey] = socket;
        Console.WriteLine($"Client connected ({clientKey})");
    }

    public void RemoveClient(string clientKey)
    {
        _clients.TryRemove(clientKey, out _);
        Console.WriteLine($"Client disconnected ({clientKey})");
    }

    public bool TryGetSocket(string clientKey, out WebSocket socket)
        => _clients.TryGetValue(clientKey, out socket!);

    public async Task BroadcastToAccounts(IEnumerable<string> clientKeys, string message)
{
        var bytes = Encoding.UTF8.GetBytes(message);

        foreach (var key in clientKeys)
        {
            var normalizedKey = key.Replace(" ", "_");

            if (_clients.TryGetValue(normalizedKey, out var socket) &&
                socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
                Console.WriteLine($"📤 Sent message to {normalizedKey}");
            }
            else
            {
                Console.WriteLine($"⚠️ Client not found or closed: {normalizedKey}");
            }
        }
    }

    public void CleanupDisconnectedAsync()
    {
        foreach (var (key, socket) in _clients)
        {
            if (socket.State != WebSocketState.Open)
            {
                _clients.TryRemove(key, out _);
                Console.WriteLine($"🧹 Removed stale connection: {key}");
            }
        }
    }
}
