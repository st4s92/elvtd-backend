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
                "/health/servers",
                new[] { "GET", "HEAD" },
                async (HealthHandler handler) =>
                {
                    return await handler.CheckAllServers();
                }
            )
            .WithName("HealthCheckAllServers")
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

        // HTTP heartbeat endpoint for ctrader-copier (no RabbitMQ access)
        group
            .MapPost(
                "/health/heartbeat",
                async (
                    [Microsoft.AspNetCore.Mvc.FromBody] Backend.Model.ServerHeartbeatRequest payload,
                    TraderHandler handler
                ) =>
                {
                    await handler.HandleHttpHeartbeat(payload);
                    return Results.Ok(new { status = true });
                }
            )
            .WithName("HttpHeartbeat")
            .WithTags("Health");
    }
}
