using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Backend.Infrastructure.Ctrader;

/// <summary>
/// Minimal cTrader Open API client using the JSON-over-WebSocket endpoint (port 5036).
/// Used to fetch LIVE open positions for an account directly from the broker,
/// independent of what the local DB thinks is open.
/// </summary>
public class CtraderOpenApiJsonClient
{
    private const int AppAuthReq = 2100;
    private const int AppAuthRes = 2101;
    private const int AccountAuthReq = 2102;
    private const int AccountAuthRes = 2103;
    private const int SymbolsListReq = 2114;
    private const int SymbolsListRes = 2115;
    private const int SymbolByIdReq = 2116;
    private const int SymbolByIdRes = 2117;
    private const int ReconcileReq = 2124;
    private const int ReconcileRes = 2125;
    private const int ErrorRes = 2142;
    private const int ProtoErrorRes = 50;
    private const int HeartbeatEvent = 51;
    private const int GetPositionUnrealizedPnlReq = 2187;
    private const int GetPositionUnrealizedPnlRes = 2188;

    private static readonly TimeSpan MessageTimeout = TimeSpan.FromSeconds(12);

    private readonly string _clientId;
    private readonly string _clientSecret;

    public CtraderOpenApiJsonClient()
    {
        _clientId = Environment.GetEnvironmentVariable("CTRADER_CLIENT_ID") ?? "";
        _clientSecret = Environment.GetEnvironmentVariable("CTRADER_CLIENT_SECRET") ?? "";
    }

    public class LiveBrokerPosition
    {
        public long PositionId { get; set; }
        public long SymbolId { get; set; }
        public string Symbol { get; set; } = "";
        public string Side { get; set; } = ""; // BUY / SELL
        public long Volume { get; set; }
        public double Lot { get; set; }
        public double OpenPrice { get; set; }
        public long OpenTimestampMs { get; set; }
        public string Label { get; set; } = "";
        public double? UnrealizedPnl { get; set; }
    }

    /// <summary>
    /// Fetches all open positions for the given cTrader account.
    /// Tries the DEMO host first, then LIVE (account auth only succeeds on the correct one).
    /// </summary>
    public async Task<List<LiveBrokerPosition>> GetLivePositions(long ctid, string accessToken, CancellationToken ct = default)
    {
        Exception? lastErr = null;
        foreach (var host in new[] { "demo.ctraderapi.com", "live.ctraderapi.com" })
        {
            try
            {
                return await FetchFromHost(host, ctid, accessToken, ct);
            }
            catch (Exception ex)
            {
                lastErr = ex;
            }
        }
        throw lastErr ?? new Exception("ctrader live fetch failed");
    }

    private async Task<List<LiveBrokerPosition>> FetchFromHost(string host, long ctid, string accessToken, CancellationToken ct)
    {
        using var ws = new ClientWebSocket();
        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connectCts.CancelAfter(MessageTimeout);
        await ws.ConnectAsync(new Uri($"wss://{host}:5036"), connectCts.Token);

        try
        {
            await Request(ws, AppAuthReq, new { clientId = _clientId, clientSecret = _clientSecret }, AppAuthRes, ct);
            await Request(ws, AccountAuthReq, new { ctidTraderAccountId = ctid, accessToken }, AccountAuthRes, ct);

            // Open positions
            var reconcile = await Request(ws, ReconcileReq, new { ctidTraderAccountId = ctid }, ReconcileRes, ct);
            var positions = new List<LiveBrokerPosition>();
            if (reconcile.TryGetProperty("position", out var posArr) && posArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var pos in posArr.EnumerateArray())
                {
                    var td = pos.GetProperty("tradeData");
                    positions.Add(new LiveBrokerPosition
                    {
                        PositionId = L(pos, "positionId"),
                        SymbolId = L(td, "symbolId"),
                        Volume = L(td, "volume"),
                        Side = Side(td),
                        OpenPrice = D(pos, "price"),
                        OpenTimestampMs = L(td, "openTimestamp"),
                        Label = td.TryGetProperty("label", out var lbl) ? (lbl.GetString() ?? "") : "",
                    });
                }
            }

            if (positions.Count == 0)
                return positions;

            // Symbol names
            var symbolNames = new Dictionary<long, string>();
            var symbols = await Request(ws, SymbolsListReq, new { ctidTraderAccountId = ctid }, SymbolsListRes, ct);
            if (symbols.TryGetProperty("symbol", out var symArr) && symArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in symArr.EnumerateArray())
                {
                    var name = s.TryGetProperty("symbolName", out var sn) ? sn.GetString() : null;
                    if (name != null) symbolNames[L(s, "symbolId")] = name;
                }
            }

