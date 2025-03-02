using Banking.Infrastructure.WebSockets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Banking.Worker.Controllers
{
    [Route("ws")]
    [ApiController]
    public class WebSocketController : ControllerBase
    {
        private readonly WebSocketService _webSocketService;

        public WebSocketController(WebSocketService webSocketService)
        {
            _webSocketService = webSocketService;
        }

        [Authorize]
        [HttpGet("connect")]
        public async Task Connect()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                var userId = Guid.Parse(User.FindFirst("sub")!.Value); // JWT User ID
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await _webSocketService.HandleWebSocketAsync(userId, webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = 400;
            }
        }
    }
}
