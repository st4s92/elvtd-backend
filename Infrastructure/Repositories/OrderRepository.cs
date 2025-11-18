using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;

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
}