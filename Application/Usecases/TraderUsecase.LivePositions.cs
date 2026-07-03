using Backend.Helper;
using Backend.Infrastructure.Ctrader;
using Backend.Model;

namespace Backend.Application.Usecases;

public class LivePositionDto
{
    public long PositionId { get; set; }
    public string Symbol { get; set; } = "";
    public string Side { get; set; } = "";
    public double Lot { get; set; }
    public double OpenPrice { get; set; }
    public long OpenTimestampMs { get; set; }
    public string Label { get; set; } = "";
    public double? UnrealizedPnl { get; set; }
    public bool Tracked { get; set; }
    public long? DbId { get; set; }
    public decimal? DbLot { get; set; }
}

public class DbOnlyOrderDto
{
    public long Id { get; set; }
    public long OrderTicket { get; set; }
    public string OrderSymbol { get; set; } = "";
    public string OrderType { get; set; } = "";
    public decimal OrderLot { get; set; }
}

public class LivePositionsResultDto
{
    public bool LiveAvailable { get; set; }
    public string? Error { get; set; }
    public List<LivePositionDto> Positions { get; set; } = new();
    public List<DbOnlyOrderDto> DbOnly { get; set; } = new();
}

public partial class TraderUsecase
{
    public async Task<(LivePositionsResultDto?, ITError?)> GetLivePositions(long accountId)
    {
        try
        {
            var account = await _accountRepository.Get(
                a => a.Id == accountId && a.DeletedAt == null
            );
            if (account == null)
                return (null, TError.NewNotFound("account not found"));

            var result = new LivePositionsResultDto();

            if (!account.PlatformName.Contains("cTrader", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(account.AccessToken)
                || account.CtidTraderAccountId is null or 0)
            {
                result.LiveAvailable = false;
                result.Error = "no ctrader credentials on account";
                return (result, null);
            }

            // ===== live positions from broker =====
            List<CtraderOpenApiJsonClient.LiveBrokerPosition> livePositions;
            try
            {
                var client = new CtraderOpenApiJsonClient();
                livePositions = await client.GetLivePositions(
                    account.CtidTraderAccountId.Value,
                    account.AccessToken!
                );
                result.LiveAvailable = true;
            }
            catch (Exception ex)
            {
                result.LiveAvailable = false;
                result.Error = ex.Message;
                return (result, null);
            }

            // ===== db side (same source as account detail) =====
            var dbByTicket = new Dictionary<long, (long Id, string Symbol, string Type, decimal Lot)>();
            if (account.Role == "MASTER")
            {
                var masterOrders = await _orderRepository.GetMany(
                    o => o.AccountId == account.Id
                        && o.DeletedAt == null
                        && o.OrderCloseAt == null
                        && o.Status == OrderStatus.Success
                );
                foreach (var o in masterOrders)
                    dbByTicket[o.OrderTicket] = (o.Id, o.OrderSymbol, o.OrderType, o.OrderLot);
            }
            else
            {
                var slaveOrders = await _activeOrderRepository.GetMany(
                    o => o.AccountId == account.Id
                        && (o.Status == OrderStatus.Progress || o.Status == OrderStatus.Success)
                );
                foreach (var o in slaveOrders)
                    dbByTicket[o.OrderTicket] = (o.Id, o.OrderSymbol, o.OrderType, o.OrderLot);
            }

            // ===== merge =====
            var matchedTickets = new HashSet<long>();
            foreach (var p in livePositions)
            {
                var dto = new LivePositionDto
                {
                    PositionId = p.PositionId,
                    Symbol = p.Symbol,
                    Side = p.Side,
                    Lot = p.Lot,
                    OpenPrice = p.OpenPrice,
                    OpenTimestampMs = p.OpenTimestampMs,
                    Label = p.Label,
                    UnrealizedPnl = p.UnrealizedPnl,
                };
                if (dbByTicket.TryGetValue(p.PositionId, out var db))
                {
                    dto.Tracked = true;
                    dto.DbId = db.Id;
                    dto.DbLot = db.Lot;
                    matchedTickets.Add(p.PositionId);
                }
                result.Positions.Add(dto);
            }

            // db entries without a matching live position (stale in DB)
            foreach (var (ticket, db) in dbByTicket)
            {
                if (matchedTickets.Contains(ticket)) continue;
                result.DbOnly.Add(new DbOnlyOrderDto
                {
                    Id = db.Id,
                    OrderTicket = ticket,
                    OrderSymbol = db.Symbol,
                    OrderType = db.Type,
                    OrderLot = db.Lot,
                });
            }

            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("live positions error", ex.Message));
        }
    }
}
