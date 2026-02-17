using Backend.Model;
using Backend.Presentation.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Routes;

public static class TraderRoutes
{
    public static void MapTraderRoutes(this RouteGroupBuilder group)
    {
        group
            .MapGet(
                "/trader/order/{id:int}",
                async (int id, TraderHandler handler) =>
                {
                    return await handler.GetOrder(id);
                }
            )
            .WithName("GetTraderOrder")
            .WithTags("Orders");

        group
            .MapGet(
                "/trader/orders",
                async ([AsParameters] OrderQuery query, TraderHandler handler) =>
                {
                    return await handler.GetOrders(query);
                }
            )
            .WithName("GetTraderOrders")
            .WithTags("Orders");

        group
            .MapGet(
                "/trader/orders/paginated",
                async ([AsParameters] OrderGetPaginatedPayload query, TraderHandler handler) =>
                {
                    return await handler.GetPaginatedOrders(query);
                }
            )
            .WithName("GetTraderPaginatedOrders")
            .WithTags("Orders");

        group
            .MapDelete(
                "/trader/orders/master-order",
                async ([FromBody] MasterOrderDeleteOrder payload, TraderHandler handler) =>
                {
                    return await handler.HandleMasterOrderDeleteOrder(payload);
                }
            )
            .WithName("MasterOrderDeleteOrder")
            .WithTags("Orders");

        group
            .MapPost(
                "/trader/bridge/master-order",
                async ([FromBody] BridgeListCreateOrderPayload payload, TraderHandler handler) =>
                {
                    return await handler.HandleBridgeMasterOrder(payload);
                }
            )
            .WithName("BridgeMasterAddOrder")
            .WithTags("Orders");

        group
            .MapPut(
                "/trader/bridge/slave-order",
                async ([FromBody] BridgeOrderPayload payload, TraderHandler handler) =>
                {
                    return await handler.HandleBridgeSlaveOrderConfirmation(payload);
                }
            )
            .WithName("BridgeSlaveConfirmOrder")
            .WithTags("Orders");

        group
            .MapPost(
                "/trader/bridge/active-position/sync",
                async (
                    [FromBody] PlatformActivePositionSyncPayload payload,
                    TraderHandler handler
                ) =>
                {
                    return await handler.HandlePlatformActivePositionSync(payload);
                }
            )
            .WithName("BridgePlatformActivePositionSync")
            .WithTags("Orders");

        // account
        group
            .MapGet(
                "/trader/account/{id:int}",
                async (int id, TraderHandler handler) =>
                {
                    return await handler.GetAccount(id);
                }
            )
            .WithName("GetTraderAccount")
            .WithTags("Account");

        group
            .MapGet(
                "/trader/account/{id:long}/detail",
                async (long id, TraderHandler handler) =>
                {
                    return await handler.GetAccountDetail(id);
                }
            ).WithName("GetTraderAccountDetail")
            .WithTags("Account");

        group
            .MapPost(
                "/trader/account",
                async ([FromBody] AccountPayload payload, TraderHandler handler) =>
                {
                    return await handler.AddAccount(payload);
                }
            )
            .WithName("AddTraderAccount")
            .WithTags("Account");

        group
            .MapPatch(
                "/trader/account/{id:int}",
                async (int id, [FromBody] AccountPayload payload, TraderHandler handler) =>
                {
                    return await handler.UpdateAccount(id, payload);
                }
            )
            .WithName("UpdateTraderAccount")
            .WithTags("Account");

        group
            .MapPost(
                "trader/account/{id:int}/install",
                async (int id, TraderHandler handler) =>
                {
                    return await handler.TriggerInstallByAccountId(id);
                }
            )
            .WithName("TriggerInstallByAccountId")
            .WithTags("Account");

        group
            .MapGet(
                "/trader/account",
                async ([AsParameters] AccountGetPayload query, TraderHandler handler) =>
                {
                    return await handler.GetAccounts(query);
                }
            )
            .WithName("GetTraderAccounts")
            .WithTags("Account");

        group
            .MapPost(
                "/trader/account/master-status-order",
                async ([FromBody] BridgeListCreateOrderPayload payload, TraderHandler handler) =>
                {
                    return await handler.GetMasterOrderStatus(payload);
                }
            )
            .WithName("GetMasterOrderStatus")
            .WithTags("Account");

        group
            .MapGet(
                "/trader/account/paginated",
                async ([AsParameters] AccountGetPaginatedPayload query, TraderHandler handler) =>
                {
                    return await handler.GetPaginatedAccounts(query);
                }
            )
            .WithName("GetTraderPaginatedAccounts")
            .WithTags("Account");

        group
            .MapDelete(
                "/trader/account/{id:int}",
                async (int id, TraderHandler handler) =>
                {
                    return await handler.DeleteAccount(id);
                }
            )
            .WithName("DeleteTraderAccount")
            .WithTags("Account");

        group
            .MapPost(
                "/trader/bridge/account-sync",
                async ([FromBody] SyncAccountStatePayload payload, TraderHandler handler) =>
                {
                    return await handler.HandleBridgeSyncAccountState(payload);
                }
            )
            .WithName("HandleBridgeSyncAccountState")
            .WithTags("Account");

        // master slave
        group
            .MapGet(
                "/trader/master-slave/full-config/{id:int}",
                async (int id, TraderHandler handler) =>
                {
                    return await handler.GetMasterSlaveFullConfig(id);
                }
            )
            .WithName("GetMasterSlaveFullConfig")
            .WithTags("Master Slave Full Config");

        group
            .MapPatch(
                "/trader/master-slave/full-config",
                async ([FromBody] MasterSlaveFullConfigPayload payload, TraderHandler handler) =>
                {
                    return await handler.EditMasterSlaveFullonfig(payload);
                }
            )
            .WithName("EditMasterSlaveFullConfig")
            .WithTags("Master Slave Full Config");

        group
            .MapGet(
                "/trader/master-slave/{id:int}",
                async (int id, TraderHandler handler) =>
                {
                    return await handler.GetMasterSlave(id);
                }
            )
            .WithName("GetTraderMasterSlave")
            .WithTags("Master Slave");

        group
            .MapPost(
                "/trader/master-slave",
                async ([FromBody] MasterSlavePayload payload, TraderHandler handler) =>
                {
                    return await handler.AddMasterSlave(payload);
                }
            )
            .WithName("AddTraderMasterSlave")
            .WithTags("Master Slave");

        group
            .MapGet(
                "/trader/master-slave",
                async ([AsParameters] MasterSlaveGetPayload query, TraderHandler handler) =>
                {
                    return await handler.GetMasterSlaves(query);
                }
            )
            .WithName("GetTraderMasterSlaves")
            .WithTags("Master Slave");

        group
            .MapGet(
                "/trader/master-slave/paginated",
                async (
                    [AsParameters] MasterSlaveGetPaginatedPayload query,
                    TraderHandler handler
                ) =>
                {
                    return await handler.GetPaginatedMasterSlaves(query);
                }
            )
            .WithName("GetTraderPaginatedMasterSlaves")
            .WithTags("Master Slave");

        // master slave pair
        group
            .MapGet(
                "/trader/master-slave-pair/{id:int}",
                async (int id, TraderHandler handler) =>
                {
                    return await handler.GetMasterSlavePair(id);
                }
            )
            .WithName("GetTraderMasterSlavePair")
            .WithTags("Master Slave Pair");

        group
            .MapPost(
                "/trader/master-slave-pair",
                async ([FromBody] MasterSlavePairPayload payload, TraderHandler handler) =>
                {
                    return await handler.AddMasterSlavePair(payload);
                }
            )
            .WithName("AddTraderMasterSlavePair")
            .WithTags("Master Slave Pair");

        group
            .MapGet(
                "/trader/master-slave-pair",
                async ([AsParameters] MasterSlavePairGetPayload query, TraderHandler handler) =>
                {
                    return await handler.GetMasterSlavePairs(query);
                }
            )
            .WithName("GetTraderMasterSlavePairs")
            .WithTags("Master Slave Pair");

        group
            .MapGet(
                "/trader/master-slave-pair/paginated",
                async (
                    [AsParameters] MasterSlavePairGetPaginatedPayload query,
                    TraderHandler handler
                ) =>
                {
                    return await handler.GetPaginatedMasterSlavePairs(query);
                }
            )
            .WithName("GetTraderPaginatedMasterSlavePairs")
            .WithTags("Master Slave Pair");

        // master slave config
        group
            .MapGet(
                "/trader/master-slave-config/{id:int}",
                async (int id, TraderHandler handler) =>
                {
                    return await handler.GetMasterSlaveConfig(id);
                }
            )
            .WithName("GetTraderMasterSlaveConfig")
            .WithTags("Master Slave Config");

        group
            .MapPost(
                "/trader/master-slave-config",
                async ([FromBody] MasterSlaveConfigPayload payload, TraderHandler handler) =>
                {
                    return await handler.AddMasterSlaveConfig(payload);
                }
            )
            .WithName("AddTraderMasterSlaveConfig")
            .WithTags("Master Slave Config");

        group
            .MapGet(
                "/trader/master-slave-config",
                async ([AsParameters] MasterSlaveConfigGetPayload query, TraderHandler handler) =>
                {
                    return await handler.GetMasterSlaveConfigs(query);
                }
            )
            .WithName("GetTraderMasterSlaveConfigs")
            .WithTags("Master Slave Config");

        group
            .MapGet(
                "/trader/master-slave-config/paginated",
                async (
                    [AsParameters] MasterSlaveConfigGetPaginatedPayload query,
                    TraderHandler handler
                ) =>
                {
                    return await handler.GetPaginatedMasterSlaveConfigs(query);
                }
            )
            .WithName("GetTraderPaginatedMasterSlaveConfigs")
            .WithTags("Master Slave Config");

        group
            .MapGet(
                "/trader/servers/paginated",
                async ([AsParameters] ServerGetPaginatedPayload query, TraderHandler handler) =>
                {
                    return await handler.GetPaginatedServers(query);
                }
            )
            .WithName("GetTraderPaginatedServers")
            .WithTags("Server");

        group
            .MapPost(
                "/trader/servers",
                async ([FromBody] ServerCreatePayload payload, TraderHandler handler) =>
                {
                    return await handler.AddServer(payload);
                }
            )
            .WithName("AddServer")
            .WithTags("Server");
    }
}
