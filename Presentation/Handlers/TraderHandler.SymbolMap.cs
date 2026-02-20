using Backend.Helper;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Handlers;

public partial class TraderHandler
{
    public async Task<IResult> GetSymbolMaps()
    {
        var (res, terr) = await _usecase.GetSymbolMaps();
        if (terr != null)
            return Response.Json(terr);
        return Response.Json(res);
    }

    public async Task<IResult> GetCanonicalSymbols()
    {
        var (res, terr) = await _usecase.GetCanonicalSymbols();
        if (terr != null)
            return Response.Json(terr);
        return Response.Json(res);
    }

    public async Task<IResult> CreateSymbolMap(SymbolMapPayload payload)
    {
        if (string.IsNullOrEmpty(payload.BrokerName) || string.IsNullOrEmpty(payload.BrokerSymbol) || string.IsNullOrEmpty(payload.CanonicalSymbol))
        {
            return Response.Json(TError.NewClient("broker_name, broker_symbol, and canonical_symbol are required"));
        }

        var (res, terr) = await _usecase.CreateSymbolMap(payload);
        if (terr != null)
            return Response.Json(terr);
        return Response.Json(res);
    }

    public async Task<IResult> UpdateSymbolMap(long id, SymbolMapPayload payload)
    {
        if (id == 0)
            return Response.Json(TError.NewClient("Id is required"));

        var (res, terr) = await _usecase.UpdateSymbolMap(id, payload);
        if (terr != null)
            return Response.Json(terr);
        return Response.Json(res);
    }

    public async Task<IResult> DeleteSymbolMap(long id)
    {
        if (id == 0)
            return Response.Json(TError.NewClient("Id is required"));

        var terr = await _usecase.DeleteSymbolMap(id);
        if (terr != null)
            return Response.Json(terr);
        return Response.Json("ok");
    }
}
