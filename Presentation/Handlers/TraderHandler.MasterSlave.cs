using Backend.Application.Usecases;
using Backend.Helper;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Handlers;

public partial class TraderHandler
{
    public async Task<IResult> AddMasterSlave(MasterSlavePayload masterSlavePayload)
    {
        var masterSlave = new MasterSlave
        {
            MasterId = masterSlavePayload.MasterId ?? 0,
            SlaveId = masterSlavePayload.SlaveId ?? 0,
            Name = masterSlavePayload.Name ?? "",
        };

        if (masterSlave.MasterId == 0 || masterSlave.SlaveId == 0)
        {
            return Response.Json(TError.NewClient("MasterSlave id should be filled"));
        }
        if (string.IsNullOrEmpty(masterSlave.Name))
        {
            return Response.Json(TError.NewClient("master slave name should be filled"));
        }
        var (res, terr) = await _usecase.AddMasterSlave(masterSlave);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetMasterSlave(int id)
    {
        var (res, terr) = await _usecase.GetMasterSlave(new MasterSlave { Id = id });
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetMasterSlaves(MasterSlaveGetPayload query)
    {
        var masterSlaveFilter = new MasterSlave
        {
            Id = query.Id ?? 0,
            MasterId = query.MasterId ?? 0,
            SlaveId = query.SlaveId ?? 0,
            Name = query.Name ?? "",
        };

        var (res, terr) = await _usecase.GetMasterSlaves(masterSlaveFilter);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetPaginatedMasterSlaves(MasterSlaveGetPaginatedPayload query)
    {
        var masterSlaveFilter = new MasterSlave
        {
            Id = query.Id ?? 0,
            MasterId = query.MasterId ?? 0,
            SlaveId = query.SlaveId ?? 0,
            Name = query.Name ?? "",
        };

        var (res, total, terr) = await _usecase.GetPaginatedMasterSlaves(masterSlaveFilter, query.Page, query.PageSize);
        if (terr != null)
        {
            return Response.Json(terr);
        }

        var resp = new GetPaginatedResponse<MasterSlave>
        {
            Data = res,
            Total = total
        };
        return Response.Json(resp);
    }

    public async Task<IResult> UpdateMasterSlave(long id, MasterSlave payload)
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

        var (_, terr) = await _usecase.UpdateMasterSlaveById(id, payload);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json("ok");
    }

    public async Task<IResult> EditMasterSlaveFullonfig(MasterSlaveFullConfigPayload payload)
    {
        if (payload == null)
        {
            return Response.Json(TError.NewClient("Invalid payload"));
        }

        if (string.IsNullOrEmpty(payload.ConnectionName))
        {
            return Response.Json(TError.NewClient("Connection name must be filled"));
        }

        if (payload.AccountId == 0 || payload.DestinationId == 0)
        {
            return Response.Json(TError.NewClient("AccountId and DestinationId must be filled"));
        }

        if (payload.Multiplier < 0.1m || payload.Multiplier > 10m)
        {
            return Response.Json(TError.NewClient("Multiplier must be between 0.1 and 10"));
        }

        var (res, terr) = await _usecase.EditMasterSlaveFullonfig(payload);
        if (terr != null)
        {
            return Response.Json(terr);
        }

        return Response.Json(new
        {
            id = res.Id,
            name = res.Name,
            master_id = res.MasterId,
            slave_id = res.SlaveId
        });
    }


}