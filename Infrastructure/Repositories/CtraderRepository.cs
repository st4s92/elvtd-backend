using Backend.Application.Interfaces;
using Backend.Helper;
using Backend.Model;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Backend.Infrastructure.Repositories;

public class CtraderRepository : ICtraderRepository
{
    private readonly HttpClient _http;
    private readonly string _baseUrl = "http://demo.ctraderapi.com:5036";
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _redirectUri;

    public CtraderRepository(HttpClient httpClient)
    {
        _http = httpClient;
        _clientId = Environment.GetEnvironmentVariable("CTRADER_CLIENT_ID")!;
        _clientSecret = Environment.GetEnvironmentVariable("CTRADER_CLIENT_SECRET")!;
        _redirectUri = Environment.GetEnvironmentVariable("CTRADER_CALLBACK_URL")!;
    }

    public async Task<(CtraderUser?, ITError?)> GetUserByTokenAsync(AppToken token)
    {
        CtraderUser? res = null;
        ITError? terr = null;

        try
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token.AuthToken);

            var response = await _http.GetAsync($"{_baseUrl}/trading/accounts/me");

            if (!response.IsSuccessStatusCode)
            {
                terr = TError.NewServer($"ctrader api error: {response.StatusCode} {response.ReasonPhrase}");
                return (null, terr);
            }

            var json = await response.Content.ReadAsStringAsync();
            res = JsonSerializer.Deserialize<CtraderUser>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            terr = TError.NewServer(ex.Message);
        }

        if (res == null)
        {
            terr = TError.NewNotFound("ctrader user not found");
        }

        return (res, terr);
    }
    
    public async Task<(AppToken?, ITError?)> GetTokenAsync(string code)
    {
        try
        {
            var url = $"{_baseUrl}/apps/token?grant_type=authorization_code&code={code}&redirect_uri={_redirectUri}&client_secret={_clientSecret}&client_id={_clientId}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return (null, TError.NewServer($"Token request failed: {err}"));
            }

            var json = await response.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<CtraderAppTokenResponse>(json);

            Console.WriteLine(token);
            if (token == null)
            {
                var terr = TError.NewNotFound("token not found");
                return (null, terr);
            }

            var appToken = new AppToken
            {
                Platform = "CTRADER",
                AuthToken = token!.AccessToken,
                RefreshToken = token!.refreshToken,
                ExpiredAt = DateTime.UtcNow.AddSeconds(token.ExpiresIn)
            };

            if (appToken == null)
            {
                var terr = TError.NewNotFound("ctrader user not found");
                return (appToken, terr);
            }

            return (appToken, null);
        }
        catch (Exception ex)
        {
            return (null, TError.NewServer(ex.Message));
        }
    }
}
