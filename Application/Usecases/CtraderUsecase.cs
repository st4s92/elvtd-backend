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
    private readonly IAccountRepository _accountRepository;
    private readonly IJobPublisher _jobPublisher;
    private readonly string _ctraderClientId;
    private readonly string _ctraderClientSecret;
    private readonly App _app;
    private readonly string _platformName = "cTrader";

    public CtraderUsecase(
        ICtraderRepository ctraderRepository,
        IAccountRepository accountRepository,
        IJobPublisher jobPublisher,
        UserUsecase userUsecase)
    {
        _userUsecase = userUsecase;
        _ctraderRepository = ctraderRepository;
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

        // 3. Create or update account in accounts table (tokens stored directly on account)
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
                AccessToken = tokenModel.AuthToken,
                RefreshToken = tokenModel.RefreshToken,
                TokenExpiredAt = tokenModel.ExpiredAt,
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

        if (string.IsNullOrEmpty(account.AccessToken))
        {
            return (null, TError.NewNotFound("token not found for this account"));
        }

        // Build AppToken from Account fields (for backward compatibility with handler response)
        var token = new AppToken
        {
            AuthToken = account.AccessToken!,
            RefreshToken = account.RefreshToken ?? "",
            ExpiredAt = account.TokenExpiredAt ?? DateTime.UtcNow,
            Platform = _platformName,
            PlatformId = account.AccountNumber.ToString(),
            UserID = account.UserId,
        };

        if (token.ExpiredAt < DateTime.UtcNow)
        {
            var (refreshedToken, terr) = await RefreshToken(token, account);
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

        // 1. Parse expiry date
        var expiredAt = DateTime.TryParse(payload.ExpiryToken, out var expiry)
            ? expiry
            : DateTime.UtcNow.AddDays(30);

        // 2. Try to get cTrader user info (optional – use manual values as fallback)
        decimal balance = 0;
        decimal equity = 0;
        try
        {
            var tokenModel = new AppToken
            {
                Platform = _platformName,
                AuthToken = payload.AccessToken,
                RefreshToken = payload.RefreshToken,
                ExpiredAt = expiredAt,
                UserID = payload.UserId,
                PlatformId = payload.AccountNumber.ToString(),
            };
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

        // 3. Create or update account (tokens stored directly on account)
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
                AccessToken = payload.AccessToken,
                RefreshToken = payload.RefreshToken,
                TokenExpiredAt = expiredAt,
            },
            a => a.PlatformName == _platformName
                && a.AccountNumber == payload.AccountNumber
                && a.UserId == payload.UserId
        );

        // 4. Notify Go bridge via RabbitMQ to connect this account
        if (account != null)
        {
            await _jobPublisher.PublishCtraderManageAccount(account);
        }

        return (account, null);
    }

    // GetAccountsForBridge returns all cTrader accounts with their tokens embedded
    // Tokens are now stored directly on the Account model (not in app_tokens table)
    public async Task<List<object>> GetAccountsForBridge()
    {
        var accounts = await _accountRepository.GetMany(
            a => a.PlatformName == _platformName
        );

        var result = new List<object>();
        foreach (var account in accounts)
        {
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
                access_token = account.AccessToken ?? "",
                refresh_token = account.RefreshToken ?? "",
                expired_at = account.TokenExpiredAt?.ToString("o") ?? "",
                ctid_trader_account_id = account.CtidTraderAccountId ?? 0,
            });
        }

        return result;
    }

    public async Task<(AppToken?, ITError?)> RefreshToken(AppToken token, Account? account = null)
    {
        var (newToken, terr) = await _ctraderRepository.RefreshTokenAsync(token.RefreshToken);
        if (terr != null)
        {
            return (null, terr);
        }

        newToken!.Platform = token.Platform;
        newToken.PlatformId = token.PlatformId;
        newToken.UserID = token.UserID;

        // Save refreshed token directly on the Account
        if (account == null)
        {
            account = await _accountRepository.Get(
                a => a.PlatformName == _platformName
                    && a.AccountNumber.ToString() == token.PlatformId
            );
        }

        if (account != null)
        {
            account.AccessToken = newToken.AuthToken;
            account.RefreshToken = newToken.RefreshToken;
            account.TokenExpiredAt = newToken.ExpiredAt;
            await _accountRepository.Save(account, a => a.Id == account.Id);
        }

        return (newToken, null);
    }
}
