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
    builder.Services.AddIdentity<AppUser, ApplicationRole>(options =>
    {
        //Password policy
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        // Lockout
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

        //         // User
        ; options.User.RequireUniqueEmail = true;
    }).AddEntityFrameworkStores<AppDbContext>();

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
