using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Authentication;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class ApiKeyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (!IsApiKeyValid(context.HttpContext))
            context.Result = new UnauthorizedResult();
    }

    private static bool IsApiKeyValid(HttpContext context)
    {
        string? apikey = context.Request.Headers["API_KEY"]!;
        if (string.IsNullOrEmpty(apikey)) return false;
        var actualApiKey = context.RequestServices.GetRequiredService<IConfiguration>().GetValue<string>("API_KEY")!;
        return apikey == actualApiKey;
    }
}
