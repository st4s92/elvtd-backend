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
    private readonly IJobPublisher _jobPublisher;
    private readonly string _ctraderClientId;
    private readonly string _ctraderClientSecret;
    private readonly App _app;
    private readonly string _platformName = "cTrader";

    public CtraderUsecase(
        ICtraderRepository ctraderRepository,
        ITradingRepository tradingRepository,
        IAccountRepository accountRepository,
        IJobPublisher jobPublisher,
        UserUsecase userUsecase)
    {
        _userUsecase = userUsecase;
        _ctraderRepository = ctraderRepository;
        _tradingRepository = tradingRepository;
        _accountRepository = accountRepository;
        _jobPublisher = jobPublisher;
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

    public async Task<(Account?, ITError?)> CreateAccountManual(ManualCtraderAccountPayload payload)
    {
        if (payload.AccountNumber <= 0)
        {
            return (null, TError.NewClient("account_number is required"));
        }

        // 1. Build token from manual input
        var tokenModel = new AppToken
        {
            Platform = _platformName,
            AuthToken = payload.AccessToken,
            RefreshToken = payload.RefreshToken,
            ExpiredAt = DateTime.Parse(payload.ExpiryToken),
            UserID = payload.UserId,
            PlatformId = payload.AccountNumber.ToString(),
        };

        // 2. Save token to database
        var (_, tokenErr) = await _tradingRepository.SaveToken(tokenModel);
        if (tokenErr != null)
        {
            return (null, tokenErr);
        }

        // 3. Try to get cTrader user info (optional – use manual values as fallback)
        decimal balance = 0;
        decimal equity = 0;
        try
        {
            var (ctraderUser, terr) = await _ctraderRepository.GetUserByTokenAsync(tokenModel);
            if (terr == null && ctraderUser != null)
            {
                balance = (decimal)ctraderUser.Balance;
                equity = (decimal)ctraderUser.Equity;
            }
        }
        catch
        {
            // cTrader API unreachable – continue with defaults
        }

        // 4. Create or update account
        var account = await _accountRepository.Save(
            new Account
            {
                PlatformName = _platformName,
                AccountNumber = payload.AccountNumber,
                AccountPassword = "",
                BrokerName = "cTrader",
                ServerName = "demo",
                UserId = payload.UserId,
                Balance = balance,
                Equity = equity,
                Status = ConnectionStatus.None,
                Role = payload.Role,
            },
            a => a.PlatformName == _platformName
                && a.AccountNumber == payload.AccountNumber
                && a.UserId == payload.UserId
        );

        // 5. Notify Go bridge via RabbitMQ to connect this account
        if (account != null)
        {
            await _jobPublisher.PublishCtraderManageAccount(account);
        }

        return (account, null);
    }

    // GetAccountsForBridge returns all cTrader accounts with their tokens embedded
    public async Task<List<object>> GetAccountsForBridge()
    {
        var accounts = await _accountRepository.GetMany(
            a => a.PlatformName == _platformName
        );

        var result = new List<object>();
        foreach (var account in accounts)
        {
            string accessToken = "";
            string refreshToken = "";
            string expiredAt = "";

            try
            {
                var token = await _tradingRepository.GetToken(
                    _platformName,
                    account.AccountNumber.ToString()
                );
                if (token != null)
                {
                    accessToken = token.AuthToken ?? "";
                    refreshToken = token.RefreshToken ?? "";
                    expiredAt = token.ExpiredAt.ToString("o");
                }
            }
            catch
            {
                // Token not found — continue with empty values
            }

            result.Add(new
            {
                id = account.Id,
                platform_name = account.PlatformName,
                account_number = account.AccountNumber,
                broker_name = account.BrokerName ?? "",
                server_name = account.ServerName ?? "",
                user_id = account.UserId,
                role = account.Role ?? "",
                balance = (double)account.Balance,
                equity = (double)account.Equity,
                status = (int)account.Status,
                access_token = accessToken,
                refresh_token = refreshToken,
                expired_at = expiredAt,
            });
        }

        return result;
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
