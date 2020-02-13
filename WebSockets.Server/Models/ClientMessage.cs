namespace WebSockets.Server.Models
{
    public abstract class ClientMessage
    {
        public ClientMessageType Type { get; set; }
        public string Text { get; set; }
        public long Date { get; set; }
    }

    public class OutgoingClientMessage : ClientMessage
    {
        public string ConnectionId { get; set; }

        public OutgoingClientMessage() { }

        public OutgoingClientMessage(IncomingClientMessage message, string connectionId)
        {
            Type = message.Type;
            Text = message.Text;
            Date = message.Date;
            ConnectionId = connectionId;
        }
    }

    public class IncomingClientMessage : ClientMessage
    {
    }

    public enum ClientMessageType
    {
        JOIN, MESSAGE, LEAVE
    }
}
