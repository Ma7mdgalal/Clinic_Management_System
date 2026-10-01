using Clinic.Api.Extensions;
using Clinic.Api.Middleware;
using Clinic.Application.Configuration;
using Clinic.Infrastructure.Configuration;
using Clinic.Infrastructure.identity;
using Serilog;

// A bootstrap logger so failures during configuration are still recorded. Without it,
// anything that throws before UseSerilog() runs would be lost entirely.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting up");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog replaces the default logging provider and reads its sinks from configuration,
    // so log destinations and levels change without a rebuild.
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration.ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName();
    });

    builder.Services.ConfigureDatabase(builder.Configuration);
    builder.Services.ConfigureIdentity();
    builder.Services.ConfigureJwt(builder.Configuration);

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplicationServices();

    builder.Services.AddControllers();
    builder.Services.ConfigureSwagger();

    // AddProblemDetails supplies the RFC 9457 writer that AppExceptionHandler uses, so every
    // error — handled or not — leaves the API in the same machine-readable shape.
    builder.Services.AddProblemDetails();
    builder.Services.AddExceptionHandler<AppExceptionHandler>();

    var app = builder.Build();

    // First in the pipeline: it can only catch what is thrown after it.
    app.UseExceptionHandler();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    // Authentication before authorization: the pipeline has to know who the caller is
    // before it can decide what they may do. Reversing these two silently rejects every
    // authenticated request.
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Roles and the first administrator are created before the app accepts requests.
    // Without this, no Admin exists, so every admin-only endpoint is unreachable and
    // CMS-010 cannot be exercised at all.
    using (var scope = app.Services.CreateScope())
    {
        var seeder = scope.ServiceProvider.GetRequiredService<IdentitySeeder>();
        await seeder.SeedAsync();
    }

    app.Run();
}
catch (HostAbortedException)
{
    // EF Core design-time tooling intentionally aborts the host after resolving services.
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
