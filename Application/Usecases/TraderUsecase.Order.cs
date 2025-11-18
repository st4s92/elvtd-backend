using System.Linq.Expressions;
using System.Text.Json;
using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Usecases;

public partial class TraderUsecase
{
    private static Expression<Func<Order, bool>> FilterOrder(Order param)
    {
        return (
            a =>
                (param.Id == 0 || a.Id == param.Id) &&
                (param.AccountId == 0 || a.AccountId == param.AccountId) &&
                (param.MasterOrderId == 0 || param.MasterOrderId == null || a.MasterOrderId == param.MasterOrderId) &&
                (param.OrderTicket == 0 || a.OrderTicket == param.OrderTicket) &&
                (param.CloseTicket == 0 || param.CloseTicket == null || a.CloseTicket == param.CloseTicket) &&
                (string.IsNullOrEmpty(param.OrderSymbol) || a.OrderSymbol == param.OrderSymbol) &&
                (string.IsNullOrEmpty(param.OrderType) || a.OrderType == param.OrderType) &&
                (param.OrderLot == 0 || a.OrderLot == param.OrderLot) &&
                (string.IsNullOrEmpty(param.OrderComment) || (a.OrderComment != null && a.OrderComment.Contains(param.OrderComment))) &&
                (param.Status == 0 || a.Status == param.Status)
        );
    }
    public async Task<(Order?, ITError?)> GetOrder(Order param)
    {
        try
        {
            var data = await _orderRepository.Get(FilterOrder(param));
            if (data == null)
                return (null, TError.NewNotFound("order not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(List<Order>, ITError?)> GetOrders(Order param)
    {
        try
        {
            var data = await _orderRepository.GetMany(FilterOrder(param));
            if (data == null)
                return ([], TError.NewNotFound("orders not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return ([], TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(List<Order>, long total, ITError?)> GetPaginatedOrders(Order param, int page, int pageSize)
    {
        try
        {
            var (data, total) = await _orderRepository.GetPaginated(FilterOrder(param), page, pageSize);
            if (data == null)
                return ([], 0, null);
            return (data, total, null);
        }
        catch (Exception ex)
        {
            return ([], 0, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(Order?, ITError?)> AddOrder(Order order)
    {
        var existingOrder = new Order
        {
            AccountId = order.AccountId,
            OrderTicket = order.OrderTicket
        };

        var (_, terr) = await GetOrder(existingOrder);
        if (terr != null)
        {
            if (terr.IsServer())
            {
                return (null, terr);
            }
        }
        else
        {
            return (null, TError.NewClient("order with the server name and order number already exist"));
        }

        try
        {
            var data = await _orderRepository.Save(order);
            if (data == null)
                return (null, TError.NewServer("cannot create new order"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(Order?, ITError?)> UpdateOrderById(long id, Order param)
    {   
        try
        {
            var (_, terr) = await GetOrder(new Order {Id = id});
            if(terr != null)
                return (null, terr);
            
            var data = await _orderRepository.Save(param, a => a.Id == id);
            if(data == null)
                return (null, TError.NewServer("cannot save order"));

            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }

    public async Task<(Order?, ITError?)> CreateOrder(Order order)
    {
        try
        {
            var data = await _orderRepository.Save(order);
            if(data == null)
                return (null, TError.NewServer("cannot create order"));

            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }

    public async Task<ITError?> ConfirmBridgeSlaveOrder(BridgeOrderPayload payload)
    {
        try
        {
            var (account, accErr) = await GetAccount(new Account
            {
                ServerName = payload.ServerName,
                AccountNumber = payload.AccountId
            });
            if (accErr != null)
                return accErr;
            
            if (account == null)
                return TError.NewNotFound("account not found");

            var (existingOrder, terr) = await GetOrder(new Order
            {
                MasterOrderId = payload.Order.MasterOrderId,
                AccountId = account.Id,
            });
            if (terr != null)
                return terr;
            
            if (existingOrder == null)
                return TError.NewNotFound("order not found");


            existingOrder.OrderTicket = payload.Order.OrderTicket;
            existingOrder.ActualPrice = payload.Order.ActualPrice;
            existingOrder.OrderLot = payload.Order.OrderLot;
            existingOrder.Status = OrderStatus.Success;

            var (_, terrs) = await UpdateOrderById(existingOrder.Id, existingOrder);
            if (terrs != null)
                return terrs;

            return null;
        }
        catch (Exception ex)
        {
            return TError.NewServer(ex.Message);
        }
    }

    public async Task<ITError?> CreateBridgeMasterOrder(BridgeListOrderPayload payload)
    {
        try
        {
            var (account, accErr) = await GetAccount(new Account
            {
                ServerName = payload.ServerName,
                AccountNumber = payload.AccountId
            });
            if (accErr != null)
                return accErr;

            var (existingOrders, ordErr) = await GetOrders(new Order
            {
                OrderCloseAt = null,
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
                .Where(po => OrderTimeHelper.IsOrderFresh(po.OrderOpenAt, 5))
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
                    OrderOpenAt = item.OrderOpenAt
                };

                var (newOdr, terr) = await CreateOrder(order);
                if (terr != null)
                    return terr;

                newOdrs.Add(newOdr!);
            }

            foreach (var item in deletedOrders)
            {
                item.OrderCloseAt = DateTime.Now;

                var (_, terr) = await UpdateOrderById(item.Id, item);
                if (terr != null)
                    return terr;
            }

            var terrz = await CopyBridgeMasterOrder(account, newOdrs, deletedOrders);
            if (terrz != null)
                return terrz;

            return null;
        }
        catch (Exception ex)
        {
            return TError.NewServer(ex.Message);
        }
    }

    public async Task<ITError?> CopyBridgeMasterOrder(Account masterAccount, List<Order> newOrders, List<Order> closedOrders)
    {
        try
        {
            var (masterSlaves, terr) = await GetMasterSlaves(new MasterSlave { MasterId = masterAccount.Id });
            if (terr != null)
            {
                return terr;
            }

            if (masterSlaves.Count <= 0)
            {
                return null;
            }

            List<Order> newSlaveOrders = [];
            List<Order> updatedSlaveOrders = [];

            // iterate list of slaves
            foreach (var item in masterSlaves)
            {
                if (item == null)
                {
                    continue;
                }

                List<BridgeOrderBroadcastPayload> messages = [];
                // iterate list of orders
                foreach (var order in newOrders)
                {
                    var (masterSlavePair, terrs) = await GetMasterSlavePair(new MasterSlavePair { MasterSlaveId = item.Id, MasterPair = order.OrderSymbol });
                    if (terrs != null || masterSlavePair == null)
                    {
                        continue;
                    }

                    double multiplier = 1;
                    var (masterSlaveConfig, terrt) = await GetMasterSlaveConfig(new MasterSlaveConfig { MasterSlaveId = item.Id });
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
                        MasterOrderId = order.Id,
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
                        Status = OrderStatus.Pending,
                        OrderOpenAt = DateTime.Now
                    };
                    newSlaveOrders.Add(slaveOrder);
                }

                // iterate list of orders
                foreach (var order in closedOrders)
                {
                    var (activedOrderSlave, terru) = await GetOrder(new Order { MasterOrderId = order.Id, AccountId = item.SlaveId });
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
                        MasterOrderId = order.Id,
                        CopyType = "MASTER_ORDER_DELETE",

                    };
                    messages.Add(newOrderMsg);

                    if (item.SlaveAccount == null)
                    {
                        continue;
                    }

                    activedOrderSlave.OrderCloseAt = DateTime.Now;
                    updatedSlaveOrders.Add(activedOrderSlave);
                }
                if(messages.Count <= 0)
                    continue;
                    
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
                    var (_, terra) = await CreateOrder(item);
                    if (terra != null)
                    {
                        return terra;
                    }
                }

                foreach (var item in updatedSlaveOrders)
                {
                    var (_, terra) = await UpdateOrderById(item.Id, item);
                    if (terra != null)
                    {
                        return terra;
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            
            return TError.NewServer(ex.Message);
        }
    }
}