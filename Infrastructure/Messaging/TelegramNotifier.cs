using System.Collections.Concurrent;

namespace Backend.Infrastructure.Messaging;

public interface ITelegramNotifier
{
    Task SendAlert(string message);
    Task SendAlertThrottled(string key, string message, TimeSpan cooldown);
}

public class TelegramNotifier : ITelegramNotifier
{
    private readonly string _botToken;
    private readonly string _chatId;
    private readonly HttpClient _http = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastSent = new();

    public TelegramNotifier()
    {
        _botToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN") ?? "";
        _chatId = Environment.GetEnvironmentVariable("TELEGRAM_CHAT_ID") ?? "";
    }

    public async Task SendAlert(string message)
    {
        if (string.IsNullOrEmpty(_botToken) || string.IsNullOrEmpty(_chatId))
            return;

        try
        {
            var url = $"https://api.telegram.org/bot{_botToken}/sendMessage";
            var payload = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("chat_id", _chatId),
                new KeyValuePair<string, string>("text", $"⚠️ ELVTD Alert\n\n{message}"),
                new KeyValuePair<string, string>("parse_mode", "HTML"),
            });
            await _http.PostAsync(url, payload);
        }
        catch
        {
            // Don't let Telegram failures break the app
        }
    }

    /// <summary>
    /// Send alert but throttle by key — max once per cooldown period.
    /// </summary>
    public async Task SendAlertThrottled(string key, string message, TimeSpan cooldown)
    {
        if (_lastSent.TryGetValue(key, out var lastTime) && DateTime.UtcNow - lastTime < cooldown)
            return;

        _lastSent[key] = DateTime.UtcNow;
        await SendAlert(message);
    }
}
