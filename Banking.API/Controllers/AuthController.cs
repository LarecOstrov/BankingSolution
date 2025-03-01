using Banking.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(string fullName, string email, string password, string role)
        {
            var success = await _authService.RegisterAsync(fullName, email, password, role);
            return success ? Ok("User registered") : BadRequest("Registration failed");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(string email, string password)
        {
            var token = await _authService.LoginAsync(email, password);
            return token != null ? Ok(new { Token = token }) : Unauthorized();
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(string refreshToken)
        {
            var newToken = await _authService.RefreshTokenAsync(refreshToken);
            return newToken != null ? Ok(new { Token = newToken }) : Unauthorized();
        }
    }
}
