using Backend.Application.Interfaces;
using Backend.Model;

namespace Backend.Presentation.Handlers;

public class HealthHandler
{
    private readonly IServerRepository _serverRepository;
    private readonly IAccountRepository _accountRepository;
    private static readonly TimeSpan Threshold = TimeSpan.FromMinutes(5);

    public HealthHandler(
        IServerRepository serverRepository,
        IAccountRepository accountRepository
    )
    {
        _serverRepository = serverRepository;
        _accountRepository = accountRepository;
    }

    public async Task<IResult> CheckServer(int id)
    {
        var server = await _serverRepository.Get(s => s.Id == id);
        if (server == null)
            return Results.NotFound(new { status = false, message = $"Server {id} not found" });

        var age = DateTime.Now - server.UpdatedAt;
        var isHealthy = age <= Threshold;

        var body = new
        {
            status = isHealthy,
            name = server.ServerName,
            ip = server.ServerIp,
            updated_at = server.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            age_seconds = (int)age.TotalSeconds,
            message = isHealthy
                ? "OK"
                : $"Not updated for {FormatAge(age)} (threshold: 5m)"
        };

        return isHealthy
            ? Results.Ok(body)
            : Results.Json(body, statusCode: 500);
    }

    public async Task<IResult> CheckAccount(int id)
    {
        var account = await _accountRepository.Get(a => a.Id == id);
        if (account == null)
            return Results.NotFound(new { status = false, message = $"Account {id} not found" });

        var age = DateTime.Now - account.UpdatedAt;
        var isHealthy = age <= Threshold;

        var body = new
        {
            status = isHealthy,
            name = $"{account.BrokerName} ({account.AccountNumber})",
            platform = account.PlatformName,
            role = account.Role,
            updated_at = account.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            age_seconds = (int)age.TotalSeconds,
            message = isHealthy
                ? "OK"
                : $"Not updated for {FormatAge(age)} (threshold: 5m)"
        };

        return isHealthy
            ? Results.Ok(body)
            : Results.Json(body, statusCode: 500);
    }

    private static string FormatAge(TimeSpan age)
    {
        if (age.TotalHours >= 1)
            return $"{(int)age.TotalHours}h {age.Minutes}m {age.Seconds}s";
        if (age.TotalMinutes >= 1)
            return $"{(int)age.TotalMinutes}m {age.Seconds}s";
        return $"{(int)age.TotalSeconds}s";
    }
}
