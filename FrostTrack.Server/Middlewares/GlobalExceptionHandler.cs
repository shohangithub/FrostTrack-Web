using Application.Framework;
using Application.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace FrostTrack.Server.Middlewares;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };


        httpContext.Response.ContentType = "application/json";
        logger.LogError(exception, "Exception occured : {Message}", exception.Message);
        logger.LogInformation("Exception Type : " + exception.GetType().ToString());


        if (exception is ValidationException)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
            var fluentException = ((ValidationException)exception);
            var fluentErrors = fluentException.Errors.GroupBy(x => x.PropertyName).ToDictionary(x => x.Key, v => v.Select(e => e.ErrorMessage).ToArray());

            string jsonString = JsonSerializer.Serialize(new ErrorResponse(Errors: fluentErrors,
                                                                           Type: "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                                                                           Title: "Validation error occured",
                                                                           Status: (int)HttpStatusCode.BadRequest,
                                                                           Message: exception?.InnerException != null ? exception.InnerException.Message : exception.Message,
                                                                           TraceId: httpContext?.TraceIdentifier ?? "not-traced"), options);


            await httpContext.Response.WriteAsync(jsonString, cancellationToken);
        }
        else if (exception is NotFoundException)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;

            string jsonString = JsonSerializer.Serialize(new ErrorResponse(Errors: new Dictionary<string, string[]>(),
                                                                           Type: "https://tools.ietf.org/html/rfc9110#section-15.5.5",
                                                                           Title: "Resource not found",
                                                                           Status: (int)HttpStatusCode.NotFound,
                                                                           Message: exception.Message,
                                                                           TraceId: httpContext?.TraceIdentifier ?? "not-traced"), options);

            await httpContext.Response.WriteAsync(jsonString, cancellationToken);
        }
        else if (exception is BusinessRuleException)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;

            string jsonString = JsonSerializer.Serialize(new ErrorResponse(Errors: new Dictionary<string, string[]>(),
                                                                           Type: "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                                                                           Title: "Business rule violation",
                                                                           Status: (int)HttpStatusCode.UnprocessableEntity,
                                                                           Message: exception.Message,
                                                                           TraceId: httpContext?.TraceIdentifier ?? "not-traced"), options);

            await httpContext.Response.WriteAsync(jsonString, cancellationToken);
        }
        else if (exception is DbUpdateException)
        {
            var errorCode = (int)HttpStatusCode.UnprocessableEntity;
            httpContext.Response.StatusCode = errorCode;

            string jsonString = JsonSerializer.Serialize(new ErrorResponse(Errors: new Dictionary<string, string[]>(),
                                                                           Type: "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                                                                           Title: "Validation error occured",
                                                                           Status: errorCode,
                                                                           Message: exception?.InnerException != null ? exception.InnerException.Message : exception.Message,
                                                                           TraceId: httpContext?.TraceIdentifier ?? "not-traced"), options);


            await httpContext.Response.WriteAsync(jsonString, cancellationToken);
        }
        else
        {
            var errorCode = (int)HttpStatusCode.InternalServerError;
            httpContext.Response.StatusCode = errorCode;

            string jsonString = JsonSerializer.Serialize(new ErrorResponse(Errors: new Dictionary<string, string[]>(),
                                                                           Type: "https://tools.ietf.org/html/rfc9110#section-15.6.1",
                                                                           Title: "Internal server error",
                                                                           Status: errorCode,
                                                                           Message: exception?.InnerException != null ? exception.InnerException.Message : exception.Message,
                                                                           TraceId: httpContext?.TraceIdentifier ?? "not-traced"), options);


            await httpContext.Response.WriteAsync(jsonString, cancellationToken);
        }

        return true;
    }
}
