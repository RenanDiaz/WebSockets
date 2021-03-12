using System;

namespace WebSockets.APIServer.Models
{
    public class OutgoingMessage
    {
        public OutgoingMessageType Type { get; set; }
        public string ReceiverSocketId { get; set; }
        public string SocketIdToOmit { get; set; }
        public object Data { get; set; }
        public DateTime Date { get; set; }
    }

    public enum OutgoingMessageType
    {
        CONNECTED, JOINED, MESSAGE, LEFT, ACTION_CONFIRMED, ACTION_DENIED
    }
}
