using Banking.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;

public class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly HttpClient _client;
    protected readonly ApplicationDbContext _dbContext;

    public IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        var scopeFactory = factory.Services.GetService<IServiceScopeFactory>();
        var scope = scopeFactory!.CreateScope();
        _dbContext = scope!.ServiceProvider.GetRequiredService<ApplicationDbContext>();


        _dbContext.Database.EnsureDeleted();
        _dbContext.Database.EnsureCreated();

        _client = factory.CreateClient();
    }
}
