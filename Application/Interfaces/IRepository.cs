using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Application.Interfaces
{
    public interface IRepository<T>
        where T : class
    {
        // transaction
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();

        // main code
        Task<T?> Get(Expression<Func<T, bool>> predicate);
        Task<List<T>> GetMany(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IQueryable<T>>? include = null
        );

        Task<(List<T> items, long total)> GetPaginated(
            Expression<Func<T, bool>> predicate,
            int page,
            int pageSize,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy,
            Func<IQueryable<T>, IQueryable<T>>? include = null
        );

        Task<T> Save(T entity, Expression<Func<T, bool>> existsPredicate);

        Task<T> Save(T entity);

        Task SaveBatch(IEnumerable<T> entities);

        Task<bool> Delete(Expression<Func<T, bool>> predicate);

        Task<bool> Update(Expression<Func<T, bool>> predicate, Action<T> updateAction);

        Task<int> UpdateMany(
            Expression<Func<T, bool>> predicate,
            Action<T> updateAction);
    }
}
