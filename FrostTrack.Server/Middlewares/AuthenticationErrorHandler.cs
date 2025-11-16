namespace FrostTrack.Server.Middlewares;

public class AuthenticationErrorHandler : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await next(context);

        if (context.Response.StatusCode == (int)HttpStatusCode.Unauthorized)
        {
            string jsonString = JsonSerializer.Serialize(new ErrorResponse(Errors: new Dictionary<string, string[]>(),
                                                                           Type: "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                                                                           Title: "Unauthorized",
                                                                           Status: (int)HttpStatusCode.Unauthorized,
                                                                           Message: "This user can not access this api !",
                                                                           TraceId: ""));
            await context.Response.WriteAsync(jsonString);
        }
    }
}
