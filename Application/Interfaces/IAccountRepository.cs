using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Interfaces;

public interface IAccountRepository : IRepository<Account>
{
    Task<List<Account>> GetAccountsByServerIpAndStatus(
        string serverIp,
        ConnectionStatus status
    );
    Task<(List<Account> data, long total)> GetPaginatedAccounts(
        Account param,
        string? type,
        int page,
        int pageSize
    );
}