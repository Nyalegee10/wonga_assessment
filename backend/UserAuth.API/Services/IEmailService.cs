namespace UserAuth.API.Services;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string firstName);
}
