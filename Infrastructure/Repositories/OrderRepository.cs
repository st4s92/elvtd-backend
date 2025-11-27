using System.Linq.Expressions;
using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Repositories;

public class OrderRepository : BaseRepository<Order>, IOrderRepository
{
    private readonly AppDbContext _context;
    private readonly AppLogger<OrderRepository> _logger;

    public OrderRepository(AppDbContext context, AppLogger<OrderRepository> logger) : base(context)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Order>> GetOrdersWithMaster(Expression<Func<Order, bool>> predicate)
    {
        return await _context.Orders
            .Include(o => o.MasterOrder)
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync();
    }
}