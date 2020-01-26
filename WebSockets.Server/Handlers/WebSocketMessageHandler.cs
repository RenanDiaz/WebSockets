using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using WebSockets.Server.SocketsManager;

namespace WebSockets.Server.Handlers
{
    public class WebSocketMessageHandler : SocketHandler
    {
        public WebSocketMessageHandler(ConnectionManager connections) : base(connections)
        {
        }

        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket);
            var socketId = Connections.GetId(socket);
            await SendMessageToAll($"{socketId} just joined the party *****");
        }

        public override async Task Receive(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var socketId = Connections.GetId(socket);
            var message = $"{socketId} said: {Encoding.UTF8.GetString(buffer, 0, result.Count)}";
            Console.WriteLine($"Message received: {message}");
            await SendMessageToAll(message);
        }
    }
}
