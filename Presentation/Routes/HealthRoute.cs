using Backend.Presentation.Handlers;

namespace Backend.Presentation.Routes;

public static class HealthRoutes
{
    public static void MapHealthRoutes(this RouteGroupBuilder group)
    {
        group
            .MapGet(
                "/health/server/{id:int}",
                async (int id, HealthHandler handler) =>
                {
                    return await handler.CheckServer(id);
                }
            )
            .WithName("HealthCheckServer")
            .WithTags("Health");

        group
            .MapGet(
                "/health/account/{id:int}",
                async (int id, HealthHandler handler) =>
                {
                    return await handler.CheckAccount(id);
                }
            )
            .WithName("HealthCheckAccount")
            .WithTags("Health");
    }
}
