using Backend.Helper;
using Backend.Model;
using OpenAPI.Net;
using OpenAPI.Net.Auth;
using OpenAPI.Net.Helpers;
using System.Reactive.Linq;
using System.Linq;
using Google.Protobuf;
using Backend.Application.Interfaces;

namespace Backend.Application.Usecases;

public class CtraderUsecase
{
    private readonly UserUsecase _userUsecase;
    private readonly ICtraderRepository _ctraderRepository;
    private readonly ITradingRepository _tradingRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly string _ctraderClientId;
    private readonly string _ctraderClientSecret;
    private readonly App _app;
    private readonly string _platformName = "cTrader";

    public CtraderUsecase(
        ICtraderRepository ctraderRepository,
        ITradingRepository tradingRepository,
        IAccountRepository accountRepository,
        UserUsecase userUsecase)
    {
        _userUsecase = userUsecase;
        _ctraderRepository = ctraderRepository;
        _tradingRepository = tradingRepository;
        _accountRepository = accountRepository;
        _ctraderClientId = Environment.GetEnvironmentVariable("CTRADER_CLIENT_ID")!;
        _ctraderClientSecret = Environment.GetEnvironmentVariable("CTRADER_CLIENT_SECRET")!;

        var redirectUrl = Environment.GetEnvironmentVariable("CTRADER_CALLBACK_URL")!;
        _app = new App(_ctraderClientId, _ctraderClientSecret, redirectUrl);
    }

    public (string, ITError?) GetAuthPage()
    {
        var authUri = _app.GetAuthUri(scope: Scope.Trading);
        if (authUri == null)
        {
            return ("", TError.NewServer("cannot generate ctrader auth uri"));
        }
        return (authUri.AbsoluteUri, null);
    }

    public async Task<(string, ITError?)> ProcessAuthCallback(string code, long userId)
    {
        // 1. Exchange code for token
        var (tokenModel, terr) = await _ctraderRepository.GetTokenAsync(code);
        if (terr != null)
        {
            return ("", terr);
        }
        tokenModel!.Platform = _platformName;

        // 2. Get cTrader user info
        CtraderUser? ctraderUser;
        (ctraderUser, terr) = await _ctraderRepository.GetUserByTokenAsync(tokenModel);
        if (terr != null)
        {
            return (tokenModel.AuthToken, terr);
        }

        tokenModel.UserID = userId;
        tokenModel.PlatformId = ctraderUser!.AccountId.ToString();

        // 3. Save token to database
        await _tradingRepository.SaveToken(tokenModel);

        // 4. Create or update account in accounts table
        await _accountRepository.Save(
            new Account
            {
                PlatformName = _platformName,
                AccountNumber = ctraderUser.AccountId,
                AccountPassword = "",
                BrokerName = "cTrader",
                ServerName = "demo",
                UserId = userId,
                Balance = (decimal)ctraderUser.Balance,
                Equity = (decimal)ctraderUser.Equity,
                Status = ConnectionStatus.None,
                Role = "",
            },
            a => a.PlatformName == _platformName
                && a.AccountNumber == ctraderUser.AccountId
                && a.UserId == userId
        );

        return (tokenModel.AuthToken, null);
    }

    public async Task<(AppToken?, ITError?)> GetTokenForAccount(long accountId)
    {
        var account = await _accountRepository.Get(a => a.Id == accountId);
        if (account == null)
        {
            return (null, TError.NewNotFound("account not found"));
        }

        if (account.PlatformName != _platformName)
        {
            return (null, TError.NewClient("account is not a cTrader account"));
        }

        var token = await _tradingRepository.GetToken(
            _platformName,
            account.AccountNumber.ToString()
        );

        if (token == null)
        {
            return (null, TError.NewNotFound("token not found for this account"));
        }

        if (token.ExpiredAt < DateTime.UtcNow)
        {
            var (refreshedToken, terr) = await RefreshToken(token);
            if (terr != null)
            {
                return (null, terr);
            }
            return (refreshedToken, null);
        }

        return (token, null);
    }

    public async Task<(AppToken?, ITError?)> RefreshToken(AppToken token)
    {
        var (newToken, terr) = await _ctraderRepository.RefreshTokenAsync(token.RefreshToken);
        if (terr != null)
        {
            return (null, terr);
        }

        newToken!.Platform = token.Platform;
        newToken.PlatformId = token.PlatformId;
        newToken.UserID = token.UserID;

        await _tradingRepository.SaveToken(newToken);
        return (newToken, null);
    }
}
