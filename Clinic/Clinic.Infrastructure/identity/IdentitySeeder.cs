using Clinic.Application.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Clinic.Infrastructure.identity;

/// <summary>
/// Makes sure the four application roles and one administrator exist before the API
/// starts serving requests. Idempotent: safe to run on every startup.
/// </summary>
public sealed class IdentitySeeder
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly BootstrapAdminSettings _adminSettings;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        RoleManager<ApplicationRole> roleManager,
        UserManager<AppUser> userManager,
        IOptions<BootstrapAdminSettings> adminSettings,
        ILogger<IdentitySeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _adminSettings = adminSettings.Value;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedAdminAsync();
    }

    /// <summary>
    /// The migration already seeds the roles via HasData; this is a safety net for a
    /// database where a role was removed by hand, and it keeps the seeder self-contained.
    /// </summary>
    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles.All)
        {
            if (await _roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var result = await _roleManager.CreateAsync(new ApplicationRole { Name = role });
            if (!result.Succeeded)
            {
                var reasons = string.Join("; ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Could not create role '{role}': {reasons}");
            }

            _logger.LogInformation("Role created by seeder. Role={Role}", role);
        }
    }

    private async Task SeedAdminAsync()
    {
        if (string.IsNullOrWhiteSpace(_adminSettings.Password))
        {
            // No default password is invented on purpose. A hardcoded fallback would be a
            // published administrator credential for every deployment of this codebase.
            _logger.LogWarning(
                "BootstrapAdmin:Password is not configured, so no administrator was created. " +
                "Set it with: dotnet user-secrets set \"BootstrapAdmin:Password\" \"<password>\"");
            return;
        }

        var existing = await _userManager.FindByNameAsync(_adminSettings.UserName);
        if (existing is not null)
        {
            // The password is never reset here. Overwriting it on every startup would
            // silently undo any password the administrator had changed.
            if (!await _userManager.IsInRoleAsync(existing, Roles.Admin))
            {
                await _userManager.AddToRoleAsync(existing, Roles.Admin);
                _logger.LogInformation(
                    "Existing bootstrap account was missing the Admin role; granted. UserId={UserId}",
                    existing.Id);
            }

            return;
        }

        var admin = new AppUser
        {
            UserName = _adminSettings.UserName,
            Email = _adminSettings.Email,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(admin, _adminSettings.Password);
        if (!createResult.Succeeded)
        {
            var reasons = string.Join("; ", createResult.Errors.Select(e => e.Description));

            // The reasons may include "password too short" — that describes the policy,
            // not the password itself, so it is safe to log. The password value is not.
            throw new InvalidOperationException(
                $"Could not create the bootstrap administrator: {reasons}");
        }

        var roleResult = await _userManager.AddToRoleAsync(admin, Roles.Admin);
        if (!roleResult.Succeeded)
        {
            var reasons = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException(
                $"Could not grant the Admin role to the bootstrap administrator: {reasons}");
        }

        _logger.LogInformation(
            "Bootstrap administrator created. UserId={UserId} UserName={UserName}",
            admin.Id, admin.UserName);
    }
}
