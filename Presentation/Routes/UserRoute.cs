using Backend.Model;
using Backend.Presentation.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Routes;

public static class UserRoutes
{
    public static void MapUserRoutes(this RouteGroupBuilder group)
    {
        group
            .MapGet(
                "/users",
                async ([AsParameters] UserGetPayload query, UserHandler handler) =>
                {
                    return await handler.GetUsers(query);
                }
            )
            .WithName("GetUsers")
            .WithTags("Users");

        group
            .MapGet(
                "/users/paginated",
                async ([AsParameters] UserGetPaginatedPayload query, UserHandler handler) =>
                {
                    return await handler.GetPaginatedUsers(query);
                }
            )
            .WithName("GetUsersPaginated")
            .WithTags("Users");

        group
            .MapGet(
                "/users/{id:int}",
                async (int id, UserHandler handler) =>
                {
                    return await handler.GetUser(id);
                }
            )
            .WithName("GetUserById")
            .WithTags("Users");

        group
            .MapPost(
                "/users/signin",
                async ([FromBody] LoginRequest payload, UserHandler handler) =>
                {
                    return await handler.SignIn(payload);
                }
            )
            .WithName("SignIn")
            .WithTags("Users");
    }
}
