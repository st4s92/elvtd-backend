using Backend.Helper;
using System.Net.WebSockets;

namespace Backend.Presentation;

public static class Socket
{
    public static void Init(WebApplication app)
    {
        var wsServer = app.Services.GetRequiredService<WebSocketServer>();

        app.UseWebSockets();

        app.Map("/ws", async context =>
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Expected WebSocket request");
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var clientKey = context.Request.Query["client_key"];

            if (string.IsNullOrEmpty(clientKey))
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("Missing or invalid client_key");
                return;
            }

            wsServer.RegisterClient(clientKey!, socket);
            Console.WriteLine($"✅ Client connected: {clientKey}");

            var buffer = new byte[1024 * 4];

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine($"🔌 Client requested close: {clientKey}");
                        break;
                    }

                    // If you expect data from client, process it here:
                    // string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                }
            }
            catch (WebSocketException ex)
            {
                Console.WriteLine($"⚠️ Client disconnected unexpectedly: {clientKey} ({ex.Message})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ WebSocket error ({clientKey}): {ex}");
            }
            finally
            {
                wsServer.RemoveClient(clientKey!);
                if (socket.State != WebSocketState.Closed)
                {
                    try
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", CancellationToken.None);
                    }
                    catch
                    {
                        // ignore if already closed
                    }
                }
                socket.Dispose();
                Console.WriteLine($"👋 Client closed: {clientKey}");
            }
        });
    }
}
