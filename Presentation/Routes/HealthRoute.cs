using Backend.Presentation.Handlers;

namespace Backend.Presentation.Routes;

public static class HealthRoutes
{
    public static void MapHealthRoutes(this RouteGroupBuilder group)
    {
        group
            .MapMethods(
                "/health/server/{id:int}",
                new[] { "GET", "HEAD" },
                async (int id, HealthHandler handler) =>
                {
                    return await handler.CheckServer(id);
                }
            )
            .WithName("HealthCheckServer")
            .WithTags("Health");

        group
            .MapMethods(
                "/health/account/{id:int}",
                new[] { "GET", "HEAD" },
                async (int id, HealthHandler handler) =>
                {
                    return await handler.CheckAccount(id);
                }
            )
            .WithName("HealthCheckAccount")
            .WithTags("Health");
    }
}
