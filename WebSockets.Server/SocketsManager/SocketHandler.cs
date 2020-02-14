using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebSockets.Server.Models;

namespace WebSockets.Server.SocketsManager
{
    public abstract class SocketHandler
    {
        public ConnectionManager Connections { get; set; }

        public SocketHandler(ConnectionManager connections)
        {
            Connections = connections;
        }

        public virtual async Task OnConnected(WebSocket socket)
        {
            await Task.Run(() => { Connections.AddSocket(socket); });
            Console.WriteLine("Socket added");
        }

        public virtual async Task OnDisconnected(WebSocket socket)
        {
            await Connections.RemoveSocketAsync(Connections.GetId(socket));
            Console.WriteLine("Socket removed");
        }

        public async Task SendMessage(WebSocket socket, OutgoingAPIServerMessage message)
        {
            if (socket.State != WebSocketState.Open) return;
            var contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };
            var messageString = JsonConvert.SerializeObject(message, new JsonSerializerSettings
            {
                ContractResolver = contractResolver
            });
            await socket.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(messageString), 0, messageString.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine($"Message \"{messageString}\"");
        }

        public async Task SendMessage(string id, OutgoingAPIServerMessage message)
        {
            await SendMessage(Connections.GetSocketById(id), message);
        }

        public async Task SendMessageToAll(OutgoingAPIServerMessage message)
        {
            await SendMessageToAllExcept(message, null);
        }

        public async Task SendMessageToAllExcept(OutgoingAPIServerMessage message, string socketId)
        {
            foreach (var con in Connections.GetAllConnections())
            {
                if (con.Key == socketId) continue;
                await SendMessage(con.Value, message);
            }
        }

        public abstract Task Receive(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);
    }
}
