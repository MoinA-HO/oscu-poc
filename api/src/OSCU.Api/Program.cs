using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Carter;
using OSCU.Api.Config;
using OSCU.Api.Middleware;
using OSCU.Application;
using OSCU.Infrastructure;
using OSCU.Persistence;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddCarter();

// Versioning is via query string /api/referrals?api-version=1 — matching the
// convention in the source template. Asp.Versioning.Http is the minimal-API
// implementation, the MVC packages are deliberately absent.
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Default;
    })
    .AddApiExplorer(options => options.GroupNameFormat = "'v'VVV");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureOptions<SwaggerConfigureOptions>();

// --------------------------------------------------------- Error handling
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ------------------------------------------------------------------- CORS
// Origins come from configuration rather than being hardcoded, so promoting
// through environments does not require a code change.
string[] allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

// ------------------------------------------------------------ Application
builder.Services
    .AddApplicationDependencies()
    .AddInfrastructureDependencies()
    .AddPersistenceDependencies(builder.Configuration);

// ----------------------------------------------------------------- Health
// Replaces the source template's DbTestController, which existed to prove
// database connectivity in Azure and was marked "DELETE THIS FROM A REAL
// PROJECT". A health check does the same job and App Service can poll it.
builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

WebApplication app = builder.Build();

// Structured request logging: one summary line per request rather than the
// framework's several.
app.UseSerilogRequestLogging();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // In development, create the database and apply pending migrations on startup
    // so a fresh Postgres container works without running dotnet ef by hand.
    // Other environments should apply migrations as a deployment step:
    // dotnet ef database update --project src/OSCU.Persistence --startup-project src/OSCU.Api
    await app.Services.ApplyMigrationsAsync();


    IApiVersionDescriptionProvider versionProvider =
        app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        foreach (ApiVersionDescription description in versionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint(
                $"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }

        options.RoutePrefix = "swagger";
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
    });

    // The root has no endpoint of its own, so opening the base URL in a
    // browser returns a bare 404. In development, send it to Swagger instead.
    // ExcludeFromDescription keeps this convenience redirect out of the
    // generated OpenAPI document.
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}
else
{
    // HSTS and HTTPS redirection only outside development, so a local HTTP
    // run is not permanently pinned to HTTPS by the browser's HSTS cache.
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors();

app.MapCarter();
app.MapHealthChecks("/health");

await app.RunAsync();
