using Backend.Application.Usecases;
using Backend.Helper;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Handlers;

public class LogHandler
{
    private readonly SystemLogUsecase _usecase;

    public LogHandler(SystemLogUsecase usecase)
    {
        _usecase = usecase;
    }

    public async Task<IResult> GetPaginatedLogs(SystemLogGetPaginatedPayload query)
    {
        var (res, terr) = await _usecase.GetPaginatedLogs(query);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }
}
