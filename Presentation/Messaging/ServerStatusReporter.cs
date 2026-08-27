using Backend.Application.Interfaces;
using Backend.Infrastructure.Messaging;
using Backend.Model;
using Microsoft.Extensions.Hosting;

namespace Backend.Presentation.Messaging;

public class ServerStatusReporter : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ServerStatusReporter(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Environment.GetEnvironmentVariable("ENABLE_STATUS_REPORTER") != "true")
        {
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendStatusReport();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ServerStatusReporter] error: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task SendStatusReport()
    {
        using var scope = _scopeFactory.CreateScope();
        var serverRepo = scope.ServiceProvider.GetRequiredService<IServerRepository>();
        var telegram = scope.ServiceProvider.GetRequiredService<ITelegramNotifier>();

        var servers = await serverRepo.GetMany(s => s.DeletedAt == null);
        if (servers.Count == 0) return;

        var now = DateTime.UtcNow;
        var onlineThreshold = now.AddMinutes(-5);

        // Check if any server has critical issues
        var criticalServers = new List<string>();
        int totalTerminals = 0;
        int onlineCount = 0;
        int offlineCount = 0;

        foreach (var s in servers.OrderBy(s => s.ServerIp))
        {
            if (s.ServerIp == "string" || s.ServerIp == "10.10.10.10") continue;

            bool isOnline = s.UpdatedAt > onlineThreshold;
            if (isOnline) onlineCount++; else offlineCount++;
            totalTerminals += s.ActiveTerminals;

            // Detect critical conditions
            var issues = new List<string>();
            if (!isOnline) issues.Add("OFFLINE");
            if (s.CpuUsage > 90) issues.Add($"CPU {s.CpuUsage:F0}%");
            if (s.RamUsage > 90) issues.Add($"RAM {s.RamUsage:F0}%");
            if (s.DiskTotalGb > 0 && (s.DiskUsedGb / s.DiskTotalGb * 100) > 85)
                issues.Add($"DISK {s.DiskUsedGb:F0}/{s.DiskTotalGb:F0}GB ({s.DiskUsedGb / s.DiskTotalGb * 100:F0}%)");

            if (issues.Count > 0)
            {
                criticalServers.Add($"🔴 <b>{s.ServerName}</b>: {string.Join(", ", issues)}");
            }
        }

        // Only send alert if there are critical issues
        if (criticalServers.Count == 0) return;

        var lines = new List<string>();
        lines.Add($"⚠️ <b>ELVTD Server Alert ({now:dd.MM HH:mm} UTC)</b>");
        lines.Add("");
        lines.AddRange(criticalServers);
        lines.Add("");
        lines.Add($"📈 {onlineCount} online, {offlineCount} offline, {totalTerminals} terminals");

        var message = string.Join("\n", lines);
        await telegram.SendAlert(message);
    }
}
