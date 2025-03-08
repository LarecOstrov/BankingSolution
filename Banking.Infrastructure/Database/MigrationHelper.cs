using Banking.Infrastructure.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Banking.Infrastructure.Database
{
    public class MigrationHelper
    {
        /// <summary>
        /// Apply database migrations
        /// </summary>
        public static async Task ApplyMigrationsAsync(WebApplication app, SolutionOptions appOptions)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var connectionStringParts = appOptions.ConnectionStrings.DefaultConnection.Split(';');
            for (int i = 0; i < connectionStringParts.Length; i++)
            {
                if (connectionStringParts[i].StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                {
                    connectionStringParts[i] = "Database=master";
                }
            }
            string masterConnectionString = string.Join(";", connectionStringParts);
            string databaseConnectionString = appOptions.ConnectionStrings.DefaultConnection;

            int maxRetries = 15;
            int delay = 6000; // Delay in ms

            Log.Information("Checking SQL Server availability...");

            // Wait for SQL Server to be available
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await using var connection = new SqlConnection(masterConnectionString);
                    await connection.OpenAsync();
                    Log.Information("SQL Server is up and running.");
                    break;
                }
                catch (Exception ex)
                {
                    Log.Warning($"SQL Server is not ready yet {ex.Message}. Retrying in {delay}ms... ({i + 1}/{maxRetries})");
                    await Task.Delay(delay);
                }
            }

            // Ensure the database exists
            try
            {
                await using var connection = new SqlConnection(masterConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{dbContext.Database.GetDbConnection().Database}') CREATE DATABASE [BankingDB];";
                await command.ExecuteNonQueryAsync();
                Log.Information("Database ensured.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to ensure the database exists.");
                throw;
            }

            // Wait for the actual database to be ready
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await using var connection = new SqlConnection(databaseConnectionString);
                    await connection.OpenAsync();
                    Log.Information($"Database '{dbContext.Database.GetDbConnection().Database}' is now accessible.");
                    break;
                }
                catch (Exception ex)
                {
                    Log.Warning($"Database '{dbContext.Database.GetDbConnection().Database}' is not ready yet: {ex.Message}. Retrying in {delay}ms... ({i + 1}/{maxRetries})");
                    await Task.Delay(delay);
                }
            }

            if (!await dbContext.Database.CanConnectAsync())
            {
                Log.Error("Database connection failed after multiple attempts.");
                throw new InvalidOperationException("Database connection failed. Ensure the database server is running.");
            }

            // Apply Migrations
            var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
            if (pendingMigrations.Any())
            {
                Log.Information($"Applying {pendingMigrations.Count} pending migrations...");
                await dbContext.Database.MigrateAsync();
                Log.Information("Database migrations applied successfully.");
            }
            else
            {
                Log.Information("No pending migrations found.");
            }
        }
    }
}
