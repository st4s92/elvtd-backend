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
    private readonly string _ctraderClientId;
    private readonly string _ctraderClientSecret;
    private readonly App _app;
    private readonly string _platformName = "CTRADER";

    public CtraderUsecase(ICtraderRepository ctraderRepository, ITradingRepository tradingRepository, UserUsecase userUsecase)
    {
        _userUsecase = userUsecase;
        _ctraderRepository = ctraderRepository;
        _tradingRepository = tradingRepository;
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

    public async Task<(string, ITError?)> ProcessAuthCallback(string code)
    {
        var (tokenModel, terr) = await _ctraderRepository.GetTokenAsync(code);
        if (terr != null)
        {
            return ("", terr);
        }
        tokenModel!.Platform = _platformName;
        Console.WriteLine($"tokenModel: {tokenModel}");

        CtraderUser? ctraderUser;
        (ctraderUser, terr) = await _ctraderRepository.GetUserByTokenAsync(tokenModel);
        if (terr != null)
        {
            return (tokenModel.AuthToken, terr);
        }

        tokenModel.UserID = ctraderUser!.AccountId;

        return (tokenModel.AuthToken, null);
    }
}