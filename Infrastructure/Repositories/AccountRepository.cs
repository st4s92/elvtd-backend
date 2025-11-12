using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;

namespace Backend.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _context;
    private readonly AppLogger<AccountRepository> _logger;

    public AccountRepository(AppDbContext context, AppLogger<AccountRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(Account?, ITError?)> SaveAccount(Account account)
    {
        Account? res = null;
        ITError? terr = null;

        try
        {
            var existing = await _context.Accounts
                .FirstOrDefaultAsync(t =>
                    t.AccountNumber == account.AccountNumber &&
                    t.BrokerName == account.BrokerName &&
                    t.DeletedAt == null);

            if (existing is not null)
            {
                existing = account;
                _context.Accounts.Update(existing);
                res = existing;
            }
            else
            {
                account.CreatedAt = DateTime.UtcNow;
                account.UpdatedAt = DateTime.UtcNow;
                _context.Accounts.Add(account);
                res = account;
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            terr = TError.NewServer(ex.Message);
        }

        return (res, terr);
    }

    public async Task<(List<Account>, ITError?)> GetAccounts(Account filter)
    {
        List<Account> res = new();
        ITError? terr = null;

        try
        {
            var query = ApplyFilters(_context.Accounts.Where(o => o.DeletedAt == null), filter);
            res = await query.ToListAsync();
        }
        catch (Exception ex)
        {
            terr = TError.NewServer(ex.Message);
        }

        return (res, terr);
    }

    public async Task<(Account?, ITError?)> GetAccount(Account filter)
    {
        Account? res = new();
        ITError? terr = null;

        try
        {
            var query = ApplyFilters(_context.Accounts.Where(o => o.DeletedAt == null), filter);
            res = await query.FirstOrDefaultAsync();
            if (res == null)
            {
                terr = TError.NewNotFound("account not found");
            }
        }
        catch (Exception ex)
        {
            terr = TError.NewServer(ex.Message);
        }

        return (res, terr);
    }

    private static IQueryable<Account> ApplyFilters(IQueryable<Account> query, Account filter)
    {
        if (filter.Id != 0)
            query = query.Where(o => o.Id == filter.Id);

        if (!string.IsNullOrWhiteSpace(filter.PlatformName))
            query = query.Where(o => o.PlatformName != null && o.PlatformName.Contains(filter.PlatformName));

        if (!string.IsNullOrWhiteSpace(filter.PlatformPath))
            query = query.Where(o => o.PlatformPath != null && o.PlatformPath.Contains(filter.PlatformPath));

        if (filter.AccountNumber != 0)
            query = query.Where(o => o.AccountNumber == filter.AccountNumber);

        if (!string.IsNullOrWhiteSpace(filter.BrokerName))
            query = query.Where(o => o.BrokerName != null && o.BrokerName.Contains(filter.BrokerName));

        if (!string.IsNullOrWhiteSpace(filter.ServerName))
            query = query.Where(o => o.ServerName != null && o.ServerName.Contains(filter.ServerName));

        if (filter.UserId != 0)
            query = query.Where(o => o.UserId == filter.UserId);

        return query;
    }

    public async Task<(List<MasterSlave>, ITError?)> GetMasterSlaves(MasterSlave filter)
    {
        List<MasterSlave> res = new();
        ITError? terr = null;

        try
        {
            var query = ApplyMasterSlaveFilters(
                _context.MasterSlaves
                    .Include(ms => ms.MasterAccount)
                    .Include(ms => ms.SlaveAccount)
                    .Where(o => o.DeletedAt == null), filter
            );
            res = await query.ToListAsync();
        }
        catch (Exception ex)
        {
            terr = TError.NewServer(ex.Message);
        }

        return (res, terr);
    }

    private static IQueryable<MasterSlave> ApplyMasterSlaveFilters(IQueryable<MasterSlave> query, MasterSlave filter)
    {
        if (filter.Id != 0)
            query = query.Where(o => o.Id == filter.Id);

        if (filter.MasterId != 0)
            query = query.Where(o => o.MasterId == filter.MasterId);

        if (filter.SlaveId != 0)
            query = query.Where(o => o.SlaveId == filter.SlaveId);

        return query;
    }
    
    public async Task<(MasterSlavePair?, ITError?)> GetMasterSlavePair(MasterSlavePair filter)
    {
        MasterSlavePair? res = null;
        ITError? terr = null;

        try
        {
            var query = ApplyMasterSlavePairFilters(_context.MasterSlavePairs.Where(o => o.DeletedAt == null), filter);
            res = await query.FirstOrDefaultAsync();
            if (res == null)
            {
                terr = TError.NewNotFound("master slave not found");
            }
        }
        catch (Exception ex)
        {
            terr = TError.NewServer(ex.Message);
        }

        return (res, terr);
    }

    private static IQueryable<MasterSlavePair> ApplyMasterSlavePairFilters(IQueryable<MasterSlavePair> query, MasterSlavePair filter)
    {
        if (filter.Id != 0)
            query = query.Where(o => o.Id == filter.Id);

        if (filter.MasterSlaveId != 0)
            query = query.Where(o => o.MasterSlaveId == filter.MasterSlaveId);

        if (!string.IsNullOrWhiteSpace(filter.MasterPair))
            query = query.Where(o => o.MasterPair != null && o.MasterPair == filter.MasterPair);

        if (!string.IsNullOrWhiteSpace(filter.SlavePair))
            query = query.Where(o => o.SlavePair != null && o.SlavePair == filter.SlavePair);

        return query;
    }
    
    public async Task<(MasterSlaveConfig?, ITError?)> GetMasterSlaveConfig(MasterSlaveConfig filter)
    {
        MasterSlaveConfig? res = null;
        ITError? terr = null;

        try
        {
            var query = ApplyMasterSlaveConfigs(_context.MasterSlaveConfigs.Where(o => o.DeletedAt == null), filter);
            res = await query.FirstOrDefaultAsync();
            if (res == null)
            {
                terr = TError.NewNotFound("master slave not found");
            }
        }
        catch (Exception ex)
        {
            terr = TError.NewServer(ex.Message);
        }

        return (res, terr);
    }

    private static IQueryable<MasterSlaveConfig> ApplyMasterSlaveConfigs(IQueryable<MasterSlaveConfig> query, MasterSlaveConfig filter)
    {
        if (filter.Id != 0)
            query = query.Where(o => o.Id == filter.Id);

        if (filter.MasterSlaveId != 0)
            query = query.Where(o => o.MasterSlaveId == filter.MasterSlaveId);

        return query;
    }
}