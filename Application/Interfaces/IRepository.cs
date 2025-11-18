using System.Linq.Expressions;

namespace Backend.Application.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<T?> Get(Expression<Func<T, bool>> predicate);
        Task<List<T>> GetMany(Expression<Func<T, bool>> predicate);

        Task<(List<T> items, long total)> GetPaginated(
            Expression<Func<T, bool>> predicate,
            int page,
            int pageSize
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