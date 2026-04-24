using Backend.Model;
using System.Linq.Expressions;

namespace Backend.Application.Interfaces;

public interface IActiveOrderRepository
{
    // -------------------------
    // GET
    // -------------------------

    Task<ActiveOrder?> Get(Expression<Func<ActiveOrder, bool>> predicate);

    Task<List<ActiveOrder>> GetMany(Expression<Func<ActiveOrder, bool>> predicate);

    Task<List<ActiveOrder>> GetByAccountId(long accountId);

    Task<(List<ActiveOrder> items, long total)> GetPaginatedByAccountId(
        long accountId,
        int page,
        int pageSize
    );

    Task<List<ActiveOrder>> GetByFilter(ActiveOrder filter);

    // -------------------------
    // WRITE
    // -------------------------

    Task<ActiveOrder> Add(ActiveOrder entity);

    Task AddBatch(IEnumerable<ActiveOrder> entities);

    Task<bool> Update(ActiveOrder entity);

    // -------------------------
    // DELETE (HARD DELETE)
    // -------------------------

    Task<bool> DeleteById(long id);

    Task<int> DeleteByAccountId(long accountId);

    Task<int> DeleteByMasterOrderId(long masterOrderId);

    Task<bool> Delete(Expression<Func<ActiveOrder, bool>> predicate);

    Task<bool> UpdateRuntime(long id, Action<ActiveOrder> update);
}
