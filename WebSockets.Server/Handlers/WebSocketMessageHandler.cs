using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WebSockets.Server.Models;
using WebSockets.Server.SocketsManager;

namespace WebSockets.Server.Handlers
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
        }

        public override async Task OnDisconnected(WebSocket socket)
        {
            var socketId = Connections.GetId(socket);
            await base.OnDisconnected(socket);
            var user = RemoveUser(socketId);
            var message = new OutgoingAPIServerMessage
            {
                Type = APIServerMessageType.LEFT,
                Data = new List<User> { user },
                Date = DateTime.Now
            };
            await SendMessageToAll(message);
        }

        public override async Task Receive(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var socketId = Connections.GetId(socket);
            var messageString = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var message = JsonConvert.DeserializeObject<IncomingClientMessage>(messageString);
            switch (message.Type)
            {
                case ClientMessageType.JOIN:
                    {
                        var user = (User)message.Data;
                        AddUser(socketId, user);
                        var confirmationMessage = new OutgoingAPIServerMessage
                        {
                            Type = APIServerMessageType.ACTION_CONFIRMED,
                            Data = new Models.Action
                            {
                                Type = ClientMessageType.JOIN.ToString()
                            },
                            Date = DateTime.Now
                        };
                        await SendMessage(socketId, confirmationMessage);
                        var outgoingMessage = new OutgoingAPIServerMessage
                        {
                            Type = APIServerMessageType.JOINED,
                            Data = new List<User> { user },
                            Date = DateTime.Now
                        };
                        await SendMessageToAllExcept(outgoingMessage, socketId);
                        break;
                    }
                case ClientMessageType.MESSAGE:
                    {
                        Console.WriteLine($"Message received: {message.Data}");
                        var user = GetUserById(socketId);
                        var textMessage = (TextMessage)message.Data;
                        textMessage.Sender = user;
                        var outgoingMessage = new OutgoingAPIServerMessage
                        {
                            Type = APIServerMessageType.MESSAGE,
                            Data = textMessage,
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
