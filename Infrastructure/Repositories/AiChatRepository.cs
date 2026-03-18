using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Repositories;

public class AiChatRepository
{
    private readonly AppDbContext _context;

    public AiChatRepository(AppDbContext context)
    {
        _context = context;
    }

    // Sessions
    public async Task<List<AiChatSession>> GetSessionsByUser(long userId)
    {
        return await _context.AiChatSessions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();
    }

    public async Task<AiChatSession?> GetSession(long id)
    {
        return await _context.AiChatSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<AiChatSession> CreateSession(AiChatSession session)
    {
        session.CreatedAt = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        _context.AiChatSessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task UpdateSessionTitle(long id, string title)
    {
        var session = await _context.AiChatSessions.FirstOrDefaultAsync(s => s.Id == id);
        if (session != null)
        {
            session.Title = title;
            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteSession(long id)
    {
        var session = await _context.AiChatSessions.FirstOrDefaultAsync(s => s.Id == id);
        if (session != null)
        {
            _context.AiChatSessions.Remove(session);
            await _context.SaveChangesAsync();
        }
    }

    // Messages
    public async Task<List<AiChatMessage>> GetMessages(long sessionId, int limit = 50)
    {
        return await _context.AiChatMessages
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderBy(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<AiChatMessage>> GetRecentMessages(long sessionId, int count = 20)
    {
        return await _context.AiChatMessages
            .AsNoTracking()
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<AiChatMessage> SaveMessage(AiChatMessage message)
    {
        message.CreatedAt = DateTime.UtcNow;
        _context.AiChatMessages.Add(message);
        await _context.SaveChangesAsync();

        // Update session timestamp
        var session = await _context.AiChatSessions.FirstOrDefaultAsync(s => s.Id == message.SessionId);
        if (session != null)
        {
            session.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return message;
    }
}
