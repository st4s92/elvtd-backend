using System.Globalization;
using Microsoft.Extensions.Logging;

public static class OrderTimeHelper
{
    /// <summary>
    /// Checks whether an order is fresh based on its open time.
    /// Accepts DateTime, ISO strings, "yyyy-MM-ddTHH:mm:ssZ", etc.
    /// Returns true if order is within allowed age.
    /// </summary>
    public static bool IsOrderFresh(object? rawTime, int thresholdSeconds = 10)
    {
        DateTime orderUtc;

        // ------------------------------------------------------------------
        // 1) Handle DateTime directly
        // ------------------------------------------------------------------
        if (rawTime is DateTime dt)
        {
            orderUtc = dt.Kind == DateTimeKind.Utc
                ? dt
                : dt.ToUniversalTime();
        }
        else
        {
            // ------------------------------------------------------------------
            // 2) Handle string formats
            // ------------------------------------------------------------------
            string? timeStr = rawTime?.ToString();

            if (string.IsNullOrWhiteSpace(timeStr))
                return false;

            // Try standard ISO formats
            if (!TryParseOrderTimeString(timeStr, out orderUtc))
                return false;
        }

        // ------------------------------------------------------------------
        // 3) Compare with now (in UTC)
        // ------------------------------------------------------------------
        DateTime nowUtc = DateTime.UtcNow;
        
        var age = nowUtc - orderUtc;

        if (age < TimeSpan.Zero){          // future timestamps
            Console.WriteLine($"{nowUtc} - {orderUtc} = {age}");
            return false;
        }

        if (age > TimeSpan.FromSeconds(thresholdSeconds)){
            Console.WriteLine($"{nowUtc} - {orderUtc} = {age}");
            return false;
        }

        return true;
    }


    /// <summary>
    /// Parses a string-based order time into a UTC DateTime.
    /// Supports many formats from MQL5 and backend.
    /// </summary>
    private static bool TryParseOrderTimeString(string raw, out DateTime utc)
    {
        utc = default;

        // Supported exact formats
        string[] formats =
        {
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy.MM.dd HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
            "o" // ISO-8601 round-trip
        };

        // --- (1) Try exact formats first ---
        if (DateTime.TryParseExact(
                raw,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out utc))
        {
            return true;
        }

        // --- (2) Your requested line should go RIGHT HERE ---
        // Force-parse using UTC assumptions
        try
        {
            utc = DateTime.Parse(raw, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            return true;
        }
        catch { }

        // --- (3) Try fallback general parse ---
        if (DateTime.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out utc))
        {
            return true;
        }

        return false;
    }
}
