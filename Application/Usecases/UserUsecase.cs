using System.Linq.Expressions;
using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;

namespace Backend.Application.Usecases;

public class UserUsecase
{
    private readonly IUserRepository _userRepository;
    private readonly AppLogger<TraderUsecase> _logger;
    public UserUsecase(
        IUserRepository userRepository,
        AppLogger<TraderUsecase> logger
    )
    {
        _userRepository = userRepository;
        _logger = logger;
    }
    private static Expression<Func<User, bool>> FilterUser(User param)
    {
        return (
            a =>
                (param.Id == 0 || a.Id == param.Id) &&
                (string.IsNullOrEmpty(param.Name) || a.Name == param.Name) &&
                (string.IsNullOrEmpty(param.Email) || a.Email == param.Email) &&
                (param.RoleId == 0 || a.RoleId == param.RoleId)
        );
    }

    public async Task<(User?, ITError?)> SignIn(LoginRequest payload)
    {
        try
        {
            var user = await _userRepository.Get(a => a.Email == payload.Email && a.Password == payload.Password);
            if(user == null){
                return (null, TError.NewNotFound("user not found"));
            }

            return (user, null);
        }
        catch (System.Exception ex)
        {
            return (null, TError.NewNotFound(ex.Message));
        }
    }
    
    public async Task<(User?, ITError?)> GetUser(User param)
    {
        try
        {
            var data = await _userRepository.Get(FilterUser(param));
            if (data == null)
                return (null, TError.NewNotFound("user not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(List<User>, ITError?)> GetUsers(User param)
    {
        try
        {
            var data = await _userRepository.GetMany(FilterUser(param));
            if (data == null)
                return ([], TError.NewNotFound("user not found"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return ([], TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(List<User>, long total, ITError?)> GetPaginatedUsers(User param, int page, int pageSize)
    {
        try
        {
            var (data, total) = await _userRepository.GetPaginated(FilterUser(param), page, pageSize, q => q.OrderByDescending(o => o.CreatedAt));
            if (data == null)
                return ([], 0, null);
            return (data, total, null);
        }
        catch (Exception ex)
        {
            return ([], 0, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(User?, ITError?)> AddUser(User user)
    {
        var existingUser = new User
        {
            Email = user.Email
        };

        var (_, terr) = await GetUser(existingUser);
        if (terr != null)
        {
            if (terr.IsServer())
            {
                return (null, terr);
            }
        }
        else
        {
            return (null, TError.NewClient("user with the email already exist"));
        }

        try
        {
            var data = await _userRepository.Save(user);
            if (data == null)
                return (null, TError.NewServer("cannot create new user"));
            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer("database error", ex.Message));
        }
    }

    public async Task<(User?, ITError?)> UpdateUserById(long id, User param)
    {   
        try
        {
            var (_, terr) = await GetUser(new User {Id = id});
            if(terr != null)
                return (null, terr);
            
            var data = await _userRepository.Save(param, a => a.Id == id);
            if(data == null)
                return (null, TError.NewServer("cannot save user"));

            return (data, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }
}