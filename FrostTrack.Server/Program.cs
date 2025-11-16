using FrostTrack.Server;
using Infrastructure;
using FrostTrack.Server.Middlewares;
using Persistence;
using Persistence.SeedData;
using Application;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddPresentation()
        .AddApplication()
        .AddPersistence(builder.Configuration)
        .AddInfrastructure(builder.Configuration);


//#region register graphql services



//builder.Services.AddGraphQLServer()
//                .AddQueryType<Query>()
//                .AddMutationType<Mutations>()
//                .AddErrorFilter(error =>
//                {
//                    if (error.Exception is NullReferenceException)
//                    {
//                        return error.WithCode("NullRef");
//                    }

//                    if (error.Exception is DuplicateWaitObjectException)
//                    {
//                        return error.WithCode("NullRef");
//                    }

//                    //StatusCodes.Status500InternalServerError

//                    error.RemoveException();
//                    error.RemoveLocations();
//                    error.RemoveSyntaxNode();
//                    error.RemoveExtensions();

//                    error.WithCode("500");
//                    error.WithMessage(error.Exception?.InnerException?.Message ?? error?.Exception?.Message ?? "");


//                    return error;
//                });

//#endregion



var app = builder.Build();

#region Exception handler middleware

app.UseExceptionHandler();
app.UseMiddleware<AuthenticationErrorHandler>();
app.UseMiddleware<AuthenticationDebugMiddleware>();

#endregion

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

//else
//{
//    app.UseDeveloperExceptionPage();
//    app.UseMigrationsEndPoint();
//}

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    await app.InitialiseDatabaseAsync();
}

app.UseHttpsRedirection();

app.UseCors(builder => builder
     .WithOrigins("http://localhost:4200", "https://localhost:4200") // Specify exact origins for credentials
     .AllowAnyMethod()
     .AllowAnyHeader()
     .AllowCredentials());

// CRITICAL: Authentication middleware MUST come in this order
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");
//app.MapGraphQL();

app.Run();
