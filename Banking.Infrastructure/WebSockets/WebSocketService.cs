using Serilog;
using System.Net.WebSockets;
using System.Text;

namespace Banking.Infrastructure.WebSockets
{
    public class WebSocketService
    {
        private static readonly Dictionary<Guid, WebSocket> _connections = new();

        public WebSocketService() { }

        public async Task HandleWebSocketAsync(Guid userId, WebSocket webSocket)
        {
            Log.Information($"User {userId} connected via WebSocket");

            if (!_connections.ContainsKey(userId))
            {
                _connections[userId] = webSocket;
            }

            var buffer = new byte[1024 * 4];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Log.Information($"User {userId} disconnected.");
                    _connections.Remove(userId);
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                }
            }
        }

        public async Task SendTransactionNotificationAsync(Guid userId, string message)
        {
            if (_connections.TryGetValue(userId, out var webSocket) && webSocket.State == WebSocketState.Open)
            {
                var bytes = Encoding.UTF8.GetBytes(message);
                await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                Log.Information($"Sent WebSocket message to user {userId}: {message}");
            }
            else
            {
                Log.Warning($"User {userId} is not connected via WebSocket.");
            }
        }
    }
}
