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
        // Wait 60s after startup before first report
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

        var lines = new List<string>();
        lines.Add($"📊 <b>Server Status ({now:dd.MM HH:mm} UTC)</b>");
        lines.Add("");

        int totalTerminals = 0;
        int onlineCount = 0;
        int offlineCount = 0;
        int diskWarnings = 0;

        foreach (var s in servers.OrderBy(s => s.ServerIp))
        {
            // Skip dummy/invalid servers
            if (s.ServerIp == "string" || s.ServerIp == "10.10.10.10") continue;

            bool isOnline = s.UpdatedAt > onlineThreshold;
            var status = isOnline ? "🟢" : "🔴";
            if (isOnline) onlineCount++; else offlineCount++;
            totalTerminals += s.ActiveTerminals;

            var diskInfo = "";
            if (s.DiskTotalGb > 0)
            {
                var diskPct = s.DiskUsedGb / s.DiskTotalGb * 100;
                diskInfo = $" | 💾 {s.DiskUsedGb:F0}/{s.DiskTotalGb:F0}GB ({diskPct:F0}%)";
                if (diskPct > 85) diskWarnings++;
            }

            lines.Add($"{status} <b>{s.ServerName}</b>");
            lines.Add($"   {s.ActiveTerminals}T | CPU {s.CpuUsage:F0}% | RAM {s.RamUsage:F0}%{diskInfo}");
        }

        lines.Add("");
        lines.Add($"📈 {onlineCount} online, {offlineCount} offline, {totalTerminals} terminals");
        if (diskWarnings > 0)
            lines.Add($"⚠️ {diskWarnings} server mit >85% Festplatte!");

        var message = string.Join("\n", lines);
        await telegram.SendAlert(message);
    }
}
