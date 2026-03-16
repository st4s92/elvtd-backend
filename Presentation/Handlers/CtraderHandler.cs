using Backend.Application.Interfaces;
using Backend.Application.Usecases;
using Backend.Helper;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Handlers;

public class CtraderHandler
{
    private readonly CtraderUsecase _usecase;
    private readonly IAccountRepository _accountRepository;

    public CtraderHandler(
        CtraderUsecase usecase,
        IAccountRepository accountRepository)
    {
        _usecase = usecase;
        _accountRepository = accountRepository;
    }

    public IResult GetAuthPage()
    {
        var (res, terr) = _usecase.GetAuthPage();
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> ProcessAuthCallback(string code, long userId)
    {
        var (res, terr) = await _usecase.ProcessAuthCallback(code, userId);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> CreateAccountManual(ManualCtraderAccountPayload payload)
    {
        var (account, terr) = await _usecase.CreateAccountManual(payload);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(account);
    }

    public async Task<IResult> GetAccountsForBridge()
    {
        try
        {
            var accounts = await _usecase.GetAccountsForBridge();
            return Response.Json(accounts);
        }
        catch (Exception ex)
        {
            return Response.Json(TError.NewServer($"GetAccountsForBridge: {ex.Message}"));
        }
    }

    public async Task<IResult> GetTokenForAccount(long accountId)
    {
        var (token, terr) = await _usecase.GetTokenForAccount(accountId);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(new
        {
            token = token!.AuthToken,
            refresh_token = token.RefreshToken,
            expired_at = token.ExpiredAt.ToString("o")
        });
    }

    /// <summary>
    /// Direkt cTrader Token im accounts-Table speichern.
    /// PUT /api/ctrader/token/save
    /// </summary>
    public async Task<IResult> SaveTokenDirect(CtraderTokenPayload payload)
    {
        if (payload.AccountNumber == 0 || string.IsNullOrEmpty(payload.AccessToken))
        {
            return Response.Json(TError.NewClient("account_number and access_token are required"));
        }

        try
        {
            // Account direkt aus DB laden
            var account = await _accountRepository.Get(a => a.AccountNumber == payload.AccountNumber && a.PlatformName == "cTrader");
            if (account == null)
            {
                return Response.Json(TError.NewClient($"cTrader account {payload.AccountNumber} not found"));
            }

            // Token-Felder direkt auf Account setzen
            account.AccessToken = payload.AccessToken;
            account.RefreshToken = payload.RefreshToken ?? "";
            account.TokenExpiredAt = DateTime.TryParse(payload.ExpiryToken, out var expiry)
                ? expiry
                : DateTime.UtcNow.AddDays(30);

            var saved = await _accountRepository.Save(account, a => a.Id == account.Id);
            if (saved == null)
            {
                return Response.Json(TError.NewServer("Failed to save account"));
            }

            return Response.Json(new
            {
                saved = true,
                account_number = payload.AccountNumber,
                access_token_preview = payload.AccessToken!.Substring(0, Math.Min(10, payload.AccessToken.Length)) + "...",
            });
        }
        catch (Exception ex)
        {
            return Response.Json(TError.NewServer($"SaveTokenDirect: {ex.Message}"));
        }
    }
}

/// <summary>
/// Payload für den direkten Token-Save Endpoint
/// </summary>
public class CtraderTokenPayload
{
    [System.Text.Json.Serialization.JsonPropertyName("account_number")]
    public long AccountNumber { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("expiry_token")]
    public string? ExpiryToken { get; set; }
}
