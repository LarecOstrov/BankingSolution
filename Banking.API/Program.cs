using Banking.API.GraphQL;
using Banking.Application.Implementations;
using Banking.Application.Repositories.Implementations;
using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services;
using Banking.Application.Services.Implementations;
using Banking.Application.Services.Interfaces;
using Banking.Infrastructure.Auth;
using Banking.Infrastructure.Caching;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.Messaging.Kafka;
using Banking.Infrastructure.Middleware;
using Banking.Infrastructure.WebSockets;
using Confluent.Kafka;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using Serilog;
using StackExchange.Redis;


try
{
    var builder = WebApplication.CreateBuilder(args);

    // Load SolutionOptions from Infrustructure appsettings.json
    var solutionOptions = LoadSolutionOptionsHelper.LoadSolutionOptions(builder);

    ConfigureLogging(builder);

    ConfigureServicesAsync(builder.Services, solutionOptions);
    builder.Services.ConfigureAuthentication(solutionOptions);

    var app = builder.Build();
    await ConfigureMiddleware(app, solutionOptions);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Configure logger
/// </summary>
void ConfigureLogging(WebApplicationBuilder builder)
{
    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
    .WriteTo.Console()
        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();

    builder.Host.UseSerilog();
}

/// <summary>
/// Register services in DI container
/// </summary>
void ConfigureServicesAsync(IServiceCollection services, SolutionOptions solutionOptions)
{
    // MSSQL
    Log.Information($"Connection MSSQL: {solutionOptions.ConnectionStrings.DefaultConnection}");
    services.AddDbContext<ApplicationDbContext>(dbOptions =>
        dbOptions.UseSqlServer(solutionOptions.ConnectionStrings.DefaultConnection));

    ConfigureMessagingAsync(services, solutionOptions);

    // Services
    services.AddControllers();    
    services.AddSingleton<WebSocketService>();
    services.AddScoped<IPublishService, PublishService>();
    services.AddScoped<ITransactionService, TransactionService>();
    services.AddScoped<IAccountService, AccountService>();
    services.AddScoped<IAuthService, AuthService>();

    // Redis configuration
    services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(solutionOptions.Redis.Host));
    services.AddScoped<IRedisCacheService, RedisCacheService>();

    // Repositories
    services.AddScoped<IAccountRepository, AccountRepository>();
    services.AddScoped<ITransactionRepository, TransactionRepository>();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IRoleRepository, RoleRepository>();
    services.AddScoped<IBalanceHistoryRepository, BalanceHistoryRepository>();
    services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    services.AddScoped<IFailedTransactionRepository, FailedTransactionRepository>();

    // GraphQL
    services.AddGraphQLServer()
        .AddAuthorization()
        .AddQueryType<Query>()
        .AddFiltering()
        .AddSorting()
        .AddInstrumentation()
        .AddProjections()
        .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true)
        .ModifyCostOptions(opt => opt.EnforceCostLimits = false);

    services.AddScoped<Query>();
    services.AddAuthorization();
    services.AddValidation();
    
    // CORS Configuration
    var corsOptions = solutionOptions.Cors;
    services.AddCors(options =>
    {
        if (corsOptions?.Api.AllowedOrigins?.Any() == true)
        {
            options.AddPolicy("AllowSpecificOrigins", builder =>
                builder.WithOrigins(corsOptions.Api.AllowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader());
        }
        else
        {
            Log.Warning("CORS is misconfigured: No allowed origins specified.");
            options.AddPolicy("AllowAll",
                builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        }
    });
}

void ConfigureMessagingAsync(IServiceCollection services, SolutionOptions solutionOptions)
{
    Log.Information($"Connection Kafka BootstrapServers: {solutionOptions.Kafka.BootstrapServers}");
    Log.Information($"Consumer Kafka ConsumerGroup: {solutionOptions.Kafka.ConsumerGroup}");

    // Kaffka configuration
    // Producer
    var kafkaProducerConfig = new ProducerConfig
    {
        BootstrapServers = solutionOptions.Kafka.BootstrapServers,
        Acks = Acks.All,
        EnableIdempotence = true,
        MessageSendMaxRetries = int.MaxValue,
        LingerMs = 2,
        BatchSize = 32 * 1024
    };

    // Consumer
    var kafkaConsumerNotificationConfig = new ConsumerConfig
    {
        BootstrapServers = solutionOptions.Kafka.BootstrapServers,
        GroupId = solutionOptions.Kafka.ConsumerGroup,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false,
        IsolationLevel = IsolationLevel.ReadCommitted,
        AllowAutoCreateTopics = true
    };

    // Services
    services.AddControllers();
    services.AddSingleton<ProducerConfig>(kafkaProducerConfig);
    services.AddSingleton<IKafkaProducer, KafkaProducer>();
    services.AddSingleton(kafkaConsumerNotificationConfig);
    services.AddHostedService<KafkaNotificationConsumerService>();
}

/// <summary>
/// Configure middleware and routing
/// </summary>
async Task ConfigureMiddleware(WebApplication app, SolutionOptions appOptions)
{
    // Proxing IP
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    // Request logging
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseRequestLogging();

    // Auto-migrate database in development
    if (app.Environment.IsDevelopment())
    {
        await MigrationHelper.ApplyMigrationsAsync(app, appOptions);
    }

    // Metrics and routing
    app.UseHttpMetrics();
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapGraphQL();
    app.UseWebSockets();
    app.MapMetrics(); // Prometheus

    // Enable CORS
    app.UseCors(appOptions.Cors.Api.AllowedOrigins.Any() ? "AllowSpecificOrigins" : "AllowAll");

    app.MapControllers();
}
