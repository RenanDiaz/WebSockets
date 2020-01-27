using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
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

        public override async Task OnDisconnected(WebSocket socket)
        {
            var socketId = Connections.GetId(socket);
            await base.OnDisconnected(socket);
            var user = RemoveUser(socketId);
            await SendMessageToAll($"{user.Username} just left the party.");
        }

        public override async Task Receive(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var socketId = Connections.GetId(socket);
            var messageString = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var message = Newtonsoft.Json.JsonConvert.DeserializeObject<Message>(messageString);
            switch (message.Type)
            {
                case MessageType.JOIN:
                    {
                        var user = new User(message.Text);
                        AddUser(socketId, user);
                        await SendMessageToAllExcept($"{user.Username} just joined the party.", socketId);
                        break;
                    }
                case MessageType.MESSAGE:
                    {
                        Console.WriteLine($"Message received: {message.Text}");
                        var user = GetUserById(socketId);
                        await SendMessageToAll($"{user.Username} said: {message.Text}");
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
