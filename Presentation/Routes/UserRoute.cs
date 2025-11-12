using Backend.Model;
using Backend.Presentation.Handlers;

namespace Backend.Presentation.Routes;

public static class UserRoutes
{
    public static void MapUserRoutes(this WebApplication app)
    {
        app.MapGet("/users", async (UserHandler handler) =>
        {
            return await handler.GetUsersAsync();
        }).WithName("GetUsers");

        app.MapGet("/users/{id:int}", async (int id, UserHandler handler) =>
        {
            return await handler.GetUserByIdAsync(id);
        })
        .WithName("GetUserById");
    }
}