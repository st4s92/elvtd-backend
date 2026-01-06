using Backend.Application.Usecases;
using Backend.Application.Interfaces;
using Backend.Model;
using System.Text.Json;

namespace Backend.Presentation.Handlers;

public class ServerHeartbeatHandler
{
    private readonly TraderUsecase _traderUsecase;

    public ServerHeartbeatHandler(TraderUsecase traderUsecase)
    {
        _traderUsecase = traderUsecase;
    }

    public async Task HandleAsync(ServerHeartbeatRequest request)
    {
        var data = JsonSerializer.Serialize(
            request,
            new JsonSerializerOptions { WriteIndented = true }
        );
        Console.WriteLine($"🟡 Handler invoked with request={data}");
        // minimal validation
        if (string.IsNullOrWhiteSpace(request.Ip))
            return;

        var (resp, terr) = await _traderUsecase.UpdateHealthCheck(request);
        if(terr != null)
        {
            Console.WriteLine(resp);
        }
    }
}
