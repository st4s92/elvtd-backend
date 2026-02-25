using System.Linq.Expressions;
using Backend.Model;

namespace Backend.Infrastructure.Repositories;

public interface ISystemLogRepository
{
    Task<SystemLog?> Save(SystemLog data, Expression<Func<SystemLog, bool>>? filter = null);
    Task<SystemLog?> Get(Expression<Func<SystemLog, bool>> filter);
    Task<IEnumerable<SystemLog>> GetMany(Expression<Func<SystemLog, bool>> filter);

    Task<(IEnumerable<SystemLog>, long total)> GetPaginatedLogs(
        SystemLog param,
        int page,
        int pageSize
    );

    Task<IEnumerable<SystemLog>> GetTopSystemLogs(long accountId, int takeRowsCount = 20);

    Task<SystemLog?> Update(
        Expression<Func<SystemLog, bool>> filter,
        Action<SystemLog> updateAction
    );
    Task UpdateMany(Expression<Func<SystemLog, bool>> filter, Action<SystemLog> updateAction);
}
