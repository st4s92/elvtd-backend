using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Usecases;

public partial class TraderUsecase
{
    public async Task<(object?, ITError?)> SendTestTrade(long accountId, TestTradePayload payload)
    {
        var account = await _accountRepository.Get<Account>(a => a.Id == accountId);
        if (account == null)
            return (null, new ITError { Status = 404, Message = "Account not found" });

        var broadcastPayload = new BridgeOrderBroadcastPayload
        {
            SlavePair = payload.Symbol,
            OrderType = payload.OrderType,
            OrderLot = payload.Lot,
            OrderTicket = 0,
            MasterOrderId = 0,
            OrderMagic = 999999,
            CopyType = "MASTER_ORDER_UPDATE",
            CreatedAt = DateTime.UtcNow,
        };

        var platform = account.PlatformName?.ToLower() ?? "";

        if (platform.Contains("ctrader"))
        {
            await _jobPublisher.PublishCtraderPacket(
                account.AccountNumber,
                "trade",
                broadcastPayload
            );
        }
        else
        {
            await _jobPublisher.PublishMt5Packet(
                account.ServerName ?? "",
                account.AccountNumber,
                "trade",
                broadcastPayload
            );
        }

        return (new
        {
            status = true,
            message = $"Test trade sent to {account.PlatformName} account {account.AccountNumber}: {payload.OrderType} {payload.Lot} {payload.Symbol}"
        }, null);
    }
}
