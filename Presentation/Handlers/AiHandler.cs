using Backend.Application.Usecases;
using Backend.Helper;
using Backend.Model;

namespace Backend.Presentation.Handlers;

public class AiHandler
{
    private readonly AiUsecase _usecase;

    public AiHandler(AiUsecase usecase)
    {
        _usecase = usecase;
    }

    public async Task<IResult> GetSessions()
    {
        // TODO: extract userId from JWT — for now use 1
        long userId = 1;
        var (res, terr) = await _usecase.GetSessions(userId);
        if (terr != null) return Response.Json(terr);
        return Response.Json(res);
    }

    public async Task<IResult> CreateSession(AiChatSessionPayload payload)
    {
        long userId = 1;
        var (res, terr) = await _usecase.CreateSession(userId, payload);
        if (terr != null) return Response.Json(terr);
        return Response.Json(res);
    }

    public async Task<IResult> GetMessages(long sessionId)
    {
        var (res, terr) = await _usecase.GetMessages(sessionId);
        if (terr != null) return Response.Json(terr);
        return Response.Json(res);
    }

    public async Task ChatStream(HttpContext context, AiChatStreamPayload payload)
    {
        long userId = 1;

        if (string.IsNullOrWhiteSpace(payload.Message))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Message is required");
            return;
        }

        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        await foreach (var chunk in _usecase.ChatStream(userId, payload, context.RequestAborted))
        {
            await context.Response.WriteAsync(chunk, context.RequestAborted);
            await context.Response.Body.FlushAsync(context.RequestAborted);
        }
    }

    public async Task<IResult> AnalyzeStrategy(AiAnalysisPayload payload)
    {
        var (res, terr) = await _usecase.AnalyzeStrategy(payload.AccountId);
        if (terr != null) return Response.Json(terr);
        return Response.Json(res);
    }

    public async Task<IResult> AnalyzeRisk(AiAnalysisPayload payload)
    {
        var (res, terr) = await _usecase.AnalyzeRisk(payload.AccountId);
        if (terr != null) return Response.Json(terr);
        return Response.Json(res);
    }
}
