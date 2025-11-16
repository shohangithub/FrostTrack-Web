using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace FrostTrack.Server.Middlewares;

public class AuthenticationDebugMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        Debug.WriteLine($"=== REQUEST: {context.Request.Method} {context.Request.Path} ===");
        Debug.WriteLine($"Authorization Header: {authHeader?.Substring(0, Math.Min(50, authHeader?.Length ?? 0))}...");

        await next(context);

        Debug.WriteLine($"After pipeline - IsAuthenticated: {context.User.Identity?.IsAuthenticated}");
        Debug.WriteLine($"Claims count: {context.User.Claims.Count()}");
        Debug.WriteLine($"Response Status: {context.Response.StatusCode}");
        Debug.WriteLine("=== END REQUEST ===");
    }
}