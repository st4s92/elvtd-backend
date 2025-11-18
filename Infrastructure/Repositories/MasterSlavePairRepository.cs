using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;

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
}