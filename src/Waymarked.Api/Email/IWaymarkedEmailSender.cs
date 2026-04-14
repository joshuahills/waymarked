namespace Waymarked.Api.Email;

using Waymarked.Api.Data;

/// <summary>
/// Extends the standard ASP.NET Identity email sender with Waymarked-specific email operations.
/// </summary>
public interface IWaymarkedEmailSender
{
    Task SendWelcomeEmailAsync(ApplicationUser user, string email);
}
