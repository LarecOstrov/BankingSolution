using Banking.Domain.ValueObjects;
using System.Security.Claims;

namespace Banking.Application.Services.Interfaces;

public interface IAuthService
{
    ClaimsPrincipal? ValidateJwtToken(string? token);
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<bool> RegisterAsync(RegisterRequest request);
    Task<string?> RefreshTokenAsync(string refreshToken);
    Guid? GetUserIdFromToken(ClaimsPrincipal user);
}
