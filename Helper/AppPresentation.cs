using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;

namespace Backend.Helper;

public record ApiResponse<T>(bool Status, int Code, T? Data, string? Message = null);

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