using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Update;

namespace Backend.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<User>, ITError?)> GetUsersAsync()
    {
        List<User> res = new();
        ITError? terr = null;
        try
        {
            res = await _context.Users.ToListAsync();
        }
        catch (Exception ex)
        {
            terr = TError.NewServer(ex.Message);
        }
        return (res, terr);
    }

    public async Task<(User?, ITError?)> GetUserByIdAsync(int id)
    {
        User? res = null;
        ITError? terr = null;
        try
        {
            res = await _context.Users.FindAsync(id);
            if (res == null)
            {
                terr = TError.NewNotFound($"user {id} was not found");
            }
        }
        catch (Exception ex)
        {
            terr = TError.NewServer(ex.Message);
        }
        return (res, terr);
    }

    public async Task<(User?, ITError?)> GetUserByEmailAsync(string email)
    {
        User? res = null;
        ITError? terr = null;
        try
        {
            res = await _context.Users.FirstAsync(u => u.Email == email);
            if (res == null)
            {
                terr = TError.NewNotFound($"user {email} was not found");
            }
        }
        catch (Exception ex)
        {
            terr = TError.NewServer(ex.Message);
        }
        return (res, terr);
    }
}