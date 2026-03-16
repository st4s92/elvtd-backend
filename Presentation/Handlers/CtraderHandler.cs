using Backend.Application.Usecases;
using Backend.Helper;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Handlers;

public class CtraderHandler
{
    private readonly CtraderUsecase _usecase;

    public CtraderHandler(
        CtraderUsecase usecase)
    {
        _usecase = usecase;
    }

    public IResult GetAuthPage()
    {
        var (res, terr) = _usecase.GetAuthPage();
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> ProcessAuthCallback(string code, long userId)
    {
        var (res, terr) = await _usecase.ProcessAuthCallback(code, userId);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetTokenForAccount(long accountId)
    {
        var (token, terr) = await _usecase.GetTokenForAccount(accountId);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(new
        {
            token = token!.AuthToken,
            refresh_token = token.RefreshToken,
            expired_at = token.ExpiredAt.ToString("o")
        });
    }
}
