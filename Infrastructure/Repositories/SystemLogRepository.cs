using System.Linq.Expressions;
using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Repositories;

public class SystemLogRepository : ISystemLogRepository
{
    private readonly AppDbContext _context;

    public SystemLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SystemLog?> Get(Expression<Func<SystemLog, bool>> filter)
    {
        return await _context.SystemLogs.Include(l => l.Account).FirstOrDefaultAsync(filter);
    }

    public async Task<IEnumerable<SystemLog>> GetMany(Expression<Func<SystemLog, bool>> filter)
    {
        return await _context.SystemLogs.Include(l => l.Account).Where(filter).ToListAsync();
    }

    public async Task<(IEnumerable<SystemLog>, long total)> GetPaginatedLogs(
        SystemLog param,
        int page,
        int pageSize
    )
    {
        var query = _context.SystemLogs
            .Include(l => l.Account)
                .ThenInclude(a => a!.ServerAccount)
                    .ThenInclude(sa => sa!.Server)
            .Where(a =>
                (param.Id == 0 || a.Id == param.Id)
                && (string.IsNullOrEmpty(param.Category) || a.Category == param.Category)
                && (string.IsNullOrEmpty(param.Action) || a.Action == param.Action)
                && (param.AccountId == null || a.AccountId == param.AccountId)
                && (string.IsNullOrEmpty(param.Level) || a.Level == param.Level)
        );

        if (!string.IsNullOrEmpty(param.Message))
        {
            query = query.Where(a => a.Message.Contains(param.Message)); // Using Message for broad search
        }

        var total = await query.CountAsync();
        var data = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, total);
    }

    public async Task<IEnumerable<SystemLog>> GetTopSystemLogs(
        long accountId,
        int takeRowsCount = 20
    )
    {
        var logs = await _context
            .SystemLogs.Where(l => l.AccountId == accountId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(takeRowsCount)
            .ToListAsync();

        return logs.OrderBy(l => l.CreatedAt).ToList();
    }

    public async Task<SystemLog?> Save(
        SystemLog data,
        Expression<Func<SystemLog, bool>>? filter = null
    )
    {
        if (filter != null)
        {
            var existingData = await _context.SystemLogs.FirstOrDefaultAsync(filter);
            if (existingData != null)
            {
                _context.Entry(existingData).CurrentValues.SetValues(data);
                await _context.SaveChangesAsync();
                return existingData;
            }
        }

        await _context.SystemLogs.AddAsync(data);
        await _context.SaveChangesAsync();
        return data;
    }

    public async Task<SystemLog?> Update(
        Expression<Func<SystemLog, bool>> filter,
        Action<SystemLog> updateAction
    )
    {
        var existingData = await _context.SystemLogs.FirstOrDefaultAsync(filter);
        if (existingData != null)
        {
            updateAction(existingData);
            await _context.SaveChangesAsync();
            return existingData;
        }
        return null;
    }

    public async Task UpdateMany(
        Expression<Func<SystemLog, bool>> filter,
        Action<SystemLog> updateAction
    )
    {
        var existingDataList = await _context.SystemLogs.Where(filter).ToListAsync();

        foreach (var existingData in existingDataList)
        {
            updateAction(existingData);
        }

        await _context.SaveChangesAsync();
    }
}
