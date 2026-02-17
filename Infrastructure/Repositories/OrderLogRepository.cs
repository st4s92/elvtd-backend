using System.Linq.Expressions;
using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Repositories;

public class OrderLogRepository : BaseRepository<OrderLog>, IOrderLogRepository
{
    private readonly AppDbContext _context;
    private readonly AppLogger<OrderRepository> _logger;

    public OrderLogRepository(AppDbContext context, AppLogger<OrderRepository> logger)
        : base(context)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<OrderLog>> GetTopOrderLogs(
        Expression<Func<OrderLog, bool>> predicate,
        int take
    )
    {
        return await _context.OrderLogs
            .Where(predicate)
            .OrderByDescending(o => o.CreatedAt)
            .Take(take)
            .ToListAsync();
    }
}
