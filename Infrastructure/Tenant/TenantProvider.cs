using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Application.Contractors.Authentication;

namespace Infrastructure.Tenant;

public sealed class TenantProvider : ITenantProvider
{
    private const string TenantHeaderName = "X-Tenant";
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IConfiguration _configuration;
    public TenantProvider(IHttpContextAccessor contextAccessor, IConfiguration configuration)
    {
        _contextAccessor = contextAccessor;
        _configuration = configuration;

    }

    public Guid GetTenantId()
    {

        //if (_contextAccessor.HttpContext is null)
        //    return new Guid("11223344-5566-7788-99AA-BBCCDDEEFF00"); //default tenant id


        var tenantHeader = _contextAccessor.HttpContext?.Request.Headers[TenantHeaderName];
        if (!tenantHeader.HasValue || !Guid.TryParse(tenantHeader.Value, out Guid tenantId) || !Tenants.All.Contains(tenantId))
        {
            var defaultTenantId = _configuration.GetSection("DefaultTenant:TenantId").Get<string>();
            if (defaultTenantId is null)
                throw new ApplicationException("Tenant header is not found !");

            return new Guid(defaultTenantId);
            // return Guid.Empty;
        }

        return tenantId;
    }
}
