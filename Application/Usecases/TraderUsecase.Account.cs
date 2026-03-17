using System.Linq.Expressions;
using System.Text.Json;
using Backend.Helper;
using Backend.Infrastructure.Repositories;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Application.Usecases;

public partial class TraderUsecase
{
    private static Expression<Func<Account, bool>> FilterAccount(Account param)
    {
        return (
            a =>
                (param.Id == 0 || a.Id == param.Id)
                && (
                    string.IsNullOrEmpty(param.PlatformName) || a.PlatformName == param.PlatformName
                )
                && (param.AccountNumber == 0 || a.AccountNumber == param.AccountNumber)
                && (
                    string.IsNullOrEmpty(param.BrokerName)
                    || a.BrokerName.Contains(param.BrokerName)
                )
                && (
                    string.IsNullOrEmpty(param.ServerName)
                    || a.ServerName.Contains(param.ServerName)
                )
                && (param.UserId == 0 || a.UserId == param.UserId)
                && (string.IsNullOrEmpty(param.Role) || a.Role.ToLower().Contains(param.Role.ToLower()))
        );
    }

    public async Task<(Account?, ITError?)> GetAccount(Account param)
    {
        try
        {
            var data = await _accountRepository.Get(FilterAccount(param));
            if (data == null)
                return (null, TError.NewNotFound("account not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(List<Account>, ITError?)> GetAccounts(Account param)
    {
        try
        {
            var data = await _accountRepository.GetMany(
                FilterAccount(param),
                query => query
                    .Include(a => a.ServerAccount)
                        .ThenInclude(sa => sa!.Server)
                    .Include(a => a.Orders.Where(o => o.OrderCloseAt == null))
                    .Include(a => a.ActiveOrders)
            );
            
            if (data == null)
                return ([], TError.NewNotFound("account not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return ([], TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(List<Account>, long total, ITError?)> GetPaginatedAccounts(
        Account param,
        int page,
        int pageSize,
        string? sortBy = null,
        string? sortOrder = null,
        string? search = null
    )
    {
        try
        {
            var (data, total) = await _accountRepository.GetPaginatedAccounts(
                param,
                page,
                pageSize,
                sortBy,
                sortOrder,
                search
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

    public async Task<(Account?, ITError?)> AddAccount(Account account)
    {
        var existingAccount = new Account
        {
            AccountNumber = account.AccountNumber,
            ServerName = account.ServerName,
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
            return (
                null,
                TError.NewClient("account with the server name and account number already exist")
            );
        }

        try
        {
            var maxAccountPerServer = int.Parse(
                Environment.GetEnvironmentVariable("MAX_SERVER_ACCOUNTS") ?? "10"
            );

            var server = await _serverRepository.GetFirstAvailableServer(maxAccountPerServer);
            if (server == null)
                return (null, TError.NewServer("no available server"));

            Console.WriteLine("available server:");
            Console.WriteLine(server);

            var data = await _accountRepository.Save(account);
            if (data == null)
                return (null, TError.NewServer("cannot create new account"));

            var serverAccount = new ServerAccount { ServerId = server.Id, AccountId = data.Id };
            serverAccount = await _serverAccountRepository.Save(serverAccount);
            if (serverAccount == null)
                return (null, TError.NewServer("cannot create new server account"));

            // message to server
            var job = new TradePlatformCreateJob
            {
                Id = data.Id,
                PlatformName = data.PlatformName,
                AccountNumber = data.AccountNumber,
                AccountPassword = data.AccountPassword,
                BrokerName = data.BrokerName,
                ServerName = data.ServerName,
                UserId = data.UserId,
                Role = data.Role,
                Status = 100,
            };
            Console.WriteLine("try to publish event");

            await _jobPublisher.PublishCreateJob(job);
            await _systemLogUsecase.CreateLog("Account", "Create", data.Id, $"Account {data.AccountNumber} created on server {server.ServerName}.");

            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<ITError?> TriggerInstallByAccountId(long accountID)
    {
        try
        {
            var (masAcc, terr) = await GetServerAccount(
                new ServerAccount { AccountId = accountID }
            );
            if (terr != null)
                return terr;

            var message = masAcc!.Message;
            if (message == "FIRST_BOOT_FAILED")
            {
                message = "";
            }
            await _serverAccountRepository.Update(
                sa => sa.Id == masAcc!.Id,
                sa =>
                {
                    sa.Status = ConnectionStatus.None;
                    sa.Message = message;
                }
            );

            Account? acc;
            (acc, terr) = await GetAccount(new Account { Id = accountID });
            if (terr != null)
                return terr;

            masAcc.Status = ConnectionStatus.None;
            masAcc = await _serverAccountRepository.Save(
                masAcc,
                x => x.AccountId == masAcc.AccountId
            );

            // Load the dedicated server to get its IP for worker targeting
            var server = await _serverRepository.Get(s => s.Id == masAcc.ServerId);
            var serverIp = server?.ServerIp ?? "";

            var job = new TradePlatformCreateJob
            {
                Id = accountID,
                PlatformName = acc!.PlatformName,
                AccountNumber = acc!.AccountNumber,
                AccountPassword = acc!.AccountPassword,
                BrokerName = acc!.BrokerName,
                ServerName = acc!.ServerName,
                UserId = acc!.UserId,
                Role = acc!.Role,
                Status = (int)masAcc.Status,
                Message = masAcc.Message,
                Pid = masAcc.PlatformPid ?? 0,
                ServerIp = serverIp,
            };
            Console.WriteLine($"try to publish event (target server IP: {serverIp})");

            await _jobPublisher.PublishCreateJob(job);
            await _systemLogUsecase.CreateLog("Account", "Install", accountID,
                $"Installation triggered for account {acc.AccountNumber}. Target server: {server?.ServerName ?? "unknown"} ({serverIp})");

            return null;
        }
        catch (Exception ex)
        {
            return TError.NewServer(ex.Message);
        }
    }

    public async Task<ITError?> TriggerRestartByAccountId(long accountID)
    {
        try
        {
            var (masAcc, terr) = await GetServerAccount(
                new ServerAccount { AccountId = accountID }
            );
            if (terr != null)
                return terr;

            Account? acc;
            (acc, terr) = await GetAccount(new Account { Id = accountID });
            if (terr != null)
                return terr;

            // Load the dedicated server to get its IP for worker targeting
            var server = await _serverRepository.Get(s => s.Id == masAcc!.ServerId);
            var serverIp = server?.ServerIp ?? "";

            var job = new TradePlatformCreateJob
            {
                Id = accountID,
                PlatformName = acc!.PlatformName,
                AccountNumber = acc!.AccountNumber,
                AccountPassword = acc!.AccountPassword,
                BrokerName = acc!.BrokerName,
                ServerName = acc!.ServerName,
                UserId = acc!.UserId,
                Role = acc!.Role,
                Status = (int)masAcc!.Status,
                Message = masAcc.Message,
                Pid = masAcc.PlatformPid ?? 0,
                ServerIp = serverIp,
            };
            Console.WriteLine($"try to publish restart event (target server IP: {serverIp})");

            await _jobPublisher.PublishRestartJob(job);
            await _systemLogUsecase.CreateLog("Account", "Restart", accountID,
                $"Restart triggered for account {acc.AccountNumber}. Target server: {server?.ServerName ?? "unknown"} ({serverIp})");

            return null;
        }
        catch (Exception ex)
        {
            return TError.NewServer(ex.Message);
        }
    }

    public async Task<(Account?, ITError?)> UpdateAccountById(long id, Account param)
    {
        try
        {
            var (existing, terr) = await GetAccount(new Account { Id = id });
            if (terr != null || existing == null)
                return (null, terr);

            existing.PlatformName = param.PlatformName;
            existing.AccountNumber = param.AccountNumber;
            existing.BrokerName = param.BrokerName;
            existing.ServerName = param.ServerName;

            if (!string.IsNullOrWhiteSpace(param.AccountPassword))
            {
                existing.AccountPassword = param.AccountPassword;
            }

            // cTrader-spezifische Felder
            if (param.CtidTraderAccountId.HasValue)
            {
                existing.CtidTraderAccountId = param.CtidTraderAccountId;
            }
            if (!string.IsNullOrEmpty(param.AccessToken))
            {
                existing.AccessToken = param.AccessToken;
                existing.RefreshToken = param.RefreshToken;
                existing.TokenExpiredAt = param.TokenExpiredAt;
            }

            var data = await _accountRepository.Save(existing, a => a.Id == id);
            if (data == null)
                return (null, TError.NewServer("cannot save account"));

            await _systemLogUsecase.CreateLog("Account", "Update", id, $"Account {data.AccountNumber} details updated.");

            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }

    public async Task<ITError?> DeleteAccountByID(long id)
    {
        try
        {
            var (existing, terr) = await GetAccount(new Account { Id = id });
            if (terr != null)
                return terr;

            _ = await _accountRepository.Update(
                x => x.Id == id,
                x =>
                {
                    x.DeletedAt = DateTime.Now;
                }
            );

            // Load server account to get the dedicated server IP for worker targeting
            var (serverAccount, _) = await GetServerAccount(
                new ServerAccount { AccountId = id }
            );
            var serverIp = "";
            string serverName = "unknown";
            if (serverAccount != null)
            {
                var server = await _serverRepository.Get(s => s.Id == serverAccount.ServerId);
                serverIp = server?.ServerIp ?? "";
                serverName = server?.ServerName ?? "unknown";
            }

            // message to server
            var job = new TradePlatformCreateJob
            {
                Id = existing!.Id,
                PlatformName = existing.PlatformName,
                AccountNumber = existing.AccountNumber,
                AccountPassword = existing.AccountPassword,
                BrokerName = existing.BrokerName,
                ServerName = existing.ServerName,
                UserId = existing.UserId,
                Role = "SLAVE",
                Status = 100,
                ServerIp = serverIp,
            };
            Console.WriteLine($"try to publish delete account event (target server IP: {serverIp})");

            await _jobPublisher.PublishDeleteJob(job);
            await _systemLogUsecase.CreateLog("Account", "Delete", id,
                $"Account {existing.AccountNumber} soft deleted. Target server: {serverName} ({serverIp})");

            return null;
        }
        catch (Exception ex)
        {
            return TError.NewServer(ex.Message);
        }
    }

    public async Task<(ServerAccount?, ITError?)> UpdateAccountServerData(
        ServerAccountPlatformUpdateRequest param
    )
    {
        try
        {
            var (serverAccount, terr) = await GetServerAccount(
                new ServerAccount { AccountId = param.AccountId }
            );
            if (terr != null)
                return (null, terr);

            if (!string.IsNullOrEmpty(param.ServerIp))
            {
                var server = await _serverRepository.Get(s => s.ServerIp == param.ServerIp);
                if (server != null)
                {
                    serverAccount!.ServerId = server.Id;
                }
            }

            serverAccount!.InstallationPath = param.InstallationPath;
            serverAccount!.Status = param.Status;
            serverAccount!.Message = param.Message;
            serverAccount!.PlatformPid = param.Pid;

            var data = await _serverAccountRepository.Save(
                serverAccount,
                a => a.AccountId == param.AccountId
            );
            if (data == null)
                return (null, TError.NewServer("cannot save server account"));

            // Log job progress/result to SystemLogs for the Job Queue UI
            var level = param.Status == ConnectionStatus.UnknownFail ? "Error" : "Info";
            var action = param.Status == ConnectionStatus.Success ? "JobSuccess" : "JobProgress";
            if (param.Status == ConnectionStatus.UnknownFail) action = "JobFailed";

            await _systemLogUsecase.CreateLog("Account", action, param.AccountId, param.Message ?? "Update received", level);

            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }

    public async Task<(PlatformActivePositionSyncPayload?, ITError?)> GetMasterOrderStatus(
        BridgeListCreateOrderPayload param
    )
    {
        try
        {
            var account = await _accountRepository.Get(a =>
                a.AccountNumber == param.AccountId
                && a.ServerName == param.ServerName
                && a.DeletedAt == null
            );

            if (account == null)
                return (null, TError.NewNotFound("account not found"));

            // 🔴 NEW: Update account state (Heartbeat)
            account.Balance = param.Balance ?? account.Balance;
            account.Equity = param.Equity ?? account.Equity;
            account.Status = ConnectionStatus.Success;
            await _accountRepository.Save(account, a => a.Id == account.Id);

            var activeOrders = await _orderRepository.GetMany(o =>
                o.AccountId == account.Id && o.OrderCloseAt == null
            );

            var payload = new PlatformActivePositionSyncPayload
            {
                AccountNumber = account.AccountNumber,
                ServerName = account.ServerName,
                IsFlushOrder = account.IsFlushOrder,
                Balance = account.Balance,
                Equity = account.Equity,
                CopierVersion = account.CopierVersion,
                PositionList = activeOrders
                    .Select(o => new PlatformPositionDto
                    {
                        OrderTicket = o.OrderTicket,
                        OrderSymbol = o.OrderSymbol,
                        OrderType = o.OrderType,
                        OrderLot = o.OrderLot,
                        OrderPrice = o.OrderPrice ?? 0,
                        OrderOpenAt = o.OrderOpenAt ?? DateTime.UtcNow,
                    })
                    .ToList(),
            };

            // reset to 0
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

            return (payload, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }

    public async Task<ITError?> FlushMasterOrder(long accountId)
    {
        try
        {
            // 1. Set flush flag for MT5 polling
            await _accountRepository.Update(
                a => a.Id == accountId && a.DeletedAt == null,
                a =>
                {
                    a.IsFlushOrder = 1;
                }
            );

            var account = await _accountRepository.Get(a => a.Id == accountId && a.DeletedAt == null);
            if (account == null) return null;

            // 2. Close all master orders in DB
            var masterOpenOrders = await _orderRepository.GetMany(o =>
                o.AccountId == accountId && o.OrderCloseAt == null && o.DeletedAt == null
            );
            if (masterOpenOrders.Any())
            {
                var masterIds = masterOpenOrders.Select(o => o.Id).ToList();
                await _orderRepository.UpdateMany(
                    o => masterIds.Contains(o.Id),
                    o =>
                    {
                        o.OrderCloseAt = DateTime.UtcNow;
                        o.Status = OrderStatus.Complete;
                    }
                );
            }

            // 3. Find all connected slaves and send close commands for their active orders
            var slaveRelations = await _masterSlaveRepository.GetMany(ms => ms.MasterId == accountId);
            var slaveIds = slaveRelations.Select(r => r.SlaveId).Distinct().ToList();

            if (slaveIds.Any())
            {
                var slaveAccounts = await _accountRepository.GetMany(a => slaveIds.Contains(a.Id) && a.DeletedAt == null);
                var slaveActiveOrders = await _activeOrderRepository.GetMany(o => slaveIds.Contains(o.AccountId));

                foreach (var slaveAccount in slaveAccounts)
                {
                    // Set flush flag on slave too
                    await _accountRepository.Update(
                        a => a.Id == slaveAccount.Id,
                        a => { a.IsFlushOrder = 1; }
                    );

                    var ordersForSlave = slaveActiveOrders.Where(o => o.AccountId == slaveAccount.Id && o.OrderTicket != 0).ToList();
                    if (!ordersForSlave.Any()) continue;

                    var closeMessages = ordersForSlave.Select(o => (object)new BridgeOrderBroadcastPayload
                    {
                        SlavePair = o.OrderSymbol,
                        OrderType = o.OrderType,
                        OrderLot = o.OrderLot,
                        OrderTicket = o.OrderTicket,
                        MasterOrderId = o.MasterOrderId,
                        OrderMagic = o.OrderMagic,
                        CopyType = "MASTER_ORDER_DELETE",
                        CreatedAt = DateTime.UtcNow,
                    }).ToList();

                    if (slaveAccount.PlatformName == "cTrader")
                    {
                        await _jobPublisher.PublishCtraderPacketBatch(slaveAccount.AccountNumber, closeMessages);
                    }
                    else
                    {
                        await _jobPublisher.PublishMt5PacketBatch(slaveAccount.ServerName, slaveAccount.AccountNumber, closeMessages);
                    }

                    await _systemLogUsecase.CreateLog("CopyTrade", "FlushSlave", slaveAccount.Id,
                        $"Flush: sent {closeMessages.Count} close commands to slave account {slaveAccount.AccountNumber}");
                }

                // Don't finalize or delete slave active orders here.
                // Let ConfirmBridgeSlaveOrder handle DB cleanup when the
                // trading platform confirms each close.
            }

            await _systemLogUsecase.CreateLog("CopyTrade", "Flush", accountId,
                $"Flush triggered: closed {masterOpenOrders.Count} master orders and sent close to {slaveIds.Count} slave accounts");

            return null;
        }
        catch (Exception ex)
        {
            return TError.NewServer(ex.Message);
        }
    }

    public async Task<ITError?> FlushAllMasterAccounts()
    {
        try
        {
            await _accountRepository.UpdateMany(
                a => a.Role == "MASTER" && a.DeletedAt == null,
                a =>
                {
                    a.IsFlushOrder = 1;
                }
            );
            return null;
        }
        catch (Exception ex)
        {
            return TError.NewServer(ex.Message);
        }
    }

    public async Task<ITError?> FlushAllAccounts()
    {
        try
        {
            await _accountRepository.UpdateMany(
                a => a.DeletedAt == null,
                a =>
                {
                    a.IsFlushOrder = 1;
                }
            );
            return null;
        }
        catch (Exception ex)
        {
            return TError.NewServer(ex.Message);
        }
    }

    public async Task<(AccountDetailDto?, ITError?)> GetAccountDetail(long accountId)
    {
        try
        {
            var account = await _accountRepository.Get(
                a => a.Id == accountId && a.DeletedAt == null
            );

            if (account == null)
                return (null, TError.NewNotFound("account not found"));


            // ===== USER =====
            var (user, terr) = await _userUsecase.GetUser(new User { Id = account.UserId });
            if (user == null)
                return (null, TError.NewNotFound("user not found"));

            // ===== SERVER ACCOUNT =====
            var serverAccount = await _serverAccountRepository.Get(
                sa => sa.AccountId == account.Id && sa.DeletedAt == null
            );

            Server? server = null;
            if (serverAccount != null)
            {
                server = await _serverRepository.Get(
                    s => s.Id == serverAccount.ServerId && s.DeletedAt == null
                );
            }

            // ===== ACTIVE ORDER LOGS (status 600) =====
            var orderLogs = await _orderLogRepository.GetTopOrderLogs(
                o => o.AccountId == account.Id
                    && o.DeletedAt == null,
                20
            );

            // ===== ACCOUNT LOGS (balance chart) =====
            var accountLogs = await _accountLogRepository.GetTopAccountLogs(account.Id, 20);

            // ===== ACTIVE ORDER LOGS (status 600) =====
            List<ActiveOrderDto> activeOrders = new();

            if (account.Role == "MASTER")
            {
                var masterOrders = await _orderRepository.GetMany(
                    o => o.AccountId == account.Id
                        && o.DeletedAt == null
                        && o.OrderCloseAt == null
                        && o.Status == OrderStatus.Success
                );

                activeOrders = masterOrders.Select(o => new ActiveOrderDto
                {
                    Id = o.Id,
                    AccountId = o.AccountId,
                    OrderTicket = o.OrderTicket,
                    OrderSymbol = o.OrderSymbol,
                    OrderType = o.OrderType,
                    OrderLot = o.OrderLot,
                    OrderPrice = o.OrderPrice,
                    OrderProfit = o.OrderProfit
                }).ToList();
            }
            else if (account.Role == "SLAVE")
            {
                var slaveOrders = await _activeOrderRepository.GetMany(
                    o => o.AccountId == account.Id
                        && (o.Status == OrderStatus.Progress || o.Status == OrderStatus.Success)
                );

                activeOrders = slaveOrders.Select(o => new ActiveOrderDto
                {
                    Id = o.Id,
                    AccountId = o.AccountId,
                    OrderTicket = o.OrderTicket,
                    OrderSymbol = o.OrderSymbol,
                    OrderType = o.OrderType,
                    OrderLot = o.OrderLot,
                    OrderPrice = o.OrderPrice,
                    OrderProfit = o.OrderProfit
                }).ToList();
            }

            var result = new AccountDetailDto
            {
                Account = account,
                User = user,
                ServerAccount = serverAccount,
                Server = server,
                OrderLogs = orderLogs.ToList(),
                AccountLogs = accountLogs.ToList(),
                Orders = activeOrders,
            };

            return (result, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }
}
