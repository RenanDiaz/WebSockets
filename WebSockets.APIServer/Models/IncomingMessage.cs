namespace WebSockets.APIServer.Models
{
    public class IncomingMessage
    {
        public string ConnectionId { get; set; }
        public IncomingMessageType Type { get; set; }
        public object Data { get; set; }
        public long Date { get; set; }
    }

    public enum IncomingMessageType
    {
        CONNECT, JOIN, MESSAGE, LEAVE
    }
}
