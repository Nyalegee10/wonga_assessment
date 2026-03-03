using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using NotificationService.Services;
using NotificationService.Workers;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/notification-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // ── AWS SQS (points at LocalStack in Docker, real AWS in production) ──────
    builder.Services.AddSingleton<IAmazonSQS>(_ =>
    {
        var serviceUrl = builder.Configuration["AWS:ServiceURL"] ?? "http://localstack:4566";
        var config = new AmazonSQSConfig { ServiceURL = serviceUrl };
        // LocalStack accepts any credentials; swap for real IAM creds in production
        return new AmazonSQSClient(new BasicAWSCredentials("test", "test"), config);
    });

    builder.Services.AddSingleton<SmtpEmailSender>();
    builder.Services.AddHostedService<SqsEmailWorker>();
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Simple health endpoint — lets Docker and monitoring tools check liveness
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "notification", timestamp = DateTime.UtcNow }));

    Log.Information("Notification Service starting up");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Notification Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
