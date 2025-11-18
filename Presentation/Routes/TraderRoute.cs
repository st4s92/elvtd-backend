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
        }).WithName("GetTraderOrder").WithTags("Orders");

        app.MapGet("/trader/orders", async ([AsParameters] OrderQuery query, TraderHandler handler) =>
        {
            return await handler.GetOrders(query);
        }).WithName("GetTraderOrders").WithTags("Orders");

        app.MapGet("/trader/orders/paginated", async ([AsParameters] OrderQuery query, TraderHandler handler) =>
        {
            return await handler.GetPaginatedOrders(query);
        }).WithName("GetTraderPaginatedOrders").WithTags("Orders");

        app.MapPost("/trader/bridge/master-order", async ([FromBody] BridgeOrderPayload payload, TraderHandler handler) =>
        {
            return await handler.HandleBridgeMasterOrder(payload);
        }).WithName("BridgeAddOrder").WithTags("Orders");

        // account
        app.MapGet("/trader/account/{id:int}", async (int id, TraderHandler handler) =>
        {
            return await handler.GetAccount(id);
        }).WithName("GetTraderAccount").WithTags("Account");

        app.MapPost("/trader/account", async ([FromBody] AccountPayload payload, TraderHandler handler) =>
        {
            return await handler.AddAccount(payload);
        }).WithName("AddTraderAccount").WithTags("Account");

        app.MapGet("/trader/account", async ([AsParameters] AccountGetPayload query, TraderHandler handler) =>
        {
            return await handler.GetAccounts(query);
        }).WithName("GetTraderAccounts").WithTags("Account");

        app.MapGet("/trader/account/paginated", async ([AsParameters] AccountGetPaginatedPayload query, TraderHandler handler) =>
        {
            return await handler.GetPaginatedAccounts(query);
        }).WithName("GetTraderPaginatedAccounts").WithTags("Account");

        // master slave
        app.MapGet("/trader/master-slave/{id:int}", async (int id, TraderHandler handler) =>
        {
            return await handler.GetMasterSlave(id);
        }).WithName("GetTraderMasterSlave").WithTags("Master Slave");

        app.MapPost("/trader/master-slave", async ([FromBody] MasterSlavePayload payload, TraderHandler handler) =>
        {
            return await handler.AddMasterSlave(payload);
        }).WithName("AddTraderMasterSlave").WithTags("Master Slave");

        app.MapGet("/trader/master-slave", async ([AsParameters] MasterSlaveGetPayload query, TraderHandler handler) =>
        {
            return await handler.GetMasterSlaves(query);
        }).WithName("GetTraderMasterSlaves").WithTags("Master Slave");

        app.MapGet("/trader/master-slave/paginated", async ([AsParameters] MasterSlaveGetPaginatedPayload query, TraderHandler handler) =>
        {
            return await handler.GetPaginatedMasterSlaves(query);
        }).WithName("GetTraderPaginatedMasterSlaves").WithTags("Master Slave");

        // master slave pair
        app.MapGet("/trader/master-slave-pair/{id:int}", async (int id, TraderHandler handler) =>
        {
            return await handler.GetMasterSlavePair(id);
        }).WithName("GetTraderMasterSlavePair").WithTags("Master Slave Pair");

        app.MapPost("/trader/master-slave-pair", async ([FromBody] MasterSlavePairPayload payload, TraderHandler handler) =>
        {
            return await handler.AddMasterSlavePair(payload);
        }).WithName("AddTraderMasterSlavePair").WithTags("Master Slave Pair");

        app.MapGet("/trader/master-slave-pair", async ([AsParameters] MasterSlavePairGetPayload query, TraderHandler handler) =>
        {
            return await handler.GetMasterSlavePairs(query);
        }).WithName("GetTraderMasterSlavePairs").WithTags("Master Slave Pair");

        app.MapGet("/trader/master-slave-pair/paginated", async ([AsParameters] MasterSlavePairGetPaginatedPayload query, TraderHandler handler) =>
        {
            return await handler.GetPaginatedMasterSlavePairs(query);
        }).WithName("GetTraderPaginatedMasterSlavePairs").WithTags("Master Slave Pair");

        // master slave config
        app.MapGet("/trader/master-slave-config/{id:int}", async (int id, TraderHandler handler) =>
        {
            return await handler.GetMasterSlaveConfig(id);
        }).WithName("GetTraderMasterSlaveConfig").WithTags("Master Slave Config");

        app.MapPost("/trader/master-slave-config", async ([FromBody] MasterSlaveConfigPayload payload, TraderHandler handler) =>
        {
            return await handler.AddMasterSlaveConfig(payload);
        }).WithName("AddTraderMasterSlaveConfig").WithTags("Master Slave Config");

        app.MapGet("/trader/master-slave-config", async ([AsParameters] MasterSlaveConfigGetPayload query, TraderHandler handler) =>
        {
            return await handler.GetMasterSlaveConfigs(query);
        }).WithName("GetTraderMasterSlaveConfigs").WithTags("Master Slave Config");

        app.MapGet("/trader/master-slave-config/paginated", async ([AsParameters] MasterSlaveConfigGetPaginatedPayload query, TraderHandler handler) =>
        {
            return await handler.GetPaginatedMasterSlaveConfigs(query);
        }).WithName("GetTraderPaginatedMasterSlaveConfigs").WithTags("Master Slave Config");
        
    }
}