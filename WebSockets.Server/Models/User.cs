namespace WebSockets.Server.Models
{
    public class User
    {
        public string Username { get; set; }

        public User() { }

        public User(string username)
        {
            Username = username;
        }
    }
}
