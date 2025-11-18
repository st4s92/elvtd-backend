using Backend.Model;
using Backend.Presentation.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Routes;

public static class UserRoutes
{
    public static void MapUserRoutes(this WebApplication app)
    {
        app.MapGet("/users", async ([AsParameters] UserGetPayload query, UserHandler handler) =>
        {
            return await handler.GetUsers(query);
        }).WithName("GetUsers").WithTags("Users");

        app.MapGet("/users/{id:int}", async (int id, UserHandler handler) =>
        {
            return await handler.GetUser(id);
        })
        .WithName("GetUserById").WithTags("Users");
    }
}