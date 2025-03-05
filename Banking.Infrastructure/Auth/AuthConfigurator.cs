using Banking.Infrastructure.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Banking.Infrastructure.Auth;
public static class AuthConfigurator
{
    /// <summary>
    /// Configures the authentication for the application
    /// </summary>
    /// <param name="services"></param>
    /// <param name="solutionOptions"></param>
    public static void ConfigureAuthentication(this IServiceCollection services, SolutionOptions solutionOptions)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(solutionOptions.Jwt.SecretKey));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = solutionOptions.Jwt.Issuer,
                    ValidAudience = solutionOptions.Jwt.Audience,
                    IssuerSigningKey = key,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddAuthorization();
    }
}


