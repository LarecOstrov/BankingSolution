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

        /// <summary>
        /// Register new user
        /// </summary>
        /// <param name="rerquest"></param>
        /// <returns>IActionResult</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest rerquest)
        {
            
                var success = await _authService.RegisterAsync(rerquest);
                return success ? Ok("User registered. Wait account verification.") : BadRequest("Registration failed");
            
            
        }

        /// <summary>
        /// Login user
        /// </summary>
        /// <param name="request"></param>
        /// <returns>IActionResult</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            
                var tokens = await _authService.LoginAsync(request);
                return tokens != null ? Ok(tokens) : Unauthorized();
            
        }

        /// <summary>
        /// Refresh token
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <returns>IActionResult</returns>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
        {
            
                var newToken = await _authService.RefreshTokenAsync(refreshToken);
                return newToken != null ? Ok(new { Token = newToken }) : Unauthorized();
            
        }
    }
}
