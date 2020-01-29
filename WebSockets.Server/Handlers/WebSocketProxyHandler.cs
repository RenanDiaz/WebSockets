using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSockets.Server.SocketsManager;

namespace WebSockets.Server.Handlers
{
    public class WebSocketProxyHandler : SocketHandler
    {
        public ClientWebSocket _client;

        public WebSocketProxyHandler(ConnectionManager connections) : base(connections)
        {
        }

        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket);
            var socketId = Connections.GetId(socket);
            if (_client == null)
            {
                _client = new ClientWebSocket();
                await _client.ConnectAsync(new Uri("ws://localhost:5001/chat"), CancellationToken.None);
                Console.WriteLine($"WebSocket connection with API established @{DateTime.UtcNow:F}.");
                var receive = ReceiveAsync(_client);
                await Task.WhenAll(receive);
            }
        }

        public override async Task OnDisconnected(WebSocket socket)
        {
            var socketId = Connections.GetId(socket);
            await base.OnDisconnected(socket);
            if (Connections.GetAllConnections().Count == 0 && _client != null)
            {
                await _client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                _client = null;
                Console.WriteLine("Disconnected from API Server.");
            }
        }

        public override async Task Receive(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var socketId = Connections.GetId(socket);
            var message = $"{socketId} said: {Encoding.UTF8.GetString(buffer, 0, result.Count)}";
            await SendMessageToAPIServer(buffer);
        }

        private async Task SendMessageToAPIServer(byte[] bytes)
        {
            if (_client != null && bytes.Length > 0)
            {
                await _client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private async Task ReceiveAsync(ClientWebSocket client)
        {
            var buffer = new byte[1024 * 4];
            while (true)
            {
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await SendMessageToAll(message);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
            }
        }
    }
}