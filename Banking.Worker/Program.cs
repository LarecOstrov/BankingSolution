using Banking.Application.Repositories.Implementations;
using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Implementations;
using Banking.Application.Services.Interfaces;
using Banking.Infrastructure.Caching;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Database;
using Banking.Infrastructure.WebSockets;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Load AppOptions from Common
    var appOptions = LoadSolutionOptionsHelper.LoadSolutionOptions(builder);

    ConfigureLogging(builder);

    ConfigureServicesAsync(builder.Services, appOptions);

    var app = builder.Build();

    // Middleware pipeline setup
    app.UseRouting();

    // Allow CORS for WebSockets
    app.UseCors(appOptions.Cors.Worker.AllowedOrigins.Any() ? "AllowSpecificOrigins" : "AllowAll");

    // WebSockets
    app.UseWebSockets();
    app.MapHub<WebSocketHub>("/ws");
    app.MapHealthChecks("/health");
    Log.Information("Starting Web Application...");

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

void ConfigureServicesAsync(IServiceCollection services, SolutionOptions solutionOptions)
{
    Log.Information($"Connection MSSQL: {solutionOptions.ConnectionStrings.DefaultConnection}");
    services.AddDbContext<ApplicationDbContext>(dbOptions =>
        dbOptions.UseSqlServer(solutionOptions.ConnectionStrings.DefaultConnection));

    Log.Information($"Connection Kafka BootstrapServers: {solutionOptions.Kafka.BootstrapServers}");
    Log.Information($"Consumer Kafka ConsumerGroup: {solutionOptions.Kafka.ConsumerGroup}");

    // Register RabbitMQ services
    var kafkaConfig = new ConsumerConfig
    {
        BootstrapServers = solutionOptions.Kafka.BootstrapServers,
        GroupId = solutionOptions.Kafka.ConsumerGroup,
        AutoOffsetReset = AutoOffsetReset.Earliest
    };

    services.AddSingleton(kafkaConfig);


    // Services
    services.AddScoped<IRedisCacheService, RedisCacheService>();
    services.AddScoped<ITransactionService, TransactionService>();

    services.AddSignalR(options =>
    {
        options.KeepAliveInterval = TimeSpan.FromSeconds(30);
        options.ClientTimeoutInterval = TimeSpan.FromMinutes(2);
    })
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });
    services.AddSingleton<WebSocketHub>();

    // Register repositories
    services.AddScoped<IAccountRepository, AccountRepository>();
    services.AddScoped<ITransactionRepository, TransactionRepository>();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IRoleRepository, RoleRepository>();
    services.AddScoped<IBalanceHistoryRepository, BalanceHistoryRepository>();


    var corsOptions = solutionOptions.Cors;
    services.AddCors(options =>
    {
        if (corsOptions?.Worker.AllowedOrigins?.Any() == true)
        {
            options.AddPolicy("AllowSpecificOrigins", builder =>
                builder.WithOrigins(corsOptions.Worker.AllowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithExposedHeaders("Sec-WebSocket-Accept")
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .WithHeaders("Sec-WebSocket-Version", "Sec-WebSocket-Key", "Upgrade", "Connection"));
        }
        else
        {
            Log.Warning("CORS is misconfigured: No allowed origins specified.");
            options.AddPolicy("AllowAll",
                builder =>
                builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("Sec-WebSocket-Accept")
                .SetIsOriginAllowedToAllowWildcardSubdomains()
                .WithHeaders("Sec-WebSocket-Version", "Sec-WebSocket-Key", "Upgrade", "Connection"));
        }
    });
}
