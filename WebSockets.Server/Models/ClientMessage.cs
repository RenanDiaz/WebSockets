namespace WebSockets.Server.Models
{
    public abstract class ClientMessage
    {
        public ClientMessageType Type { get; set; }
        public object Data { get; set; }
        public long Date { get; set; }
    }

    public class OutgoingClientMessage : ClientMessage
    {
        public string ConnectionId { get; set; }

        public OutgoingClientMessage() { }

        public OutgoingClientMessage(IncomingClientMessage message, string connectionId)
        {
            Type = message.Type;
            Data = message.Data;
            Date = message.Date;
            ConnectionId = connectionId;
        }
    }

    public class IncomingClientMessage : ClientMessage
    {
    }

    public enum ClientMessageType
    {
        CONNECT, JOIN, MESSAGE, LEAVE
    }
}
