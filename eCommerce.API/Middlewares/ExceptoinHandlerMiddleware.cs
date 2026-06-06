using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Threading.Tasks;

namespace eCommerce.API.Middlewares;

public class ExceptoinHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptoinHandlerMiddleware> _logger;
    public ExceptoinHandlerMiddleware(RequestDelegate next, ILogger<ExceptoinHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch(Exception ex)
        {
            _logger.LogError($"Type: {ex.GetType().Name} , Details: {ex.Message}");
            if(ex.InnerException is not null)
                _logger.LogError($"Type: {ex.InnerException.GetType().Name} , Details: {ex.InnerException.Message}");

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;


            await httpContext.Response.WriteAsJsonAsync(new { Type = ex.GetType().Name, Message = ex.Message });
        }
    }
}

public static class ExceptoinHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptoinHandlerMiddleware>();
    }
}
