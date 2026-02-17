using System.Linq.Expressions;
using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Repositories;

public class AccountLogRepository : BaseRepository<AccountLog>, IAccountLogRepository
{
    private readonly AppDbContext _context;
    private readonly AppLogger<AccountLogRepository> _logger;

    public AccountLogRepository(AppDbContext context, AppLogger<AccountLogRepository> logger)
        : base(context)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AccountLog>> GetTopAccountLogs(
        long accountId,
        int take
    )
    {
        return await _context.AccountLogs
            .FromSqlRaw(@"
            SELECT al.*
            FROM account_logs al
            INNER JOIN (
                SELECT 
                    DATE(created_at) as log_date,
                    MAX(created_at) as max_time
                FROM account_logs
                WHERE account_id = {0}
                AND deleted_at IS NULL
                GROUP BY DATE(created_at)
            ) daily
            ON DATE(al.created_at) = daily.log_date
            AND al.created_at = daily.max_time
            WHERE al.account_id = {0}
            AND al.deleted_at IS NULL
            ORDER BY al.created_at DESC
            LIMIT {1}
        ", accountId, take)
            .ToListAsync();
    }

}
