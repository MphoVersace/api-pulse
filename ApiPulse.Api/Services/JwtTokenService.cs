
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiPulse.Api.DTOs;
using ApiPulse.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace ApiPulse.Api.Services;

/// <summary>
/// Generates JWT access tokens for authenticated API Pulse users.
/// </summary>
public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Creates a signed JWT access token for the specified user.
    /// </summary>
    public AuthResponseDto GenerateToken(ApplicationUser user)
    {
        var secretKey = _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException(
                "JWT secret key is not configured.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is not configured.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is not configured.");

        var expirationMinutes = _configuration.GetValue<int>(
            "Jwt:ExpirationInMinutes", 60);

        var expiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);

        // Define the identity information included in the token.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Create the cryptographic signing key.
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(secretKey));

        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        // Generate the signed JWT.
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler()
            .WriteToken(token);

        // Return the token together with the user's account information.
        return new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Token = tokenString,
            ExpiresAt = expiresAt
        };
    }
}
