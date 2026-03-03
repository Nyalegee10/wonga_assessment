using Amazon.SQS;
using Amazon.SQS.Model;
using NotificationService.Models;
using NotificationService.Services;
using System.Text.Json;

namespace NotificationService.Workers;

public class SqsEmailWorker : BackgroundService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly SmtpEmailSender _emailSender;
    private readonly string _queueUrl;
    private readonly ILogger<SqsEmailWorker> _logger;

    public SqsEmailWorker(
        IAmazonSQS sqsClient,
        SmtpEmailSender emailSender,
        IConfiguration config,
        ILogger<SqsEmailWorker> logger)
    {
        _sqsClient = sqsClient;
        _emailSender = emailSender;
        _logger = logger;
        _queueUrl = config["AWS:QueueUrl"]
            ?? "http://localstack:4566/000000000000/welcome-emails";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SQS Email Worker started — polling: {QueueUrl}", _queueUrl);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20   // long-polling — reduces empty responses
                }, stoppingToken);

                foreach (var message in response.Messages)
                {
                    await ProcessMessageAsync(message, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling SQS queue — retrying in 5s");
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("SQS Email Worker stopped");
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken ct)
    {
        try
        {
            var emailMsg = JsonSerializer.Deserialize<EmailMessage>(
                message.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (emailMsg is null)
            {
                _logger.LogWarning("Could not deserialise message {Id} — deleting", message.MessageId);
            }
            else
            {
                _logger.LogInformation("Processing welcome email for {Email}", emailMsg.Email);
                await _emailSender.SendWelcomeEmailAsync(emailMsg.Email, emailMsg.FirstName);
            }

            // Delete after successful processing (or on bad message to avoid poison-pill loop)
            await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message {Id} — will retry on next poll", message.MessageId);
        }
    }
}
