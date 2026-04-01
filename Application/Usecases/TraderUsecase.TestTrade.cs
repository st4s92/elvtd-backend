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

        var orderType = payload.OrderType.ToUpper() switch
        {
            "BUY" => "DEAL_TYPE_BUY",
            "SELL" => "DEAL_TYPE_SELL",
            _ => payload.OrderType,
        };

        // Generate a unique magic number (same logic as CopyMasterOrderToSlaves)
        var magic = GenerateBridgeMagicNumber(0, account.Id);

        // 1. Create Order in orders table (same as CopyMasterOrderToSlaves line 2334)
        var order = new Order
        {
            AccountId = account.Id,
            OrderTicket = 0,
            OrderSymbol = payload.Symbol,
            OrderType = orderType,
            OrderLot = payload.Lot,
            OrderMagic = magic,
            Status = OrderStatus.Progress,
            OrderLabel = "test_trade",
            CopyMessage = "Test trade",
            CreatedAt = DateTime.UtcNow,
        };

        var (savedOrder, saveErr) = await CreateOrder(order);
        if (saveErr != null || savedOrder == null)
            return (null, TError.NewServer($"Failed to create order: {saveErr?.Message}"));

        // 2. Create ActiveOrder (this is what the slave-copier reads via sync)
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
            OrderLabel = "test_trade",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _activeOrderRepository.Add(activeOrder);

        return (new
        {
            status = true,
            message = $"Test trade sent to {account.PlatformName} account {account.AccountNumber}: {orderType} {payload.Lot} {payload.Symbol} (Order: {savedOrder.Id}, Magic: {magic})",
            order_id = savedOrder.Id,
            magic = magic,
        }, null);
    }
}
