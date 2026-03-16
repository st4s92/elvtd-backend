using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;

namespace Backend.Infrastructure.Repositories;

public class TradingRepository : ITradingRepository
{
    private readonly AppDbContext _context;
    private readonly AppLogger<AccountRepository> _logger;

    public TradingRepository(AppDbContext context, AppLogger<AccountRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(AppToken?, ITError?)> SaveToken(AppToken token)
    {
        AppToken? res = null;
        ITError? terr = null;
        try
        {
            var existing = await _context.AppTokens
                .FirstOrDefaultAsync(t =>
                    t.Platform == token.Platform &&
                    t.PlatformId == token.PlatformId &&
                    t.DeletedAt == null);

            if (existing is not null)
            {
                existing.AuthToken = token.AuthToken;
                existing.RefreshToken = token.RefreshToken;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.ExpiredAt = token.ExpiredAt;
            }
            else
            {
                // Insert new token
                token.CreatedAt = DateTime.UtcNow;
                token.UpdatedAt = DateTime.UtcNow;
                _context.AppTokens.Add(token);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            terr = TError.NewServer(ex.Message);
        }
        return (res, terr);
    }

    public async Task<AppToken?> GetToken(string platform, string platformId)
    {
        return await _context.AppTokens
            .FirstOrDefaultAsync(t =>
                t.Platform == platform &&
                t.PlatformId == platformId);
    }
}