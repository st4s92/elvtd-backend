using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Usecases;

public partial class TraderUsecase
{
    public async Task<(object?, ITError?)> SendTestTrade(long accountId, TestTradePayload payload)
    {
        var account = await _accountRepository.Get(a => a.Id == accountId);
        if (account == null)
            return (null, TError.NewNotFound("Account not found"));

        var magic = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var orderType = payload.OrderType.ToUpper() switch
        {
            "BUY" => "DEAL_TYPE_BUY",
            "SELL" => "DEAL_TYPE_SELL",
            _ => payload.OrderType,
        };

        // The slave-copier reads from the active_orders table (via /api/trader/bridge/active-position/sync),
        // NOT from the orders table. We must insert into active_orders for the intent to appear.
        var activeOrder = new ActiveOrder
        {
            AccountId = account.Id,
            AccountNumber = account.AccountNumber,
            ServerName = account.ServerName,
            OrderTicket = 0,
            OrderSymbol = payload.Symbol,
            OrderType = orderType,
            OrderLot = payload.Lot,
            OrderMagic = magic,
            Status = OrderStatus.Success,
            OrderLabel = "test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var saved = await _activeOrderRepository.Add(activeOrder);

        return (new
        {
            status = true,
            message = $"Test trade created for {account.PlatformName} account {account.AccountNumber}: {orderType} {payload.Lot} {payload.Symbol} (ActiveOrder ID: {saved.Id}, Magic: {magic})",
            order_id = saved.Id,
            magic = magic,
        }, null);
    }
}
