using Backend.Application.Usecases;
using Backend.Helper;
using Backend.Model;

namespace Backend.Presentation.Handlers;

public class UserHandler
{
    private readonly UserUsecase _usecase;

    public UserHandler(UserUsecase usecase)
    {
        _usecase = usecase;
    }

    public async Task<IResult> AddUser(UserPayload userPayload)
    {
        var user = new User
        {
            Email = userPayload.Email ?? "",
            Name = userPayload.Name ?? "",
            RoleId = userPayload.RoleId ?? 0,
            Password = userPayload.Password ?? ""
        };

        if (string.IsNullOrEmpty(user.Email))
        {
            return Response.Json(TError.NewClient("User Email should be filled"));
        }
        if (string.IsNullOrEmpty(user.Name))
        {
            return Response.Json(TError.NewClient("User name should be filled"));
        }
        if (string.IsNullOrEmpty(user.Password))
        {
            return Response.Json(TError.NewClient("User Password should be filled"));
        }
        if (user.RoleId <= 0)
        {
            return Response.Json(TError.NewClient("Role should be filled"));
        }
        var (res, terr) = await _usecase.AddUser(user);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetUser(int id)
    {
        var (res, terr) = await _usecase.GetUser(new User { Id = id });
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetUsers(UserGetPayload query)
    {
        var userFilter = new User
        {
            Id = query.Id ?? 0,
            Email = query.Email ?? "",
            Name = query.Name ?? "",
            RoleId = query.RoleId ?? 0,
            Password = query.Password ?? ""
        };

        var (res, terr) = await _usecase.GetUsers(userFilter);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetPaginatedUsers(UserGetPaginatedPayload query)
    {
        var userFilter = new User
        {
            Id = query.Id ?? 0,
            Email = query.Email ?? "",
            Name = query.Name ?? "",
            RoleId = query.RoleId ?? 0,
            Password = query.Password ?? ""
        };

        var (res, total, terr) = await _usecase.GetPaginatedUsers(userFilter, query.Page, query.PageSize);
        if (terr != null)
        {
            return Response.Json(terr);
        }

        var resp = new GetPaginatedResponse<User>
        {
            Data = res,
            Total = total
        };
        return Response.Json(resp);
    }
}