using Backend.Application.Usecases;
using Backend.Helper;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Handlers;

public partial class TraderHandler
{
    public async Task<IResult> AddMasterSlavePair(MasterSlavePairPayload masterSlavePairPayload)
    {
        var masterSlavePair = new MasterSlavePair
        {
            MasterSlaveId = masterSlavePairPayload.MasterSlaveId ?? 0,
            SlavePair = masterSlavePairPayload.SlavePair ?? "",
            MasterPair = masterSlavePairPayload.MasterPair ?? "",
        };

        if (string.IsNullOrEmpty(masterSlavePair.MasterPair) || string.IsNullOrEmpty(masterSlavePair.SlavePair))
        {
            return Response.Json(TError.NewClient("master / slave pair should be filled"));
        }
        if (masterSlavePair.MasterSlaveId == 0)
        {
            return Response.Json(TError.NewClient("masterslave id should be filled"));
        }
        var (res, terr) = await _usecase.AddMasterSlavePair(masterSlavePair);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetMasterSlavePair(int id)
    {
        var (res, terr) = await _usecase.GetMasterSlavePair(new MasterSlavePair { Id = id });
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetMasterSlavePairs(MasterSlavePairGetPayload query)
    {
        var masterSlavePairFilter = new MasterSlavePair
        {
            Id = query.Id ?? 0,
            MasterSlaveId = query.MasterSlaveId ?? 0,
            SlavePair = query.SlavePair ?? "",
            MasterPair = query.MasterPair ?? "",
        };

        var (res, terr) = await _usecase.GetMasterSlavePairs(masterSlavePairFilter);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json(res);
    }

    public async Task<IResult> GetPaginatedMasterSlavePairs(MasterSlavePairGetPaginatedPayload query)
    {
        var masterSlavePairFilter = new MasterSlavePair
        {
            Id = query.Id ?? 0,
            MasterSlaveId = query.MasterSlaveId ?? 0,
            SlavePair = query.SlavePair ?? "",
            MasterPair = query.MasterPair ?? "",
        };

        var (res, total, terr) = await _usecase.GetPaginatedMasterSlavePairs(masterSlavePairFilter, query.Page, query.PageSize);
        if (terr != null)
        {
            return Response.Json(terr);
        }

        var resp = new GetPaginatedResponse<MasterSlavePair>
        {
            Data = res,
            Total = total
        };
        return Response.Json(resp);
    }

    public async Task<IResult> UpdateMasterSlavePair(long id, MasterSlavePair payload)
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

        var (_, terr) = await _usecase.UpdateMasterSlavePairById(id, payload);
        if (terr != null)
        {
            return Response.Json(terr);
        }
        return Response.Json("ok");
    }
}