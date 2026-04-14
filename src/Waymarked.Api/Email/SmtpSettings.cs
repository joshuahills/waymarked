namespace Waymarked.Api.Email;

public class SmtpSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 2525;
    public string FromAddress { get; set; } = "noreply@waymarked.local";
    public string FromName { get; set; } = "Waymarked";
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// The public-facing base URL of the web frontend, used to build links in emails.
    /// e.g. https://waymarked.dev.localhost:2007
    /// </summary>
    public string FrontendBaseUrl { get; set; } = "https://localhost";
}