            // stepVolume for lot conversion (lots = volume / stepVolume)
            var stepVolumes = new Dictionary<long, long>();
            var distinctIds = positions.Select(p => p.SymbolId).Distinct().ToArray();
            try
            {
                var full = await Request(ws, SymbolByIdReq, new { ctidTraderAccountId = ctid, symbolId = distinctIds }, SymbolByIdRes, ct);
                if (full.TryGetProperty("symbol", out var fullArr) && fullArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var s in fullArr.EnumerateArray())
                    {
                        var step = s.TryGetProperty("stepVolume", out var sv) ? L(sv) : 0;
                        if (step > 0) stepVolumes[L(s, "symbolId")] = step;
                    }
                }
            }
            catch
            {
                // non-fatal: lot falls back to volume/100 below
            }

            // Unrealized PnL
            var pnlMap = new Dictionary<long, double>();
            try
            {
                var pnl = await Request(ws, GetPositionUnrealizedPnlReq, new { ctidTraderAccountId = ctid }, GetPositionUnrealizedPnlRes, ct);
                var moneyDigits = pnl.TryGetProperty("moneyDigits", out var md) ? L(md) : 2;
                var divisor = Math.Pow(10, moneyDigits > 0 ? moneyDigits : 2);
                if (pnl.TryGetProperty("positionUnrealizedPnL", out var pnlArr) && pnlArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var e in pnlArr.EnumerateArray())
                    {
                        pnlMap[L(e, "positionId")] = L(e, "netUnrealizedPnL") / divisor;
                    }
                }
            }
            catch
            {
                // non-fatal: PnL stays null
            }

            foreach (var p in positions)
            {
                p.Symbol = symbolNames.TryGetValue(p.SymbolId, out var name) ? name : $"#{p.SymbolId}";
                var step = stepVolumes.TryGetValue(p.SymbolId, out var s) && s > 0 ? s : 100;
                p.Lot = Math.Round((double)p.Volume / step, 2);
                if (pnlMap.TryGetValue(p.PositionId, out var upnl)) p.UnrealizedPnl = Math.Round(upnl, 2);
            }

            return positions;
        }
        finally
        {
            try
            {
                if (ws.State == WebSocketState.Open)
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
            }
            catch { /* ignore close errors */ }
        }
    }

    // ===== wire helpers =====

    private static async Task<JsonElement> Request(ClientWebSocket ws, int payloadType, object payload, int expectType, CancellationToken ct)
    {
        var msgId = Guid.NewGuid().ToString("N");
        var json = JsonSerializer.Serialize(new { clientMsgId = msgId, payloadType, payload });
        await ws.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, ct);

        var deadline = DateTime.UtcNow + MessageTimeout;
        while (DateTime.UtcNow < deadline)
        {
            using var doc = JsonDocument.Parse(await ReceiveMessage(ws, deadline, ct));
            var root = doc.RootElement;
            var type = (int)L(root, "payloadType");

            if (type == HeartbeatEvent) continue;

            var hasPayload = root.TryGetProperty("payload", out var pl);
            if (type == ErrorRes || type == ProtoErrorRes)
            {
                var code = hasPayload && pl.TryGetProperty("errorCode", out var ec) ? ec.GetString() : "?";
                var desc = hasPayload && pl.TryGetProperty("description", out var de) ? de.GetString() : "";
                throw new Exception($"ctrader error: {code} {desc}");
            }
            if (type == expectType)
            {
                return hasPayload ? pl.Clone() : default;
            }
            // unrelated event (execution, spots, ...) → keep waiting
        }
        throw new TimeoutException($"timeout waiting for payloadType {expectType}");
    }

    private static async Task<string> ReceiveMessage(ClientWebSocket ws, DateTime deadline, CancellationToken ct)
    {
        var buffer = new byte[64 * 1024];
        var sb = new StringBuilder();
        while (true)
        {
            var remaining = deadline - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) throw new TimeoutException("ctrader receive timeout");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(remaining);
            var result = await ws.ReceiveAsync(buffer, cts.Token);
            if (result.MessageType == WebSocketMessageType.Close)
                throw new Exception("ctrader websocket closed");
            sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            if (result.EndOfMessage) return sb.ToString();
        }
    }

    // int64 fields arrive as JSON strings (proto3 JSON mapping) — parse both forms
    private static long L(JsonElement e) =>
        e.ValueKind == JsonValueKind.String ? long.Parse(e.GetString()!) : e.GetInt64();

    private static long L(JsonElement parent, string prop) =>
        parent.TryGetProperty(prop, out var e) ? L(e) : 0;

    private static double D(JsonElement parent, string prop)
    {
        if (!parent.TryGetProperty(prop, out var e)) return 0;
        return e.ValueKind == JsonValueKind.String ? double.Parse(e.GetString()!, System.Globalization.CultureInfo.InvariantCulture) : e.GetDouble();
    }

    private static string Side(JsonElement tradeData)
    {
        if (!tradeData.TryGetProperty("tradeSide", out var e)) return "BUY";
        if (e.ValueKind == JsonValueKind.String) return e.GetString() ?? "BUY";
        return L(e) == 2 ? "SELL" : "BUY";
    }
}
