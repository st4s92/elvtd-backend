using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Repositories;

public class MasterSlavePairRepository : BaseRepository<MasterSlavePair>, IMasterSlavePairRepository
{
    private readonly AppDbContext _context;
    private readonly AppLogger<MasterSlavePairRepository> _logger;

    public MasterSlavePairRepository(AppDbContext context, AppLogger<MasterSlavePairRepository> logger) : base(context)
    {
        _context = context;
        _logger = logger;
    }

    public async Task DeleteByMasterSlaveId(long masterSlaveId)
    {
        await _context.Set<MasterSlavePair>()
            .Where(p => p.MasterSlaveId == masterSlaveId)
            .ExecuteDeleteAsync();
    }
}