using System;
using System.Linq.Expressions;
using System.Text.Json;
using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Application.Usecases;

public partial class TraderUsecase
{
    private static Expression<Func<Order, bool>> FilterOrder(Order param)
    {
        return a =>
            (param.Id == 0 || a.Id == param.Id)
            && (param.AccountId == 0 || a.AccountId == param.AccountId)
            && (!param.MasterOrderId.HasValue || a.MasterOrderId == param.MasterOrderId.Value)
            && (param.IsMasterOnly != true || a.MasterOrderId == null)
            && (param.OrderTicket == 0 || a.OrderTicket == param.OrderTicket)
            && (string.IsNullOrEmpty(param.OrderSymbol) || a.OrderSymbol == param.OrderSymbol)
            && (string.IsNullOrEmpty(param.OrderType) || a.OrderType == param.OrderType)
            && (param.OrderLot == 0 || a.OrderLot == param.OrderLot)
            && (param.Status == 0 || a.Status == param.Status)
            && (param.IsClosed == null || (param.IsClosed == true ? a.OrderCloseAt != null : a.OrderCloseAt == null))
            && (string.IsNullOrEmpty(param.CopyMessage) || (
                (a.OrderSymbol != null && a.OrderSymbol.Contains(param.CopyMessage)) || 
                (a.OrderType != null && a.OrderType.Contains(param.CopyMessage)) ||
                (a.CopyMessage != null && a.CopyMessage.Contains(param.CopyMessage))
            ));
    }

    private static Expression<Func<ActiveOrder, bool>> FilterActiveOrder(Order param)
    {
        return a =>
            (param.AccountId == 0 || a.AccountId == param.AccountId)
            && (!param.MasterOrderId.HasValue || a.MasterOrderId == param.MasterOrderId.Value)
            && (param.IsMasterOnly != true) // Active orders are always slaves
            && (param.OrderTicket == 0 || a.OrderTicket == param.OrderTicket)
            && (string.IsNullOrEmpty(param.OrderSymbol) || a.OrderSymbol == param.OrderSymbol)
            && (string.IsNullOrEmpty(param.OrderType) || a.OrderType == param.OrderType)
            && (param.OrderLot == 0 || a.OrderLot == param.OrderLot)
            && (param.Status == 0 || a.Status == param.Status);
    }

    // Status-Filter-freie Varianten für PATH A (Merge-Logik):
    // Status wird erst NACH dem Merge angewendet, damit active_orders-Überschreibung
    // korrekt berücksichtigt wird (ein Slave mit Status 600 in orders, aber 200 in
    // active_orders soll im "Progress"-Filter korrekt erscheinen).
    private static Expression<Func<Order, bool>> FilterOrderForMerge(Order param)
    {
        return a =>
            (param.Id == 0 || a.Id == param.Id)
            && (param.AccountId == 0 || a.AccountId == param.AccountId)
            && (!param.MasterOrderId.HasValue || a.MasterOrderId == param.MasterOrderId.Value)
            && (param.IsMasterOnly != true || a.MasterOrderId == null)
            && (param.OrderTicket == 0 || a.OrderTicket == param.OrderTicket)
            && (string.IsNullOrEmpty(param.OrderSymbol) || a.OrderSymbol == param.OrderSymbol)
            && (string.IsNullOrEmpty(param.OrderType) || a.OrderType == param.OrderType)
            && (param.OrderLot == 0 || a.OrderLot == param.OrderLot)
            // Status-Filter absichtlich weggelassen – wird nach dem Merge angewendet
            && (param.IsClosed == null || (param.IsClosed == true ? a.OrderCloseAt != null : a.OrderCloseAt == null))
            && (string.IsNullOrEmpty(param.CopyMessage) || (
                (a.OrderSymbol != null && a.OrderSymbol.Contains(param.CopyMessage)) ||
                (a.OrderType != null && a.OrderType.Contains(param.CopyMessage)) ||
                (a.CopyMessage != null && a.CopyMessage.Contains(param.CopyMessage))
            ));
    }

