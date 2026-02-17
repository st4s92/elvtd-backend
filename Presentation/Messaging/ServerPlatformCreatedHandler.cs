using System.Text.Json;
using Backend.Application.Interfaces;
using Backend.Application.Usecases;
using Backend.Model;

namespace Backend.Presentation.Handlers;

public class ServerPlatformCreatedHandler
{
    private readonly TraderUsecase _traderUsecase;

    public ServerPlatformCreatedHandler(TraderUsecase traderUsecase)
    {
        _traderUsecase = traderUsecase;
    }

    public async Task HandleAsync(TradePlatformCreatedEvent request)
    {
        var req = JsonSerializer.Serialize(
            request,
            new JsonSerializerOptions { WriteIndented = true }
        );
        Console.WriteLine($"🟡 Handler invoked created platform with request={req}");

        // minimal validation
        if (request.AccountId == 0)
            return;

        var data = new ServerAccountPlatformUpdateRequest
        {
            AccountId = request.AccountId,
            InstallationPath = request.InstallationPath,
            Status = (ConnectionStatus)request.Status,
            Message = request.Message,
            Pid = request.Pid,
        };

        await _traderUsecase.UpdateAccountServerData(data);
    }
}
