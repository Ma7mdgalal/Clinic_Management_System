using Clinic.Api.Extensions;
using Clinic.Infrastructure.Configuration;
using Clinic.Infrastructure.Data;
using Clinic.Infrastructure.identity;
using Serilog;

Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

try
{

    Log.Information("Starting Up:");
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.

    // Configure Serilog system and read configuration from appsetting.
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


    // Dependency Injection
    builder.Services.AddInfrastructure();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // Configure the HTTP request pipeline.

    // Serilog

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

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
