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

    public async Task<Server?> GetFirstAvailableServer(int maxAccountPerServer, string platformName)
    {
        return await _context.Server
            .Where(s => s.DeletedAt == null)
            .Where(s => s.Status == ConnectionStatus.Success)
            .Where(s =>
                _context.ServerAccount
                    .Count(sa => sa.ServerId == s.Id && sa.DeletedAt == null)
                < maxAccountPerServer
            )
            .Where(s => s.ServerIp.StartsWith("192.168.")) // nur MT-Worker-VMs (nicht ctrader-bridge)
            // Plattform-Trennung: nur Server, die bereits >=1 Account derselben Plattform hosten
            // (MT5 nur auf MT5-VMs .10x, MT4 nur auf MT4-VMs .9x)
            .Where(s =>
                _context.ServerAccount.Any(sa =>
                    sa.ServerId == s.Id && sa.DeletedAt == null
                    && sa.Account.PlatformName == platformName
                )
            )
            .OrderBy(s =>
                _context.ServerAccount
                    .Count(sa => sa.ServerId == s.Id && sa.DeletedAt == null)
            ) // least-loaded: Server mit wenigsten Accounts zuerst
            .ThenBy(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }
}