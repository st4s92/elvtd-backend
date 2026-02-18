using System.Linq.Expressions;
using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Infrastructure.Repositories
{
    public class BaseRepository<T> : IRepository<T>
        where T : class, IAuditableEntity
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _db;

        public BaseRepository(AppDbContext context)
        {
            _context = context;
            _db = context.Set<T>();
        }

        // GET (single)
        public async Task<T?> Get(Expression<Func<T, bool>> predicate)
        {
            return await _db.AsNoTracking().FirstOrDefaultAsync(predicate);
        }

        // GET MANY
        public async Task<List<T>> GetMany(Expression<Func<T, bool>> predicate)
        {
            return await _db.AsNoTracking().Where(predicate).ToListAsync();
        }

        // PAGINATED
        public async Task<(List<T> items, long total)> GetPaginated(
            Expression<Func<T, bool>> predicate,
            int page,
            int pageSize,
            Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null
        )
        {
            var query = _db.AsNoTracking().Where(predicate);

            if (include != null)
                query = include(query);

            if (orderBy != null)
                query = orderBy(query);

            var total = await query.CountAsync();

            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return (items, total);
        }

        // SAVE (CREATE OR UPDATE)
        public async Task<T> Save(T entity, Expression<Func<T, bool>> existsPredicate)
        {
            var existing = await _db.FirstOrDefaultAsync(existsPredicate);

            if (existing == null)
            {
                entity.CreatedAt = DateTime.Now;
                entity.UpdatedAt = entity.CreatedAt;
                _db.Add(entity);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(entity);
                existing.UpdatedAt = DateTime.Now;
                entity = existing;
            }

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<T> Save(T entity)
        {
            _db.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        // DELETE
        public async Task<bool> Delete(Expression<Func<T, bool>> predicate)
        {
            var entity = await _db.FirstOrDefaultAsync(predicate);

            if (entity == null)
                return false;

            _db.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        // unit of work
        private IDbContextTransaction? _transaction;

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
            return _transaction;
        }

        public async Task CommitAsync()
        {
            if (_transaction != null)
                await _transaction.CommitAsync();
        }

        public async Task RollbackAsync()
        {
            if (_transaction != null)
                await _transaction.RollbackAsync();
        }

        public async Task<bool> Update(Expression<Func<T, bool>> predicate, Action<T> updateAction)
        {
            var entity = await _db.FirstOrDefaultAsync(predicate);
            if (entity == null)
                return false;

            updateAction(entity);
            entity.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> UpdateMany(
            Expression<Func<T, bool>> predicate,
            Action<T> updateAction)
        {
            var entities = await _db.Where(predicate).ToListAsync();

            foreach (var entity in entities)
            {
                updateAction(entity);
                entity.UpdatedAt = DateTime.UtcNow;
            }

            return await _context.SaveChangesAsync();
        }
    }
}
