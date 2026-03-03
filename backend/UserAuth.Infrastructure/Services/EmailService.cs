using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using UserAuth.Domain.Interfaces.Services;
using UserAuth.Infrastructure.Settings;

namespace UserAuth.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string firstName)
    {
        if (string.IsNullOrEmpty(_emailSettings.FromEmail) || string.IsNullOrEmpty(_emailSettings.Password))
        {
            _logger.LogWarning("Email settings not configured. Skipping welcome email to {Email}.", toEmail);
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
            message.To.Add(new MailboxAddress(firstName, toEmail));
            message.Subject = "Welcome to Wonga App!";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 0;'>
                    <div style='background: linear-gradient(135deg, #00aeef, #0079c1); padding: 32px; text-align: center;'>
                        <div style='width: 64px; height: 64px; background: rgba(255,255,255,0.2); border-radius: 16px; display: inline-flex; align-items: center; justify-content: center; margin-bottom: 16px;'>
                            <span style='font-size: 32px; font-weight: bold; color: white;'>W</span>
                        </div>
                        <h1 style='color: white; margin: 0; font-size: 28px;'>Welcome to Wonga!</h1>
                    </div>
                    <div style='padding: 40px 32px; background-color: #f8f9fa;'>
                        <h2 style='color: #1a1a2e;'>Hi {firstName},</h2>
                        <p style='color: #555; line-height: 1.6;'>
                            Thank you for creating your account. You can now log in and access your profile.
                        </p>
                        <div style='margin: 32px 0; text-align: center;'>
                            <a href='http://localhost:80/login' style='background: linear-gradient(135deg, #00aeef, #0079c1); color: white; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600;'>
                                Sign In Now
                            </a>
                        </div>
                        <p style='color: #888; font-size: 13px;'>Best regards,<br/>The Wonga Team</p>
                    </div>
                </body>
                </html>",
                TextBody = $"Hi {firstName},\n\nThank you for registering! Your account has been successfully created.\n\nBest regards,\nThe Wonga Team"
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_emailSettings.SmtpHost, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Welcome email sent to {Email}.", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}.", toEmail);
        }
    }
}
