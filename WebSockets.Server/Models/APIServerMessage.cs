using System;

namespace WebSockets.Server.Models
{
    public abstract class APIServerMessage
    {
        public APIServerMessageType Type { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }
    }

    public class OutgoingAPIServerMessage : APIServerMessage
    {
        public OutgoingAPIServerMessage() { }

        public OutgoingAPIServerMessage(IncomingAPIServerMessage message)
        {
            Type = message.Type;
            Text = message.Text;
            Date = message.Date;
        }
    }

    public class IncomingAPIServerMessage : APIServerMessage
    {
        public string ReceiverSocketId { get; set; }
        public string SocketIdToOmit { get; set; }
    }

    public enum APIServerMessageType
    {
        NEW_CONNECTION, JOINED, MESSAGE, LEFT, ACTION_CONFIRMED, ACTION_DENIED
    }
}
