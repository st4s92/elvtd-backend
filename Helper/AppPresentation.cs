using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace Backend.Helper;

public record ApiResponse<T>(
    [property: JsonPropertyName("status")] bool Status,
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("data")] T? Data,
    [property: JsonPropertyName("message")] string? Message = null
);

public static class Response
{
    public static IResult Json<T>(T data, string? message = null)
    {
        if (data is ITError terr)
        {
            // Map to appropriate HTTP response
            var failResult = new ApiResponse<T>(
                Status: false,
                Code: terr.Code,
                Data: data,
                Message: message
            );
            return Results.Ok(failResult);
        }
        var okResult = new ApiResponse<T>(
            Status: true,
            Code: StatusCodes.Status200OK,
            Data: data,
            Message: message
        );
        return Results.Ok(okResult);
    }
}