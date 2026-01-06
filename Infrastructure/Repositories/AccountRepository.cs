using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Repositories;

public class AccountRepository : BaseRepository<Account>, IAccountRepository
{
    private readonly AppDbContext _context;
    private readonly AppLogger<AccountRepository> _logger;

    public AccountRepository(AppDbContext context, AppLogger<AccountRepository> logger) : base(context)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Account>> GetAccountsByServerIpAndStatus(
        string serverIp,
        ConnectionStatus status
    )
    {
        return await _context.ServerAccount
            .Where(x =>
                x.Server != null &&
                x.Server.ServerIp == serverIp &&
                x.Status == status
            )
            .Select(x => x.Account)
            .Distinct()
            .ToListAsync();
    }
}