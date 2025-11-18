using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;

namespace Backend.Infrastructure.Repositories;

public class MasterSlaveConfigRepository : BaseRepository<MasterSlaveConfig>, IMasterSlaveConfigRepository
{
    private readonly AppDbContext _context;
    private readonly AppLogger<MasterSlaveConfigRepository> _logger;

    public MasterSlaveConfigRepository(AppDbContext context, AppLogger<MasterSlaveConfigRepository> logger) : base(context)
    {
        _context = context;
        _logger = logger;
    }
}