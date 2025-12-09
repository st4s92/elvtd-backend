using Backend.Application.Usecases;
using Backend.Helper;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Handlers;

public partial class TraderHandler
{
    public async Task<IResult> AddAccount(AccountPayload accountPayload)
    {
        var account = new Account
        {
            PlatformName = accountPayload.PlatformName ?? "",
            PlatformPath = accountPayload.PlatformPath ?? "",
            AccountNumber = accountPayload.AccountNumber ?? 0,
            BrokerName = accountPayload.BrokerName ?? "",
            ServerName = accountPayload.ServerName ?? "",
            UserId = accountPayload.UserId ?? 0
        };

        if (account.AccountNumber == 0 || account.ServerName == "")
        {
            return Response.Json(TError.NewClient("Account number and Server Name should be filled"));
        }
        if (account.UserId == 0)
        {
            return Response.Json(TError.NewClient("User Id should be filled"));
        }
        if (account.PlatformName == "" || account.PlatformPath == "")
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

    public async Task<IResult> GetAccount(int id)
    {
        var (res, terr) = await _usecase.GetAccount(new Account { Id = id });
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetAccounts(AccountGetPayload query)
    {
        var accountFilter = new Account
        {
            Id = query.Id ?? 0,
            PlatformName = query.PlatformName ?? "",
            PlatformPath = query.PlatformPath ?? "",
            AccountNumber = query.AccountNumber ?? 0,
            BrokerName = query.BrokerName ?? "",
            ServerName = query.ServerName ?? "",
            UserId = query.UserId ?? 0
        };

        var (res, terr) = await _usecase.GetAccounts(accountFilter);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetPaginatedAccounts(AccountGetPaginatedPayload query)
    {
        var accountFilter = new Account
        {
            Id = query.Id ?? 0,
            PlatformName = query.PlatformName ?? "",
            PlatformPath = query.PlatformPath ?? "",
            AccountNumber = query.AccountNumber ?? 0,
            BrokerName = query.BrokerName ?? "",
            ServerName = query.ServerName ?? "",
            UserId = query.UserId ?? 0
        };

        var (res, total, terr) = await _usecase.GetPaginatedAccounts(accountFilter, query.Page, query.PerPage);
        if (terr != null)
        {
            return Response.Json(terr);
        }

        var resp = new GetPaginatedResponse<Account>
        {
            Data = res,
            Total = total
        };
        return Response.Json(resp);
    }

    public async Task<IResult> UpdateAccount(long id, Account payload)
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

        var (_, terr) = await _usecase.UpdateAccountById(id, payload);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json("ok");
    }
}