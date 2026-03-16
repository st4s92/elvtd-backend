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

        group.MapGet("/ctrader/auth/callback", async (
            [FromServices] CtraderHandler handler,
            [FromQuery] string code,
            [FromQuery] long userId) =>
        {
            return await handler.ProcessAuthCallback(code, userId);
        }).WithName("GetCtraderAuthCallback");

        group.MapPost("/ctrader/account/manual", async (
            [FromServices] CtraderHandler handler,
            [FromBody] ManualCtraderAccountPayload payload) =>
        {
            return await handler.CreateAccountManual(payload);
        }).WithName("CreateCtraderAccountManual");

        group.MapGet("/ctrader/token/{accountId}", async (
            [FromServices] CtraderHandler handler,
            long accountId) =>
        {
            return await handler.GetTokenForAccount(accountId);
        }).WithName("GetCtraderTokenForAccount");

        // Bridge endpoint: returns all cTrader accounts WITH tokens in one call
        group.MapGet("/ctrader/bridge/accounts", async (
            [FromServices] CtraderHandler handler) =>
        {
            return await handler.GetAccountsForBridge();
        }).WithName("GetCtraderBridgeAccounts");
    }
}
