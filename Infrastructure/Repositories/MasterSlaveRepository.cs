using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

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

    public async Task SoftDeleteByAccountId(long accountId, string role)
    {
        IQueryable<MasterSlave> query = _context.MasterSlaves
        .Where(m => m.DeletedAt == null);

        if (role == "SLAVE")
        {
            query = query.Where(m => m.SlaveId == accountId);
        }
        else if (role == "MASTER")
        {
            query = query.Where(m => m.MasterId == accountId);
        }

        var relations = await query.ToListAsync();

        foreach (var rel in relations)
        {
            rel.DeletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}