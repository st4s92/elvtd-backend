using System.Linq.Expressions;
using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Repositories;

public class ActiveOrderRepository : IActiveOrderRepository
{
    private readonly AppDbContext _context;
    private readonly DbSet<ActiveOrder> _db;

    public ActiveOrderRepository(AppDbContext context)
    {
        _context = context;
        _db = context.Set<ActiveOrder>();
    }

    // -------------------------
    // GET
    // -------------------------

    public async Task<ActiveOrder?> Get(Expression<Func<ActiveOrder, bool>> predicate)
    {
        return await _db.AsNoTracking().FirstOrDefaultAsync(predicate);
    }

    public async Task<List<ActiveOrder>> GetMany(Expression<Func<ActiveOrder, bool>> predicate)
    {
        return await _db.AsNoTracking().Where(predicate).ToListAsync();
    }

    public async Task<List<ActiveOrder>> GetByAccountId(long accountId)
    {
        return await _db.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<ActiveOrder> items, long total)> GetPaginatedByAccountId(
        long accountId,
        int page,
        int pageSize
    )
    {
        var query = _db.AsNoTracking().Where(x => x.AccountId == accountId);

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    /// <summary>
    /// Dynamic filter: only non-null / non-default values will be applied
    /// </summary>
    public async Task<List<ActiveOrder>> GetByFilter(ActiveOrder filter)
    {
        IQueryable<ActiveOrder> query = _db.AsNoTracking();

        if (filter.AccountId > 0)
            query = query.Where(x => x.AccountId == filter.AccountId);

        if (filter.AccountNumber > 0)
            query = query.Where(x => x.AccountNumber == filter.AccountNumber);

        if (!string.IsNullOrEmpty(filter.ServerName))
            query = query.Where(x => x.ServerName == filter.ServerName);

        if (filter.MasterOrderId > 0)
            query = query.Where(x => x.MasterOrderId == filter.MasterOrderId);

        if (filter.OrderMagic > 0)
            query = query.Where(x => x.OrderMagic == filter.OrderMagic);

        if (filter.OrderTicket > 0)
            query = query.Where(x => x.OrderTicket == filter.OrderTicket);

        if (!string.IsNullOrEmpty(filter.OrderType))
            query = query.Where(x => x.OrderType == filter.OrderType);

        if (filter.CreatedAt != default)
            query = query.Where(x => x.CreatedAt >= filter.CreatedAt);

        return await query.OrderBy(x => x.CreatedAt).ToListAsync();
    }

    // -------------------------
    // WRITE
    // -------------------------

    public async Task<ActiveOrder> Add(ActiveOrder entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = entity.CreatedAt;

        _db.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task AddBatch(IEnumerable<ActiveOrder> entities)
    {
        var now = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            entity.CreatedAt = now;
            entity.UpdatedAt = now;
            _db.Add(entity);
        }
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Update by primary key (whole object overwrite)
    /// </summary>
    public async Task<bool> Update(ActiveOrder entity)
    {
        var existing = await _db.FirstOrDefaultAsync(x => x.Id == entity.Id);
        if (existing == null)
            return false;

        _context.Entry(existing).CurrentValues.SetValues(entity);
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    // -------------------------
    // DELETE (HARD DELETE)
    // -------------------------
    public async Task<bool> DeleteById(long id)
    {
        var entity = await _db.FirstOrDefaultAsync(a => a.Id == id);
        if (entity == null)
            return false;

        _db.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> DeleteByAccountId(long accountId)
    {
        var rows = await _db.Where(x => x.AccountId == accountId).ExecuteDeleteAsync();

        return rows;
    }

    public async Task<int> DeleteByMasterOrderId(long masterOrderId)
    {
        var rows = await _db.Where(x => x.MasterOrderId == masterOrderId).ExecuteDeleteAsync();

        return rows;
    }

    public async Task<bool> Delete(Expression<Func<ActiveOrder, bool>> predicate)
    {
        var entity = await _db.FirstOrDefaultAsync(predicate);
        if (entity == null)
            return false;

        _db.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateRuntime(long id, Action<ActiveOrder> update)
    {
        var entity = await _db.FirstOrDefaultAsync(x => x.Id == id);
        if (entity == null)
            return false;

        update(entity);
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}
