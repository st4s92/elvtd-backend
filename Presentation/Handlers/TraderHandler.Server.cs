using Backend.Application.Usecases;
using Backend.Helper;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Handlers;

public partial class TraderHandler
{
    public async Task<IResult> AddServer(ServerCreatePayload serverPayload)
    {
        var server = new Server
        {
            ServerName = serverPayload.ServerName ?? "",
            ServerIp = serverPayload.ServerIp ?? "",
            ServerOs = serverPayload.ServerOs ?? "",
        };

        if (server.ServerName == "")
        {
            return Response.Json(TError.NewClient("Server Name should be filled"));
        }
        if (server.ServerIp == "")
        {
            return Response.Json(TError.NewClient("Server IP should be filled"));
        }
        if (server.ServerOs == "")
        {
            return Response.Json(TError.NewClient("Server OS should be filled"));
        }
        var (res, terr) = await _usecase.AddServer(server);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetServer(int id)
    {
        var (res, terr) = await _usecase.GetServer(new Server { Id = id });
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetServers(ServerGetPayload query)
    {
        var accountFilter = new Server
        {
            Id = query.Id ?? 0,
            ServerName = query.ServerName ?? "",
            ServerIp = query.ServerIp ?? "",
            ServerOs = query.ServerOs ?? "",
        };

        var (res, terr) = await _usecase.GetServers(accountFilter);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetPaginatedServers(ServerGetPaginatedPayload query)
    {
        var accountFilter = new Server
        {
            Id = query.Id ?? 0,
            ServerName = query.ServerName ?? "",
            ServerIp = query.ServerIp ?? "",
            ServerOs = query.ServerOs ?? "",
        };

        var (res, total, terr) = await _usecase.GetPaginatedServers(accountFilter, query.Page, query.PerPage);
        if (terr != null)
        {
            return Response.Json(terr);
        }

        var resp = new GetPaginatedResponse<Server>
        {
            Data = res,
            Total = total
        };
        return Response.Json(resp);
    }

    public async Task<IResult> UpdateServer(long id, Server payload)
    {
        if (id == 0)
        {
            var terrs = TError.NewClient("Id should be filled");
            return Response.Json(terrs);
        }
        if (payload == null)
        {
            var terrs = TError.NewClient("Invalid payload");
            return Response.Json(terrs);
        }

        var (_, terr) = await _usecase.UpdateServerById(id, payload);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json("ok");
    }

    public async Task<IResult> DeleteServer(long id)
    {
        var terr = await _usecase.DeleteServerByID(id);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json("ok");
    }

    public async Task<IResult> ReassignStaleAccounts(int minutes = 60, long? accountId = null)
    {
        var (count, terr) = await _usecase.ReassignStaleAccounts(minutes, accountId);
        if (terr != null)
            return Response.Json(terr);

        var msg = accountId.HasValue
            ? $"Deleted {count} server-account assignments for accountId={accountId.Value}"
            : $"Deleted {count} stale server-account assignments (>{minutes}min)";
        return Response.Json(new { reassigned = count, message = msg });
    }
}