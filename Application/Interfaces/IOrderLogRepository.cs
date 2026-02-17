using System.Linq.Expressions;
using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Interfaces;

public interface IOrderLogRepository : IRepository<OrderLog>
{
    Task<List<OrderLog>> GetTopOrderLogs(
        Expression<Func<OrderLog, bool>> predicate,
        int take
    );
}
