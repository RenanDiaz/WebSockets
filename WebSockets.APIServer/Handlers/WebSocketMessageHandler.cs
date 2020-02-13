using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using WebSockets.APIServer.Models;
using WebSockets.APIServer.SocketsManager;

namespace WebSockets.APIServer.Handlers
{
    public class WebSocketMessageHandler : SocketHandler
    {
        public ConcurrentDictionary<string, User> _users = new ConcurrentDictionary<string, User>();
        public WebSocketMessageHandler(ConnectionManager connections) : base(connections)
        {
        }

        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket);
            var socketId = Connections.GetId(socket);
            Console.WriteLine($"WebSocket connection established with {socketId} @ {DateTime.Now:F}.");
        }

        public override async Task OnDisconnected(WebSocket socket)
        {
            var socketId = Connections.GetId(socket);
            await base.OnDisconnected(socket);
            Console.WriteLine($"Disconnected from client {socketId}.");
        }

        public override async Task Receive(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var messageString = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Console.WriteLine($"Received message: {messageString}");
            var socketId = Connections.GetId(socket);
            var message = Newtonsoft.Json.JsonConvert.DeserializeObject<IncomingMessage>(messageString);
            switch (message.Type)
            {
                case IncomingMessageType.JOIN:
                    {
                        var user = new User(message.Text, socketId);
                        AddUser(message.ConnectionId, user);
                        var confirmationMessage = new OutgoingMessage()
                        {
                            Type = OutgoingMessageType.ACTION_CONFIRMED,
                            ReceiverSocketId = message.ConnectionId,
                            Date = DateTime.Now
                        };
                        await SendMessage(socketId, confirmationMessage);
                        var outgoingMessage = new OutgoingMessage()
                        {
                            Type = OutgoingMessageType.JOINED,
                            SocketIdToOmit = message.ConnectionId,
                            Text = $"{user.Username} just joined the party.",
                            Date = DateTime.Now
                        };
                        await SendMessageToAll(outgoingMessage);
                        break;
                    }
                case IncomingMessageType.MESSAGE:
                    {
                        var user = GetUserById(message.ConnectionId);
                        var confirmationMessage = new OutgoingMessage()
                        {
                            Type = OutgoingMessageType.ACTION_CONFIRMED,
                            ReceiverSocketId = message.ConnectionId,
                            Date = DateTime.Now
                        };
                        await SendMessage(socketId, confirmationMessage);
                        var outgoingMessage = new OutgoingMessage()
                        {
                            Type = OutgoingMessageType.MESSAGE,
                            SocketIdToOmit = user.ConnectionId,
                            Text = $"{user.Username} said: {message.Text}",
                            Date = DateTime.Now
                        };
                        await SendMessageToAll(outgoingMessage);
                        break;
                    }
                case IncomingMessageType.LEAVE:
                    {
                        var user = RemoveUser(message.ConnectionId);
                        var outgoingMessage = new OutgoingMessage()
                        {
                            Type = OutgoingMessageType.LEFT,
                            Text = $"{user.Username} just left the party.",
                            Date = DateTime.Now
                        };
                        await SendMessageToAll(outgoingMessage);
                        break;
                    }
                default:
                    throw new Exception("Invalid message type.");
            }
        }

        public ConcurrentDictionary<string, User> GetAllUsers()
        {
            return _users;
        }

        public User GetUserById(string id)
        {
            Console.WriteLine($"Will get user {id}.");
            return _users.FirstOrDefault(x => x.Key == id).Value;
        }

        public string GetId(User user)
        {
            return _users.FirstOrDefault(x => x.Value == user).Key;
        }

        public void AddUser(string id, User user)
        {
            _users.TryAdd(id, user);
            Console.WriteLine($"New user added: {user.Username} ({id})");
        }

        public User RemoveUser(string id)
        {
            _users.TryRemove(id, out var user);
            Console.WriteLine($"User {user.Username} removed.");
            return user;
        }
    }
}