    private static Expression<Func<ActiveOrder, bool>> FilterActiveOrderForMerge(Order param)
    {
        return a =>
            (param.AccountId == 0 || a.AccountId == param.AccountId)
            && (!param.MasterOrderId.HasValue || a.MasterOrderId == param.MasterOrderId.Value)
            && (param.IsMasterOnly != true) // Active orders are always slaves
            && (param.OrderTicket == 0 || a.OrderTicket == param.OrderTicket)
            && (string.IsNullOrEmpty(param.OrderSymbol) || a.OrderSymbol == param.OrderSymbol)
            && (string.IsNullOrEmpty(param.OrderType) || a.OrderType == param.OrderType)
            && (param.OrderLot == 0 || a.OrderLot == param.OrderLot);
            // Status-Filter absichtlich weggelassen – wird nach dem Merge angewendet
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

    public async Task<(List<Order>, long total, ITError?)> GetPaginatedOrders(
        Order param,
        int page,
        int pageSize,
        string? sortBy = null,
        string? sortOrder = null
    )
    {
        try
        {
            List<Order> combinedOrders = new List<Order>();
            long total = 0;

            // If we are NOT explicitly asking for only closed orders, we should include active_orders
            if (param.IsClosed != true)
            {
                // Fetch Master and potential open/closed orders from orders table.
                // Kein Status-Filter hier – wird nach dem Merge mit active_orders angewendet,
                // damit der live-Status aus active_orders korrekt berücksichtigt wird.
                var (ordersFromDb, _) = await _orderRepository.GetPaginated(
                    FilterOrderForMerge(param),
                    1, 2000, // Fetch more for combination
                    q => q.OrderByDescending(o => o.CreatedAt),
                    q => q.Include(o => o.Account)
                );

                // Fetch Slave Open Orders from active_orders table (ebenfalls ohne Status-Filter).
                var activeOrders = await _activeOrderRepository.GetMany(FilterActiveOrderForMerge(param));
                var activeAsOrders = activeOrders.Select(ao => new Order
                {
                    Id = -ao.Id,
                    AccountId = ao.AccountId,
                    MasterOrderId = ao.MasterOrderId,
                    OrderTicket = ao.OrderTicket,
                    OrderSymbol = ao.OrderSymbol,
                    OrderType = ao.OrderType,
                    OrderLot = ao.OrderLot,
                    OrderPrice = ao.OrderPrice,
                    OrderProfit = ao.OrderProfit,
                    Status = ao.Status,
                    CreatedAt = ao.CreatedAt,
                    UpdatedAt = ao.UpdatedAt,
                    OrderOpenAt = ao.OrderOpenAt ?? ao.CreatedAt,
                    OrderLabel = ao.OrderLabel,
                }).ToList();

                // Hydrate Account for active orders
                if (activeAsOrders.Count > 0)
                {
                    var accIds = activeAsOrders.Select(o => o.AccountId).Distinct().ToList();
                    var accounts = await _accountRepository.GetMany(
                        a => accIds.Contains(a.Id)
                    );
                    foreach (var o in activeAsOrders)
                    {
                        o.Account = accounts.FirstOrDefault(a => a.Id == o.AccountId);
                    }
                }

                // Deduplicate: If an order exists in both tables, merge or prefer activeAsOrders for live data
                var uniqueOrdersMap = new Dictionary<string, Order>();

                foreach (var o in ordersFromDb)
                {
                    // If MasterOrderId is null, it's a MASTER order -> needs unique key (Id)
                    // If MasterOrderId is NOT null, it's a SLAVE order -> deduplicate by Account+MasterRef
                    var key = o.MasterOrderId == null ? $"m_{o.Id}" : $"s_{o.AccountId}_{o.MasterOrderId}";
                    uniqueOrdersMap[key] = o;
                }

                foreach (var ao in activeAsOrders)
                {
                    // External orders (MasterOrderId=null) use ticket for uniqueness
                    var key = ao.MasterOrderId == null
                        ? $"ext_{ao.AccountId}_{ao.OrderTicket}"
                        : $"s_{ao.AccountId}_{ao.MasterOrderId}";
                    if (uniqueOrdersMap.TryGetValue(key, out var existing))
                    {
                        // Merge live data into existing DB record
                        existing.OrderPrice = ao.OrderPrice;
                        existing.OrderProfit = ao.OrderProfit;
                        existing.OrderTicket = ao.OrderTicket;
                        existing.Status = ao.Status;
                        existing.OrderOpenAt = ao.OrderOpenAt;
                        if (existing.Account == null) existing.Account = ao.Account;
                    }
                    else
                    {
                        uniqueOrdersMap[key] = ao;
                    }
                }

                combinedOrders = uniqueOrdersMap.Values.ToList();

                // Status-Filter NACH dem Merge anwenden.
                // Dadurch wird der live-Status aus active_orders (der ggf. den orders-Tabellen-Status
                // überschrieben hat) korrekt für die Filterung verwendet.
                // Beispiel: Slave hat Status 600 in orders-Tabelle, aber Status 200 in active_orders
                // → nach Merge ist Status 200 → "Progress"-Filter findet den Slave korrekt.
                if (param.Status != 0)
                {
                    combinedOrders = combinedOrders.Where(o => o.Status == param.Status).ToList();
                }

                // Sorting Combined
                bool isDesc = sortOrder?.ToLower() == "desc";
                var sorted = combinedOrders.AsQueryable();
                string sortField = sortBy?.ToLower().Replace("_", "") ?? "createdat";
                sorted = sortField switch
                {
                    "id" => isDesc ? sorted.OrderByDescending(o => o.Id) : sorted.OrderBy(o => o.Id),
                    "createdat" => isDesc ? sorted.OrderByDescending(o => o.CreatedAt) : sorted.OrderBy(o => o.CreatedAt),
                    "orderopenat" => isDesc ? sorted.OrderByDescending(o => o.OrderOpenAt) : sorted.OrderBy(o => o.OrderOpenAt),
                    "orderprofit" => isDesc ? sorted.OrderByDescending(o => o.OrderProfit) : sorted.OrderBy(o => o.OrderProfit),
                    "orderprice" => isDesc ? sorted.OrderByDescending(o => o.OrderPrice) : sorted.OrderBy(o => o.OrderPrice),
                    "orderlot" => isDesc ? sorted.OrderByDescending(o => o.OrderLot) : sorted.OrderBy(o => o.OrderLot),
                    _ => isDesc ? sorted.OrderByDescending(o => o.CreatedAt) : sorted.OrderBy(o => o.CreatedAt)
                };

                total = combinedOrders.Count;
                var data = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                // Final Hydration Pass: Ensure all orders in result set have Account info
                var missingAccIds = data.Where(o => o.Account == null).Select(o => o.AccountId).Distinct().ToList();
                if (missingAccIds.Count > 0)
                {
                    var missingAccounts = await _accountRepository.GetMany(
                        a => missingAccIds.Contains(a.Id)
                    );
                    foreach (var o in data.Where(o => o.Account == null))
                    {
                        o.Account = missingAccounts.FirstOrDefault(a => a.Id == o.AccountId);
                    }
                }

                // Populate SlaveCounts etc for masters.
                // active_orders werden mit einbezogen (gleiche Merge-Logik wie oben),
                // damit der angezeigte SlaveSuccessCount den tatsächlichen Live-Status widerspiegelt.
                // Also populate when querying individual orders that happen to be masters.
                var masterOrders = data.Where(o => o.MasterOrderId == null).ToList();
                if ((param.IsMasterOnly == true || masterOrders.Any()) && data.Count > 0)
                {
                    var masterIds = (param.IsMasterOnly == true ? data : masterOrders).Select(o => o.Id).ToList();
                    var slaveStatsFromDb = await _orderRepository.GetMany(
                        o => o.MasterOrderId.HasValue && masterIds.Contains(o.MasterOrderId.Value)
                    );
                    var activeSlaveStatsFromDb = await _activeOrderRepository.GetMany(
                        o => o.MasterOrderId.HasValue && masterIds.Contains(o.MasterOrderId.Value)
                    );

                    // Slave-Statusmap aufbauen: orders-Tabelle als Basis, active_orders überschreiben
                    // Key: "{MasterOrderId}_{AccountId}" – ein Slave pro (Master, Account)
                    var globalSlaveMap = new Dictionary<string, (long MasterOrderId, long AccountId, OrderStatus Status, DateTime CreatedAt)>();

                    foreach (var s in slaveStatsFromDb)
                    {
                        if (!s.MasterOrderId.HasValue) continue;
                        var key = $"{s.MasterOrderId}_{s.AccountId}";
                        globalSlaveMap[key] = (s.MasterOrderId.Value, s.AccountId, s.Status, s.CreatedAt);
                    }
                    foreach (var ao in activeSlaveStatsFromDb)
                    {
                        if (!ao.MasterOrderId.HasValue) continue;
                        var key = $"{ao.MasterOrderId}_{ao.AccountId}";
                        if (globalSlaveMap.TryGetValue(key, out var existing))
                            globalSlaveMap[key] = (existing.MasterOrderId, existing.AccountId, ao.Status, existing.CreatedAt);
                        else
                            globalSlaveMap[key] = (ao.MasterOrderId.Value, ao.AccountId, ao.Status, ao.CreatedAt);
                    }

                    var statsMap = masterIds.ToDictionary(
                        masterId => masterId,
                        masterId =>
                        {
                            var masterSlaves = globalSlaveMap.Values.Where(s => s.MasterOrderId == masterId).ToList();
                            var masterOrder = data.FirstOrDefault(d => d.Id == masterId);

                            long avgLag = 0;
                            long maxLag = 0;
                            if (masterOrder != null && masterSlaves.Any())
                            {
                                var lags = masterSlaves
                                    .Select(s => (long)(s.CreatedAt - masterOrder.CreatedAt).TotalMilliseconds)
                                    .ToList();
                                avgLag = (long)lags.Average();
                                maxLag = lags.Max();
                            }

                            return new
                            {
                                Total = masterSlaves.Count,
                                Success = masterSlaves.Count(s => s.Status == OrderStatus.Success || s.Status == OrderStatus.Complete),
                                Failure = masterSlaves.Count(s => s.Status == OrderStatus.Failed),
                                AvgLag = avgLag,
                                MaxLag = maxLag
                            };
                        }
                    );

                    foreach (var order in data)
                    {
                        if (statsMap.TryGetValue(order.Id, out var stats))
                        {
                            order.SlaveCount = stats.Total;
                            order.SlaveSuccessCount = stats.Success;
                            order.SlaveFailureCount = stats.Failure;
                            order.AverageExecutionLag = stats.AvgLag;
                            order.MaxExecutionLag = stats.MaxLag;
                        }
                    }
                }

                // Strip sensitive account data before returning
                SanitizeAccountsInOrders(data);

                return (data, total, null);
            }
            else
            {
                var (data, totalRes) = await _orderRepository.GetPaginated(
                    FilterOrder(param),
                    page,
                    pageSize,
                    q =>
                    {
                        bool isDesc = sortOrder?.ToLower() == "desc";
                        string sortField = sortBy?.ToLower().Replace("_", "") ?? "createdat";
                        return sortField switch
                        {
                            "id" => isDesc ? q.OrderByDescending(o => o.Id) : q.OrderBy(o => o.Id),
                            "masterorderid" => isDesc ? q.OrderByDescending(o => o.MasterOrderId) : q.OrderBy(o => o.MasterOrderId),
                            "ordersymbol" => isDesc ? q.OrderByDescending(o => o.OrderSymbol) : q.OrderBy(o => o.OrderSymbol),
                            "ordertype" => isDesc ? q.OrderByDescending(o => o.OrderType) : q.OrderBy(o => o.OrderType),
                            "orderlot" => isDesc ? q.OrderByDescending(o => o.OrderLot) : q.OrderBy(o => o.OrderLot),
                            "orderprofit" => isDesc ? q.OrderByDescending(o => o.OrderProfit) : q.OrderBy(o => o.OrderProfit),
                            "orderprice" => isDesc ? q.OrderByDescending(o => o.OrderPrice) : q.OrderBy(o => o.OrderPrice),
                            "createdat" => isDesc ? q.OrderByDescending(o => o.CreatedAt) : q.OrderBy(o => o.CreatedAt),
                            _ => q.OrderByDescending(o => o.CreatedAt)
                        };
                    },
                    q => q.Include(o => o.Account)
                );

                if (data == null)
                    return ([], 0, null);

                // Populate SlaveCounts if searching for master orders (PATH B – closed orders).
                // Gleiche Merge-Logik wie PATH A: active_orders überschreiben den DB-Status.
                var masterOrdersB = data.Where(o => o.MasterOrderId == null).ToList();
                if ((param.IsMasterOnly == true || masterOrdersB.Any()) && data.Count > 0)
                {
                    var masterIds = (param.IsMasterOnly == true ? data : masterOrdersB).Select(o => o.Id).ToList();
                    var slaveStatsFromDb = await _orderRepository.GetMany(
                        o => o.MasterOrderId.HasValue && masterIds.Contains(o.MasterOrderId.Value)
                    );
                    var activeSlaveStatsFromDb = await _activeOrderRepository.GetMany(
                        o => o.MasterOrderId.HasValue && masterIds.Contains(o.MasterOrderId.Value)
                    );

                    var globalSlaveMap = new Dictionary<string, (long MasterOrderId, long AccountId, OrderStatus Status, DateTime CreatedAt)>();

                    foreach (var s in slaveStatsFromDb)
                    {
                        if (!s.MasterOrderId.HasValue) continue;
                        var key = $"{s.MasterOrderId}_{s.AccountId}";
                        globalSlaveMap[key] = (s.MasterOrderId.Value, s.AccountId, s.Status, s.CreatedAt);
                    }
                    foreach (var ao in activeSlaveStatsFromDb)
                    {
                        if (!ao.MasterOrderId.HasValue) continue;
                        var key = $"{ao.MasterOrderId}_{ao.AccountId}";
                        if (globalSlaveMap.TryGetValue(key, out var existing))
                            globalSlaveMap[key] = (existing.MasterOrderId, existing.AccountId, ao.Status, existing.CreatedAt);
                        else
                            globalSlaveMap[key] = (ao.MasterOrderId.Value, ao.AccountId, ao.Status, ao.CreatedAt);
                    }

                    var statsMap = masterIds.ToDictionary(
                        masterId => masterId,
                        masterId =>
                        {
                            var masterSlaves = globalSlaveMap.Values.Where(s => s.MasterOrderId == masterId).ToList();
                            var masterOrder = data.FirstOrDefault(d => d.Id == masterId);

                            long avgLag = 0;
                            long maxLag = 0;
                            if (masterOrder != null && masterSlaves.Any())
                            {
                                var lags = masterSlaves
                                    .Select(s => (long)(s.CreatedAt - masterOrder.CreatedAt).TotalMilliseconds)
                                    .ToList();
                                avgLag = (long)lags.Average();
                                maxLag = lags.Max();
                            }

                            return new
                            {
                                Total = masterSlaves.Count,
                                Success = masterSlaves.Count(s => s.Status == OrderStatus.Success || s.Status == OrderStatus.Complete),
                                Failure = masterSlaves.Count(s => s.Status == OrderStatus.Failed),
                                AvgLag = avgLag,
                                MaxLag = maxLag
                            };
                        }
                    );

                    foreach (var order in data)
                    {
                        if (statsMap.TryGetValue(order.Id, out var stats))
                        {
                            order.SlaveCount = stats.Total;
                            order.SlaveSuccessCount = stats.Success;
                            order.SlaveFailureCount = stats.Failure;
                            order.AverageExecutionLag = stats.AvgLag;
                            order.MaxExecutionLag = stats.MaxLag;
                        }
                    }
                }

                // Final Hydration Pass: Ensure all orders in result set have Account info
                var missingAccIds = data.Where(o => o.Account == null).Select(o => o.AccountId).Distinct().ToList();
                if (missingAccIds.Count > 0)
                {
                    var missingAccounts = await _accountRepository.GetMany(
                        a => missingAccIds.Contains(a.Id)
                    );
                    foreach (var o in data.Where(o => o.Account == null))
                    {
                        o.Account = missingAccounts.FirstOrDefault(a => a.Id == o.AccountId);
                    }
                }

                // Strip sensitive account data before returning
                SanitizeAccountsInOrders(data);

                return (data, totalRes, null);
            }
        }
        catch (Exception ex)
        {
            return ([], 0, TError.NewServer("database error", ex.Message));
        }
    }

    /// <summary>
    /// Removes sensitive fields (password, tokens) from Account objects in order list.
    /// Only keeps: Id, PlatformName, AccountNumber, UserId, Equity, Balance, Status, Role.
    /// </summary>
    private static void SanitizeAccountsInOrders(List<Order> orders)
    {
        foreach (var order in orders)
        {
            if (order.Account == null) continue;
            order.Account.AccountPassword = "";
            order.Account.AccessToken = null;
            order.Account.RefreshToken = null;
            order.Account.TokenExpiredAt = null;
            order.Account.CtidTraderAccountId = null;
        }
    }

    public async Task<(Order?, ITError?)> AddOrder(Order order)
    {
        var existingOrder = new Order
        {
            AccountId = order.AccountId,
            OrderTicket = order.OrderTicket,
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
            return (
                null,
                TError.NewClient("order with the server name and order number already exist")
            );
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
            Console.WriteLine(id);
            var (_, terr) = await GetOrder(new Order { Id = id });
            if (terr != null)
                return (null, terr);

            var data = await _orderRepository.Save(param, a => a.Id == id);
            if (data == null)
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
            if (data == null)
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
            var (account, accErr) = await GetAccount(
                new Account { ServerName = payload.ServerName, AccountNumber = payload.AccountId }
            );
            if (accErr != null)
            {
                await _systemLogUsecase.CreateLog("CopyTrade", "SlaveConfirm", null,
                    $"FAILED: account not found for server={payload.ServerName} account={payload.AccountId}", "Error");
                return accErr;
            }

            if (account == null)
            {
                await _systemLogUsecase.CreateLog("CopyTrade", "SlaveConfirm", null,
                    $"FAILED: account is null for server={payload.ServerName} account={payload.AccountId}", "Error");
                return TError.NewNotFound("account not found");
            }

            Order? existingOrder = null;
            ITError? terr = null;
            if (
                payload.Order.OrderType == "DEAL_TYPE_BUY"
                || payload.Order.OrderType == "DEAL_TYPE_SELL"
            )
            {
                (existingOrder, terr) = await GetOrder(
                    new Order
                    {
                        MasterOrderId = payload.Order.MasterOrderId,
                        AccountId = account.Id,
                    }
                );
                if (terr != null)
                {
                    await _systemLogUsecase.CreateLog("CopyTrade", "SlaveConfirm", account.Id,
                        $"FAILED: order not found for masterOrderId={payload.Order.MasterOrderId} type={payload.Order.OrderType}", "Error");
                    return terr;
                }

                if (existingOrder == null)
                {
                    await _systemLogUsecase.CreateLog("CopyTrade", "SlaveConfirm", account.Id,
                        $"FAILED: order is null for masterOrderId={payload.Order.MasterOrderId} type={payload.Order.OrderType}", "Error");
                    return TError.NewNotFound("order not found");
                }

                existingOrder.OrderOpenAt = payload.Order.OrderOpenAt ?? DateTime.UtcNow;
                existingOrder.OrderTicket = payload.Order.OrderTicket;
                existingOrder.OrderPrice = payload.Order.OrderPrice;
                existingOrder.OrderLot = payload.Order.OrderLot;
                if (!string.IsNullOrEmpty(payload.Order.OrderLabel))
                    existingOrder.OrderLabel = payload.Order.OrderLabel;

                // Set status based on what the copier reported
                if (payload.Order.OrderStatus == "FAILED")
                {
                    existingOrder.Status = OrderStatus.Failed;
                    existingOrder.CopyMessage = !string.IsNullOrEmpty(payload.Order.CopyMessage)
                        ? payload.Order.CopyMessage
                        : "Execution failed on slave platform";
                }
                else
                {
                    existingOrder.Status = OrderStatus.Success;
                    existingOrder.CopyMessage = null;
                }

                // ALSO update the corresponding ActiveOrder
                var activeOrder = await _activeOrderRepository.Get(
                    ao => ao.MasterOrderId == payload.Order.MasterOrderId && ao.AccountId == account.Id
                );
                if (activeOrder != null)
                {
                    activeOrder.OrderTicket = payload.Order.OrderTicket;
                    activeOrder.OrderPrice = payload.Order.OrderPrice;
                    activeOrder.OrderLot = payload.Order.OrderLot;
                    activeOrder.Status = OrderStatus.Success;
                    await _activeOrderRepository.Update(activeOrder);
                }

                await _systemLogUsecase.CreateLog("CopyTrade", "SlaveConfirm", account.Id,
                    $"Slave OPEN confirmed: ticket={payload.Order.OrderTicket} symbol={payload.Order.OrderSymbol} type={payload.Order.OrderType} lot={payload.Order.OrderLot} price={payload.Order.OrderPrice} masterOrderId={payload.Order.MasterOrderId}");
            }
            else if (payload.Order.OrderType == "DEAL_TYPE_DELETE")
            {
                // Find the slave order — try MasterOrderId first, then OrderTicket
                if (payload.Order.MasterOrderId != 0)
                {
                    (existingOrder, terr) = await GetOrder(
                        new Order
                        {
                            MasterOrderId = payload.Order.MasterOrderId,
                            AccountId = account.Id,
                        }
                    );
                }

                // Fallback: find by OrderTicket if MasterOrderId=0 or not found
                if (existingOrder == null && payload.Order.OrderTicket != 0)
                {
                    (existingOrder, terr) = await GetOrder(
                        new Order
                        {
                            OrderTicket = payload.Order.OrderTicket,
                            AccountId = account.Id,
                        }
                    );
                }

                if (existingOrder != null)
                {
                    existingOrder.OrderCloseAt = DateTime.UtcNow;
                    existingOrder.ClosePrice = payload.Order.OrderClosePrice;
                    existingOrder.Status = OrderStatus.Complete;
                    existingOrder.OrderProfit = payload.Order.OrderProfit;
                }

                // ALWAYS clean up ActiveOrders regardless of whether we found the order
                // Match by MasterOrderId OR by OrderTicket to catch all cases
                var closedActiveOrders = await _activeOrderRepository.GetMany(
                    ao => ao.AccountId == account.Id && (
                        (payload.Order.MasterOrderId != 0 && ao.MasterOrderId == payload.Order.MasterOrderId) ||
                        (payload.Order.OrderTicket != 0 && ao.OrderTicket == payload.Order.OrderTicket)
                    )
                );
                foreach (var closedAo in closedActiveOrders)
                {
                    await _activeOrderRepository.Delete(ao => ao.Id == closedAo.Id);
                    _logger.Info($"ActiveOrder removed on close confirm: id={closedAo.Id} ticket={closedAo.OrderTicket} masterOrderId={closedAo.MasterOrderId}");
                }

                if (existingOrder == null)
                {
                    // No order found but ActiveOrders cleaned up — not an error
                    await _systemLogUsecase.CreateLog("CopyTrade", "SlaveConfirm", account.Id,
                        $"Close confirm: no order found for masterOrderId={payload.Order.MasterOrderId} ticket={payload.Order.OrderTicket} but ActiveOrders cleaned up");
                    return null;
                }

                await _systemLogUsecase.CreateLog("CopyTrade", "SlaveConfirm", account.Id,
                    $"Slave CLOSE confirmed: orderId={existingOrder.Id} ticket={existingOrder.OrderTicket} symbol={existingOrder.OrderSymbol} closePrice={payload.Order.OrderClosePrice} profit={payload.Order.OrderProfit} masterOrderId={payload.Order.MasterOrderId}");
            }

            if (existingOrder == null)
                return TError.NewNotFound("order not found");

            var (_, terrs) = await UpdateOrderById(existingOrder.Id, existingOrder);
            if (terrs != null)
            {
                await _systemLogUsecase.CreateLog("CopyTrade", "SlaveConfirm", account.Id,
                    $"FAILED: could not update order id={existingOrder.Id} error={terrs.Message}", "Error");
                return terrs;
            }

            return null;
        }
        catch (Exception ex)
        {
            await _systemLogUsecase.CreateLog("CopyTrade", "Error", null,
                $"ConfirmBridgeSlaveOrder EXCEPTION: account={payload.AccountId} error={ex.Message}", "Error");
            return TError.NewServer(ex.Message);
        }
    }

    public async Task<(string, ITError?)> CreateBridgeMasterOrder(
        BridgeListCreateOrderPayload payload
    )
    {
        await using var tx = await _orderRepository.BeginTransactionAsync();
        try
        {
            var (account, accErr) = await GetAccount(
                new Account { ServerName = payload.ServerName, AccountNumber = payload.AccountId }
            );
            if (accErr != null)
                return ("", accErr);

            var existingOrders = await _orderRepository.GetMany(a =>
                a.OrderCloseAt == null && a.AccountId == account!.Id
            );

            var payloadOrderTickets = payload.Orders.Select(o => o.OrderTicket).ToHashSet();

            List<Order> deletedOrders =
            [
                .. existingOrders.Where(dbOrder =>
                    !payloadOrderTickets.Contains(dbOrder.OrderTicket)
                ),
            ];

            var existingOrderTickets = existingOrders.Select(o => o.OrderTicket).ToHashSet();
            var toleranceSeconds = int.Parse(
                Environment.GetEnvironmentVariable("COPY_TOLERANCE_SECOND") ?? "0"
            );

            var newOrders = payload
                .Orders.Where(po => !existingOrderTickets.Contains(po.OrderTicket))
                .Where(po => toleranceSeconds <= 0 || OrderTimeHelper.IsOrderFresh(po.OrderOpenAt, toleranceSeconds))
                .ToList();

            foreach (var item in newOrders)
            {
                var order = new Order
                {
                    AccountId = account!.Id,
                    MasterOrderId = null,
                    CopyMessage = null,
                    OrderTicket = item.OrderTicket,
                    OrderSymbol = item.OrderSymbol,
                    OrderType = item.OrderType,
                    OrderLot = item.OrderLot,
                    OrderPrice = item.OrderPrice,
                    Status = OrderStatus.Success,
                    OrderOpenAt = item.OrderOpenAt,
                };

                var (newOdr, terr) = await CreateOrder(order);
                if (terr != null)
                {
                    await _systemLogUsecase.CreateLog("CopyTrade", "MasterOpen", account!.Id,
                        $"FAILED to save master order: ticket={item.OrderTicket} symbol={item.OrderSymbol} type={item.OrderType} lot={item.OrderLot} error={terr.Message}", "Error");
                    return ("", terr);
                }

                await _systemLogUsecase.CreateLog("CopyTrade", "MasterOpen", account!.Id,
                    $"Master position opened: ticket={item.OrderTicket} symbol={item.OrderSymbol} type={item.OrderType} lot={item.OrderLot} price={item.OrderPrice}");
            }

            var closeOrderAt = DateTime.UtcNow;
            if (deletedOrders.Any())
            {
                var deletedIds = deletedOrders.Select(o => o.Id).ToList();
                var deletedTickets = deletedOrders.Select(o => o.OrderTicket).ToList();

                var latestLogs = await _orderLogRepository.GetMany(log => log.AccountId == account!.Id && deletedTickets.Contains(log.OrderTicket));
                var logMap = latestLogs.GroupBy(log => log.OrderTicket).ToDictionary(g => g.Key, g => g.OrderByDescending(log => log.CreatedAt).FirstOrDefault());

                // Pre-fetch slave close prices: for each master order being closed,
                // find any slave order that already has a close_price (from copier/bridge)
                var slaveClosePrices = new Dictionary<long, decimal>();
                foreach (var del in deletedOrders)
                {
                    var slaveWithPrice = await _orderRepository.Get(o =>
                        o.MasterOrderId == del.Id &&
                        o.ClosePrice != null &&
                        o.ClosePrice > 0
                    );
                    if (slaveWithPrice?.ClosePrice != null)
                        slaveClosePrices[del.Id] = slaveWithPrice.ClosePrice.Value;
                }

                await _orderRepository.UpdateMany(
                    o => deletedIds.Contains(o.Id),
                    item =>
                    {
                        item.OrderCloseAt = closeOrderAt;
                        item.Status = OrderStatus.Complete;

                        // Capture PnL from the last known OrderLog
                        if (logMap.TryGetValue(item.OrderTicket, out var log) && log != null)
                        {
                            item.OrderProfit = log.OrderProfit;
                            item.ClosePrice = log.LastPrice ?? log.OrderPrice;
                        }

                        // Fallback: if close_price is still missing (e.g. cTrader accounts
                        // where OrderLog.LastPrice is never set), use the close_price from
                        // an already-closed slave order
                        if ((item.ClosePrice == null || item.ClosePrice == 0) &&
                            slaveClosePrices.TryGetValue(item.Id, out var slaveClosePrice))
                        {
                            item.ClosePrice = slaveClosePrice;
                        }
                    }
                );

                foreach (var del in deletedOrders)
                {
                    await _systemLogUsecase.CreateLog("CopyTrade", "MasterClose", account!.Id,
                        $"Master position closed: ticket={del.OrderTicket} symbol={del.OrderSymbol} type={del.OrderType} lot={del.OrderLot}");
                }
            }

            string message = "";
            if (newOrders.Count <= 0)
            {
                message = String.Concat(message, " ", "no new orders was made");
            }
            if (deletedOrders.Count <= 0)
            {
                message = String.Concat(message, " ", "no orders was deleted");
            }

            // Update master account balance/equity from bridge payload
            if (payload.Balance.HasValue && payload.Balance > 0)
            {
                account!.Balance = payload.Balance.Value;
            }
            if (payload.Equity.HasValue && payload.Equity > 0)
            {
                account!.Equity = payload.Equity.Value;
            }
            await _accountRepository.Save(account!, a => a.Id == account!.Id);

            await _orderRepository.CommitAsync();

            // 🔴 NEW: Copy each new master order to slaves
            foreach (var item in newOrders)
            {
                // Find the DB order we just created to get its ID
                var dbOrder = await _orderRepository.Get(o => o.AccountId == account!.Id && o.OrderTicket == item.OrderTicket && o.OrderCloseAt == null);
                if (dbOrder != null)
                {
                    await CopyMasterOrderToSlaves(dbOrder, account!, payload.Balance ?? 0);
                }
            }

            await SyncSlaveActiveOrders(account!.Id);

            return (message, null);
        }
        catch (Exception ex)
        {
            await _orderRepository.RollbackAsync();
            await _systemLogUsecase.CreateLog("CopyTrade", "Error", null,
                $"CreateBridgeMasterOrder failed: account={payload.AccountId} server={payload.ServerName} error={ex.Message}", "Error");
            return ("", TError.NewServer(ex.Message));
        }
    }

    public async Task<ITError?> CopyBridgeMasterOrder(Account masterAccount)
    {
        try
        {
            // -------------------------------
            // 1. GET ALL MASTER-SLAVE RELATIONS FIRST (NO DUPLICATE QUERIES)
            // -------------------------------

            var (masterSlaves, terr) = await GetMasterSlaves(
                new MasterSlave { MasterId = masterAccount.Id }
            );
            if (terr != null)
            {
                return terr;
            }

            if (masterSlaves.Count <= 0)
            {
                return null;
            }

            // -------------------------------
            // 2. GET ALL NEW MASTER ORDERS IN ONE QUERY
            // -------------------------------

            var toleranceSeconds = int.Parse(
                Environment.GetEnvironmentVariable("COPY_TOLERANCE_SECOND") ?? "0"
            );
            var threshold = DateTime.UtcNow.AddSeconds(-toleranceSeconds);
            var now = DateTime.UtcNow;

            var newOrders = await _orderRepository.GetMany(a =>
                a.OrderOpenAt >= threshold
                && a.OrderOpenAt <= now
                && a.OrderCloseAt == null
                && a.OrderCopiedAt == null
                && a.AccountId == masterAccount.Id
            );

            // -------------------------------
            // PRELOAD ALL MASTER SLAVE PAIRS & CONFIG IN ONE GO
            // so we don’t repeat SQL queries for each masterSlave
            // -------------------------------

            var allMasterSlaveIds = masterSlaves.Select(x => x.Id).ToList();

            // load ALL pairs for ALL slaves
            var allPairs = await _masterSlavePairRepository.GetMany(x =>
                allMasterSlaveIds.Contains(x.MasterSlaveId)
            );

            // load ALL configs
            var allConfigs = await _masterSlaveConfigRepository.GetMany(x =>
                allMasterSlaveIds.Contains(x.MasterSlaveId)
            );

            // group pairs into a dictionary (used as OVERRIDE)
            var allPairsMap = allPairs
                .GroupBy(x => x.MasterSlaveId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.MasterPair, x => x.SlavePair));

            // config lookup dictionary
            var configMap = allConfigs.ToDictionary(x => x.MasterSlaveId, x => x);

            // -----------------------------------------------
            // PRELOAD SYMBOL MAP for canonical resolution
            // -----------------------------------------------
            var allSymbolMaps = await _symbolMapRepository.GetMany(x => x.DeletedAt == null);

            // broker_symbol+broker_name → canonical
            var toCanonical = allSymbolMaps
                .GroupBy(x => (x.BrokerSymbol.ToUpper(), x.BrokerName.ToUpper()))
                .ToDictionary(g => g.Key, g => g.First().CanonicalSymbol.ToUpper());

            // canonical+broker_name → broker_symbol
            var fromCanonical = allSymbolMaps
                .GroupBy(x => (x.CanonicalSymbol.ToUpper(), x.BrokerName.ToUpper()))
                .ToDictionary(g => g.Key, g => g.First().BrokerSymbol);

            var closedThreshold = DateTime.UtcNow.AddDays(-30);

            List<Order> newSlaveOrders = [];
            List<Order> updatedSlaveOrders = [];

            // -------------------------------------------
            // LOOP EACH MASTER-SLAVE
            // -------------------------------------------
            foreach (var item in masterSlaves)
            {
                if (item == null)
                    continue;

                // 1. Check MasterSlavePair override first
                allPairsMap.TryGetValue(item.Id, out var masterSlavePair);

                decimal multiplier = 1;
                if (configMap.TryGetValue(item.Id, out var cfg))
                {
                    multiplier = cfg.Multiplier == 0 ? multiplier : cfg.Multiplier;
                }

                List<BridgeOrderBroadcastPayload> messages = [];

                // -------------------------------------------
                // APPLY NEW MASTER ORDERS → SLAVE
                // -------------------------------------------
                foreach (var order in newOrders)
                {
                    // PRIORITY 1: MasterSlavePair override
                    string? slavePair = null;
                    if (masterSlavePair != null && masterSlavePair.TryGetValue(order.OrderSymbol, out var overridePair))
                    {
                        slavePair = overridePair;

                        // FIX CASING:
                        var exactOverride = allSymbolMaps.FirstOrDefault(x => 
                            x.BrokerName.ToUpper() == (item.SlaveAccount?.BrokerName ?? "").ToUpper() && 
                            x.BrokerSymbol.ToUpper() == slavePair.ToUpper());
                        if (exactOverride != null)
                        {
                            slavePair = exactOverride.BrokerSymbol;
                        }
                    }
                    else
                    {
                        // PRIORITY 2: Exact Canonical resolution
                        var masterBroker = masterAccount.BrokerName?.ToUpper() ?? "";
                        var slaveBroker = item.SlaveAccount?.BrokerName?.ToUpper() ?? "";
                        var masterSymbolKey = (order.OrderSymbol.ToUpper(), masterBroker);

                        if (toCanonical.TryGetValue(masterSymbolKey, out var canonical))
                        {
                            var slaveKey = (canonical, slaveBroker);
                            if (fromCanonical.TryGetValue(slaveKey, out var resolved))
                                slavePair = resolved;
                        }

                        // PRIORITY 3: Fuzzy Canonical resolution (Cleaned Master Symbol)
                        if (string.IsNullOrEmpty(slavePair))
                        {
                            var cleanedMaster = CleanSymbol(order.OrderSymbol);
                            var masterFuzzyKey = (cleanedMaster, masterBroker);
                            if (toCanonical.TryGetValue(masterFuzzyKey, out var fuzzyCanonical))
                            {
                                var slaveKey = (fuzzyCanonical, slaveBroker);
                                if (fromCanonical.TryGetValue(slaveKey, out var resolved))
                                    slavePair = resolved;
                            }
                        }

                        // PRIORITY 4: Pattern-based fallback (Learn from slave broker mappings)
                        if (string.IsNullOrEmpty(slavePair))
                        {
                            var cleanedMaster = CleanSymbol(order.OrderSymbol);
                            var likelySlaveSymbol = allSymbolMaps
                                .Where(x => x.BrokerName.ToUpper() == slaveBroker &&
                                           (x.BrokerSymbol.ToUpper() == cleanedMaster || x.BrokerSymbol.ToUpper().StartsWith(cleanedMaster)))
                                .OrderBy(x => x.BrokerSymbol.Length)
                                .Select(x => x.BrokerSymbol)
                                .FirstOrDefault();

                            if (!string.IsNullOrEmpty(likelySlaveSymbol))
                                slavePair = likelySlaveSymbol;
                        }

                        // PRIORITY 5: Fallback — use master symbol as-is
                        if (string.IsNullOrEmpty(slavePair))
                            slavePair = order.OrderSymbol;
                    }

                    if (string.IsNullOrEmpty(slavePair))
                        continue;

                    var newOrderMsg = new BridgeOrderBroadcastPayload
                    {
                        SlavePair = slavePair,
                        OrderType = order.OrderType,
                        OrderLot = order.OrderLot * multiplier,
                        OrderTicket = order.OrderTicket,
                        MasterOrderId = order.Id,
                        CopyType = "MASTER_ORDER_UPDATE",
                        CreatedAt = DateTime.UtcNow,
                    };

                    messages.Add(newOrderMsg);

                    if (item.SlaveAccount == null)
                        continue;

                    var slaveOrder = new Order
                    {
                        AccountId = item.SlaveAccount.Id,
                        MasterOrderId = order.Id,
                        OrderTicket = 0,
                        OrderSymbol = slavePair,
                        OrderType = order.OrderType,
                        OrderLot = Math.Round(order.OrderLot * multiplier, 2),
                        OrderPrice = 0,
                        Status = OrderStatus.Progress,
                        OrderOpenAt = DateTime.UtcNow,
                    };
                    newSlaveOrders.Add(slaveOrder);
                }

                // -------------------------------------------
                // CLOSED ORDERS BROADCAST
                // -------------------------------------------

                var closedOrders = await _orderRepository.GetMany(a =>
                    a.MasterOrder != null
                    && a.MasterOrder.OrderCloseAt != null
                    && a.MasterOrder.OrderCloseAt > closedThreshold
                    && a.OrderTicket != 0
                    && (a.Status == OrderStatus.Success || a.Status == OrderStatus.Progress)
                    && a.AccountId == item.SlaveId
                );

                closedOrders ??= [];

                foreach (var closeOrder in closedOrders)
                {
                    if (closeOrder == null)
                        continue;
                    if (closeOrder.ClosePrice != null)
                        continue;

                    var msg = new BridgeOrderBroadcastPayload
                    {
                        SlavePair = closeOrder.OrderSymbol,
                        OrderType = closeOrder.OrderType,
                        OrderLot = closeOrder.OrderLot,
                        OrderTicket = closeOrder.OrderTicket,
                        MasterOrderId = closeOrder.Id,
                        CopyType = "MASTER_ORDER_DELETE",
                        CreatedAt = DateTime.UtcNow,
                    };
                    messages.Add(msg);

                    if (item.SlaveAccount == null)
                        continue;

                    closeOrder.OrderCloseAt = DateTime.UtcNow;
                    updatedSlaveOrders.Add(closeOrder);
                }

                if (messages.Count > 0 && item.SlaveAccount?.AccountNumber != null)
                {
                    _logger.Info("publish-mt5-batch", messages);

                    if (item.SlaveAccount.PlatformName == "cTrader")
                    {
                        await _jobPublisher.PublishCtraderPacketBatch(
                            item.SlaveAccount.AccountNumber,
                            messages
                        );
                    }
                    else
                    {
                        await _jobPublisher.PublishMt5PacketBatch(
                            item.SlaveAccount.ServerName,
                            item.SlaveAccount.AccountNumber,
                            messages
                        );
                    }
                }
            }

            // -------------------------------------------
            // SAVE NEW + UPDATED SLAVE ORDERS
            // -------------------------------------------
            foreach (var item in newSlaveOrders)
            {
                var (_, terra) = await CreateOrder(item);
                if (terra != null)
                    return terra;
            }

            foreach (var item in updatedSlaveOrders)
            {
                var (_, terra) = await UpdateOrderById(item.Id, item);
                if (terra != null)
                    return terra;
            }

            // mark all master orders as copied
            foreach (var order in newOrders)
            {
                order.OrderCopiedAt = DateTime.UtcNow;

                await _orderRepository.Update(
                    o => o.Id == order.Id,
                    o =>
                    {
                        o.OrderCopiedAt = DateTime.UtcNow;
                    }
                );
            }

            return null;
        }
        catch (Exception ex)
        {
            return TError.NewServer(ex.Message);
        }
    }

