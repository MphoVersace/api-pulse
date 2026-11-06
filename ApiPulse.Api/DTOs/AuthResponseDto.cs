
namespace ApiPulse.Api.DTOs;

/// <summary>
/// Represents the authentication response returned after successful login or registration.
/// </summary>
public class AuthResponseDto
{
    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
}
