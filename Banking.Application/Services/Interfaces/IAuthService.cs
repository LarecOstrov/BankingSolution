using System.Security.Claims;

namespace Banking.Application.Services.Interfaces;

public interface IAuthService
{
    Task<string?> LoginAsync(string email, string password);
    Task<bool> RegisterAsync(string fullName, string email, string password, string roleName);
    Guid? GetUserIdFromToken(ClaimsPrincipal user);
}
