using Backend.Application.Usecases;
using Backend.Helper;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Handlers;

public partial class TraderHandler
{
    public async Task<IResult> AddAccount(AccountPayload accountPayload)
    {
        var isCtrader = accountPayload.PlatformName == "cTrader";

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

        // cTrader-spezifische Felder direkt auf Account setzen
        if (isCtrader)
        {
            account.CtidTraderAccountId = accountPayload.CtidTraderAccountId;
            if (!string.IsNullOrEmpty(accountPayload.AccessToken))
            {
                account.AccessToken = accountPayload.AccessToken;
                account.RefreshToken = accountPayload.RefreshToken ?? "";
                account.TokenExpiredAt = DateTime.TryParse(accountPayload.ExpiryToken, out var addExpiry) ? addExpiry : DateTime.UtcNow.AddDays(30);
            }
        }

        if (isCtrader)
        {
            // cTrader: nur Account Number ist Pflicht (kein Passwort, OAuth stattdessen)
            if (accountPayload.AccountNumber == 0)
            {
                return Response.Json(TError.NewClient("Account number should be filled"));
            }
        }
        else
        {
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
            UpdatedAt = a.UpdatedAt,
            Balance = a.Balance,
            Equity = a.Equity,
            OpenPositionsCount = a.ActiveOrders?.Count ?? 0,
            DedicatedServerName = a.ServerAccount?.Server?.ServerName ?? "-",
            CopierVersion = a.CopierVersion,
            AccessToken = a.AccessToken,
            RefreshToken = a.RefreshToken,
            TokenExpiredAt = a.TokenExpiredAt,
            CtidTraderAccountId = a.CtidTraderAccountId,
        }).ToList();

        return Response.Json(data);
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

        Console.WriteLine($"[DEBUG] GetPaginatedAccounts: Search='{query.Search}', Page={query.Page}, PerPage={query.PerPage}");

        var (res, total, terr) = await _usecase.GetPaginatedAccounts(
            accountFilter,
            query.Page,
            query.PerPage,
            query.SortBy,
            query.SortOrder,
            query.Search
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
            UpdatedAt = a.UpdatedAt,
            Balance = a.Balance,
            Equity = a.Equity,
            OpenPositionsCount = a.ActiveOrders.Count,
            DedicatedServerName = a.ServerAccount?.Server?.ServerName ?? "-",
            CopierVersion = a.CopierVersion,
            AccessToken = a.AccessToken,
            RefreshToken = a.RefreshToken,
            TokenExpiredAt = a.TokenExpiredAt,
            CtidTraderAccountId = a.CtidTraderAccountId,
        })
            .ToList();

        var resp = new GetPaginatedResponse<AccountGetPaginatedObject>
        {
            Data = data,
            Total = total,
        };

        Console.WriteLine($"[DEBUG] GetPaginatedAccounts Result: {data.Count} items, Total {total}");

        return Response.Json(resp);
    }

    public async Task<IResult> UpdateAccount(long id, AccountPayload payload)
    {
        Console.WriteLine($"[UpdateAccount] id={id}, platform={payload?.PlatformName}, accNum={payload?.AccountNumber}, accessToken={(!string.IsNullOrEmpty(payload?.AccessToken) ? "SET" : "EMPTY")}");

        if (id == 0)
        {
            Console.WriteLine("[UpdateAccount] Error: id is 0");
            var terrs = TError.NewClient("Id should be filled");
            return Response.Json(terrs);
        }
        if (payload == null)
        {
            Console.WriteLine("[UpdateAccount] Error: payload is null");
            var terrs = TError.NewClient("Invalid payload");
            return Response.Json(terrs);
        }

        // Status-only update: skip platform validation
        if (payload.Status.HasValue && string.IsNullOrEmpty(payload.PlatformName))
        {
            var statusAccount = new Account { Status = payload.Status.Value };
            var (_, statusErr) = await _usecase.UpdateAccountById(id, statusAccount);
            if (statusErr != null)
            {
                Console.WriteLine($"[UpdateAccount] status update failed: {statusErr}");
                return Response.Json(statusErr);
            }
            return Response.Json("ok");
        }

        var isCtrader = payload.PlatformName == "cTrader";

        if (isCtrader)
        {
            // cTrader: nur Account Number ist Pflicht
            if (payload.AccountNumber == 0)
            {
                Console.WriteLine("[UpdateAccount] Error: cTrader account number is 0");
                return Response.Json(TError.NewClient("Account number should be filled"));
            }
        }
        else
        {
            if (payload.AccountNumber == 0 || payload.ServerName == "" || payload.AccountPassword == "")
            {
                return Response.Json(
                    TError.NewClient("Account number, Server Name and Password should be filled")
                );
            }
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

        // Update connection status if provided
        if (payload.Status.HasValue)
        {
            account.Status = payload.Status.Value;
        }

        // cTrader-spezifische Felder direkt im Account speichern
        if (isCtrader)
        {
            account.CtidTraderAccountId = payload.CtidTraderAccountId;
            if (!string.IsNullOrEmpty(payload.AccessToken))
            {
                account.AccessToken = payload.AccessToken;
                account.RefreshToken = payload.RefreshToken ?? "";
                account.TokenExpiredAt = DateTime.TryParse(payload.ExpiryToken, out var expiry) ? expiry : DateTime.UtcNow.AddDays(30);
            }
        }

        var (_, terr) = await _usecase.UpdateAccountById(id, account);
        if (terr != null)
        {
            Console.WriteLine($"[UpdateAccount] UpdateAccountById failed: {terr}");
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

    public async Task<IResult> TriggerRestartByAccountId(long id)
    {
        if (id == 0)
        {
            var terrs = TError.NewClient("Id should be filled");
            return Response.Json(terrs);
        }

        var terr = await _usecase.TriggerRestartByAccountId(id);
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
