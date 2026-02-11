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
}
