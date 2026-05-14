namespace SubTrack.Api.Services.Email;

/// <summary>
/// No-op email sender for the course project: logs the message via Serilog
/// instead of dispatching. Production SMTP/SendGrid implementation tracked
/// as an open item in CLAUDE.md Bolum 16.
/// </summary>
public sealed class NullEmailSender(ILogger<NullEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[null-email] Would send to {To} — subject: {Subject}",
            message.To,
            message.Subject);
        return Task.CompletedTask;
    }
}
