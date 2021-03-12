namespace WebSockets.Server.Models
{
    public class TextMessage
    {
        public string Message { get; set; }
        public User Sender { get; set; }
    }
}
