namespace NotificationService.Models;

public class EmailMessage
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string Type { get; set; } = "welcome";
}
