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
            AccountNumber = accountPayload.AccountNumber ?? 0,
            BrokerName = accountPayload.BrokerName ?? "",
            ServerName = accountPayload.ServerName ?? "",
            AccountPassword = accountPayload.AccountPassword ?? "",
            UserId = accountPayload.UserId ?? 0,
            Role = accountPayload.Role ?? "SLAVE",
        };

        if (
            accountPayload.AccountNumber == 0
            || accountPayload.ServerName == ""
            || accountPayload.AccountPassword == ""
        )
        {
            return Response.Json(
                TError.NewClient("Account number, Server Name and Password should be filled")
            );
        }
        if (account.UserId == 0)
        {
            return Response.Json(TError.NewClient("User Id should be filled"));
        }
        if (account.PlatformName == "")
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
            AccountNumber = query.AccountNumber ?? 0,
            BrokerName = query.BrokerName ?? "",
            ServerName = query.ServerName ?? "",
            UserId = query.UserId ?? 0,
            Role = query.Role ?? "",
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
            AccountNumber = query.AccountNumber ?? 0,
            BrokerName = query.BrokerName ?? "",
            ServerName = query.ServerName ?? "",
            UserId = query.UserId ?? 0,
            Role = query.Role ?? "",
        };

        var (res, total, terr) = await _usecase.GetPaginatedAccounts(
            accountFilter,
            query.Page,
            query.PerPage
        );
        if (terr != null)
        {
            return Response.Json(terr);
        }

        var data = res.Select(a => new AccountGetPaginatedObject
        {
            Id = a.Id,
            PlatformName = a.PlatformName,
            AccountNumber = a.AccountNumber,
            BrokerName = a.BrokerName,
            ServerName = a.ServerName,
            UserId = a.UserId,
            Role = a.Role,
            Status = a.Status,
            ServerStatus = a.ServerAccount?.Status.ToString(),
            ServerStatusMessage = a.ServerAccount?.Message,
            CreatedAt = a.CreatedAt,
        })
            .ToList();

        var resp = new GetPaginatedResponse<AccountGetPaginatedObject>
        {
            Data = data,
            Total = total,
        };

        return Response.Json(resp);
    }

    public async Task<IResult> UpdateAccount(long id, AccountPayload payload)
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
        if (payload.AccountNumber == 0 || payload.ServerName == "" || payload.AccountPassword == "")
        {
            return Response.Json(
                TError.NewClient("Account number, Server Name and Password should be filled")
            );
        }
        if (payload.PlatformName == "")
        {
            return Response.Json(TError.NewClient("Platform info should be filled"));
        }

        var account = new Account
        {
            PlatformName = payload.PlatformName ?? "",
            AccountNumber = payload.AccountNumber ?? 0,
            BrokerName = payload.BrokerName ?? "",
            ServerName = payload.ServerName ?? "",
            AccountPassword = payload.AccountPassword ?? "",
        };

        var (_, terr) = await _usecase.UpdateAccountById(id, account);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json("ok");
    }

    public async Task<IResult> DeleteAccount(long id)
    {
        if (id == 0)
        {
            var terrs = TError.NewClient("Id should be filled");
            return Response.Json(terrs);
        }

        var terr = await _usecase.DeleteAccountByID(id);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json("ok");
    }

    public async Task<IResult> TriggerInstallByAccountId(long id)
    {
        if (id == 0)
        {
            var terrs = TError.NewClient("Id should be filled");
            return Response.Json(terrs);
        }

        var terr = await _usecase.TriggerInstallByAccountId(id);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json("ok");
    }

    public async Task<IResult> GetMasterOrderStatus(BridgeListCreateOrderPayload payload)
    {
        if (payload.AccountId == 0 || payload.ServerName == "")
        {
            return Response.Json(TError.NewClient("Account number, Server Name should be filled"));
        }

        var (res, terr) = await _usecase.GetMasterOrderStatus(payload);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetAccountDetail(long id)
    {
        if (id == 0)
        {
            return Response.Json(
                TError.NewClient("account id must be provided")
            );
        }

        var (res, terr) = await _usecase.GetAccountDetail(id);
        if (terr != null)
        {
            return Response.Json(terr);
        }

        return Response.Json(res);
    }

}
