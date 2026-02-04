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

    public async Task<(List<Account> data, long total)> GetPaginatedAccounts(
        Account param,
        string? type,
        int page,
        int pageSize
    )
    {
        var query = _context.Accounts
            .Where(a => a.DeletedAt == null)
            .AsQueryable();

        // ===== FILTER BASIC =====
        if (param.Id != 0)
            query = query.Where(a => a.Id == param.Id);

        if (!string.IsNullOrEmpty(param.PlatformName))
            query = query.Where(a => a.PlatformName == param.PlatformName);

        if (param.AccountNumber != 0)
            query = query.Where(a => a.AccountNumber == param.AccountNumber);

        if (!string.IsNullOrEmpty(param.BrokerName))
            query = query.Where(a => a.BrokerName.Contains(param.BrokerName));

        if (!string.IsNullOrEmpty(param.ServerName))
            query = query.Where(a => a.ServerName.Contains(param.ServerName));

        if (param.UserId != 0)
            query = query.Where(a => a.UserId == param.UserId);

        if (!string.IsNullOrEmpty(type))
        {
            if (type == "MASTER")
            {
                query = query.Where(a =>
                    _context.MasterSlaves.Any(ms =>
                        ms.MasterId == a.Id && ms.DeletedAt == null));
            }
            else if (type == "SLAVE")
            {
                query = query.Where(a =>
                    _context.MasterSlaves.Any(ms =>
                        ms.SlaveId == a.Id && ms.DeletedAt == null));
            }
        }

        var total = await query.CountAsync();

        var data = await query
            .Include(a => a.ServerAccount)
            .Include(a => a.MasterRelations)
            .Include(a => a.SlaveRelations)
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, total);
    }
}