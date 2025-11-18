using Backend.Application.Usecases;
using Backend.Helper;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Handlers;

public partial class TraderHandler
{
    public async Task<IResult> AddMasterSlaveConfig(MasterSlaveConfigPayload masterSlaveConfigPayload)
    {
        var masterSlaveConfig = new MasterSlaveConfig
        {
            MasterSlaveId = masterSlaveConfigPayload.MasterSlaveId ?? 0,
            Multiplier = masterSlaveConfigPayload.Multiplier ?? 0,
        };

        if (masterSlaveConfig.MasterSlaveId == 0)
        {
            return Response.Json(TError.NewClient("master slave id should be filled"));
        }
        if (masterSlaveConfig.Multiplier == 0)
        {
            return Response.Json(TError.NewClient("multiplier should be more than 0"));
        }
        var (res, terr) = await _usecase.AddMasterSlaveConfig(masterSlaveConfig);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetMasterSlaveConfig(int id)
    {
        var (res, terr) = await _usecase.GetMasterSlaveConfig(new MasterSlaveConfig { Id = id });
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetMasterSlaveConfigs(MasterSlaveConfigGetPayload query)
    {
        var masterSlaveConfigFilter = new MasterSlaveConfig
        {
            Id = query.Id ?? 0,
            MasterSlaveId = query.MasterSlaveId ?? 0,
            Multiplier = query.Multiplier ?? 0,
        };

        var (res, terr) = await _usecase.GetMasterSlaveConfigs(masterSlaveConfigFilter);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetPaginatedMasterSlaveConfigs(MasterSlaveConfigGetPaginatedPayload query)
    {
        var masterSlaveConfigFilter = new MasterSlaveConfig
        {
            Id = query.Id ?? 0,
            MasterSlaveId = query.MasterSlaveId ?? 0,
            Multiplier = query.Multiplier ?? 0,
        };

        var (res, total, terr) = await _usecase.GetPaginatedMasterSlaveConfigs(masterSlaveConfigFilter, query.Page, query.PageSize);
        if (terr != null)
        {
            return Response.Json(terr);
        }

        var resp = new GetPaginatedResponse<MasterSlaveConfig>
        {
            Data = res,
            Total = total
        };
        return Response.Json(resp);
    }

    public async Task<IResult> UpdateMasterSlaveConfig(long id, MasterSlaveConfig payload)
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

        var (_, terr) = await _usecase.UpdateMasterSlaveConfigById(id, payload);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json("ok");
    }
}