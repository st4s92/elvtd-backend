using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;

namespace Backend.Infrastructure.Repositories;

public class ServerAccountRepository : BaseRepository<ServerAccount>, IServerAccountRepository
{
    private readonly AppDbContext _context;
    private readonly AppLogger<ServerAccountRepository> _logger;

    public ServerAccountRepository(AppDbContext context, AppLogger<ServerAccountRepository> logger) : base(context)
    {
        _context = context;
        _logger = logger;
    }
}