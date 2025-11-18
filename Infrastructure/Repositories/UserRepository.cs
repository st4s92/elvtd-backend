using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;

namespace Backend.Infrastructure.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) : base(context)
    {
        _context = context;
    }
}