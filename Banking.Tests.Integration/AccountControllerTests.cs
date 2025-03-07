using Banking.Infrastructure.Database.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Net.Http.Json;

public class AccountControllerTests : IntegrationTestBase
{
    public AccountControllerTests(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task GetAccountDetails_ShouldReturnOk_WhenAccountExists()
    {
        // Arrange

        var testUser = new UserEntity
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "test@example.com",
            PasswordHash = "hashedpassword123",
            Confirmed = true
        };
        var testAccount = new AccountEntity
        {
            Id = Guid.NewGuid(),
            UserId = testUser.Id,
            Balance = 1000,
            AccountNumber = "UA2038080552523943628122"
        };

        _dbContext.Users.Add(testUser);
        _dbContext.Accounts.Add(testAccount);
        await _dbContext.SaveChangesAsync();

        // Авторизація (якщо потрібен токен)
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "valid_token");

        // Act
        var response = await _client.GetAsync($"/api/account/{testAccount.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var account = await response.Content.ReadFromJsonAsync<AccountEntity>();
        account.Should().NotBeNull();
        account.Balance.Should().Be(1000);
    }

    [Fact]
    public async Task GetAccountDetails_ShouldReturnNotFound_WhenAccountDoesNotExist()
    {
        // Arrange
        var testUser = new UserEntity
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "test@example.com",
            PasswordHash = "hashedpassword123",
            Confirmed = true
        };

        _dbContext.Users.Add(testUser);
        await _dbContext.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "valid_token");

        // Act
        var response = await _client.GetAsync($"/api/account/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAccountDetails_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Act
        var response = await _client.GetAsync($"/api/account/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBalance_ShouldReturnCorrectBalance_WhenAccountExists()
    {
        // Arrange
        var testUser = new UserEntity
        {
            Id = Guid.NewGuid(),
            FullName = "John Doe",
            Email = "test@example.com",
            PasswordHash = "hashedpassword123",
            Confirmed = true
        };
        var testAccount = new AccountEntity
        {
            Id = Guid.NewGuid(),
            UserId = testUser.Id,
            Balance = 1500,
            AccountNumber = "UA2038080552523943628122"
        };

        _dbContext.Users.Add(testUser);
        _dbContext.Accounts.Add(testAccount);
        await _dbContext.SaveChangesAsync();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "valid_token");

        // Act
        var response = await _client.GetAsync($"/api/account/balance?accountId={testAccount.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var balanceText = await response.Content.ReadAsStringAsync();
        balanceText.Should().Contain("1500");
    }
}
