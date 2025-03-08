using System.Net.WebSockets;

namespace Banking.Infrastructure.WebSockets;

public interface IWebSocketService
{
    Task HandleWebSocketAsync(Guid userId, WebSocket webSocket);
    Task SendTransactionNotificationAsync(Guid userId, string message);
}
