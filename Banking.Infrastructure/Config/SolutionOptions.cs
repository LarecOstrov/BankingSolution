namespace Banking.Infrastructure.Config
{
    public class SolutionOptions
    {
        public required ConnectionStringsOptions ConnectionStrings { get; set; }
        public required RedisOptions Redis { get; set; }
        public required KafkaOptions Kafka { get; set; }
        public required LoggingOptions Logging { get; set; }
        public CorsOptions Cors { get; set; } = new CorsOptions();
        public required JwtOptions Jwt { get; set; }
        public required BankInfo BankInfo { get; set; }
        public required TransactionRetryPolicy TransactionRetryPolicy { get; set; }
    }

    public class ConnectionStringsOptions
    {
        public required string DefaultConnection { get; set; }
    }

    public class RedisOptions
    {
        public required string Host { get; set; }
        public required string InstanceName { get; set; }
    }

    public class KafkaOptions
    {
        public required string BootstrapServers { get; set; }
        public required string ConsumerGroup { get; set; }
        public required string TransactionsTopic { get; set; }
        public required string NotificationsTopic { get; set; }

    }

    public class LoggingOptions
    {
        public string Default { get; set; } = "Information";
        public string Microsoft { get; set; } = "Warning";
    }

    public class CorsOptions
    {
        public ServiceCors Api { get; init; } = new ServiceCors();
        public ServiceCors Worker { get; init; } = new ServiceCors();
    }

    public class ServiceCors
    {
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    }

    public class JwtOptions
    {
        public required string SecretKey { get; set; }
        public required string Issuer { get; set; }
        public required string Audience { get; set; }
        public required int ExpiryMinutes { get; set; }
    }

    public class BankInfo
    {
        public required string Name { get; set; }
        public required string Country { get; set; }
        public required string Code { get; set; }
        public required int AccountLength { get; set; }
    }
    //    "TransactionRetryPolicy": {
    //      "MaxRetries": 3,
    //      "DelayMiliseconds": 2000
    //    },

    public class TransactionRetryPolicy
    {
        public required int MaxRetries { get; set; } = 1;
        public required int DelayMilliseconds { get; set; } = 1000;
    }
}
