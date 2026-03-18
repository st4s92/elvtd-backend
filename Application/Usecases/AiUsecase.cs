using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Infrastructure.Repositories;
using Backend.Model;

namespace Backend.Application.Usecases;

public class AiUsecase
{
    private readonly AiChatRepository _chatRepo;
    private readonly IOrderRepository _orderRepo;
    private readonly IAccountRepository _accountRepo;
    private readonly HttpClient _httpClient;
    private readonly AppLogger<AiUsecase> _logger;

    private static readonly string ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "";
    private const string Model = "gemini-2.0-flash-lite";
    private static readonly string BaseUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}";

    private const string SystemPromptChat = @"Du bist der KI-Assistent der ""elvtd"" Copy-Trading-Plattform. Du hilfst Benutzern, ihre Trading-Performance zu verstehen, Strategien zu analysieren und Copy-Trading zu optimieren.

Regeln:
- Sei präzise mit Zahlen und Statistiken
- Antworte auf Deutsch wenn der Benutzer Deutsch schreibt, sonst auf Englisch
- Gib keine Finanzberatung — fokussiere dich auf Muster-Analyse und Daten-Interpretation
- Nutze Markdown-Formatierung für übersichtliche Antworten
- Wenn du Account-Kontext hast, beziehe dich auf die konkreten Daten";

    private const string SystemPromptStrategy = @"Du bist ein Trading-Strategie-Analyst. Analysiere die bereitgestellten Trading-Statistiken und klassifiziere die Strategie.

Antworte ausschließlich im folgenden JSON-Format:
{
  ""strategy_type"": ""Scalping | Day Trading | Swing Trading | Trend Following | Mean Reversion | Grid/Martingale | Mixed"",
  ""confidence"": 0.0-1.0,
  ""characteristics"": [""...""],
  ""strengths"": [""...""],
  ""weaknesses"": [""...""],
  ""recommendations"": [""...""],
  ""summary"": ""Kurze Zusammenfassung in 2-3 Sätzen""
}";

    private const string SystemPromptRisk = @"Du bist ein Trading-Risiko-Analyst. Bewerte das Risikoprofil anhand der bereitgestellten Statistiken.

Antworte ausschließlich im folgenden JSON-Format:
{
  ""overall_score"": 1-10,
  ""dimensions"": {
    ""drawdown_risk"": { ""score"": 1-10, ""detail"": ""..."" },
    ""position_sizing"": { ""score"": 1-10, ""detail"": ""..."" },
    ""concentration"": { ""score"": 1-10, ""detail"": ""..."" },
    ""overtrading"": { ""score"": 1-10, ""detail"": ""..."" },
    ""copy_lag"": { ""score"": 1-10, ""detail"": ""..."" }
  },
  ""critical_warnings"": [""...""],
  ""recommendations"": [""...""],
  ""summary"": ""Kurze Zusammenfassung in 2-3 Sätzen""
}

