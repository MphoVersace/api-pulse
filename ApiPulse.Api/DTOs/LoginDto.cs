
using System.ComponentModel.DataAnnotations;

namespace ApiPulse.Api.DTOs;

/// <summary>
/// Represents the credentials required to log in to an API Pulse account.
/// </summary>
public class LoginDto
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
