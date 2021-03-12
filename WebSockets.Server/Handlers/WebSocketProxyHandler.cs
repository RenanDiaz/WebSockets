using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
                var thread = new Thread(new ThreadStart(ReceiveMessageFromAPIServer));
                thread.Start();
            }

            var socketId = Connections.GetId(socket);
            var newConnectionMessage = new OutgoingClientMessage
            {
                ConnectionId = socketId,
                Type = ClientMessageType.CONNECT,
                Data = $"Client {socketId} requested a connection.",
                Date = DateTime.Now.Ticks
            };
            await SendMessageToAPIServer(newConnectionMessage);
        }

        public override async Task OnDisconnected(WebSocket socket)
        {
            var socketId = Connections.GetId(socket);
            await base.OnDisconnected(socket);
            var disconnectMessage = new OutgoingClientMessage
            {
                ConnectionId = socketId,
                Type = ClientMessageType.LEAVE,
                Data = $"Client {socketId} left the party.",
                Date = DateTime.Now.Ticks
            };
            await SendMessageToAPIServer(disconnectMessage);

            if (_client != null && Connections.GetAllConnections().Any())
            {
                await _client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "No connections left", CancellationToken.None);
                _client = null;
                Console.WriteLine("Disconnected from API Server");
            }
        }

        public override async Task Receive(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var messageString = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Received message: {messageString}");
            var incomingMessage = JsonConvert.DeserializeObject<IncomingClientMessage>(messageString);
            var socketId = Connections.GetId(socket);
            var outgoingMessage = new OutgoingClientMessage(incomingMessage, socketId);
            await SendMessageToAPIServer(outgoingMessage);
        }

        private async Task SendMessageToAPIServer(OutgoingClientMessage message)
        {
            if (_client != null && message != null)
            {
                var contractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                };
                var messageString = JsonConvert.SerializeObject(message, new JsonSerializerSettings
                {
                    ContractResolver = contractResolver
                });
                var bytes = Encoding.UTF8.GetBytes(messageString);
                await _client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private async void ReceiveMessageFromAPIServer()
        {
            var buffer = new byte[1024 * 4];
            while (_client != null)
            {
                var result = await _client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
                var messageString = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Received message from API Server: {messageString}");
                var incomingMessage = JsonConvert.DeserializeObject<IncomingAPIServerMessage>(messageString);
                if (incomingMessage.Type == APIServerMessageType.ACTION_CONFIRMED && incomingMessage.Data is Models.Action && ((Models.Action)incomingMessage.Data).Type == ClientMessageType.LEAVE.ToString())
                {
                    Console.WriteLine("Action confirmed");
                    await _client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
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
            Console.WriteLine("Stopped listening API Server.");
        }
    }
}
