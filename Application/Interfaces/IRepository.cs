using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Application.Interfaces
{
    public interface IRepository<T> where T : class
    {
        // transaction
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();

        // main code
        Task<T?> Get(Expression<Func<T, bool>> predicate);
        Task<List<T>> GetMany(Expression<Func<T, bool>> predicate);

        Task<(List<T> items, long total)> GetPaginated(
            Expression<Func<T, bool>> predicate,
            int page,
            int pageSize,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy
        );

        Task<T> Save(
            T entity,
            Expression<Func<T, bool>> existsPredicate
        );

        Task<T> Save(
            T entity
        );

        Task<bool> Delete(Expression<Func<T, bool>> predicate);
    }
}