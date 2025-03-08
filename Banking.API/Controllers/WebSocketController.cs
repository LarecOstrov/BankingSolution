using Banking.Application.Services.Interfaces;
using Banking.Infrastructure.WebSockets;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Banking.Worker.Controllers
{
    [ApiController]
    [Route("ws")]
    public class WebSocketController : ControllerBase
    {
        private readonly IWebSocketService _webSocketService;
        private readonly IAuthService _authService;

        public WebSocketController(IWebSocketService webSocketService, IAuthService authService)
        {
            _webSocketService = webSocketService;
            _authService = authService;
        }

        [HttpGet("connect")]
        public async Task ConnectAsync()
        {
            if (!HttpContext.WebSockets.IsWebSocketRequest)
            {
                HttpContext.Response.StatusCode = 400;
                return;
            }

            var token = Request.Query["access_token"].ToString();
            if (string.IsNullOrEmpty(token))
            {
                HttpContext.Response.StatusCode = 401;
                return;
            }

            var principal = _authService.ValidateJwtToken(token);
            if (principal == null)
            {
                HttpContext.Response.StatusCode = 401;
                return;
            }

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                      ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var userId))
            {
                HttpContext.Response.StatusCode = 401;
                return;
            }

            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
            await _webSocketService.HandleWebSocketAsync(userId, webSocket);
        }
    }

}
