using Backend.Helper;

namespace Backend.Presentation.Middleware;

public class NotFoundMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<NotFoundMiddleware> _logger;

    public NotFoundMiddleware(RequestDelegate next, ILogger<NotFoundMiddleware> logger)
    {
        _next = next;
         _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.StatusCode == StatusCodes.Status404NotFound && !context.Response.HasStarted)
        {
            var terr = TError.NewNotFound("Resource not found");
            _logger.LogError(terr.ToString());

            // Build IResult using your Response.Json helper
            var result = Response.Json(terr);

            // Reset the response
            context.Response.Clear();

            // Execute the result manually
            await result.ExecuteAsync(context);
        } 
    }
}
