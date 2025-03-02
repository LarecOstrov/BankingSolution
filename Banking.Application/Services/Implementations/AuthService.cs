using Banking.Application.Repositories.Interfaces;
using Banking.Application.Services.Interfaces;
using Banking.Infrastructure.Config;
using Banking.Infrastructure.Database.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Banking.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly string _jwtSecretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly SolutionOptions _solutionOptions;

    public AuthService(IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IOptions<SolutionOptions> solutionOptions)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _solutionOptions = solutionOptions.Value;
        _jwtSecretKey = _solutionOptions.Jwt.SecretKey;
        _issuer = _solutionOptions.Jwt.Issuer;
        _audience = _solutionOptions.Jwt.Audience;
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        var user = await _userRepository.GetUserByEmailAsync(email);
        if (user == null || !VerifyPassword(password, user.PasswordHash))
            return null;

        if (!user.Confirmed)
            return null;

        return GenerateJwtToken(user);
    }

    public async Task<bool> RegisterAsync(string fullName, string email, string password, string roleName)
    {
        if (await _userRepository.GetUserByEmailAsync(email) != null)
            return false;

        var role = await _roleRepository.GetRoleByNameAsync(roleName);
        if (role == null) return false;

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = HashPassword(password),
            RoleId = role.Id,
            Confirmed = false
        };

        return await _userRepository.AddAsync(user) is not null;
    }

    public async Task<string?> RefreshTokenAsync(string refreshToken)
    {
        var tokenEntity = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
        if (tokenEntity == null || tokenEntity.ExpiryDate < DateTime.UtcNow)
            return null;

        var user = await _userRepository.GetByIdAsync(tokenEntity.UserId);
        if (user == null) return null;

        var newToken = GenerateJwtToken(user);

        await _refreshTokenRepository.DeleteAsync(refreshToken);

        var newRefreshToken = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiryDate = DateTime.UtcNow.AddDays(7)
        };
        await _refreshTokenRepository.AddAsync(newRefreshToken);

        return newToken;
    }

    /// <summary>
    /// Get user ID from JWT token
    /// </summary>
    public Guid? GetUserIdFromToken(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }


    #region Private
    private string GenerateJwtToken(UserEntity user)
    {
        if (user.Role == null) throw new Exception("User role not found");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.Name)
        };

        var token = new JwtSecurityToken(
            _issuer,
            _audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        return HashPassword(password) == storedHash;
    }
    #endregion
}
