using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NotificationService.Services;

public class SmtpEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string firstName)
    {
        var host = _config["EmailSettings:SmtpHost"];
        var port = int.Parse(_config["EmailSettings:SmtpPort"] ?? "587");
        var fromEmail = _config["EmailSettings:FromEmail"];
        var fromName = _config["EmailSettings:FromName"] ?? "Wonga App";
        var username = _config["EmailSettings:Username"];
        var password = _config["EmailSettings:Password"];

        if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(password))
        {
            _logger.LogWarning("Email credentials not configured — skipping email to {Email}", toEmail);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(firstName, toEmail));
        message.Subject = "Welcome to Wonga App!";
        message.Body = new BodyBuilder
        {
            HtmlBody = $@"
                <html><body style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto'>
                <div style='background:linear-gradient(135deg,#00aeef,#0079c1);padding:32px;text-align:center'>
                    <h1 style='color:white;margin:0'>Welcome to Wonga!</h1>
                </div>
                <div style='padding:40px 32px;background:#f8f9fa'>
                    <h2 style='color:#1a1a2e'>Hi {firstName},</h2>
                    <p style='color:#555;line-height:1.6'>
                        Your account has been created. You can now log in and access your profile.
                    </p>
                    <p style='color:#888;font-size:13px'>Best regards,<br/>The Wonga Team</p>
                </div>
                </body></html>",
            TextBody = $"Hi {firstName},\n\nYour account has been created.\n\nBest regards,\nThe Wonga Team"
        }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(username, password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Welcome email sent to {Email}", toEmail);
    }
}
