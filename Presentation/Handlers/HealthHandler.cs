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

        if (!isHealthy)
            return Results.Json(new
            {
                status = false,
                name = server.ServerName,
                updated_at = server.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                age_seconds = (int)age.TotalSeconds,
                message = $"Not updated for {FormatAge(age)} (threshold: 5m)"
            }, statusCode: 500);

        return Results.Content(BuildHtml(
            "Server", server.ServerName, server.ServerIp, server.UpdatedAt, age
        ), "text/html");
    }

    public async Task<IResult> CheckAccount(int id)
    {
        var account = await _accountRepository.Get(a => a.Id == id);
        if (account == null)
            return Results.NotFound(new { status = false, message = $"Account {id} not found" });

        var age = DateTime.Now - account.UpdatedAt;
        var isHealthy = age <= Threshold;

        var name = $"{account.BrokerName} ({account.AccountNumber})";

        if (!isHealthy)
            return Results.Json(new
            {
                status = false,
                name,
                platform = account.PlatformName,
                updated_at = account.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                age_seconds = (int)age.TotalSeconds,
                message = $"Not updated for {FormatAge(age)} (threshold: 5m)"
            }, statusCode: 500);

        return Results.Content(BuildHtml(
            account.PlatformName, name, account.Role, account.UpdatedAt, age
        ), "text/html");
    }

    private static string BuildHtml(string type, string name, string detail, DateTime updatedAt, TimeSpan age)
    {
        return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <title>Health OK - {name}</title>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background: #0f172a; color: #e2e8f0; display: flex; justify-content: center; align-items: center; min-height: 100vh; margin: 0; }}
    .card {{ background: #1e293b; border-radius: 12px; padding: 32px 40px; box-shadow: 0 4px 24px rgba(0,0,0,0.3); min-width: 340px; }}
    .status {{ display: flex; align-items: center; gap: 10px; margin-bottom: 20px; }}
    .dot {{ width: 14px; height: 14px; border-radius: 50%; background: #22c55e; box-shadow: 0 0 8px #22c55e; }}
    .status h1 {{ font-size: 22px; margin: 0; color: #22c55e; }}
    .row {{ display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #334155; }}
    .row:last-child {{ border-bottom: none; }}
    .label {{ color: #94a3b8; font-size: 14px; }}
    .value {{ color: #f1f5f9; font-size: 14px; font-weight: 500; }}
  </style>
</head>
<body>
  <div class=""card"">
    <div class=""status""><div class=""dot""></div><h1>Healthy</h1></div>
    <div class=""row""><span class=""label"">Type</span><span class=""value"">{type}</span></div>
    <div class=""row""><span class=""label"">Name</span><span class=""value"">{name}</span></div>
    <div class=""row""><span class=""label"">Detail</span><span class=""value"">{detail}</span></div>
    <div class=""row""><span class=""label"">Last Update</span><span class=""value"">{updatedAt:yyyy-MM-dd HH:mm:ss}</span></div>
    <div class=""row""><span class=""label"">Age</span><span class=""value"">{FormatAge(age)}</span></div>
  </div>
</body>
</html>";
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
