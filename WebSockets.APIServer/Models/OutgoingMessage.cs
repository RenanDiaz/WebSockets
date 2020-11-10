using System;

namespace WebSockets.APIServer.Models
{
    public class OutgoingMessage
    {
        public OutgoingMessageType Type { get; set; }
        public string ReceiverSocketId { get; set; }
        public string SocketIdToOmit { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }
    }

    public enum OutgoingMessageType
    {
        CONNECTION_ESTABLISHED, JOINED, MESSAGE, LEFT, ACTION_CONFIRMED, ACTION_DENIED
    }
}
