using Backend.Application.Interfaces;
using Backend.Application.Usecases;
using Backend.Helper;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Handlers;

public class CtraderHandler
{
    private readonly CtraderUsecase _usecase;
    private readonly ITradingRepository _tradingRepository;

    public CtraderHandler(
        CtraderUsecase usecase,
        ITradingRepository tradingRepository)
    {
        _usecase = usecase;
        _tradingRepository = tradingRepository;
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
    /// Direkt einen cTrader Token in app_tokens speichern/aktualisieren.
    /// PUT /api/ctrader/token/save
    /// </summary>
    public async Task<IResult> SaveTokenDirect(CtraderTokenPayload payload)
    {
        Console.WriteLine($"[SaveTokenDirect] accountNumber={payload.AccountNumber}, hasAccessToken={!string.IsNullOrEmpty(payload.AccessToken)}");

        if (payload.AccountNumber == 0 || string.IsNullOrEmpty(payload.AccessToken))
        {
            return Response.Json(TError.NewClient("account_number and access_token are required"));
        }

        var token = new AppToken
        {
            Platform = "cTrader",
            PlatformId = payload.AccountNumber.ToString(),
            AuthToken = payload.AccessToken!,
            RefreshToken = payload.RefreshToken ?? "",
            ExpiredAt = DateTime.TryParse(payload.ExpiryToken, out var expiry)
                ? expiry
                : DateTime.UtcNow.AddDays(30),
            UserID = 0,
        };

        var (_, tokenErr) = await _tradingRepository.SaveToken(token);
        if (tokenErr != null)
        {
            Console.WriteLine($"[SaveTokenDirect] SaveToken FAILED: {tokenErr}");
            return Response.Json(TError.NewServer($"SaveToken failed: {tokenErr}"));
        }

        Console.WriteLine($"[SaveTokenDirect] Token saved OK for account {payload.AccountNumber}");

        // Verify it was saved
        var verify = await _tradingRepository.GetToken("cTrader", payload.AccountNumber.ToString());
        Console.WriteLine($"[SaveTokenDirect] Verify: token={verify?.AuthToken?.Substring(0, Math.Min(10, verify?.AuthToken?.Length ?? 0))}...");

        return Response.Json(new
        {
            saved = true,
            account_number = payload.AccountNumber,
            verify_token_exists = verify != null,
            verify_token_value = verify?.AuthToken?.Substring(0, Math.Min(10, verify?.AuthToken?.Length ?? 0)) + "...",
        });
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
