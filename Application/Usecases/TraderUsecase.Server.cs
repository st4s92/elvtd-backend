using System.Linq.Expressions;
using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Usecases;

public partial class TraderUsecase
{
    private static Expression<Func<Server, bool>> FilterServer(Server param)
    {
        return (
            a =>
                (param.Id == 0 || a.Id == param.Id) &&
                (string.IsNullOrEmpty(param.ServerIp) || a.ServerName == param.ServerIp) &&
                (string.IsNullOrEmpty(param.ServerName) || (a.ServerName != null && a.ServerName.Contains(param.ServerName))) &&
                (string.IsNullOrEmpty(param.ServerOs) || (a.ServerOs != null && a.ServerOs.Contains(param.ServerOs)))
        );
    }
    public async Task<(Server?, ITError?)> GetServer(Server param)
    {
        try
        {
            var data = await _serverRepository.Get(FilterServer(param));
            if (data == null)
                return (null, TError.NewNotFound("server not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(List<Server>, ITError?)> GetServers(Server param)
    {
        try
        {
            var data = await _serverRepository.GetMany(FilterServer(param));
            if (data == null)
                return ([], TError.NewNotFound("server not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return ([], TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(List<Server>, long total, ITError?)> GetPaginatedServers(Server param, int page, int pageSize)
    {
        try
        {
            var (data, total) = await _serverRepository.GetPaginated(FilterServer(param), page, pageSize, q => q.OrderByDescending(o => o.CreatedAt));
            if (data == null)
                return ([], 0, null);
            return (data, total, null);
        }
        catch (Exception ex)
        {
            return ([], 0, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(Server?, ITError?)> AddServer(Server server)
    {
        var existingServer = new Server
        {
            ServerIp = server.ServerIp,
        };

        var (_, terr) = await GetServer(existingServer);
        if (terr != null)
        {
            if (terr.IsServer())
            {
                return (null, terr);
            }
        }
        else
        {
            return (null, TError.NewClient("server with the server name and server number already exist"));
        }

        try
        {
            var data = await _serverRepository.Save(server);
            if (data == null)
                return (null, TError.NewServer("cannot create new server"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(Server?, ITError?)> UpdateServerById(long id, Server param)
    {
        try
        {
            var (_, terr) = await GetServer(new Server { Id = id });
            if (terr != null)
                return (null, terr);

            var data = await _serverRepository.Save(param, a => a.Id == id);
            if (data == null)
                return (null, TError.NewServer("cannot save server"));

            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }

    /*  =============
        Server Account
        ============== */

    private static Expression<Func<ServerAccount, bool>> FilterServerAccount(ServerAccount param)
    {
        return (
            a =>
                (param.Id == 0 || a.Id == param.Id) &&
                (param.AccountId == 0 || a.AccountId == param.AccountId) &&
                (param.ServerId == 0 || a.ServerId == param.ServerId)
        );
    }
    public async Task<(ServerAccount?, ITError?)> GetServerAccount(ServerAccount param)
    {
        try
        {
            var data = await _serverAccountRepository.Get(FilterServerAccount(param));
            if (data == null)
                return (null, TError.NewNotFound("server account not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(List<ServerAccount>, ITError?)> GetServerAccounts(ServerAccount param)
    {
        try
        {
            var data = await _serverAccountRepository.GetMany(FilterServerAccount(param));
            if (data == null)
                return ([], TError.NewNotFound("server account not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return ([], TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(ServerAccount?, ITError?)> AddServerAccount(ServerAccount serverAccount)
    {
        try
        {
            var existing = await _serverAccountRepository.Get(a =>
                (a.ServerId == serverAccount.ServerId) &&
                (a.AccountId == serverAccount.AccountId)
            );

            if (existing != null)
            {
                return (null, TError.NewServer("server id and account id existed"));
            }

            var getAccounts = await _serverAccountRepository.GetMany(a =>
                a.ServerId == serverAccount.ServerId
            );
            var maxAccountPerServer = int.Parse(Environment.GetEnvironmentVariable("MAX_SERVER_ACCOUNTS") ?? "10");
            if (getAccounts.Count >= maxAccountPerServer)
            {
                return (null, TError.NewServer("unsufficient server quota"));
            }

            var data = await _serverAccountRepository.Save(serverAccount);
            if (data == null)
                return (null, TError.NewServer("cannot create new server account"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(ServerAccount?, ITError?)> UpdateServerAccountById(long id, ServerAccount param)
    {
        try
        {
            var data = await _serverAccountRepository.Get(a => a.Id == id);
            if (data == null)
                return (null, TError.NewNotFound("server account not found"));

            var updatedData = await _serverAccountRepository.Save(param, a => a.Id == id);
            if (data == null)
                return (null, TError.NewServer("cannot save server account"));

            return (updatedData, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }

    public async Task<(Server?, ITError?)> UpdateHealthCheck(
            ServerHeartbeatRequest param
        )
    {
        try
        {
            var server = await _serverRepository.Get(a => a.ServerIp == param.Ip);
            if (server == null)
            {
                // AUTO-REGISTER new server
                var serverName = !string.IsNullOrEmpty(param.Name)
                    ? param.Name
                    : $"Worker_{param.Ip.Replace(".", "_")}";
                var newServer = new Server
                {
                    ServerIp = param.Ip,
                    ServerName = serverName,
                    Status = param.Status,
                    ServerOs = "Auto-Registered",
                    ActiveTerminals = param.ActiveTerminals,
                    CpuUsage = param.CpuUsage,
                    RamUsage = param.RamUsage,
                    UptimeString = param.Uptime,
                };
                var (created, terr) = await AddServer(newServer);
                if (terr != null) return (null, terr);
                return (created, null);
            }

            server.Status = param.Status;
            server.ActiveTerminals = param.ActiveTerminals;
            server.CpuUsage = param.CpuUsage;
            server.RamUsage = param.RamUsage;
            server.UptimeString = param.Uptime;
            // Update server name if the worker sends one
            if (!string.IsNullOrEmpty(param.Name))
            {
                server.ServerName = param.Name;
            }
            var (_, terrs) = await UpdateServerById(server.Id, server);
            if (terrs != null)
            {
                return (server, TError.NewServer("cannot save server status"));
            }

            return (server, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }

    public async Task<ITError?> DeleteServerByID(long id)
    {
        try
        {
            var (existing, terr) = await GetServer(new Server { Id = id });
            if (terr != null)
                return terr;

            await _serverRepository.Update(
                x => x.Id == id,
                x =>
                {
                    x.DeletedAt = DateTime.Now;
                }
            );

            return null;
        }
        catch (Exception ex)
        {
            return TError.NewServer("database error", ex.Message);
        }
    }

    /// <summary>
    /// Removes server assignments for all accounts that haven't been updated
    /// within the given number of minutes. This frees them up for reassignment
    /// to a new server on next install/restart.
    /// </summary>
    public async Task<(int count, ITError?)> ReassignStaleAccounts(int staleMinutes = 60, long? accountId = null)
    {
        try
        {
            // Use UpdateMany with tracked entities for reliable updates
            int count;
            if (accountId.HasValue && accountId.Value > 0)
            {
                count = await _serverAccountRepository.UpdateMany(
                    sa => sa.DeletedAt == null && sa.AccountId == accountId.Value,
                    sa => sa.DeletedAt = DateTime.UtcNow
                );
            }
            else
            {
                var threshold = DateTime.UtcNow.AddMinutes(-staleMinutes);
                count = await _serverAccountRepository.UpdateMany(
                    sa => sa.DeletedAt == null && sa.Account.UpdatedAt < threshold,
                    sa => sa.DeletedAt = DateTime.UtcNow
                );
            }

            var logMsg = accountId.HasValue
                ? $"ReassignStaleAccounts: soft-deleted {count} server_account entries for accountId={accountId.Value}"
                : $"ReassignStaleAccounts: soft-deleted {count} server_account entries (stale > {staleMinutes}min)";
            _logger.Info(logMsg);
            return (count, null);
        }
        catch (Exception ex)
        {
            return (0, TError.NewServer("database error", ex.Message));
        }
    }
}