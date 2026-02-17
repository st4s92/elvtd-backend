using System.Linq.Expressions;
using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Interfaces;

public interface IAccountLogRepository : IRepository<AccountLog>
{
    Task<List<AccountLog>> GetTopAccountLogs(
        long accountId,
        int take
    );
}
