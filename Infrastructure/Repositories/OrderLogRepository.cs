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
}
