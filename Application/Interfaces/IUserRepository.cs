using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Interfaces;

public interface IUserRepository
{
    public Task<(IEnumerable<User>, ITError?)> GetUsersAsync();
    public Task<(User?, ITError?)> GetUserByIdAsync(int id);
    public Task<(User?, ITError?)> GetUserByEmailAsync(string email);
}