Score 1 = sehr sicher, 10 = sehr riskant.";

    public AiUsecase(
        AiChatRepository chatRepo,
        IOrderRepository orderRepo,
        IAccountRepository accountRepo,
        IHttpClientFactory httpClientFactory,
        AppLogger<AiUsecase> logger)
    {
        _chatRepo = chatRepo;
        _orderRepo = orderRepo;
        _accountRepo = accountRepo;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    // === Session Management ===

    public async Task<(List<AiChatSession>, ITError?)> GetSessions(long userId)
    {
        try
        {
            var sessions = await _chatRepo.GetSessionsByUser(userId);
            return (sessions, null);
        }
        catch (Exception ex)
        {
            return ([], TError.NewServer("Failed to load sessions", ex.Message));
        }
    }

    public async Task<(AiChatSession?, ITError?)> CreateSession(long userId, AiChatSessionPayload payload)
    {
        try
        {
            var session = new AiChatSession
            {
                UserId = userId,
                Title = payload.Title ?? "Neuer Chat"
            };

            if (payload.AccountId.HasValue && payload.AccountId.Value > 0)
            {
                session.ContextSnapshot = await BuildAccountContext(payload.AccountId.Value);
            }

            var created = await _chatRepo.CreateSession(session);
            return (created, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("Failed to create session", ex.Message));
        }
    }

    public async Task<(List<AiChatMessage>, ITError?)> GetMessages(long sessionId)
    {
        try
        {
            var messages = await _chatRepo.GetMessages(sessionId);
            return (messages, null);
        }
        catch (Exception ex)
        {
            return ([], TError.NewServer("Failed to load messages", ex.Message));
        }
    }

    // === Context Building ===

    public async Task<string> BuildAccountContext(long accountId)
    {
        var account = await _accountRepo.Get(a => a.Id == accountId);
        if (account == null) return "Account not found.";

        var orders = await _orderRepo.GetMany(
            o => o.AccountId == accountId && o.Status == OrderStatus.Complete
        );

        if (orders.Count == 0)
            return $"Account {account.AccountNumber} ({account.BrokerName}) — Keine abgeschlossenen Orders.";

        var sb = new StringBuilder();
        sb.AppendLine($"## Account: {account.AccountNumber} ({account.BrokerName})");
        sb.AppendLine($"- Platform: {account.PlatformName}");
        sb.AppendLine($"- Total Trades: {orders.Count}");

        var wins = orders.Where(o => o.OrderProfit > 0).ToList();
        var losses = orders.Where(o => o.OrderProfit <= 0).ToList();
        var winRate = orders.Count > 0 ? (double)wins.Count / orders.Count * 100 : 0;
        var totalProfit = orders.Sum(o => o.OrderProfit ?? 0);
        var avgWin = wins.Count > 0 ? wins.Average(o => o.OrderProfit ?? 0) : 0;
        var avgLoss = losses.Count > 0 ? losses.Average(o => o.OrderProfit ?? 0) : 0;
        var profitFactor = Math.Abs(losses.Sum(o => o.OrderProfit ?? 0)) > 0
            ? wins.Sum(o => o.OrderProfit ?? 0) / Math.Abs(losses.Sum(o => o.OrderProfit ?? 0))
            : 0;

        sb.AppendLine($"- Win Rate: {winRate:F1}%");
        sb.AppendLine($"- Profit Factor: {profitFactor:F2}");
        sb.AppendLine($"- Total P/L: {totalProfit:F2}");
        sb.AppendLine($"- Avg Win: {avgWin:F2}, Avg Loss: {avgLoss:F2}");

        var bySymbol = orders
            .GroupBy(o => o.OrderSymbol)
            .Select(g => new
            {
                Symbol = g.Key,
                Count = g.Count(),
                Profit = g.Sum(o => o.OrderProfit ?? 0),
                WinRate = g.Count(o => o.OrderProfit > 0) * 100.0 / g.Count()
            })
            .OrderByDescending(s => s.Count)
            .Take(10);

        sb.AppendLine("\n### Top Symbols:");
        foreach (var s in bySymbol)
            sb.AppendLine($"- {s.Symbol}: {s.Count} trades, P/L {s.Profit:F2}, WR {s.WinRate:F1}%");

        var durations = orders
            .Where(o => o.OrderOpenAt.HasValue && o.OrderCloseAt.HasValue)
            .Select(o => (o.OrderCloseAt!.Value - o.OrderOpenAt!.Value).TotalMinutes)
            .ToList();

        if (durations.Count > 0)
        {
            sb.AppendLine($"\n### Haltedauer:");
            sb.AppendLine($"- Median: {durations.OrderBy(d => d).ElementAt(durations.Count / 2):F0} Min");
            sb.AppendLine($"- Avg: {durations.Average():F0} Min");
            sb.AppendLine($"- Min: {durations.Min():F0} Min, Max: {durations.Max():F0} Min");
        }

        var byHour = orders
            .Where(o => o.OrderOpenAt.HasValue)
            .GroupBy(o => o.OrderOpenAt!.Value.Hour)
            .OrderByDescending(g => g.Count())
            .Take(5);

        sb.AppendLine("\n### Aktivste Handelszeiten (UTC):");
        foreach (var h in byHour)
            sb.AppendLine($"- {h.Key}:00 — {h.Count()} trades");

        var streaks = CalculateStreaks(orders);
        sb.AppendLine($"\n### Streaks:");
        sb.AppendLine($"- Max Consecutive Wins: {streaks.maxWins}");
        sb.AppendLine($"- Max Consecutive Losses: {streaks.maxLosses}");

        sb.AppendLine($"\n### Position Sizing:");
        sb.AppendLine($"- Avg Lot: {orders.Average(o => o.OrderLot):F3}");
        sb.AppendLine($"- Max Lot: {orders.Max(o => o.OrderLot):F3}");
        sb.AppendLine($"- Min Lot: {orders.Min(o => o.OrderLot):F3}");

        var copiedOrders = orders.Where(o => o.MasterOrderId.HasValue).ToList();
        if (copiedOrders.Count > 0)
        {
            var copyLags = copiedOrders
                .Where(o => o.OrderOpenAt.HasValue && o.OrderCopiedAt.HasValue)
                .Select(o => (o.OrderCopiedAt!.Value - o.OrderOpenAt!.Value).TotalSeconds)
                .ToList();

            sb.AppendLine($"\n### Copy-Trade Metriken:");
            sb.AppendLine($"- Copied Orders: {copiedOrders.Count} / {orders.Count}");
            if (copyLags.Count > 0)
            {
                sb.AppendLine($"- Avg Copy Lag: {copyLags.Average():F1}s");
                sb.AppendLine($"- Max Copy Lag: {copyLags.Max():F1}s");
            }
        }

        return sb.ToString();
    }

    private static (int maxWins, int maxLosses) CalculateStreaks(List<Order> orders)
    {
        int maxWins = 0, maxLosses = 0, currentWins = 0, currentLosses = 0;
        foreach (var o in orders.OrderBy(o => o.OrderCloseAt ?? o.CreatedAt))
        {
            if (o.OrderProfit > 0)
            {
                currentWins++;
                currentLosses = 0;
                maxWins = Math.Max(maxWins, currentWins);
            }
            else
            {
                currentLosses++;
                currentWins = 0;
                maxLosses = Math.Max(maxLosses, currentLosses);
            }
        }
        return (maxWins, maxLosses);
    }

    // === Chat Streaming ===

    public async IAsyncEnumerable<string> ChatStream(
        long userId,
        AiChatStreamPayload payload,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var session = await _chatRepo.GetSession(payload.SessionId);
        if (session == null)
        {
            yield return "data: {\"error\": \"Session not found\"}\n\n";
            yield break;
        }

        await _chatRepo.SaveMessage(new AiChatMessage
        {
            SessionId = payload.SessionId,
            Role = "user",
            Content = payload.Message
        });

        var contextText = session.ContextSnapshot;
        if (string.IsNullOrEmpty(contextText) && payload.AccountId.HasValue && payload.AccountId.Value > 0)
        {
            contextText = await BuildAccountContext(payload.AccountId.Value);
        }

        var recentMessages = await _chatRepo.GetRecentMessages(payload.SessionId, 20);

        // Build Gemini contents array
        var contents = new List<object>();
        foreach (var msg in recentMessages)
        {
            contents.Add(new
            {
                role = msg.Role == "assistant" ? "model" : "user",
                parts = new[] { new { text = msg.Content } }
            });
        }

        var systemPrompt = SystemPromptChat;
        if (!string.IsNullOrEmpty(contextText))
        {
            systemPrompt += $"\n\n## Account-Kontext:\n{contextText}";
        }

        var fullResponse = new StringBuilder();

        await foreach (var chunk in CallGeminiStream(systemPrompt, contents, cancellationToken))
        {
            fullResponse.Append(chunk);
            var escaped = JsonSerializer.Serialize(chunk);
            yield return $"data: {{\"content\": {escaped}}}\n\n";
        }

        await _chatRepo.SaveMessage(new AiChatMessage
        {
            SessionId = payload.SessionId,
            Role = "assistant",
            Content = fullResponse.ToString(),
            TokensUsed = fullResponse.Length / 4
        });

        if (recentMessages.Count <= 1)
        {
            var title = payload.Message.Length > 50
                ? payload.Message[..50] + "..."
                : payload.Message;
            await _chatRepo.UpdateSessionTitle(payload.SessionId, title);
        }

        yield return "data: {\"done\": true}\n\n";
    }

    // === Analysis ===

    public async Task<(object?, ITError?)> AnalyzeStrategy(long? accountId)
    {
        try
        {
            if (!accountId.HasValue || accountId.Value <= 0)
                return (null, TError.NewClient("Account ID is required"));

            var context = await BuildAccountContext(accountId.Value);
            var result = await CallGemini(SystemPromptStrategy, context);

            try
            {
                // Strip markdown code fences if present
                var cleaned = result.Trim();
                if (cleaned.StartsWith("```")) {
                    var firstNewline = cleaned.IndexOf('\n');
                    cleaned = cleaned[(firstNewline + 1)..];
                    if (cleaned.EndsWith("```"))
                        cleaned = cleaned[..^3].Trim();
                }
                var json = JsonSerializer.Deserialize<JsonElement>(cleaned);
                return (json, null);
            }
            catch
            {
                return (new { raw = result }, null);
            }
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("Strategy analysis failed", ex.Message));
        }
    }

    public async Task<(object?, ITError?)> AnalyzeRisk(long? accountId)
    {
        try
        {
            if (!accountId.HasValue || accountId.Value <= 0)
                return (null, TError.NewClient("Account ID is required"));

            var context = await BuildAccountContext(accountId.Value);
            var result = await CallGemini(SystemPromptRisk, context);

            try
            {
                var cleaned = result.Trim();
                if (cleaned.StartsWith("```")) {
                    var firstNewline = cleaned.IndexOf('\n');
                    cleaned = cleaned[(firstNewline + 1)..];
                    if (cleaned.EndsWith("```"))
                        cleaned = cleaned[..^3].Trim();
                }
                var json = JsonSerializer.Deserialize<JsonElement>(cleaned);
                return (json, null);
            }
            catch
            {
                return (new { raw = result }, null);
            }
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("Risk analysis failed", ex.Message));
        }
    }

    // === Gemini API Calls ===

    private async Task<string> CallGemini(string systemPrompt, string userMessage)
    {
        var request = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = userMessage } } }
            },
            generationConfig = new { maxOutputTokens = 4096 }
        };

        var json = JsonSerializer.Serialize(request);
        var url = $"{BaseUrl}:generateContent?key={ApiKey}";
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.Fail($"Gemini API error: {response.StatusCode} — {responseBody}");
            throw new Exception($"Gemini API returned {response.StatusCode}");
        }

        var parsed = JsonSerializer.Deserialize<JsonElement>(responseBody);
        var text = parsed
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? "";
    }

    private async IAsyncEnumerable<string> CallGeminiStream(
        string systemPrompt,
        List<object> contents,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents,
            generationConfig = new { maxOutputTokens = 4096 }
        };

        var json = JsonSerializer.Serialize(request);
        var url = $"{BaseUrl}:streamGenerateContent?alt=sse&key={ApiKey}";
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.Fail($"Gemini API stream error: {response.StatusCode} — {errorBody}");
            yield return $"Error: Gemini API returned {response.StatusCode}";
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("data: ")) continue;

            var data = line[6..];
            string? extracted = null;

            try
            {
                var evt = JsonSerializer.Deserialize<JsonElement>(data);

                if (evt.TryGetProperty("candidates", out var candidates))
                {
                    var parts = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts");

                    var sb = new StringBuilder();
                    foreach (var part in parts.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var text))
                        {
                            var textValue = text.GetString();
                            if (!string.IsNullOrEmpty(textValue))
                                sb.Append(textValue);
                        }
                    }
                    if (sb.Length > 0)
                        extracted = sb.ToString();
                }
            }
            catch
            {
                // Skip unparseable SSE events
            }

            if (extracted != null)
                yield return extracted;
        }
    }
}
