using FrostTrack.Server.Middlewares;
using System.Text.Json;
using Infrastructure.Converters;

namespace FrostTrack.Server;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {

        services.AddTransient<AuthenticationErrorHandler>();
        services.AddTransient<AuthenticationDebugMiddleware>();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        
        // Configure Newtonsoft.Json to handle UTC dates
        services.AddControllers().AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
            options.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat;
        });

        // Also configure System.Text.Json for services that use it
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
            options.JsonSerializerOptions.Converters.Add(new UtcNullableDateTimeConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        });

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        #region register exception service

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        #endregion


        return services;
    }
}

