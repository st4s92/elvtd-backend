using Backend.Model;
using Backend.Presentation.Handlers;

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
    }
}
