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

        // Create an order in the DB that the slave-copier will pick up
        // on its next sync poll (via /api/trader/bridge/account-sync).
        // The slave-copier reads orders with OrderCloseAt == null as intents.
        var magic = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var order = new Order
        {
            AccountId = account.Id,
            OrderTicket = 0,
            OrderSymbol = payload.Symbol,
            // Python slave-copier expects DEAL_TYPE_BUY / DEAL_TYPE_SELL
            OrderType = payload.OrderType.ToUpper() switch
            {
                "BUY" => "DEAL_TYPE_BUY",
                "SELL" => "DEAL_TYPE_SELL",
                _ => payload.OrderType,
            },
            OrderLot = payload.Lot,
            OrderMagic = magic,
            Status = OrderStatus.Success,
            CopyMessage = "Test trade",
            OrderLabel = "test",
        };

        var saved = await _orderRepository.Save(order);

        return (new
        {
            status = true,
            message = $"Test trade created for {account.PlatformName} account {account.AccountNumber}: {payload.OrderType} {payload.Lot} {payload.Symbol} (Order ID: {saved.Id}, Magic: {magic})",
            order_id = saved.Id,
            magic = magic,
        }, null);
    }
}
