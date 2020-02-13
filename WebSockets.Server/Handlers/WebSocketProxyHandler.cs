using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSockets.Server.Models;
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
            if (_client == null)
            {
                _client = new ClientWebSocket();
                await _client.ConnectAsync(new Uri("ws://localhost:5001/chat"), CancellationToken.None);
                Console.WriteLine($"WebSocket connection with API established @ {DateTime.Now:F}");
                await ReceiveMessageFromAPIServer(_client);
            }
        }

        public override async Task OnDisconnected(WebSocket socket)
        {
            var socketId = Connections.GetId(socket);
            await base.OnDisconnected(socket);
            var disconnectMessage = new OutgoingClientMessage()
            {
                ConnectionId = socketId,
                Type = ClientMessageType.LEAVE,
                Date = DateTime.Now.Ticks
            };
            await SendMessageToAPIServer(disconnectMessage);
            if (_client != null && Connections.GetAllConnections().Count == 0)
            {
                await _client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                _client = null;
                Console.WriteLine("Disconnected from API Server");
            }
        }

        public override async Task Receive(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var messageString = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Received message: {messageString}");
            var incomingMessage = Newtonsoft.Json.JsonConvert.DeserializeObject<IncomingClientMessage>(messageString);
            var socketId = Connections.GetId(socket);
            var outgoingMessage = new OutgoingClientMessage(incomingMessage, socketId);
            await SendMessageToAPIServer(outgoingMessage);
        }

        private async Task SendMessageToAPIServer(OutgoingClientMessage message)
        {
            if (_client != null && message != null)
            {
                var messageString = Newtonsoft.Json.JsonConvert.SerializeObject(message);
                var bytes = Encoding.UTF8.GetBytes(messageString);
                await _client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private async Task ReceiveMessageFromAPIServer(ClientWebSocket client)
        {
            var buffer = new byte[1024 * 4];
            while (true)
            {
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
                var messageString = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received message from API Server: {messageString}");
                var incomingMessage = Newtonsoft.Json.JsonConvert.DeserializeObject<IncomingAPIServerMessage>(messageString);
                var outgoingMessage = new OutgoingAPIServerMessage(incomingMessage);
                if (!string.IsNullOrEmpty(incomingMessage.ReceiverSocketId))
                {
                    await SendMessage(incomingMessage.ReceiverSocketId, outgoingMessage);
                }
                else if (!string.IsNullOrEmpty(incomingMessage.SocketIdToOmit))
                {
                    await SendMessageToAllExcept(outgoingMessage, incomingMessage.SocketIdToOmit);
                }
                else
                {
                    await SendMessageToAll(outgoingMessage);
                }
            }
        }
    }
}
