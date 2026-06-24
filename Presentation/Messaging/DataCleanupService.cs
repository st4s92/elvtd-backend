using Backend.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Backend.Helper;
using Microsoft.Extensions.Hosting;

namespace Backend.Presentation.Messaging;

/// <summary>
/// Runs daily at 03:00 UTC:
/// 1. account_logs older than 10 days: keep only first + last snapshot per account per day
/// 2. Hard-delete all data for soft-deleted accounts (orders, logs, active_orders, server_account)
/// </summary>
public class DataCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public DataCleanupService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Only run on one instance
        if (Environment.GetEnvironmentVariable("ENABLE_STATUS_REPORTER") != "true")
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            // Calculate delay until next 03:00 UTC
            var now = DateTime.UtcNow;
            var next = now.Date.AddHours(3);
            if (now >= next) next = next.AddDays(1);
            var delay = next - now;

            Console.WriteLine($"[DataCleanup] Next run at {next:yyyy-MM-dd HH:mm} UTC (in {delay.TotalHours:F1}h)");
            await Task.Delay(delay, stoppingToken);

            try
            {
                await RunCleanup();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataCleanup] Error: {ex.Message}");
            }
        }
    }

    private async Task RunCleanup()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Console.WriteLine("[DataCleanup] Starting daily cleanup...");

        // ============================================================
        // 1. account_logs: for data older than 10 days, keep only
        //    first and last snapshot per account per day
        // ============================================================
        var cutoff = DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-dd HH:mm:ss");
        var pruneSql = $@"
            DELETE al FROM account_logs al
            INNER JOIN (
                SELECT al2.id
                FROM account_logs al2
                WHERE al2.created_at < '{cutoff}'
                  AND al2.deleted_at IS NULL
                  AND al2.id NOT IN (
                    SELECT id FROM (
                        SELECT MIN(a3.id) as id FROM account_logs a3
                        WHERE a3.created_at < '{cutoff}' AND a3.deleted_at IS NULL
                        GROUP BY a3.account_id, DATE(a3.created_at)
                        UNION
                        SELECT MAX(a4.id) as id FROM account_logs a4
                        WHERE a4.created_at < '{cutoff}' AND a4.deleted_at IS NULL
                        GROUP BY a4.account_id, DATE(a4.created_at)
                    ) keep_ids
                  )
                LIMIT 100000
            ) to_delete ON al.id = to_delete.id";

        int totalPruned = 0;
        int batchPruned;
        do
        {
            batchPruned = await db.Database.ExecuteSqlRawAsync(pruneSql);
            totalPruned += batchPruned;
            if (batchPruned > 0)
                Console.WriteLine($"[DataCleanup] Pruned {batchPruned} account_logs (total: {totalPruned})");
        } while (batchPruned > 0);

        Console.WriteLine($"[DataCleanup] account_logs pruned: {totalPruned} rows removed");

        // ============================================================
        // 2. Hard-delete data for soft-deleted accounts
        // ============================================================
        var deletedAccountIds = await db.Database
            .SqlQueryRaw<long>("SELECT id AS Value FROM accounts WHERE deleted_at IS NOT NULL")
            .ToListAsync();

        if (deletedAccountIds.Count > 0)
        {
            var ids = string.Join(",", deletedAccountIds);

            var tables = new[]
            {
                "account_logs",
                "active_orders",
                "order_logs",
                "orders",
                "server_account",
            };

            foreach (var table in tables)
            {
                var deleted = await db.Database.ExecuteSqlRawAsync(
                    $"DELETE FROM {table} WHERE account_id IN ({ids})");
                if (deleted > 0)
                    Console.WriteLine($"[DataCleanup] {table}: deleted {deleted} rows for {deletedAccountIds.Count} deleted accounts");
            }

            // system_logs: delete by account_id
            var sysDeleted = await db.Database.ExecuteSqlRawAsync(
                $"DELETE FROM system_logs WHERE account_id IN ({ids})");
            if (sysDeleted > 0)
                Console.WriteLine($"[DataCleanup] system_logs: deleted {sysDeleted} rows");

            // Finally hard-delete the accounts themselves
            var accDeleted = await db.Database.ExecuteSqlRawAsync(
                $"DELETE FROM accounts WHERE id IN ({ids})");
            Console.WriteLine($"[DataCleanup] Hard-deleted {accDeleted} accounts");
        }

        Console.WriteLine("[DataCleanup] Daily cleanup complete.");
    }
}
