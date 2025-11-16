using Application.Framework;
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


            ////else if (context.Exception is LockException)
            ////{
            ////    code = HttpStatusCode.UnprocessableEntity;
            ////    message = context.Exception.Message;
            ////}
            ////else if (context.Exception is BrokenCircuitException)
            ////{
            ////    code = HttpStatusCode.FailedDependency;
            ////    message = "Service is inoperative, please try later on. (Business message due to Circuit-Breaker)";
            ////}
            ////else if (context.Exception is UnauthorizedAccessException)
            ////{
            ////    code = HttpStatusCode.Unauthorized;
            ////    message = "Unauthorized Token";
            ////}
            ////else if (context.Exception is DuplicateNameException)
            ////{
            ////    code = HttpStatusCode.Conflict;
            ////    message = "Duplicate Data Found";
            ////}
            ////else if (context.Exception is ArgumentNullException)
            ////{
            ////    code = HttpStatusCode.NotFound;
            ////    message = "Data Not Found";
            ////}
            ////else if (context.Exception is NotFoundException)
            ////{
            ////    code = HttpStatusCode.NotFound;
            ////    message = "Data Not Found";
            ////}
            ////else if (context.Exception is FailedException)
            ////{
            ////    code = HttpStatusCode.FailedDependency;
            ////    message = "Operation Failed";
            ////}





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
