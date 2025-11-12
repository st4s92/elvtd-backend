using Backend.Model;
using Backend.Presentation.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Routes;

public static class TraderRoutes
{
    public static void MapTraderRoutes(this WebApplication app)
    {
        app.MapGet("/trader/order/{id:int}", async (int id, TraderHandler handler) =>
        {
            return await handler.GetOrder(id);
        }).WithName("GetTraderOrder");

        app.MapGet("/trader/orders", async ([AsParameters] OrderQuery query, TraderHandler handler) =>
        {
            return await handler.GetOrders(query);
        }).WithName("GetTraderOrders");

        app.MapPost("/trader/bridge/master-order", async ([FromBody] BridgeOrderPayload payload, TraderHandler handler) =>
        {
            return await handler.HandleBridgeMasterOrder(payload);
        }).WithName("BridgeAddOrder");

        app.MapPost("/account", async ([FromBody] AccountAddPayload payload, TraderHandler handler) =>
        {
            return await handler.AddAccount(payload);
        }).WithName("AddAccount");
    }
}