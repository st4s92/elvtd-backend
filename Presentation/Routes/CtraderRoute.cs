using Backend.Model;
using Backend.Presentation.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Routes;

public static class CtraderRoutes
{
    public static void MapCtraderRoutes(this RouteGroupBuilder group)
    {
        group.MapGet("/ctrader/auth", (CtraderHandler handler) =>
        {
            return handler.GetAuthPage();
        }).WithName("GetCtraderAuthUrl");

        group.MapGet("/ctrader/auth/callback", async ([FromServices] CtraderHandler handler, [FromQuery] string code) =>
        {
            Console.WriteLine($"code1: {code}");
            return await handler.ProcessAuthCallback(code);
        }).WithName("GetCtraderAuthCallback");
    }
}