using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UserAuth.Domain.Interfaces.Repositories;
using UserAuth.Domain.Interfaces.Services;
using UserAuth.Infrastructure.Data;
using UserAuth.Infrastructure.Data.Repositories;
using UserAuth.Infrastructure.Services;
using UserAuth.Infrastructure.Settings;

namespace UserAuth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // AWS SQS — points at LocalStack locally, real SQS in production
        services.AddSingleton<IAmazonSQS>(_ =>
        {
            var serviceUrl = configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
            var config = new AmazonSQSConfig { ServiceURL = serviceUrl };
            return new AmazonSQSClient(new BasicAWSCredentials("test", "test"), config);
        });

        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailService, SqsEmailPublisher>();  // publishes to SQS → notification-service delivers
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();

        services.AddHealthChecks()
            .AddDbContextCheck<AppDbContext>("database");

        return services;
    }

    public static void InitialiseDatabase(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (db.Database.IsRelational())
            db.Database.Migrate();   // runs pending EF Core migrations on startup
        else
            db.Database.EnsureCreated();  // InMemory (tests)
    }
}
