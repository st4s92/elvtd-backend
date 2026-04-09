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

        if (account.Balance <= 0)
            return (null, TError.NewClient($"Account balance is {account.Balance} — must be positive"));

        if (payload.MasterBalance <= 0)
            return (null, TError.NewClient("Master balance must be positive"));

        var orderType = payload.OrderType.ToUpper() switch
        {
            "BUY" => "DEAL_TYPE_BUY",
            "SELL" => "DEAL_TYPE_SELL",
            _ => payload.OrderType,
        };

        // Lot calculation — same formula as CopyMasterOrderToSlaves:
        // slaveLot = (masterLot / masterBalance) * slaveBalance * multiplier
        // For test trades, multiplier = 1.0
        decimal riskRatio = payload.MasterLot / payload.MasterBalance;
        decimal slaveLot = Math.Round(riskRatio * account.Balance * 1.0m, 2);
        if (slaveLot < 0.01m) slaveLot = 0.01m;
        if (slaveLot > 100.0m) slaveLot = 100.0m;

        // Symbol mapping — same as CopyMasterOrderToSlaves
        var allSymbolMaps = await _symbolMapRepository.GetMany(x => x.DeletedAt == null);
        var mappedSymbol = ResolveSymbol(
            payload.Symbol,
            "TEST",  // master broker (test)
            account.BrokerName ?? "",
            new Dictionary<string, string>(), // no pair overrides
            allSymbolMaps
        ) ?? payload.Symbol;

        var magic = GenerateBridgeMagicNumber(0, account.Id);

        // 1. Create Order in orders table
        var order = new Order
        {
            AccountId = account.Id,
            OrderTicket = 0,
            OrderSymbol = mappedSymbol,
            OrderType = orderType,
            OrderLot = slaveLot,
            OrderMagic = magic,
            Status = OrderStatus.Progress,
            OrderLabel = "test_trade",
            CopyMessage = $"Test trade: masterLot={payload.MasterLot} masterBal={payload.MasterBalance} → slaveLot={slaveLot}",
            CreatedAt = DateTime.UtcNow,
        };

        var (savedOrder, saveErr) = await CreateOrder(order);
        if (saveErr != null || savedOrder == null)
            return (null, TError.NewServer($"Failed to create order: {saveErr?.Message}"));

        // 2. Create ActiveOrder (slave-copier reads this via sync)
        var activeOrder = new ActiveOrder
        {
            AccountId = account.Id,
            AccountNumber = account.AccountNumber,
            ServerName = account.ServerName,
            OrderTicket = 0,
            OrderSymbol = mappedSymbol,
            OrderType = orderType,
            OrderLot = slaveLot,
            OrderMagic = magic,
            Status = OrderStatus.Success,
            OrderLabel = "test_trade",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await _activeOrderRepository.Add(activeOrder);

        // v3: Push trade command via RabbitMQ for instant execution
        var broadcastPayload = new BridgeOrderBroadcastPayload
        {
            SlavePair = mappedSymbol,
            OrderType = orderType,
            OrderLot = slaveLot,
            OrderTicket = 0,
            MasterOrderID = savedOrder.Id,
            OrderMagic = magic,
            CopyType = "MASTER_ORDER_UPDATE",
            CreatedAt = DateTime.UtcNow.ToString("o"),
        };

        if (account.PlatformName == "cTrader")
        {
            await _jobPublisher.PublishCtraderPacketBatch(
                account.AccountNumber,
                new List<object> { broadcastPayload });
        }
        else
        {
            await _jobPublisher.PublishMt5PacketBatch(
                account.ServerName,
                account.AccountNumber,
                new List<object> { broadcastPayload });
        }

        return (new
        {
            status = true,
            message = $"Test trade sent: {orderType} {mappedSymbol} | MasterLot: {payload.MasterLot} (Bal: {payload.MasterBalance}) → SlaveLot: {slaveLot} (Bal: {account.Balance}) | Magic: {magic}",
            order_id = savedOrder.Id,
            magic = magic,
            calculated_lot = slaveLot,
            mapped_symbol = mappedSymbol,
            slave_balance = account.Balance,
        }, null);
    }
}
