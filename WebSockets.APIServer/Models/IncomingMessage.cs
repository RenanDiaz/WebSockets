namespace WebSockets.APIServer.Models
{
    public class IncomingMessage
    {
        public string ConnectionId { get; set; }
        public IncomingMessageType Type { get; set; }
        public string Text { get; set; }
        public long Date { get; set; }
    }

    public enum IncomingMessageType
    {
        JOIN, MESSAGE, LEAVE
    }
}
