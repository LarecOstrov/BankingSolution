using Serilog;
using System.Net.WebSockets;
using System.Text;

namespace Banking.Infrastructure.WebSockets
{
    public class WebSocketService : IWebSocketService
    {
        private static readonly Dictionary<Guid, WebSocket> _connections = new();

        public WebSocketService() { }

        /// <summary>
        ///  Handle WebSocket connection
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="webSocket"></param>
        /// <returns>Task</returns>
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

        /// <summary>
        /// Send transaction notification to user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="message"></param>
        /// <returns>Task</returns>
        public virtual async Task SendTransactionNotificationAsync(Guid userId, string message)
        {
            var retries = 0;
            var maxRetries = 3;
            while (retries < maxRetries)
            {
                try
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
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error sending WebSocket message to user {userId}");
                    retries++;
                }
            }
        }
    }
}
