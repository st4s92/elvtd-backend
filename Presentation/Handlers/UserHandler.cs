using Backend.Application.Usecases;
using Backend.Helper;

namespace Backend.Presentation.Handlers;

public class UserHandler
{
    private readonly UserUsecase _usecase;

    public UserHandler(UserUsecase usecase)
    {
        _usecase = usecase;
    }

    public async Task<IResult> GetUsersAsync()
    {
        var (res, terr) = await _usecase.GetUsersAsync();
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }
    
    public async Task<IResult> GetUserByIdAsync(int id)
    {
        var (res, terr) = await _usecase.GetUserByIdAsync(id);
        if(terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }
}