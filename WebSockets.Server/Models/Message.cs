namespace WebSockets.Server.Models
{
    public class Message
    {
        public MessageType Type { get; set; }
        public string Text { get; set; }
        public long Date { get; set; }
    }

    public enum MessageType
    {
        JOIN, MESSAGE
    }
}
