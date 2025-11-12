using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Usecases;

public class UserUsecase
{
    private readonly IUserRepository _repo;

    public UserUsecase(IUserRepository repo)
    {
        _repo = repo;
    }

    public async Task<(IEnumerable<User>, ITError?)> GetUsersAsync()
    {
        return await _repo.GetUsersAsync();
    }

    public async Task<(User?, ITError?)> GetUserByIdAsync(int id)
    {
        return await _repo.GetUserByIdAsync(id);
    }
}