
using Microsoft.AspNetCore.Identity;

namespace ApiPulse.Api.Models;

/// <summary>
/// Represents an API Pulse user and extends the
/// built-in ASP.NET Core Identity account model.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Gets or sets the user's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;
}
