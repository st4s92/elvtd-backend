namespace Backend.Helper;

using Backend.Model;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    // === DbSets ===
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AppToken> AppTokens => Set<AppToken>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<AccountLog> AccountLogs => Set<AccountLog>();
    public DbSet<OrderLog> OrderLogs => Set<OrderLog>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<MasterSlave> MasterSlaves => Set<MasterSlave>();
    public DbSet<MasterSlavePair> MasterSlavePairs => Set<MasterSlavePair>();
    public DbSet<MasterSlaveConfig> MasterSlaveConfigs => Set<MasterSlaveConfig>();
    public DbSet<Server> Server => Set<Server>();
    public DbSet<ServerAccount> ServerAccount => Set<ServerAccount>();

    public DbSet<ActiveOrder> ActiveOrders => Set<ActiveOrder>();
    public DbSet<SymbolMap> SymbolMaps => Set<SymbolMap>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>().Property(o => o.Status).HasConversion<int>(); // ensure stored as int

        // === Global soft delete filters ===
        modelBuilder.Entity<User>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Role>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<AppToken>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Account>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Order>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<MasterSlave>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<MasterSlavePair>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<MasterSlaveConfig>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<ServerAccount>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<Server>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<OrderLog>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<AccountLog>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<SymbolMap>().HasQueryFilter(e => e.DeletedAt == null);
        modelBuilder.Entity<SystemLog>().HasQueryFilter(e => e.DeletedAt == null);

        // Unique index: one broker_symbol per broker/server
        modelBuilder.Entity<SymbolMap>()
            .HasIndex(s => new { s.BrokerName, s.BrokerSymbol })
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        // === Relationships ===
        // Account → User
        modelBuilder
            .Entity<Account>()
            .HasOne(a => a.User)
            .WithMany(u => u.Accounts)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<Account>()
            .HasOne(a => a.ServerAccount)
            .WithOne(sa => sa.Account)
            .HasForeignKey<ServerAccount>(sa => sa.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Orders → Accounts
        modelBuilder
            .Entity<Order>()
            .HasOne(a => a.Account)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // ActiveOrders → Accounts
        modelBuilder
            .Entity<ActiveOrder>()
            .HasOne(a => a.Account)
            .WithMany(u => u.ActiveOrders)
            .HasForeignKey(o => o.AccountId)
            .OnDelete(DeleteBehavior.Cascade);

        // Orders self-reference for master-slave link
        modelBuilder
            .Entity<Order>()
            .HasOne(a => a.MasterOrder)
            .WithMany()
            .HasForeignKey(o => o.MasterOrderId)
            .OnDelete(DeleteBehavior.SetNull);

        // ✅ Explicitly define Account ↔ MasterSlave relationships
        modelBuilder
            .Entity<MasterSlave>()
            .HasOne(ms => ms.MasterAccount)
            .WithMany(a => a.MasterRelations)
            .HasForeignKey(ms => ms.MasterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder
            .Entity<MasterSlave>()
            .HasOne(ms => ms.SlaveAccount)
            .WithMany(a => a.SlaveRelations)
            .HasForeignKey(ms => ms.SlaveId)
            .OnDelete(DeleteBehavior.Restrict);

        // MasterSlave → MasterSlaveConfig (1:N)
        modelBuilder
            .Entity<MasterSlaveConfig>()
            .HasOne(cfg => cfg.MasterSlave)
            .WithMany(ms => ms.Configs) // ✅ link to collection in MasterSlave
            .HasForeignKey(cfg => cfg.MasterSlaveId)
            .OnDelete(DeleteBehavior.Cascade);

        // MasterSlave → MasterSlavePair (1:N)
        modelBuilder
            .Entity<MasterSlavePair>()
            .HasOne(pair => pair.MasterSlave)
            .WithMany(ms => ms.Pairs) // ✅ link to collection in MasterSlave
            .HasForeignKey(pair => pair.MasterSlaveId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<ServerAccount>()
            .HasOne(a => a.Server)
            .WithMany(s => s.ServerAccounts)
            .HasForeignKey(sa => sa.ServerId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private void ApplyAuditableRules()
    {
        var entries = ChangeTracker
            .Entries<IAuditableEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                // Avoid changing CreatedAt on update (unless you intend to)
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    public override int SaveChanges()
    {
        ApplyAuditableRules();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default
    )
    {
        ApplyAuditableRules();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // optional convenience override
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(true, cancellationToken);
}
