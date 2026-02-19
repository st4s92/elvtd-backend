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
            && (param.OrderTicket == 0 || a.OrderTicket == param.OrderTicket)
            && (!param.CloseTicket.HasValue || a.CloseTicket == param.CloseTicket.Value)
            && (string.IsNullOrEmpty(param.OrderSymbol) || a.OrderSymbol == param.OrderSymbol)
            && (string.IsNullOrEmpty(param.OrderType) || a.OrderType == param.OrderType)
            && (param.OrderLot == 0 || a.OrderLot == param.OrderLot)
            && (!param.OrderMagic.HasValue || a.OrderMagic == param.OrderMagic.Value)
            && (param.Status == 0 || a.Status == param.Status);
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
        int pageSize
    )
    {
        try
        {
            var (data, total) = await _orderRepository.GetPaginated(
                FilterOrder(param),
                page,
                pageSize,
                q => q.OrderByDescending(o => o.CreatedAt)
            );
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
                return accErr;

            if (account == null)
                return TError.NewNotFound("account not found");

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
                    return terr;

                if (existingOrder == null)
                    return TError.NewNotFound("order not found");

                existingOrder.OrderOpenAt = DateTime.UtcNow;
                existingOrder.OrderTicket = payload.Order.OrderTicket;
                existingOrder.OrderPrice = payload.Order.OrderPrice;
                existingOrder.OrderLot = payload.Order.OrderLot;
                existingOrder.Status = OrderStatus.Success;
            }
            else if (payload.Order.OrderType == "DEAL_TYPE_DELETE")
            {
                (existingOrder, terr) = await GetOrder(
                    new Order
                    {
                        Id = payload.Order.MasterOrderId, // use master order id? it should be order id, it just using same payload
                    }
                );
                if (terr != null)
                    return terr;

                if (existingOrder == null)
                    return TError.NewNotFound("order not found");

                existingOrder.OrderCloseAt = DateTime.UtcNow;
                existingOrder.ClosePrice = payload.Order.OrderClosePrice;
                existingOrder.Status = OrderStatus.Complete;
            }

            if (existingOrder == null)
                return TError.NewNotFound("order not found");

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
                .Where(po => OrderTimeHelper.IsOrderFresh(po.OrderOpenAt, toleranceSeconds))
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
                    return ("", terr);
            }

            var closeOrderAt = DateTime.UtcNow;
            if (deletedOrders.Any())
            {
                var deletedIds = deletedOrders.Select(o => o.Id).ToList();
                await _orderRepository.UpdateMany(
                    o => deletedIds.Contains(o.Id),
                    item =>
                    {
                        item.OrderCloseAt = closeOrderAt;
                        item.Status = OrderStatus.Complete;
                    }
                );
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

            // group pairs into a dictionary
            var allPairsMap = allPairs
                .GroupBy(x => x.MasterSlaveId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.MasterPair, x => x.SlavePair));

            // config lookup dictionary
            var configMap = allConfigs.ToDictionary(x => x.MasterSlaveId, x => x);

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

                // safe: if pairs not exist
                if (!allPairsMap.TryGetValue(item.Id, out var masterSlavePair))
                    continue;

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
                    // SAFE DICTIONARY ACCESS
                    if (!masterSlavePair.TryGetValue(order.OrderSymbol, out var slavePair))
                        continue; // skip if symbol not mapped

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

                    await _jobPublisher.PublishMt5PacketBatch(
                        item.SlaveAccount.ServerName,
                        item.SlaveAccount.AccountNumber,
                        messages
                    );
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

                // 3. ambil active order slave existing
                activeOrdersByAccountId.TryGetValue(slaveAccountId, out var slaveActiveOrders);

                slaveActiveOrders ??= new List<ActiveOrder>();

                var slaveActiveByMasterId = slaveActiveOrders
                    .GroupBy(a => a.MasterOrderId)
                    .ToDictionary(g => g.Key, g => g.First());

                // 4. CREATE missing active orders
                foreach (var masterOrder in masterOrders)
                {
                    if (slaveActiveByMasterId.ContainsKey(masterOrder.Id))
                        continue;

                    if (
                        DateTime.UtcNow - masterOrder.OrderOpenAt
                        > TimeSpan.FromSeconds(toleranceSeconds)
                    )
                    {
                        _logger.Info($"Skip stale master order {masterOrder.Id}");
                        continue;
                    }

                    // ambil pair mapping untuk relation ini
                    if (!pairMap.TryGetValue(relation.Id, out var relationPairs))
                        continue;

                    // ambil slave pair
                    if (!relationPairs.TryGetValue(masterOrder.OrderSymbol, out var slavePair))
                        continue;

                    if (string.IsNullOrEmpty(slavePair))
                        continue;

                    // ambil multiplier
                    decimal multiplier = 1;
                    if (configMap.TryGetValue(relation.Id, out var cfg))
                    {
                        multiplier = cfg.Multiplier == 0 ? 1 : cfg.Multiplier;
                    }

                    // hitung lot
                    var finalLot = Math.Round(masterOrder.OrderLot * multiplier, 2);

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
                    };

                    await _activeOrderRepository.Add(activeOrder);
                }

                // 5. BATCH PROCESSING: Close orphan active orders (master already closed)
                var orphanActiveOrders = slaveActiveOrders
                    .Where(a => !masterOrderIds.Contains(a.MasterOrderId))
                    .ToList();

                if (orphanActiveOrders.Any())
                {
                    List<BridgeOrderBroadcastPayload> closeMessages = [];
                    foreach (var orphan in orphanActiveOrders)
                    {
                        // Finalize to Order (we can keep this for now or batch it if needed, but the push is critical)
                        await FinalizeActiveOrderToOrder(orphan);

                        closeMessages.Add(new BridgeOrderBroadcastPayload
                        {
                            SlavePair = orphan.OrderSymbol,
                            OrderType = orphan.OrderType,
                            OrderLot = orphan.OrderLot,
                            OrderTicket = orphan.OrderTicket,
                            MasterOrderId = orphan.MasterOrderId,
                            CopyType = "MASTER_ORDER_DELETE",
                            CreatedAt = DateTime.UtcNow,
                        });
                    }

                    // Push ALL close packets in ONE batch
                    await _jobPublisher.PublishMt5PacketBatch(
                        slaveAccount.ServerName,
                        slaveAccount.AccountNumber,
                        closeMessages.Cast<object>().ToList()
                    );

                    // Batch delete from ActiveOrder
                    var orphanIds = orphanActiveOrders.Select(o => o.Id).ToList();
                    await _activeOrderRepository.Delete(o => orphanIds.Contains(o.Id));

                    _logger.LogInformation(
                        "BatchClose: slave={S}, count={C}",
                        slaveAccount.Id, orphanActiveOrders.Count
                    );
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            return TError.NewServer(ex.Message);
        }
    }

    private async Task FinalizeActiveOrderToOrder(ActiveOrder activeOrder)
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
            Status = OrderStatus.Closed,
            OrderCloseAt = DateTime.UtcNow,
        };

        await _orderRepository.Save(order);
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

            await _accountRepository.Save(account, a => a.Id == account.Id);

            // ------------------------------------
            // 3. Load active orders from DB
            // ------------------------------------
            var dbActiveOrders = await _activeOrderRepository.GetMany(a =>
                a.AccountId == account.Id
            );

            var dbByMagic = dbActiveOrders.ToDictionary(a => a.OrderMagic);

            // ------------------------------------
            // 4. Update EXISTING active orders
            // ------------------------------------
            foreach (var mtPos in payload.PositionList)
            {
                if (!dbByMagic.TryGetValue(mtPos.OrderMagic, out var dbOrder))
                    continue;

                dbOrder.OrderTicket = mtPos.OrderTicket;
                dbOrder.OrderProfit = mtPos.OrderProfit;
                dbOrder.OrderPrice = mtPos.OrderPrice;
                dbOrder.Status = OrderStatus.Success;

                await _activeOrderRepository.Update(dbOrder);
            }

            // ------------------------------------
            // 5. Detect NEW orders (created by master)
            // ------------------------------------
            var mtMagicSet = payload.PositionList.Select(x => x.OrderMagic).ToHashSet();

            // ------------------------------------
            // 6. Build DELTA response
            // ------------------------------------
            return new PlatformActivePositionSyncPayload
            {
                AccountNumber = payload.AccountNumber,
                ServerName = payload.ServerName,
                Balance = payload.Balance,
                Equity = payload.Equity,

                PositionList = dbActiveOrders
                    .Select(o => new PlatformPositionDto
                    {
                        OrderTicket = 0,
                        OrderMagic = o.OrderMagic,
                        OrderType = o.OrderType,
                        OrderLot = o.OrderLot,
                        OrderPrice = 0,
                        OrderProfit = 0,
                        OrderSymbol = o.OrderSymbol,
                        Status = o.Status,
                    })
                    .ToList(),
            };
        }
        catch (Exception ex)
        {
            _logger.Fail("SyncActiveOrdersFromMt5 failed", ex);

            // FAIL-SAFE:
            // echo MT5 snapshot back unchanged
            return payload;
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
                return TError.NewValidation("Master balance must be positive");

            // 2. Find Slaves
            var (slaves, terr) = await GetMasterSlaves(new MasterSlave { MasterId = masterAccount.Id });
            if (terr != null || slaves.Count == 0)
                return null; // Not an error, just no slaves

            // 3. Process each slave
            foreach (var slaveRelation in slaves)
            {
                // Load config for this specific Master-Slave relation (MasterSlaveId is relation.Id)
                var config = await _masterSlaveConfigRepository.Get(c => c.MasterSlaveId == slaveRelation.Id);
                var multiplier = config?.Multiplier ?? 1.0m;
                if (multiplier == 0) multiplier = 1.0m;

                // Load symbol pairs for mapping
                var pairs = await _masterSlavePairRepository.GetMany(p => p.MasterSlaveId == slaveRelation.Id);

                // Load slave account to get balance for proportional lot calculation
                var (slaveAccount, err) = await GetAccount(new Account { Id = slaveRelation.SlaveId });
                if (err != null || slaveAccount == null || slaveAccount.Balance <= 0) continue;

                // BERECHNE LOT: (masterLot / masterBalance) * slaveBalance * multiplier
                decimal riskRatio = masterOrder.OrderLot / masterBalance;
                decimal slaveLot = Math.Round(
                    riskRatio * slaveAccount.Balance * multiplier,
                    2 // Round to 0.01
                );

                // Validate Lot
                if (slaveLot < 0.01m) slaveLot = 0.01m; // Minimum lot size
                if (slaveLot > 100.0m) slaveLot = 100.0m; // Safety cap

                // Symbol-Mapping
                var mappedSymbol = pairs
                    .FirstOrDefault(p => p.MasterPair == masterOrder.OrderSymbol)
                    ?.SlavePair ?? masterOrder.OrderSymbol;

                // Create Order
                var slaveOrder = new Order
                {
                    AccountId = slaveAccount.Id,
                    MasterOrderId = masterOrder.Id,
                    OrderSymbol = mappedSymbol,
                    OrderType = masterOrder.OrderType,
                    OrderLot = slaveLot,
                    OrderMagic = GenerateBridgeMagicNumber(masterOrder.Id, slaveAccount.Id),
                    Status = OrderStatus.Progress,
                    CreatedAt = DateTime.UtcNow
                };

                var (newSlaveOrder, saveErr) = await CreateOrder(slaveOrder);
                if (saveErr != null || newSlaveOrder == null) continue;

                // 4. Publish to RabbitMQ
                var broadcastPayload = new BridgeOrderBroadcastPayload
                {
                    SlavePair = mappedSymbol,
                    OrderType = masterOrder.OrderType,
                    OrderLot = slaveLot,
                    OrderTicket = 0, // Ticket will be filled by slave EA execution
                    MasterOrderId = masterOrder.Id,
                    CopyType = "MASTER_ORDER_UPDATE",
                    CreatedAt = DateTime.UtcNow,
                };

                await _jobPublisher.PublishMt5PacketBatch(
                    slaveAccount.ServerName,
                    slaveAccount.AccountNumber,
                    new List<object> { broadcastPayload }
                );

                _logger.LogInformation(
                    "CopyOrder: master={M}, slave={S}, lot={L}",
                    masterOrder.Id, slaveAccount.Id, slaveLot
                );
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.Fail("CopyMasterOrderToSlaves failed", ex);
            return TError.NewServer(ex.Message);
        }
    }

    private long GenerateBridgeMagicNumber(long masterOrderId, long slaveAccountId)
    {
        // Simple magic number generation that combines master order id and slave account id
        return ((masterOrderId & 0xFFFFFFFF) << 32) | (slaveAccountId & 0xFFFFFFFF);
    }
}
