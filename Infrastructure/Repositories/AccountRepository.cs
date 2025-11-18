using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;

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

}