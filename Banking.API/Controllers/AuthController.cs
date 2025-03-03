using Banking.Application.Services.Interfaces;
using Banking.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest rerquest)
        {
            var success = await _authService.RegisterAsync(rerquest);
            return success ? Ok("User registered. Wait account verification.") : BadRequest("Registration failed");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var tokens = await _authService.LoginAsync(request);
            return tokens != null ? Ok(tokens) : Unauthorized();
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
        {
            var newToken = await _authService.RefreshTokenAsync(refreshToken);
            return newToken != null ? Ok(new { Token = newToken }) : Unauthorized();
        }
    }
}
