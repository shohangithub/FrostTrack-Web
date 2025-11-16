using Application.Contractors.Authentication;
using Domain.Enums;
using Infrastructure.Authentication;
using Infrastructure.Authentication.OptionSetup;
using Infrastructure.Authentication.TokenGenerator;
using Infrastructure.BackgroundServices;
using Infrastructure.Tenant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register CurrentUserProvider and JWT User Service
        services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
        services.AddScoped<IJwtUserService, JwtUserService>();
        services.AddScoped<IUserContextService, UserContextService>();
        services
            .AddHttpContextAccessor()
            .AddMultiTenantServices()
            .ManageAuthentication(configuration)
            .ManageAuthorization()
            .AddBackgroundServices(configuration);

        return services;
    }


    private static IServiceCollection ManageAuthentication(this IServiceCollection services, IConfiguration configuration)
    {

        #region JWT Authentication
        // configure strongly typed settings objects
        services.ConfigureOptions<ConfigureJwtOptions>();

        // stopre default cookie settings and override redirect to login path for API calls
        services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = context =>
            {
                // override unauthorize access redirect path
                if (context.Request.Path.StartsWithSegments("/api") && context.Response.StatusCode == StatusCodes.Status200OK)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });
        // configure jwt authentication
        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                RequireExpirationTime = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                ClockSkew = TimeSpan.Zero, //the default for this setting is 5 minutes
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
            };

            // Add debugging events
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    System.Diagnostics.Debug.WriteLine($"JWT Authentication failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    System.Diagnostics.Debug.WriteLine($"JWT Token validated successfully for user: {context.Principal?.Identity?.Name}");
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    System.Diagnostics.Debug.WriteLine($"JWT Token received: {context.Token?.Substring(0, Math.Min(50, context.Token?.Length ?? 0))}...");
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    System.Diagnostics.Debug.WriteLine($"JWT Challenge: {context.Error} - {context.ErrorDescription}");
                    return Task.CompletedTask;
                }
            };
        });

        services.AddTransient<IJwtProvider, JwtProvider>();

        #endregion

        return services;
    }

    private static IServiceCollection ManageAuthorization(this IServiceCollection services)
    {
        //services.AddScoped<Application.Contractors.Common.IAuthorizationService, AuthorizationService>();
        //services.AddSingleton<IPolicyEnforcer, PolicyEnforcer>();
        //services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        //services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();


        //register authorization handler

        /*
         * Authorization Policies Summary:
         * 
         * Role-based Policies:
         * - RequireSuperAdmin: Only SUPERADMIN role
         * - RequireAdmin: Only ADMIN role  
         * - RequireManager: Only MANAGER role
         * - RequireSeller: Only SELLER role
         * - RequireStandard: Only STANDARD role
         * 
         * Multi-role Policies:
         * - RequireAdminOrManager: SUPERADMIN, ADMIN, MANAGER
         * - RequireAdminLevel: SUPERADMIN, ADMIN
         * 
         * Feature-based Policies:
         * - CanManageProducts: SUPERADMIN, ADMIN, MANAGER (for ProductCategory, Product, etc.)
         * - CanManageSales: SUPERADMIN, ADMIN, MANAGER, SELLER (for Sales, SaleReturn, etc.)
         * - CanViewReports: SUPERADMIN, ADMIN, MANAGER
         * 
         * Department/Claim-based Policies:
         * - DepartmentAccounts: Department claim = "Accounts"
         * - DepartmentIt: Department claim = "It"
         * - AdminInAccounts: ADMIN role + Department claim = "Accounts"
         * 
         * Usage in Controllers:
         * [Authorize(Policy = "CanManageProducts")] - For product-related controllers
         * [Authorize(Policy = "CanManageSales")] - For sales-related controllers
         * [Authorize(Policy = "RequireAdminLevel")] - For admin-only operations like delete
         * [AllowAnonymous] - For public endpoints like lookups
         */

        // Authorization policies (roles and department claims)
        services.AddAuthorization(options =>
        {
            // role based policies - Using imported RoleNames constants
            options.AddPolicy("RequireSuperAdmin", policy => policy.RequireRole(RoleNames.SuperAdmin));
            options.AddPolicy("RequireAdmin", policy => policy.RequireRole(RoleNames.Admin));
            options.AddPolicy("RequireManager", policy => policy.RequireRole(RoleNames.Manager));
            options.AddPolicy("RequireSeller", policy => policy.RequireRole(RoleNames.Seller));
            options.AddPolicy("RequireStandard", policy => policy.RequireRole(RoleNames.Standard));

            // Multiple role policies
            options.AddPolicy("RequireAdminOrManager", policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.Admin, RoleNames.Manager));

            options.AddPolicy("RequireAdminLevel", policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.Admin));

            // department claim based policies
            options.AddPolicy("DepartmentAccounts", policy => policy.RequireClaim("Department", "Accounts"));
            options.AddPolicy("DepartmentIt", policy => policy.RequireClaim("Department", "It"));

            // example combined policy: Admin in Accounts
            options.AddPolicy("AdminInAccounts", policy =>
                policy.RequireRole(RoleNames.Admin).RequireClaim("Department", "Accounts"));

            // Permission-based policies for fine-grained control
            options.AddPolicy("CanManageProducts", policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.Admin, RoleNames.Manager));

            options.AddPolicy("CanManageSales", policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.Admin, RoleNames.Manager, RoleNames.Seller));

            options.AddPolicy("CanViewReports", policy =>
                policy.RequireRole(RoleNames.SuperAdmin, RoleNames.Admin, RoleNames.Manager));
        });


        return services;
    }


    private static IServiceCollection AddBackgroundServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEmailNotifications(configuration);

        return services;
    }

    private static IServiceCollection AddEmailNotifications(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        EmailSettings emailSettings = new();
        configuration.Bind(EmailSettings.Section, emailSettings);

        if (!emailSettings.EnableEmailNotifications)
        {
            return services;
        }

        services.AddHostedService<EmailBackgroundService>();

        services
            .AddFluentEmail(emailSettings.DefaultFromEmail)
            .AddSmtpSender(new SmtpClient(emailSettings.SmtpSettings.Server)
            {
                Port = emailSettings.SmtpSettings.Port,
                Credentials = new NetworkCredential(
                    emailSettings.SmtpSettings.Username,
                    emailSettings.SmtpSettings.Password),
            });

        return services;
    }

    private static IServiceCollection AddMultiTenantServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, TenantProvider>();

        return services;
    }

}
