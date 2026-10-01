using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Clinic.Application.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Clinic.Infrastructure.identity.Services;

public record JwtToken(string Value, DateTime ExpiresAtUtc);

public sealed class JwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<AppUser> _userManager;

    public JwtService(IOptions<JwtSettings> jwtSettings, UserManager<AppUser> userManager)
    {
        _jwtSettings = jwtSettings.Value;
        _userManager = userManager;
    }

    public async Task<JwtToken> GenerateTokenAsync(AppUser user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            // 'sub' is the subject. The JWT bearer handler's default inbound claim
            // mapping turns it into ClaimTypes.NameIdentifier on the server side, which
            // is what User.FindFirstValue(ClaimTypes.NameIdentifier) reads. Emitting
            // NameIdentifier here as well would only duplicate the same value.
            new(JwtRegisteredClaimNames.Sub, user.Id),

            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName!)
        };

        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
