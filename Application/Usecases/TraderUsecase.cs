using System.Text.Json;
using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Usecases;

public class TraderUsecase
{
    private readonly ITradingRepository _tradingRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly AppLogger<TraderUsecase> _logger;
    private readonly WebSocketServer _wsServer;
    public TraderUsecase(
        ITradingRepository tradingRepository,
        IAccountRepository accountRepository,
        IOrderRepository orderRepository,
        AppLogger<TraderUsecase> logger,
        WebSocketServer wsServer
    )
    {
        _tradingRepository = tradingRepository;
        _accountRepository = accountRepository;
        _orderRepository = orderRepository;
        _logger = logger;
        _wsServer = wsServer;
    }

    /* Order Section */
    public async Task<(Order?, ITError?)> SaveOrder(Order? order)
    {
        if (order == null)
        {
            return (null, TError.NewClient("cannot handle order payload"));
        }
        return await _orderRepository.SaveOrder(order!);
    }

    public async Task<(Order?, ITError?)> GetOrder(Order order)
    {
        return await _orderRepository.GetOrder(order);
    }

    public async Task<(List<Order>, ITError?)> GetOrders(Order order)
    {
        return await _orderRepository.GetOrders(order);
    }

    public async Task<ITError?> CreateBridgeMasterOrder(BridgeOrderPayload payload)
    {
        var (account, accErr) = await GetAccount(new Account
        {
            ServerName = payload.ServerName,
            AccountNumber = payload.AccountId
        });
        if (accErr != null)
            return accErr;

        var (existingOrders, ordErr) = await _orderRepository.GetActiveOrders(new Order
        {
            AccountId = account!.Id
        });

        if (ordErr != null)
            return ordErr;

        var payloadOrderTickets = payload.Orders
            .Select(o => o.OrderTicket)
            .ToHashSet();

        List<Order> deletedOrders = [.. existingOrders.Where(dbOrder => !payloadOrderTickets.Contains(dbOrder.OrderTicket))];

        var existingOrderTickets = existingOrders.Select(o => o.OrderTicket).ToHashSet();
        var newOrders = payload.Orders
            .Where(po => !existingOrderTickets.Contains(po.OrderTicket))
            .ToList();

        List<Order> newOdrs = [];
        foreach (var item in newOrders)
        {
            var order = new Order
            {
                AccountId = account.Id,
                MasterOrderId = null,
                CopyMessage = null,
                OrderTicket = item.OrderTicket,
                CloseTicket = item.CloseTicket,
                OrderSymbol = item.OrderSymbol,
                OrderType = item.OrderType,
                OrderLot = item.OrderLot,
                OrderPrice = item.OrderPrice,
                ActualPrice = item.ActualPrice ?? item.OrderPrice,
                OrderComment = item.OrderComment,
                Status = OrderStatus.Success,
                OrderOpenAt = DateTime.Now
            };

            var (newOdr, terr) = await SaveOrder(order);
            if (terr != null)
                return terr;

            newOdrs.Add(newOdr!);
        }

        foreach (var item in deletedOrders)
        {
            item.OrderCloseAt = DateTime.Now;

            var (_, terr) = await SaveOrder(item);
            if (terr != null)
                return terr;
        }

        var terrz = await CopyBridgeMasterOrder(account, newOdrs, deletedOrders);
        if (terrz != null)
            return terrz;

        return null;
    }

