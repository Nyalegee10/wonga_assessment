namespace UserAuth.Infrastructure.Settings;

public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Wonga App";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
