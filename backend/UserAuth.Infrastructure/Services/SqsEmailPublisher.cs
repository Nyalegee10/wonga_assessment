using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using UserAuth.Domain.Interfaces.Services;

namespace UserAuth.Infrastructure.Services;

public class SqsEmailPublisher : IEmailService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly string _queueUrl;
    private readonly ILogger<SqsEmailPublisher> _logger;

    public SqsEmailPublisher(IAmazonSQS sqsClient, IConfiguration config, ILogger<SqsEmailPublisher> logger)
    {
        _sqsClient = sqsClient;
        _logger = logger;
        _queueUrl = config["AWS:QueueUrl"]
            ?? "http://localhost:4566/000000000000/welcome-emails";
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string firstName)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new { email = toEmail, firstName, type = "welcome" });
            await _sqsClient.SendMessageAsync(_queueUrl, payload);
            _logger.LogInformation("Welcome email queued for {Email}", toEmail);
        }
        catch (Exception ex)
        {
            // Non-fatal — registration still succeeds even if queueing fails
            _logger.LogError(ex, "Failed to queue welcome email for {Email}", toEmail);
        }
    }
}
