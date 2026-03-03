namespace UserAuth.Domain.Interfaces.Services;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string firstName);
}
