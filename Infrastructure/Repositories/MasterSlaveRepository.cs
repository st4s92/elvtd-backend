using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;

namespace Backend.Infrastructure.Repositories;

public class MasterSlaveRepository : BaseRepository<MasterSlave>, IMasterSlaveRepository
{
    private readonly AppDbContext _context;
    private readonly AppLogger<MasterSlaveRepository> _logger;

    public MasterSlaveRepository(AppDbContext context, AppLogger<MasterSlaveRepository> logger) : base(context)
    {
        _context = context;
        _logger = logger;
    }
}