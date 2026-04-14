namespace Waymarked.Api.Email;

using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;
using Waymarked.Api.Data;

public class SmtpEmailSender(IOptions<SmtpSettings> options, ILogger<SmtpEmailSender> logger)
    : IEmailSender<ApplicationUser>, IWaymarkedEmailSender
{
    private readonly SmtpSettings _settings = options.Value;

    public Task SendWelcomeEmailAsync(ApplicationUser user, string email) =>
        SendAsync(email, "Welcome to Waymarked!",
            """
            <p>Welcome to Waymarked!</p>
            <p>Your account is all set. Start planning your next adventure.</p>
            """);

    // Interface stub — email confirmation is not used; registration auto-signs in.
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        Task.CompletedTask;

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(email, "Reset your Waymarked password",
            $"""
            <p>We received a request to reset your Waymarked password.</p>
            <p><a href="{resetLink}">Click here to reset your password</a></p>
            <p>This link will expire after 24 hours. If you didn't request this, you can safely ignore this email.</p>
            """);

    // Interface stub — code-based password reset is not implemented.
    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        Task.CompletedTask;

    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        try
        {
            var socketOptions = _settings.UseSsl
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.None;

            await client.ConnectAsync(_settings.Host, _settings.Port, socketOptions);

            if (!string.IsNullOrEmpty(_settings.Username))
                await client.AuthenticateAsync(_settings.Username, _settings.Password);

            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);

            logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To} with subject {Subject}", to, subject);
            throw;
        }
    }
}
