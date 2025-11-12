using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Backend.Helper
{
    public class AppLogger<T>
    {
        private readonly ILogger<T> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public AppLogger(ILogger<T> logger)
        {
            _logger = logger;

            // Configure consistent and safe JSON output
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true, // prettier logs
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };
        }

        // === INFO LOG ===
        public void Info(string message, object? obj = null)
        {
            if (obj != null)
            {
                var json = JsonSerializer.Serialize(obj, _jsonOptions);
                _logger.LogInformation("{Message}\n{Json}", message, json);
            }
            else
            {
                _logger.LogInformation(message);
            }
        }

        // === WARNING LOG ===
        public void Warning(string message, object? obj = null)
        {
            if (obj != null)
            {
                var json = JsonSerializer.Serialize(obj, _jsonOptions);
                _logger.LogWarning("{Message}\n{Json}", message, json);
            }
            else
            {
                _logger.LogWarning(message);
            }
        }

        // === ERROR / FAIL LOG ===
        public void Fail(string message, Exception? ex = null, object? obj = null)
        {
            string json = obj != null
                ? JsonSerializer.Serialize(obj, _jsonOptions)
                : "";

            if (ex != null)
            {
                _logger.LogError(ex, "{Message}\n{Json}", message, json);
            }
            else
            {
                _logger.LogError("{Message}\n{Json}", message, json);
            }
        }
    }
}