    public async Task<ITError?> CopyBridgeMasterOrder(Account masterAccount, List<Order> newOrders, List<Order> closedOrders)
    {
        var (masterSlave, terr) = await _accountRepository.GetMasterSlaves(new MasterSlave { MasterId = masterAccount.Id });
        if (terr != null)
        {
            return terr;
        }

        if (masterSlave.Count <= 0)
        {
            return null;
        }

        List<Order> newSlaveOrders = [];
        List<Order> updatedSalveOrders = [];

        // iterate list of slaves
        foreach (var item in masterSlave)
        {
            if (item == null)
            {
                continue;
            }

            List<BridgeOrderBroadcastPayload> messages = [];
            // iterate list of orders
            foreach (var order in newOrders)
            {
                var (masterSlavePair, terrs) = await _accountRepository.GetMasterSlavePair(new MasterSlavePair { MasterSlaveId = item.Id, MasterPair = order.OrderSymbol });
                if (terrs != null || masterSlavePair == null)
                {
                    continue;
                }

                double multiplier = 1;
                var (masterSlaveConfig, terrt) = await _accountRepository.GetMasterSlaveConfig(new MasterSlaveConfig { MasterSlaveId = item.Id });
                if (terrt == null && masterSlaveConfig != null)
                {
                    multiplier = (double)(masterSlaveConfig!.Multiplier * order.OrderLot);
                    if (multiplier < 0.01)
                    {
                        multiplier = 0.01;
                    }
                }

                var newOrderMsg = new BridgeOrderBroadcastPayload
                {
                    SlavePair = masterSlavePair.SlavePair,
                    OrderType = order.OrderType,
                    OrderLot = multiplier,
                    OrderTicket = order.OrderTicket,
                    MasterAccountId = order.AccountId,
                    CopyType = "MASTER_ORDER_UPDATE"
                };

                messages.Add(newOrderMsg);

                if (item.SlaveAccount == null)
                {
                    continue;
                }
                var slaveOrder = new Order
                {
                    AccountId = item.SlaveAccount.Id,
                    MasterOrderId = order.Id,
                    OrderTicket = 0,
                    OrderSymbol = masterSlavePair.SlavePair,
                    OrderType = order.OrderType,
                    OrderLot = (decimal)multiplier,
                    OrderPrice = 0,
                    Status = OrderStatus.Success,
                    OrderOpenAt = DateTime.Now
                };
                newSlaveOrders.Add(slaveOrder);
            }

            // iterate list of orders
            foreach (var order in closedOrders)
            {
                var (activedOrderSlave, terru) = await _orderRepository.GetOrder(new Order { MasterOrderId = order.Id, AccountId = item.SlaveId });
                if (terru != null)
                {
                    continue;
                }

                if (activedOrderSlave?.OrderCloseAt != null)
                {
                    continue;
                }

                var newOrderMsg = new BridgeOrderBroadcastPayload
                {
                    SlavePair = order.OrderSymbol,
                    OrderType = order.OrderType,
                    OrderLot = (double)order.OrderLot,
                    OrderTicket = activedOrderSlave!.OrderTicket,
                    MasterAccountId = order.AccountId,
                    CopyType = "MASTER_ORDER_DELETE"
                };
                messages.Add(newOrderMsg);

                if (item.SlaveAccount == null)
                {
                    continue;
                }

                activedOrderSlave.OrderCloseAt = DateTime.Now;
                newSlaveOrders.Add(activedOrderSlave);
            }
            _logger.Info("newSlaveOrders", newSlaveOrders);

            var msgs = JsonSerializer.Serialize(messages);
            if (item.SlaveAccount?.AccountNumber != null)
            {
                await _wsServer.BroadcastToAccounts(
                    [$"{item.SlaveAccount.ServerName}:{item.SlaveAccount.AccountNumber}"],
                    msgs
                );
            }

            _logger.Info("✅ Broadcasted order update to slaves", msgs);
        }

        // handle post broadcast
        {
            foreach (var item in newSlaveOrders)
            {
                var (_, terra) = await SaveOrder(item);
                if (terra != null)
                {
                    return terra;
                }
            }
        }

        return null;
    }

    /* Account Section */
    public async Task<(Account?, ITError?)> GetAccount(Account account)
    {
        return await _accountRepository.GetAccount(account);
    }

    public async Task<(Account?, ITError?)> AddAccount(Account account)
    {
        var existingAccount = new Account
        {
            AccountNumber = account.AccountNumber,
            ServerName = account.ServerName
        };

        var (_, terr) = await GetAccount(existingAccount);
        if (terr != null)
        {
            if (terr.IsServer())
            {
                return (null, terr);
            }
        }
        else
        {
            return (null, TError.NewClient("account with the server name and account number already exist"));
        }
        return await SaveAccount(account);
    }
    
    public async Task<(Account?, ITError?)> SaveAccount(Account? account)
    {
        if (account == null)
        {
            return (null, TError.NewClient("cannot handle order payload"));
        }
        return await _accountRepository.SaveAccount(account!);
    }

}