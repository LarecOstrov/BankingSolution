using Banking.Domain.ValueObjects;
using Microsoft.AspNetCore.SignalR;

namespace Banking.Infrastructure.WebSockets
{
    public class WebSocketHub : Hub
    {
        public async Task BroadcastComment(Transaction transaction)
        {
            await Clients.All.SendAsync("Transaction", transaction);
        }

        public async Task KeepAlive()
        {
            await Clients.Caller.SendAsync("KeepAliveAck", "alive");
        }
    }
}
