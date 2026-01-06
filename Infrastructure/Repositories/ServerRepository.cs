using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Repositories;

public class ServerRepository : BaseRepository<Server>, IServerRepository
{
    private readonly AppDbContext _context;
    private readonly AppLogger<ServerRepository> _logger;

    public ServerRepository(AppDbContext context, AppLogger<ServerRepository> logger) : base(context)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Server?> GetFirstAvailableServer(int maxAccountPerServer)
    {
        return await _context.Server
            .Where(s => s.DeletedAt == null)
            .Where(s => s.Status == ConnectionStatus.Success)
            .Where(s =>
                _context.ServerAccount
                    .Count(sa => sa.ServerId == s.Id && sa.DeletedAt == null)
                < maxAccountPerServer
            )
            .OrderBy(s => s.CreatedAt) // FIFO, or change strategy
            .FirstOrDefaultAsync();
    }
}