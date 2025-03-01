using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Banking.Infrastructure.Config;

public static class LoadSolutionOptionsHelper
{
    public static SolutionOptions LoadSolutionOptions(WebApplicationBuilder builder)
    {
        var basePath = Directory.GetCurrentDirectory();
        var configPath = Path.Combine(basePath, "../Banking.Infrastructure/appsettings.json");

        if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
        {
            basePath = AppContext.BaseDirectory;
            configPath = Path.Combine(basePath, "appsettings.json");
        }

        if (!File.Exists(configPath))
        {
            var errorMsg = $"Configuration file not found: {configPath}";
            Log.Fatal(errorMsg);
            throw new FileNotFoundException(errorMsg);
        }

        builder.Configuration
            .SetBasePath(Path.GetDirectoryName(configPath)!)
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .AddCommandLine(Environment.GetCommandLineArgs());

        var solutionOptions = builder.Configuration.GetSection("SolutionOptions").Get<SolutionOptions>();
        if (solutionOptions == null)
        {
            var errorMsg = "Missing SolutionOptions configuration in Banking.Infrastructure/appsettings.json";
            Log.Fatal(errorMsg);
            throw new InvalidOperationException(errorMsg);
        }

        builder.Services.Configure<SolutionOptions>(builder.Configuration.GetSection("SolutionOptions"));
        return solutionOptions!;
    }
}