    public async Task<ITError?> SyncSlaveActiveOrders(long masterAccountId)
    {
        try
        {
            // 1. ambil semua order master yang masih aktif
            var masterOrders = await _orderRepository.GetMany(o =>
                o.AccountId == masterAccountId
                && o.OrderCloseAt == null
                && o.Status == OrderStatus.Success
            );

            // map cepat
            var masterOrderIds = masterOrders.Select(o => o.Id).ToHashSet();

            // 2. ambil semua slave yang terhubung ke master
            var slaveRelations = await _masterSlaveRepository.GetMany(ms =>
                ms.MasterId == masterAccountId
            );

            if (slaveRelations.Count == 0)
                return null; // no slave, nothing to do

            // mapping slave relation
            var slaveIds = slaveRelations.Select(x => x.SlaveId).Distinct().ToList();

            // Load master account for balance ratio
            var masterAccount = await _accountRepository.Get(a => a.Id == masterAccountId);
            decimal masterBalance = masterAccount?.Balance ?? 0;
            if (masterBalance <= 0)
            {
                await _systemLogUsecase.CreateLog("CopyTrade", "SyncActive", masterAccountId,
                    $"SyncSlaveActiveOrders skipped: masterBalance={masterBalance} (must be > 0). ActiveOrders will NOT be created/checked.", "Warning");
                return null;
            }

            var slaves = await _accountRepository.GetMany(a => slaveIds.Contains(a.Id));

            var slaveMap = slaves.ToDictionary(x => x.Id);

            var masterSlaveIds = slaveRelations.Select(x => x.Id).ToList();

            // load ALL pairs
            var allPairs = await _masterSlavePairRepository.GetMany(x =>
                masterSlaveIds.Contains(x.MasterSlaveId)
            );

            // load ALL configs
            var allConfigs = await _masterSlaveConfigRepository.GetMany(x =>
                masterSlaveIds.Contains(x.MasterSlaveId)
            );

            // mapping pair: MasterSlaveId -> (masterPair -> slavePair)
            var pairMap = allPairs
                .GroupBy(x => x.MasterSlaveId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.MasterPair, x => x.SlavePair));

            // mapping config
            var configMap = allConfigs.ToDictionary(x => x.MasterSlaveId);

            // -----------------------------------------------
            // PRELOAD SYMBOL MAP for canonical resolution
            // -----------------------------------------------
            var allSymbolMaps = await _symbolMapRepository.GetMany(x => x.DeletedAt == null);

            var toleranceSeconds = int.Parse(
                Environment.GetEnvironmentVariable("COPY_TOLERANCE_SECOND") ?? "0"
            );

            // preload ALL active orders for all slave accounts
            var allSlaveActiveOrders = await _activeOrderRepository.GetMany(a =>
                slaveIds.Contains(a.AccountId)
            );

            // group by AccountId
            var activeOrdersByAccountId = allSlaveActiveOrders
                .GroupBy(a => a.AccountId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // iterasi slave relation
            foreach (var relation in slaveRelations)
            {
                var slaveAccountId = relation.SlaveId;

                if (!slaveMap.TryGetValue(relation.SlaveId, out var slaveAccount))
                {
                    _logger.Warning($"SlaveAccount not found for SlaveId={relation.SlaveId}");
                    continue; // jangan bikin ActiveOrder tanpa account
                }

                // Skip slaves with zero balance — no point creating intents with lot=0
                if (slaveAccount.Balance <= 0)
                    continue;

                // 3. ambil active order slave existing
                activeOrdersByAccountId.TryGetValue(slaveAccountId, out var slaveActiveOrders);

                slaveActiveOrders ??= new List<ActiveOrder>();

                var slaveActiveByMasterId = slaveActiveOrders
                    .Where(a => a.MasterOrderId.HasValue)
                    .GroupBy(a => a.MasterOrderId!.Value)
                    .ToDictionary(g => g.Key, g => g.First());

                // 4. CREATE or UPDATE missing active orders
                foreach (var masterOrder in masterOrders)
                {
                    // Calculate expected lot first
                    decimal multiplier = 1;
                    if (configMap.TryGetValue(relation.Id, out var cfg))
                    {
                        multiplier = cfg.Multiplier == 0 ? 1 : cfg.Multiplier;
                    }
                    decimal riskRatio = masterOrder.OrderLot / masterBalance;
                    decimal finalLot = Math.Round(riskRatio * slaveAccount.Balance * multiplier, 2);

                    if (slaveActiveByMasterId.TryGetValue(masterOrder.Id, out var existingActive))
                    {
                        // Update lot if it's still in Progress and incorrect
                        if (existingActive.Status == OrderStatus.Progress && existingActive.OrderLot != finalLot)
                        {
                            _logger.Info($"Updating queued trade {masterOrder.Id} lot from {existingActive.OrderLot} to {finalLot}");
                            existingActive.OrderLot = finalLot;
                            await _activeOrderRepository.Update(existingActive);
                        }
                        continue;
                    }

                    if (
                        DateTime.UtcNow - masterOrder.OrderOpenAt
                        > TimeSpan.FromSeconds(toleranceSeconds)
                    )
                    {
                        _logger.Info($"Skip stale master order {masterOrder.Id}");
                        await _systemLogUsecase.CreateLog("CopyTrade", "SyncActive", slaveAccount.Id,
                            $"Skipped stale master order: masterOrderId={masterOrder.Id} ticket={masterOrder.OrderTicket} age={(DateTime.UtcNow - masterOrder.OrderOpenAt)?.TotalSeconds:F0}s > tolerance={toleranceSeconds}s", "Warning");
                        continue;
                    }

                    // ambil pair mapping untuk relation ini
                    pairMap.TryGetValue(relation.Id, out var relationPairs);

                    // Symbol resolution with 5-tier priority using Helper Function
                    string? slavePair = ResolveSymbol(
                        masterOrder.OrderSymbol,
                        masterAccount?.BrokerName ?? "",
                        slaveAccount.BrokerName ?? "",
                        relationPairs,
                        allSymbolMaps
                    );

                    if (string.IsNullOrEmpty(slavePair))
                        continue;

                    // CREATE ActiveOrder (intent)
                    var activeOrder = new ActiveOrder
                    {
                        AccountId = slaveAccountId,
                        MasterOrderId = masterOrder.Id,

                        AccountNumber = slaveAccount.AccountNumber,
                        ServerName = slaveAccount.ServerName,

                        OrderTicket = 0,
                        OrderMagic = GenerateBridgeMagicNumber(masterOrder.Id, slaveAccountId),

                        OrderType = masterOrder.OrderType,
                        OrderLot = finalLot,
                        OrderProfit = 0,

                        OrderSymbol = slavePair,
                        Status = OrderStatus.Progress,
                        OrderOpenAt = masterOrder.OrderOpenAt,
                    };

                    await _activeOrderRepository.Add(activeOrder);

                    await _systemLogUsecase.CreateLog("CopyTrade", "ActiveOrder", slaveAccount.Id,
                        $"ActiveOrder created: masterOrderId={masterOrder.Id} ticket={masterOrder.OrderTicket} symbol={slavePair} type={masterOrder.OrderType} lot={finalLot}");
                }

                // 5. BATCH PROCESSING: Close orphan active orders (master already closed)
                var orphanActiveOrders = slaveActiveOrders
                    .Where(a => a.MasterOrderId.HasValue && !masterOrderIds.Contains(a.MasterOrderId.Value))
                    .ToList();

                if (orphanActiveOrders.Any())
                {
                    // Split orphans: executed (has ticket) vs non-executed (ticket=0, never opened on platform)
                    var executedOrphans = orphanActiveOrders.Where(o => o.OrderTicket != 0).ToList();
                    var nonExecutedOrphans = orphanActiveOrders.Where(o => o.OrderTicket == 0).ToList();

                    // --- Handle NON-EXECUTED orphans: mark as Failed, NO close signal needed ---
                    foreach (var orphan in nonExecutedOrphans)
                    {
                        await FinalizeActiveOrderToOrder(orphan, asFailed: true);

                        await _systemLogUsecase.CreateLog("CopyTrade", "SlaveClose", slaveAccount.Id,
                            $"Non-executed intent marked as Failed: masterOrderId={orphan.MasterOrderId} symbol={orphan.OrderSymbol} type={orphan.OrderType} lot={orphan.OrderLot} platform={slaveAccount.PlatformName}");
                    }

                    // Also mark corresponding intent orders in orders table as Failed
                    // (these were created by CopyMasterOrderToSlaves with ticket=0)
                    var nonExecutedMasterOrderIds = nonExecutedOrphans
                        .Where(o => o.MasterOrderId.HasValue)
                        .Select(o => o.MasterOrderId!.Value)
                        .ToList();
                    if (nonExecutedMasterOrderIds.Any())
                    {
                        var intentOrders = await _orderRepository.GetMany(o =>
                            o.AccountId == slaveAccountId
                            && o.OrderTicket == 0
                            && nonExecutedMasterOrderIds.Contains(o.MasterOrderId ?? 0)
                            && (o.Status == OrderStatus.Progress || o.Status == OrderStatus.Success)
                        );
                        foreach (var intentOrder in intentOrders)
                        {
                            await _orderRepository.Update(
                                o => o.Id == intentOrder.Id,
                                o =>
                                {
                                    o.Status = OrderStatus.Failed;
                                    o.OrderCloseAt = DateTime.UtcNow;
                                    o.CopyMessage = "Never executed on slave platform";
                                }
                            );
                        }
                    }

                    // --- Handle EXECUTED orphans: send close signal ---
                    List<BridgeOrderBroadcastPayload> closeMessages = [];
                    foreach (var orphan in executedOrphans)
                    {
                        await FinalizeActiveOrderToOrder(orphan);

                        closeMessages.Add(new BridgeOrderBroadcastPayload
                        {
                            SlavePair = orphan.OrderSymbol,
                            OrderType = orphan.OrderType,
                            OrderLot = orphan.OrderLot,
                            OrderTicket = orphan.OrderTicket,
                            MasterOrderId = orphan.MasterOrderId ?? 0,
                            OrderMagic = orphan.OrderMagic,
                            CopyType = "MASTER_ORDER_DELETE",
                            CreatedAt = DateTime.UtcNow,
                        });
                    }

                    if (closeMessages.Count > 0)
                    {
                        // Push ALL close packets in ONE batch
                        if (slaveAccount.PlatformName == "cTrader")
                        {
                            await _jobPublisher.PublishCtraderPacketBatch(
                                slaveAccount.AccountNumber,
                                closeMessages.Cast<object>().ToList()
                            );
                        }
                        else
                        {
                            await _jobPublisher.PublishMt5PacketBatch(
                                slaveAccount.ServerName,
                                slaveAccount.AccountNumber,
                                closeMessages.Cast<object>().ToList()
                            );
                        }
                    }

                    // Batch delete from ActiveOrder
                    var orphanIds = orphanActiveOrders.Select(o => o.Id).ToList();
                    await _activeOrderRepository.Delete(o => orphanIds.Contains(o.Id));

                    _logger.Info($"BatchClose: slave={slaveAccount.Id}, executed={executedOrphans.Count}, nonExecuted(Failed)={nonExecutedOrphans.Count}");

                    foreach (var orphan in executedOrphans)
                    {
                        await _systemLogUsecase.CreateLog("CopyTrade", "SlaveClose", slaveAccount.Id,
                            $"Close signal sent (MASTER_ORDER_DELETE): masterOrderId={orphan.MasterOrderId} symbol={orphan.OrderSymbol} type={orphan.OrderType} lot={orphan.OrderLot} ticket={orphan.OrderTicket} platform={slaveAccount.PlatformName}");
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            await _systemLogUsecase.CreateLog("CopyTrade", "Error", masterAccountId,
                $"SyncSlaveActiveOrders EXCEPTION: error={ex.Message}", "Error");
            return TError.NewServer(ex.Message);
        }
    }

    private async Task FinalizeActiveOrderToOrder(ActiveOrder activeOrder, bool asFailed = false)
    {
        var order = new Order
        {
            AccountId = activeOrder.AccountId,
            MasterOrderId = activeOrder.MasterOrderId,
            OrderTicket = activeOrder.OrderTicket,
            OrderSymbol = activeOrder.OrderSymbol,
            OrderType = activeOrder.OrderType,
            OrderLot = activeOrder.OrderLot,
            OrderPrice = activeOrder.OrderPrice,
            OrderProfit = activeOrder.OrderProfit ?? 0,
            OrderMagic = activeOrder.OrderMagic,
            Status = asFailed ? OrderStatus.Failed : OrderStatus.Closed,
            OrderCloseAt = DateTime.UtcNow,
            CopyMessage = asFailed ? "Never executed on slave platform" : null,
        };

        await _orderRepository.Save(order);
    }

    private string? ResolveSymbol(
        string masterSymbol,
        string masterBroker,
        string slaveBroker,
        Dictionary<string, string>? relationPairs,
        List<SymbolMap> allSymbolMaps
    )
    {
        string? slavePair = null;

        // PRIORITY 1: MasterSlavePair override
        if (relationPairs != null && relationPairs.TryGetValue(masterSymbol, out var overridePair))
        {
            slavePair = overridePair;

            // FIX CASING:
            var exactOverride = allSymbolMaps.FirstOrDefault(x => 
                x.BrokerName.ToUpper() == slaveBroker.ToUpper() && 
                x.BrokerSymbol.ToUpper() == slavePair.ToUpper());
            if (exactOverride != null)
            {
                slavePair = exactOverride.BrokerSymbol;
            }
            return slavePair;
        }

        masterBroker = masterBroker.ToUpper();
        slaveBroker = slaveBroker.ToUpper();
        var masterSymbolUpper = masterSymbol.ToUpper();

        // PRIORITY 2: Exact Canonical resolution
        var masterMap = allSymbolMaps.FirstOrDefault(x => 
            x.BrokerSymbol.ToUpper() == masterSymbolUpper && 
            (masterBroker.StartsWith(x.BrokerName.ToUpper()) || x.BrokerName.ToUpper() == "ANY"));
            
        var canonical = masterMap != null ? masterMap.CanonicalSymbol.ToUpper() : masterSymbolUpper;

        var slaveMap = allSymbolMaps.FirstOrDefault(x => 
            x.CanonicalSymbol.ToUpper() == canonical && 
            (slaveBroker.StartsWith(x.BrokerName.ToUpper()) || x.BrokerName.ToUpper() == "ANY"));
            
        if (slaveMap != null)
            return slaveMap.BrokerSymbol;

        // PRIORITY 3: Fuzzy Canonical resolution
        var cleanedMaster = CleanSymbol(masterSymbol);
        var masterFuzzyMap = allSymbolMaps.FirstOrDefault(x => 
            x.BrokerSymbol.ToUpper() == cleanedMaster.ToUpper() && 
            (masterBroker.StartsWith(x.BrokerName.ToUpper()) || x.BrokerName.ToUpper() == "ANY"));
            
        if (masterFuzzyMap != null)
        {
            var fuzzyCanonical = masterFuzzyMap.CanonicalSymbol.ToUpper();
            var slaveFuzzyMap = allSymbolMaps.FirstOrDefault(x => 
                x.CanonicalSymbol.ToUpper() == fuzzyCanonical && 
                (slaveBroker.StartsWith(x.BrokerName.ToUpper()) || x.BrokerName.ToUpper() == "ANY"));
            
            if (slaveFuzzyMap != null)
                return slaveFuzzyMap.BrokerSymbol;
        }

        // PRIORITY 4: Fuzzy Match (Ignore Suffixes/Prefixes)
        // 4.1 First attempt: Does the cleaned master perfectly equal a cleaned slave symbol we know?
        var perfectCleanMatch = allSymbolMaps
            .Where(x => (slaveBroker.StartsWith(x.BrokerName.ToUpper()) || x.BrokerName.ToUpper() == "ANY") &&
                        CleanSymbol(x.BrokerSymbol).ToUpper() == cleanedMaster.ToUpper())
            .OrderBy(x => x.BrokerSymbol.Length)
            .FirstOrDefault();

        if (perfectCleanMatch != null)
            return perfectCleanMatch.BrokerSymbol;

        // 4.2 Second attempt: StartsWith Fallback (e.g. US30 -> US30.cash if no perfect clean match exists)
        var startsWithMatch = allSymbolMaps
            .Where(x => (slaveBroker.StartsWith(x.BrokerName.ToUpper()) || x.BrokerName.ToUpper() == "ANY") &&
                        x.BrokerSymbol.ToUpper().StartsWith(cleanedMaster.ToUpper()))
            .OrderBy(x => x.BrokerSymbol.Length)
            .FirstOrDefault();

        if (startsWithMatch != null)
            return startsWithMatch.BrokerSymbol;

        // PRIORITY 5: Fallback -- use master symbol as-is
        return masterSymbol;
    }

    private static int _magicCounter = 0;
    private static readonly object _magicLock = new();

    public int GenerateMagicNumber(long slaveAccountId)
    {
        var sss = (int)(slaveAccountId % 1000); // 0–999
        var ttt = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds() % 1000); // 0–999

        int c;
        lock (_magicLock)
        {
            c = _magicCounter++ % 10; // 0–9
        }

        return sss * 1_000_000 + ttt * 10 + c; // max 999,999,999
    }

    public async Task<PlatformActivePositionSyncPayload> SyncActiveOrdersFromPlatform(
        PlatformActivePositionSyncPayload payload
    )
    {
        try
        {
            // ------------------------------------
            // 1. Resolve account
            // ------------------------------------
            var (account, terr) = await GetAccount(
                new Account
                {
                    AccountNumber = payload.AccountNumber,
                    ServerName = payload.ServerName,
                }
            );

            if (terr != null || account == null)
            {
                _logger.Warning(
                    "Account not found for MT5 sync",
                    new { payload.AccountNumber, payload.ServerName }
                );

                // FAIL-SAFE: do nothing on MT5
                return payload;
            }

            // ------------------------------------
            // 2. UPDATE ACCOUNT RUNTIME STATE
            // ------------------------------------
            account.Balance = payload.Balance;
            account.Equity = payload.Equity;
            account.Status = ConnectionStatus.Success;
            account.CopierVersion = payload.CopierVersion;

            await _accountRepository.Save(account, a => a.Id == account.Id);

            // ------------------------------------
            // 3. Load active orders from DB
            // ------------------------------------
            var dbActiveOrders = await _activeOrderRepository.GetMany(a =>
                a.AccountId == account.Id
            );

            // Build lookup by magic (exclude magic=0, cTrader always sends 0)
            var dbByMagic = dbActiveOrders
                .Where(a => a.OrderMagic != 0)
                .GroupBy(a => a.OrderMagic)
                .ToDictionary(g => g.Key, g => g.First());
            // Also build a lookup by OrderTicket for cTrader (which sends OrderMagic=0)
            var dbByTicket = dbActiveOrders
                .Where(a => a.OrderTicket != 0)
                .GroupBy(a => a.OrderTicket)
                .ToDictionary(g => g.Key, g => g.First());

            // Build lookup for intents (OrderTicket=0) by MasterOrderId for label-based matching
            var dbIntentsByMasterOrderId = dbActiveOrders
                .Where(a => a.OrderTicket == 0 && a.MasterOrderId.HasValue)
                .GroupBy(a => a.MasterOrderId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            // Track which intents have been matched (to avoid double-matching)
            var matchedIntentIds = new HashSet<long>();

            // ------------------------------------
            // 4. Update EXISTING active orders
            // ------------------------------------
            foreach (var mtPos in payload.PositionList)
            {
                ActiveOrder? dbOrder = null;
                // Try matching by magic first (MT5), then by ticket (cTrader)
                if (mtPos.OrderMagic != 0 && dbByMagic.TryGetValue(mtPos.OrderMagic, out dbOrder))
                {
                    // matched by magic
                }
                else if (mtPos.OrderTicket != 0 && dbByTicket.TryGetValue(mtPos.OrderTicket, out dbOrder))
                {
                    // matched by ticket (cTrader)
                }

                // FALLBACK: Match by OrderLabel → MasterOrderId (cTrader sends label "ELVTD_{masterOrderId}" or "copy_{masterOrderId}")
                if (dbOrder == null && !string.IsNullOrEmpty(mtPos.OrderLabel))
                {
                    long? labelMasterOrderId = null;
                    if (mtPos.OrderLabel.StartsWith("ELVTD_") && long.TryParse(mtPos.OrderLabel[6..], out var eid))
                        labelMasterOrderId = eid;
                    else if (mtPos.OrderLabel.StartsWith("copy_") && long.TryParse(mtPos.OrderLabel[5..], out var cid))
                        labelMasterOrderId = cid;

                    if (labelMasterOrderId.HasValue
                        && dbIntentsByMasterOrderId.TryGetValue(labelMasterOrderId.Value, out var intentByLabel)
                        && !matchedIntentIds.Contains(intentByLabel.Id))
                    {
                        dbOrder = intentByLabel;
                        matchedIntentIds.Add(intentByLabel.Id);
                        _logger.Info($"Matched by label: ticket={mtPos.OrderTicket} label={mtPos.OrderLabel} → intent id={dbOrder.Id} masterOrderId={labelMasterOrderId.Value} account={account.Id}");
                    }
                }

                // FALLBACK 2: Match by symbol+type against unmatched intents (OrderTicket=0)
                if (dbOrder == null)
                {
                    var intentBySymbol = dbActiveOrders.FirstOrDefault(a =>
                        a.OrderTicket == 0
                        && a.MasterOrderId.HasValue
                        && a.OrderSymbol == mtPos.OrderSymbol
                        && a.OrderType == mtPos.OrderType
                        && !matchedIntentIds.Contains(a.Id)
                    );
                    if (intentBySymbol != null)
                    {
                        dbOrder = intentBySymbol;
                        matchedIntentIds.Add(intentBySymbol.Id);
                        _logger.Info($"Matched by symbol+type: ticket={mtPos.OrderTicket} symbol={mtPos.OrderSymbol} type={mtPos.OrderType} → intent id={dbOrder.Id} masterOrderId={dbOrder.MasterOrderId} account={account.Id}");
                    }
                }

                if (dbOrder == null)
                {
                    // CHECK: Is this a position that was already closed/finalized?
                    // If a closed Order exists with the same magic or ticket, do NOT re-create the ActiveOrder.
                    // This prevents MT5 positions from being "re-adopted" after SyncSlaveActiveOrders deleted them.
                    bool alreadyClosed = false;
                    if (mtPos.OrderMagic != 0)
                    {
                        var closedByMagic = await _orderRepository.Get(o =>
                            o.AccountId == account.Id
                            && o.OrderMagic == mtPos.OrderMagic
                            && o.OrderCloseAt != null
                        );
                        if (closedByMagic != null)
                        {
                            alreadyClosed = true;
                            _logger.Info($"Skipping re-creation of closed position: ticket={mtPos.OrderTicket} magic={mtPos.OrderMagic} symbol={mtPos.OrderSymbol} account={account.Id} (already closed as orderId={closedByMagic.Id})");
                        }
                    }

                    if (!alreadyClosed && mtPos.OrderTicket != 0)
                    {
                        var closedByTicket = await _orderRepository.Get(o =>
                            o.AccountId == account.Id
                            && o.OrderTicket == mtPos.OrderTicket
                            && o.OrderCloseAt != null
                        );
                        if (closedByTicket != null)
                        {
                            alreadyClosed = true;
                            _logger.Info($"Skipping re-creation of closed position: ticket={mtPos.OrderTicket} symbol={mtPos.OrderSymbol} account={account.Id} (already closed as orderId={closedByTicket.Id})");
                        }
                    }

                    if (alreadyClosed)
                    {
                        // Do NOT create ActiveOrder — MT5 reconcile_deletes will close this position
                        continue;
                    }

                    // External position (no master) — create new ActiveOrder
                    try
                    {
                        var newActiveOrder = new ActiveOrder
                        {
                            AccountId = account.Id,
                            AccountNumber = payload.AccountNumber,
                            ServerName = payload.ServerName,
                            MasterOrderId = null,
                            OrderTicket = mtPos.OrderTicket,
                            OrderSymbol = mtPos.OrderSymbol,
                            OrderMagic = mtPos.OrderMagic,
                            OrderType = mtPos.OrderType,
                            OrderLot = mtPos.OrderLot,
                            OrderPrice = mtPos.OrderPrice,
                            OrderOpenAt = mtPos.OrderOpenAt,
                            OrderProfit = mtPos.OrderProfit,
                            OrderLabel = string.IsNullOrEmpty(mtPos.OrderLabel) ? "manual" : mtPos.OrderLabel,
                            Status = OrderStatus.Success,
                        };
                        await _activeOrderRepository.Add(newActiveOrder);
                        _logger.Info($"External ActiveOrder created: ticket={mtPos.OrderTicket} symbol={mtPos.OrderSymbol} label={mtPos.OrderLabel ?? ""} account={account.Id}");
                    }
                    catch (Exception insertEx)
                    {
                        _logger.Fail($"INSERT external ActiveOrder FAILED: ticket={mtPos.OrderTicket} symbol={mtPos.OrderSymbol} account={account.Id}", insertEx);
                    }
                    continue;
                }

                dbOrder.OrderTicket = mtPos.OrderTicket;
                dbOrder.OrderProfit = mtPos.OrderProfit;
                dbOrder.OrderPrice = mtPos.OrderPrice;
                dbOrder.OrderOpenAt = mtPos.OrderOpenAt;
                if (!string.IsNullOrEmpty(mtPos.OrderLabel))
                    dbOrder.OrderLabel = mtPos.OrderLabel;
                else if (string.IsNullOrEmpty(dbOrder.OrderLabel))
                    dbOrder.OrderLabel = "manual";
                dbOrder.Status = OrderStatus.Success;

                await _activeOrderRepository.Update(dbOrder);

                // ALSO update the corresponding Order in the orders table (for MT5 close flow)
                // The orders table has intents with ticket=0 that need the real ticket
                if (mtPos.OrderTicket != 0 && dbOrder.MasterOrderId.HasValue)
                {
                    try
                    {
                        var correspondingOrder = await _orderRepository.Get(
                            o => o.AccountId == account.Id
                                 && o.MasterOrderId == dbOrder.MasterOrderId
                                 && o.OrderTicket == 0
                                 && (o.Status == OrderStatus.Progress || o.Status == OrderStatus.Success)
                        );
                        if (correspondingOrder != null)
                        {
                            await _orderRepository.Update(
                                o => o.Id == correspondingOrder.Id,
                                o =>
                                {
                                    o.OrderTicket = mtPos.OrderTicket;
                                    o.OrderPrice = mtPos.OrderPrice;
                                    o.OrderOpenAt = mtPos.OrderOpenAt;
                                    o.OrderProfit = mtPos.OrderProfit;
                                    o.Status = OrderStatus.Success;
                                }
                            );
                            _logger.Info($"Orders table synced: ticket={mtPos.OrderTicket} orderId={correspondingOrder.Id} masterOrderId={dbOrder.MasterOrderId} account={account.Id}");
                        }
                    }
                    catch (Exception orderSyncEx)
                    {
                        _logger.Fail($"Orders table sync FAILED: ticket={mtPos.OrderTicket} masterOrderId={dbOrder.MasterOrderId} account={account.Id}", orderSyncEx);
                    }
                }
            }

            // ------------------------------------
            // 5. Cleanup: finalize external ActiveOrders no longer open on platform
            // ------------------------------------
            var platformTickets = payload.PositionList.Select(x => x.OrderTicket).ToHashSet();

            // Re-fetch active orders FRESH after all updates above
            var freshActiveOrders = await _activeOrderRepository.GetMany(a => a.AccountId == account.Id);

            // Cleanup ALL ActiveOrders that are no longer open on cTrader
            // Case 1: Has ticket but not on platform anymore
            var staleWithTicket = freshActiveOrders
                .Where(a => a.OrderTicket != 0 && !platformTickets.Contains(a.OrderTicket))
                .ToList();

            // Case 2: Has NO ticket (OrderTicket=0) and older than 5 minutes
            var staleNoTicket = freshActiveOrders
                .Where(a => a.OrderTicket == 0 && a.CreatedAt < DateTime.UtcNow.AddMinutes(-5))
                .ToList();

            var allStale = staleWithTicket.Concat(staleNoTicket).ToList();

            if (allStale.Count > 0)
            {
                _logger.Info($"Cleanup: account={account.Id} accountNumber={account.AccountNumber} platformPositions={platformTickets.Count} dbActive={freshActiveOrders.Count} staleWithTicket={staleWithTicket.Count} staleNoTicket={staleNoTicket.Count}");
            }

            foreach (var staleOrder in allStale)
            {
                // Move to orders table as Complete (only if has a ticket)
                if (staleOrder.OrderTicket != 0)
                {
                    // Check if order already exists in orders table (avoid SetValues crash on PK)
                    var existingOrder = await _orderRepository.Get(
                        o => o.OrderTicket == staleOrder.OrderTicket && o.AccountId == staleOrder.AccountId
                    );

                    if (existingOrder != null)
                    {
                        // Update existing order
                        await _orderRepository.Update(
                            o => o.Id == existingOrder.Id,
                            o =>
                            {
                                o.Status = OrderStatus.Complete;
                                o.OrderCloseAt = DateTime.UtcNow;
                                o.OrderProfit = staleOrder.OrderProfit;
                            }
                        );
                    }
                    else
                    {
                        // Insert new closed order
                        var closedOrder = new Order
                        {
                            AccountId = staleOrder.AccountId,
                            MasterOrderId = staleOrder.MasterOrderId,
                            OrderTicket = staleOrder.OrderTicket,
                            OrderSymbol = staleOrder.OrderSymbol,
                            OrderType = staleOrder.OrderType,
                            OrderLot = staleOrder.OrderLot,
                            OrderPrice = staleOrder.OrderPrice,
                            OrderMagic = staleOrder.OrderMagic,
                            OrderOpenAt = staleOrder.OrderOpenAt,
                            OrderCloseAt = DateTime.UtcNow,
                            OrderProfit = staleOrder.OrderProfit,
                            Status = OrderStatus.Complete,
                        };
                        await _orderRepository.Save(closedOrder);
                    }
                }

                await _activeOrderRepository.DeleteById(staleOrder.Id);

                _logger.Info($"Stale ActiveOrder cleaned up: ticket={staleOrder.OrderTicket} symbol={staleOrder.OrderSymbol} account={staleOrder.AccountId} masterOrderId={staleOrder.MasterOrderId} hadTicket={staleOrder.OrderTicket != 0}");
            }

            // ------------------------------------
            // 5b. Detect closed positions: Orders with ticket that no longer exists on platform
            // This handles MT5 close (reconcile_deletes closes position, backend never gets callback)
            // ------------------------------------
            if (platformTickets.Count > 0 || payload.PositionList.Count == 0)
            {
                var openOrdersWithTicket = await _orderRepository.GetMany(o =>
                    o.AccountId == account.Id
                    && o.OrderTicket != 0
                    && o.OrderCloseAt == null
                    && (o.Status == OrderStatus.Success || o.Status == OrderStatus.Progress)
                );

                foreach (var openOrder in openOrdersWithTicket)
                {
                    if (!platformTickets.Contains(openOrder.OrderTicket))
                    {
                        await _orderRepository.Update(
                            o => o.Id == openOrder.Id,
                            o =>
                            {
                                o.Status = OrderStatus.Complete;
                                o.OrderCloseAt = DateTime.UtcNow;
                            }
                        );
                        _logger.Info($"Order auto-closed (position gone from platform): orderId={openOrder.Id} ticket={openOrder.OrderTicket} symbol={openOrder.OrderSymbol} account={account.Id}");
                    }
                }
            }

            // ------------------------------------
            // 6. Build DELTA response (re-read from DB to include newly created orders)
            // ------------------------------------
            var updatedActiveOrders = await _activeOrderRepository.GetMany(a =>
                a.AccountId == account.Id
            );

            var syncResponse = new PlatformActivePositionSyncPayload
            {
                AccountNumber = payload.AccountNumber,
                ServerName = payload.ServerName,
                IsFlushOrder = account.IsFlushOrder,
                Balance = payload.Balance,
                Equity = payload.Equity,
                CopierVersion = account.CopierVersion,
                PositionList = updatedActiveOrders
                    .Select(o => new PlatformPositionDto
                    {
                        OrderTicket = o.OrderTicket,
                        OrderMagic = o.OrderMagic,
                        OrderType = o.OrderType,
                        OrderLot = o.OrderLot,
                        OrderPrice = o.OrderPrice,
                        OrderProfit = o.OrderProfit ?? 0,
                        OrderSymbol = o.OrderSymbol,
                        OrderOpenAt = o.OrderOpenAt ?? DateTime.MinValue,
                        Status = o.Status,
                    })
                    .ToList(),
            };

            // reset to 0 after sync if it was 1
            if (account.IsFlushOrder == 1)
            {
                await _accountRepository.Update(
                    a => a.Id == account.Id,
                    a =>
                    {
                        a.IsFlushOrder = 0;
                    }
                );
            }

            return syncResponse;
        }
        catch (Exception ex)
        {
            _logger.Fail("SyncActiveOrdersFromMt5 failed", ex);

            // FAIL-SAFE:
            // echo MT5 snapshot back unchanged
            return payload;
        }
    }

    public async Task<ITError?> SyncPositionHistory(PositionHistorySyncPayload payload)
    {
        try
        {
            // ------------------------------------
            // 1. Resolve account
            // ------------------------------------
            var (account, terr) = await GetAccount(
                new Account
                {
                    AccountNumber = payload.AccountNumber,
                    ServerName = payload.ServerName,
                }
            );

            if (terr != null || account == null)
            {
                _logger.Warning(
                    "Account not found for position history sync",
                    new { payload.AccountNumber, payload.ServerName }
                );
                return TError.NewNotFound("account not found");
            }

            // ------------------------------------
            // 2. Upsert each historical position into orders table
            // ------------------------------------
            foreach (var pos in payload.Positions)
            {
                var existingOrder = await _orderRepository.Get(
                    o => o.OrderTicket == pos.PositionId && o.AccountId == account.Id
                );

                if (existingOrder != null)
                {
                    // Update existing order with latest data
                    await _orderRepository.Update(
                        o => o.Id == existingOrder.Id,
                        o =>
                        {
                            o.OrderProfit = pos.OrderProfit;
                            o.ClosePrice = pos.ClosePrice;
                            o.OrderCloseAt = pos.OrderCloseAt;
                            o.Status = (OrderStatus)pos.Status;
                        }
                    );
                }
                else
                {
                    // Create new order (external trade, no master)
                    var newOrder = new Order
                    {
                        AccountId = account.Id,
                        MasterOrderId = null,
                        OrderTicket = pos.PositionId,
                        OrderSymbol = pos.OrderSymbol,
                        OrderType = pos.OrderType,
                        OrderLot = pos.OrderLot,
                        OrderPrice = pos.OrderPrice,
                        ClosePrice = pos.ClosePrice,
                        OrderProfit = pos.OrderProfit,
                        OrderOpenAt = pos.OrderOpenAt,
                        OrderCloseAt = pos.OrderCloseAt,
                        OrderLabel = string.IsNullOrEmpty(pos.OrderLabel) ? "manual" : pos.OrderLabel,
                        Status = (OrderStatus)pos.Status,
                    };
                    await _orderRepository.Save(newOrder);
                }
            }

            _logger.Info(
                "Position history synced",
                new { payload.AccountNumber, Count = payload.Positions.Count }
            );

            return null;
        }
        catch (Exception ex)
        {
            _logger.Fail("SyncPositionHistory failed", ex);
            return TError.NewServer("position history sync failed");
        }
    }

    public async Task<ITError?> SyncAccountState(SyncAccountStatePayload dto)
    {
        try
        {
            // ------------------------------------
            // 1. Resolve account
            // ------------------------------------
            var (account, terr) = await GetAccount(
                new Account { AccountNumber = dto.AccountNumber, ServerName = dto.ServerName }
            );

            if (terr != null || account == null)
            {
                _logger.Warning(
                    "Account not found for MT5 sync",
                    new { dto.AccountNumber, dto.ServerName }
                );

                return TError.NewNotFound("account not found");
            }

            // =========================
            // 2. LOG ACCOUNT SNAPSHOT
            // =========================
            var accountLog = new AccountLog
            {
                AccountId = account.Id,
                Balance = dto.Balance,
                Equity = dto.Equity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await _accountLogRepository.Save(accountLog);

            // =========================
            // 3. ACCOUNT UPDATE LATEST DATA
            // =========================
            account.Balance = dto.Balance;
            account.Equity = dto.Equity;
            account.Status = ConnectionStatus.Success;
            account.CopierVersion = dto.CopierVersion;

            // Update server status if reported by copier (e.g. 10017 Close-Only)
            if (!string.IsNullOrEmpty(dto.ServerStatus))
            {
                account.ServerStatus = dto.ServerStatus;
                account.ServerStatusMessage = dto.ServerStatusMessage;
            }
            else if (account.ServerStatus == "ERROR")
            {
                // Clear error when copier reports normal sync (no error status)
                account.ServerStatus = null;
                account.ServerStatusMessage = null;
            }

            // Update expert log if provided (sent every 5 minutes by EA)
            if (!string.IsNullOrEmpty(dto.ExpertLog))
            {
                account.ExpertLog = dto.ExpertLog;
            }

            await _accountRepository.Save(account, a => a.Id == account.Id);

            // =========================
            // 4. PRELOAD EXISTING ORDERS
            // =========================
            var tickets = dto.Positions.Select(p => p.OrderTicket).Distinct().ToList();

            var existingLogs = await _orderLogRepository.GetMany(o =>
                o.AccountId == account.Id && tickets.Contains(o.OrderTicket)
            );

            var existingMap = existingLogs.ToDictionary(o => o.OrderTicket);

            // =========================
            // 5. UPSERT ORDER LOGS
            // =========================
            foreach (var p in dto.Positions)
            {
                var status = (OrderStatus)p.Status;

                if (!existingMap.TryGetValue(p.OrderTicket, out var log))
                {
                    // -------- CREATE --------
                    log = new OrderLog
                    {
                        AccountId = account.Id,

                        OrderSymbol = p.OrderSymbol,
                        OrderTicket = p.OrderTicket,
                        OrderType = p.OrderType,
                        OrderLot = p.OrderLot,

                        OrderPrice = p.OrderPrice,
                        SlPrice = p.SlPrice,
                        TpPrice = p.TpPrice,

                        LastPrice = p.LastPrice,
                        LastTime = p.LastTime,

                        OrderProfit = p.OrderProfit,
                        Change = p.Change,

                        Status = status,

                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                    };

                    await _orderLogRepository.Save(log);
                }
                else
                {
                    // -------- UPDATE --------
                    log.OrderLot = p.OrderLot;
                    log.OrderPrice = p.OrderPrice;

                    log.SlPrice = p.SlPrice;
                    log.TpPrice = p.TpPrice;

                    log.LastPrice = p.LastPrice;
                    log.LastTime = p.LastTime;

                    log.OrderProfit = p.OrderProfit;
                    log.Change = p.Change;

                    log.Status = status;
                    log.UpdatedAt = DateTime.UtcNow;

                    await _orderLogRepository.Save(log, x => x.Id == log.Id);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            return TError.NewServer(ex.Message);
        }
    }

    public async Task<(string, ITError?)> DeleteMasterOrders(long accountId, List<long> orderIds)
    {
        try
        {
            if (orderIds == null || orderIds.Count == 0)
                return ("", TError.NewClient("orderIds cannot be empty"));

            var (account, accErr) = await GetAccount(new Account { Id = accountId });
            if (accErr != null || account == null)
                return ("", accErr ?? TError.NewNotFound("account not found"));

            // take open orders
            var openOrders = await _orderRepository.GetMany(o =>
                o.AccountId == accountId && o.OrderCloseAt == null
            );

            // exclude orders which deleted
            var remainingOrders = openOrders.Where(o => !orderIds.Contains(o.Id)).ToList();

            // build bridge payload
            var bridgePayload = new BridgeListCreateOrderPayload
            {
                ServerName = account.ServerName,
                AccountId = account.AccountNumber,
                Orders = remainingOrders
                    .Select(o => new BridgeCreateOrderItem
                    {
                        OrderTicket = o.OrderTicket,
                        OrderSymbol = o.OrderSymbol,
                        OrderType = o.OrderType,
                        OrderLot = o.OrderLot,
                        OrderPrice = o.OrderPrice,
                        OrderOpenAt = o.OrderOpenAt,
                    })
                    .ToList(),
            };

            // reuse logic existing
            return await CreateBridgeMasterOrder(bridgePayload);
        }
        catch (Exception ex)
        {
            return ("", TError.NewServer(ex.Message));
        }
    }

    private async Task<ITError?> CopyMasterOrderToSlaves(
        Order masterOrder,
        Account masterAccount,
        decimal masterBalance
    )
    {
        try
        {
            // 1. Validate Input
            if (masterBalance <= 0)
            {
                await _systemLogUsecase.CreateLog("CopyTrade", "SlaveCopy", masterAccount.Id,
                    $"Skipped copy: master balance is {masterBalance} (must be > 0) for ticket={masterOrder.OrderTicket} symbol={masterOrder.OrderSymbol}", "Warning");
                return TError.NewClient("Master balance must be positive");
            }

            // 2. Find Slaves
            var (slaves, terr) = await GetMasterSlaves(new MasterSlave { MasterId = masterAccount.Id });
            if (terr != null || slaves.Count == 0)
                return null; // Not an error, just no slaves

            // 3. Preload ALL data ONCE before the loop (not per slave)
            var allSymbolMaps = await _symbolMapRepository.GetMany(x => x.DeletedAt == null);
            var slaveIds = slaves.Select(s => s.Id).ToList();
            var allConfigs = await _masterSlaveConfigRepository.GetMany(c => slaveIds.Contains(c.MasterSlaveId));
            var allPairs = await _masterSlavePairRepository.GetMany(p => slaveIds.Contains(p.MasterSlaveId));

            // 4. Process each slave
            foreach (var slaveRelation in slaves)
            {
                // Use preloaded config
                var config = allConfigs.FirstOrDefault(c => c.MasterSlaveId == slaveRelation.Id);
                var multiplier = config?.Multiplier ?? 1.0m;
                if (multiplier == 0) multiplier = 1.0m;

                // Use preloaded pairs
                var pairs = allPairs.Where(p => p.MasterSlaveId == slaveRelation.Id).ToList();

                // Use slave account already loaded by GetMasterSlaves()
                var slaveAccount = slaveRelation.SlaveAccount;
                if (slaveAccount == null || slaveAccount.Balance <= 0)
                {
                    _logger.Info($"Skipped slave {slaveRelation.SlaveId}: account not found or balance <= 0");
                    continue;
                }

                // BERECHNE LOT: (masterLot / masterBalance) * slaveBalance * multiplier
                decimal riskRatio = masterOrder.OrderLot / masterBalance;
                decimal slaveLot = Math.Round(
                    riskRatio * slaveAccount.Balance * multiplier,
                    2 // Round to 0.01
                );

                _logger.Info($"Lot Calculation | Master: {masterAccount.AccountNumber} ({masterBalance}), Slave: {slaveAccount.AccountNumber} ({slaveAccount.Balance}), Multiplier: {multiplier}, MasterLot: {masterOrder.OrderLot} => SlaveLot: {slaveLot}");

                // Validate Lot
                if (slaveLot < 0.01m) slaveLot = 0.01m; // Minimum lot size
                if (slaveLot > 100.0m) slaveLot = 100.0m; // Safety cap

                // Symbol-Mapping using preloaded symbol maps
                var relationPairs = pairs.ToDictionary(x => x.MasterPair, x => x.SlavePair);
                
                var mappedSymbol = ResolveSymbol(
                    masterOrder.OrderSymbol,
                    masterAccount.BrokerName ?? "",
                    slaveAccount.BrokerName ?? "",
                    relationPairs,
                    allSymbolMaps
                ) ?? masterOrder.OrderSymbol;

                // Create Order
                var slaveOrder = new Order
                {
                    AccountId = slaveAccount.Id,
                    MasterOrderId = masterOrder.Id,
                    OrderSymbol = mappedSymbol,
                    OrderType = masterOrder.OrderType,
                    OrderLot = slaveLot,
                    OrderMagic = GenerateBridgeMagicNumber(masterOrder.Id, slaveAccount.Id),
                    OrderLabel = $"copy_{masterOrder.Id}",
                    Status = OrderStatus.Progress,
                    CreatedAt = DateTime.UtcNow
                };

                var (newSlaveOrder, saveErr) = await CreateOrder(slaveOrder);
                if (saveErr != null || newSlaveOrder == null)
                {
                    await _systemLogUsecase.CreateLog("CopyTrade", "SlaveCopy", slaveAccount.Id,
                        $"FAILED to create slave order: masterTicket={masterOrder.OrderTicket} symbol={mappedSymbol} lot={slaveLot} error={saveErr?.Message}", "Error");
                    continue;
                }

                // 4. Publish to RabbitMQ
                var broadcastPayload = new BridgeOrderBroadcastPayload
                {
                    SlavePair = mappedSymbol,
                    OrderType = masterOrder.OrderType,
                    OrderLot = slaveLot,
                    OrderTicket = 0, // Ticket will be filled by slave EA execution
                    MasterOrderId = masterOrder.Id,
                    OrderMagic = slaveOrder.OrderMagic ?? 0,
                    CopyType = "MASTER_ORDER_UPDATE",
                    CreatedAt = DateTime.UtcNow,
                };

                if (slaveAccount.PlatformName == "cTrader")
                {
                    await _jobPublisher.PublishCtraderPacketBatch(
                        slaveAccount.AccountNumber,
                        new List<object> { broadcastPayload }
                    );
                }
                else
                {
                    await _jobPublisher.PublishMt5PacketBatch(
                        slaveAccount.ServerName,
                        slaveAccount.AccountNumber,
                        new List<object> { broadcastPayload }
                    );
                }

                _logger.Info($"CopyOrder: master={masterOrder.Id}, slave={slaveAccount.Id}, symbol={mappedSymbol}, lot={slaveLot}");
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.Fail("CopyMasterOrderToSlaves failed", ex);
            await _systemLogUsecase.CreateLog("CopyTrade", "SlaveCopy", masterAccount.Id,
                $"CopyMasterOrderToSlaves EXCEPTION: masterTicket={masterOrder.OrderTicket} error={ex.Message}", "Error");
            return TError.NewServer(ex.Message);
        }
    }

    private long GenerateBridgeMagicNumber(long masterOrderId, long slaveAccountId)
    {
        // Simple magic number generation that combines master order id and slave account id
        return ((masterOrderId & 0xFFFFFFFF) << 32) | (slaveAccountId & 0xFFFFFFFF);
    }

    public async Task<(List<SlaveAccountOrdersDto>, ITError?)> GetSlaveOrdersForMaster(long masterAccountId)
    {
        try
        {
            var relations = await _masterSlaveRepository.GetMany(ms => ms.MasterId == masterAccountId);
            if (relations.Count == 0) return (new List<SlaveAccountOrdersDto>(), null);

            var slaveIds = relations.Select(r => r.SlaveId).Distinct().ToList();
            var slaves = await _accountRepository.GetMany(a => slaveIds.Contains(a.Id) && a.DeletedAt == null);
            var activeOrders = await _activeOrderRepository.GetMany(o => slaveIds.Contains(o.AccountId));

            var result = slaves.Select(s => new SlaveAccountOrdersDto
            {
                AccountId = s.Id,
                AccountNumber = s.AccountNumber,
                BrokerName = s.BrokerName,
                ServerName = s.ServerName,
                Status = s.Status,
                Orders = activeOrders.Where(o => o.AccountId == s.Id).ToList()
            }).ToList();

            return (result, null);
        }
        catch (Exception ex)
        {
            return (new List<SlaveAccountOrdersDto>(), TError.NewServer(ex.Message));
        }
    }

    public async Task<long> FindSlaveTicketByMaster(long masterTicket, long accountNumber)
    {
        var account = await _accountRepository.Get(a => a.AccountNumber == accountNumber);
        if (account == null) return 0;

        // Look in active_orders first
        var activeOrder = await _activeOrderRepository.Get(
            o => o.AccountId == account.Id && o.MasterOrderId != null && o.OrderTicket != 0
        );

        // Find the master order by ticket
        var masterOrder = await _orderRepository.Get(o => o.OrderTicket == masterTicket && o.MasterOrderId == null);
        if (masterOrder == null) return 0;

        // Find slave active order linked to this master
        var slaveActive = await _activeOrderRepository.Get(
            o => o.AccountId == account.Id && o.MasterOrderId == masterOrder.Id && o.OrderTicket != 0
        );
        if (slaveActive != null) return slaveActive.OrderTicket;

        // Fallback: check orders table
        var slaveOrder = await _orderRepository.Get(
            o => o.AccountId == account.Id && o.MasterOrderId == masterOrder.Id
                 && o.OrderTicket != 0 && o.Status == OrderStatus.Success
        );
        return slaveOrder?.OrderTicket ?? 0;
    }

    public async Task<ITError?> DeleteActiveOrder(long id)
    {
        try
        {
            var existing = await _activeOrderRepository.Get(o => o.Id == id);
            if (existing == null) return TError.NewNotFound("Active order not found");

            // Get the slave account to send close command to the trading platform
            var account = await _accountRepository.Get(a => a.Id == existing.AccountId);
            if (account != null)
            {
                var closePayload = new BridgeOrderBroadcastPayload
                {
                    SlavePair = existing.OrderSymbol,
                    OrderType = existing.OrderType,
                    OrderLot = existing.OrderLot,
                    OrderTicket = existing.OrderTicket,
                    MasterOrderId = existing.MasterOrderId ?? 0,
                    OrderMagic = existing.OrderMagic,
                    CopyType = "MASTER_ORDER_DELETE",
                    CreatedAt = DateTime.UtcNow,
                };

                if (account.PlatformName == "cTrader")
                {
                    await _jobPublisher.PublishCtraderPacketBatch(
                        account.AccountNumber,
                        new List<object> { closePayload }
                    );
                }
                else
                {
                    await _jobPublisher.PublishMt5PacketBatch(
                        account.ServerName,
                        account.AccountNumber,
                        new List<object> { closePayload }
                    );
                }

                await _systemLogUsecase.CreateLog("CopyTrade", "SlaveClose", account.Id,
                    $"Close command sent for active order: id={id} ticket={existing.OrderTicket} symbol={existing.OrderSymbol} platform={account.PlatformName}");
            }

            // Don't delete or finalize here — let ConfirmBridgeSlaveOrder handle
            // the DB cleanup when the trading platform confirms the close.
            return null;
        }
        catch (Exception ex)
        {
            return TError.NewServer(ex.Message);
        }
    }

    public async Task<bool> SoftDeleteOrder(long id)
    {
        return await _orderRepository.Update(
            o => o.Id == id,
            o => o.DeletedAt = DateTime.UtcNow
        );
    }
}

public class SlaveAccountOrdersDto
{
    public long AccountId { get; set; }
    public long AccountNumber { get; set; }
    public string BrokerName { get; set; } = "";
    public string ServerName { get; set; } = "";
    public ConnectionStatus Status { get; set; }
    public List<ActiveOrder> Orders { get; set; } = [];
}
