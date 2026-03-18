using Backend.Model;
using Backend.Presentation.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Routes;

public static class LogRoutes
{
    public static void MapLogRoutes(this RouteGroupBuilder group)
    {
        group
            .MapGet(
                "/logs/paginated",
                async ([AsParameters] SystemLogGetPaginatedPayload query, LogHandler handler) =>
                {
                    return await handler.GetPaginatedLogs(query);
                }
            )
            .WithName("GetSystemLogsPaginated")
            .WithTags("Logs");

        group
            .MapPost(
                "/logs",
                async ([FromBody] SystemLogCreatePayload payload, LogHandler handler) =>
                {
                    return await handler.CreateLog(payload);
                }
            )
            .WithName("CreateSystemLog")
            .WithTags("Logs");
    }
}
