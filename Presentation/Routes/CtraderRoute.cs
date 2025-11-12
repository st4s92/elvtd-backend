using Backend.Model;
using Backend.Presentation.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Routes;

public static class CtraderRoutes
{
    public static void MapCtraderRoutes(this WebApplication app)
    {
        app.MapGet("/ctrader/auth", (CtraderHandler handler) =>
        {
            return handler.GetAuthPage();
        }).WithName("GetCtraderAuthUrl");

        app.MapGet("/ctrader/auth/callback", async ([FromServices] CtraderHandler handler, [FromQuery] string code) =>
        {
            Console.WriteLine($"code1: {code}");
            return await handler.ProcessAuthCallback(code);
        }).WithName("GetCtraderAuthCallback");
    }
}