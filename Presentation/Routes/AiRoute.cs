using Backend.Model;
using Backend.Presentation.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Presentation.Routes;

public static class AiRoutes
{
    public static void MapAiRoutes(this RouteGroupBuilder group)
    {
        group
            .MapGet(
                "/trader/ai/chat/sessions",
                async (AiHandler handler) =>
                {
                    return await handler.GetSessions();
                }
            )
            .WithName("GetAiSessions")
            .WithTags("AI");

        group
            .MapPost(
                "/trader/ai/chat/sessions",
                async ([FromBody] AiChatSessionPayload payload, AiHandler handler) =>
                {
                    return await handler.CreateSession(payload);
                }
            )
            .WithName("CreateAiSession")
            .WithTags("AI");

        group
            .MapGet(
                "/trader/ai/chat/sessions/{sessionId:long}/messages",
                async (long sessionId, AiHandler handler) =>
                {
                    return await handler.GetMessages(sessionId);
                }
            )
            .WithName("GetAiMessages")
            .WithTags("AI");

        group
            .MapPost(
                "/trader/ai/chat/stream",
                async (HttpContext context, [FromBody] AiChatStreamPayload payload, AiHandler handler) =>
                {
                    await handler.ChatStream(context, payload);
                }
            )
            .WithName("AiChatStream")
            .WithTags("AI");

        group
            .MapPost(
                "/trader/ai/analysis/strategy",
                async ([FromBody] AiAnalysisPayload payload, AiHandler handler) =>
                {
                    return await handler.AnalyzeStrategy(payload);
                }
            )
            .WithName("AiAnalyzeStrategy")
            .WithTags("AI");

        group
            .MapPost(
                "/trader/ai/analysis/risk",
                async ([FromBody] AiAnalysisPayload payload, AiHandler handler) =>
                {
                    return await handler.AnalyzeRisk(payload);
                }
            )
            .WithName("AiAnalyzeRisk")
            .WithTags("AI");
    }
}
