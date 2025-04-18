using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ipvcr.Auth;

public interface ITokenManager
{
    string CreateToken(string username, IEnumerable<string>? roles = null);
    ClaimsPrincipal? ValidateToken(string token);
    // No need for InvalidateToken methods as we'll use JWT expiration instead

    string CreateHash(string input);
}

public class TokenManager : ITokenManager
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly TimeSpan _tokenLifetime;

    public TokenManager(string secretKey = "your_secret_key_at_least_32_chars_long", 
                      string issuer = "IPTVScheduler", 
                      string audience = "IPTVSchedulerClients",
                      int tokenLifetimeMinutes = 60)
    {
        _secretKey = secretKey;
        _issuer = issuer;
        _audience = audience;
        _tokenLifetime = TimeSpan.FromMinutes(tokenLifetimeMinutes);
    }

    public string CreateToken(string username, IEnumerable<string>? roles = null)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Add roles if provided
        if (roles != null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(_tokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _issuer,
            ValidAudience = _audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
            ClockSkew = TimeSpan.Zero // No tolerance for token expiration
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null; // Token is invalid
        }
    }

    public string CreateHash(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;
        // Implement a proper hashing algorithm here
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(input.PadLeft(16, '0')));
    }
}