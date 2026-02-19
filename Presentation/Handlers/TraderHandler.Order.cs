using Backend.Application.Usecases;
using Backend.Helper;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Handlers;

public partial class TraderHandler
{
    public async Task<IResult> GetOrder(int id)
    {
        var (res, terr) = await _usecase.GetOrder(new Order { Id = id });
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetOrders(OrderQuery query)
    {
        var orderFilter = new Order
        {
            Id = query.Id ?? 0,
            AccountId = query.AccountId ?? 0,
            MasterOrderId = query.MasterOrderId,
            OrderSymbol = query.OrderSymbol ?? "",
            OrderType = query.OrderType ?? "",
            Status = query.Status ?? 0,
        };

        var (res, terr) = await _usecase.GetOrders(orderFilter);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetPaginatedOrders(OrderGetPaginatedPayload query)
    {
        var orderFilter = new Order
        {
            Id = query.Id ?? 0,
            AccountId = query.AccountId ?? 0,
            MasterOrderId = query.MasterOrderId,
            OrderSymbol = query.OrderSymbol ?? "",
            OrderType = query.OrderType ?? "",
            Status = query.Status ?? 0,
        };

        var (res, total, terr) = await _usecase.GetPaginatedOrders(
            orderFilter,
            query.Page,
            query.PerPage
        );
        if (terr != null)
        {
            return Response.Json(terr);
        }

        var resp = new GetPaginatedResponse<Order> { Data = res, Total = total };
        return Response.Json(resp);
    }

    public async Task<IResult> UpdateOrder(long id, Order payload)
    {
        if (id == 0)
        {
            var terrs = TError.NewClient("Id should be filled");
            return Response.Json(terrs);
        }
        if (payload == null)
        {
            var terrs = TError.NewClient("Invalid payload");
            return Response.Json(terrs);
        }

        var (_, terr) = await _usecase.UpdateOrderById(id, payload);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json("ok");
    }

    public async Task<IResult> HandleBridgeMasterOrder(BridgeListCreateOrderPayload payload)
    {
        if (payload == null)
        {
            var terrs = TError.NewClient("Invalid payload");
            return Response.Json(terrs);
        }

        var (msg, terr) = await _usecase.CreateBridgeMasterOrder(payload);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(msg);
    }

    public async Task<IResult> HandleBridgeSlaveOrderConfirmation(BridgeOrderPayload payload)
    {
        if (payload == null)
        {
            var terrs = TError.NewClient("Invalid payload");
            return Response.Json(terrs);
        }

        var terr = await _usecase.ConfirmBridgeSlaveOrder(payload);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json("ok");
    }

    public async Task<IResult> HandlePlatformActivePositionSync(
        PlatformActivePositionSyncPayload payload
    )
    {
        if (payload == null)
        {
            // platform consumer → no-op
            return Response.Json(payload);
        }

        var result = await _usecase.SyncActiveOrdersFromPlatform(payload);

        return Response.Json(result);
    }

    public async Task<IResult> HandleBridgeSyncAccountState(SyncAccountStatePayload payload)
    {
        if (payload == null)
        {
            return Response.Json(payload);
        }

        var result = await _usecase.SyncAccountState(payload);

        return Response.Json(result);
    }

    public async Task<IResult> HandleMasterOrderDeleteOrder(MasterOrderDeleteOrder payload)
    {
        if (payload.AccountId == 0)
        {
            return Response.Json(TError.NewClient("AccountId should be filled"));
        }

        // ==========================
        // CASE 1: FLUSH MASTER ORDER
        // ==========================
        if (payload.IsFlushOrder)
        {
            var terr = await _usecase.FlushMasterOrder(payload.AccountId);
            if (terr != null)
                return Response.Json(terr);

            return Response.Json(new { message = "Flush master order triggered" });
        }

        // ==========================
        // CASE 2: DELETE SPECIFIC ORDERS
        // ==========================
        if (payload.OrderIds == null || payload.OrderIds.Count == 0)
        {
            return Response.Json(TError.NewClient("OrderIds must be provided when not flushing"));
        }

        var (res, terr2) = await _usecase.DeleteMasterOrders(payload.AccountId, payload.OrderIds);

        if (terr2 != null)
            return Response.Json(terr2);

        return Response.Json(new { message = res });
    }

    public async Task<IResult> HandleForceCloseAllMasterTrades()
    {
        var terr = await _usecase.FlushAllMasterAccounts();
        if (terr != null)
            return Response.Json(terr);

        return Response.Json(new { message = "Force close all master trades triggered" });
    }

    public async Task<IResult> HandleKillAllTrades()
    {
        var terr = await _usecase.FlushAllAccounts();
        if (terr != null)
            return Response.Json(terr);

        return Response.Json(new { message = "Global kill switch triggered" });
    }

    public async Task<IResult> GetSlaveOrdersForMaster(long id)
    {
        var (res, terr) = await _usecase.GetSlaveOrdersForMaster(id);
        if (terr != null)
            return Response.Json(terr);

        return Response.Json(res);
    }

    public async Task<IResult> DeleteActiveOrder(long id)
    {
        var terr = await _usecase.DeleteActiveOrder(id);
        if (terr != null)
            return Response.Json(terr);

        return Response.Json("ok");
    }
}
