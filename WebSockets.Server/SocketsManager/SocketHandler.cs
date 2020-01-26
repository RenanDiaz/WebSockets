using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public async Task SendMessage(WebSocket socket, string message)
        {
            if (socket.State != WebSocketState.Open) return;
            await socket.SendAsync(new ArraySegment<byte>(Encoding.ASCII.GetBytes(message), 0, message.Length), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.Write($"Message \"{message}\"");
        }

        public async Task SendMessage(string id, string message)
        {
            await SendMessage(Connections.GetSocketById(id), message);
            Console.WriteLine($" sent to {id}.");
        }

        public async Task SendMessageToAll(string message)
        {
            foreach (var con in Connections.GetAllConnections())
            {
                await SendMessage(con.Value, message);
            }
        }

        public abstract Task Receive(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);
    }
}
