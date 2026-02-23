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
                && (string.IsNullOrEmpty(param.Role) || a.Role.Contains(param.Role))
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
            var data = await _accountRepository.GetMany(FilterAccount(param));
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
        int pageSize
    )
    {
        try
        {
            var (data, total) = await _accountRepository.GetPaginatedAccounts(
                param,
                page,
                pageSize
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
            };
            Console.WriteLine("try to publish event");

            await _jobPublisher.PublishCreateJob(job);

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

            var data = await _accountRepository.Save(existing, a => a.Id == id);
            if (data == null)
                return (null, TError.NewServer("cannot save account"));

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
            };
            Console.WriteLine("try to publish delete account event");

            await _jobPublisher.PublishDeleteJob(job);

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
            await _accountRepository.Update(
                a => a.Id == accountId && a.DeletedAt == null,
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
                        && o.Status == OrderStatus.Success
                );

                activeOrders = slaveOrders.Select(o => new ActiveOrderDto
                {
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
