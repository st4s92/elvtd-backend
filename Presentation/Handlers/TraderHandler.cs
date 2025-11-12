using Backend.Application.Usecases;
using Backend.Helper;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Handlers;

public class TraderHandler
{    
    private readonly TraderUsecase _usecase;
    private readonly AppLogger<TraderHandler> _logger;

    public TraderHandler(
        TraderUsecase usecase,
        AppLogger<TraderHandler>logger
    )
    {
        _usecase = usecase;
        _logger = logger;
    }

    public async Task<IResult> AddAccount(AccountAddPayload accountPayload)
    {
        var account = new Account
        {
            PlatformName = accountPayload.PlatformName,
            PlatformPath = accountPayload.PlatformPath,
            AccountNumber = accountPayload.AccountNumber,
            BrokerName = accountPayload.BrokerName,
            ServerName = accountPayload.ServerName,
            UserId = accountPayload.UserId
        };
        
        if (account.AccountNumber == 0 || account.ServerName == "")
        {
            return Response.Json(TError.NewClient("Account number and Server Name should be filled"));
        }
        if(account.UserId == 0)
        {
            return Response.Json(TError.NewClient("User Id should be filled"));
        }
        if(account.PlatformName == "" || account.PlatformPath == "")
        {
            return Response.Json(TError.NewClient("Platform info should be filled"));
        }
        var (res, terr) = await _usecase.AddAccount(account);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> AddOrder(Order order)
    {
        var (res, terr) = await _usecase.SaveOrder(order);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetOrder(int id)
    {
        var (res, terr) = await _usecase.GetOrder(new Order { Id = id});
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
            MasterOrderId = query.MasterOrderId ?? 0,
            OrderSymbol = query.OrderSymbol ?? "",
            OrderType = query.OrderType ?? "",
            Status = query.Status ?? 0
        };

        var (res, terr) = await _usecase.GetOrders(orderFilter);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> HandleBridgeMasterOrder(BridgeOrderPayload payload)
    {
        if (payload == null)
        {
            var terrs = TError.NewClient("Invalid payload");
            return Response.Json(terrs);
        }

        var terr = await _usecase.CreateBridgeMasterOrder(payload);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json("ok");
    }
}