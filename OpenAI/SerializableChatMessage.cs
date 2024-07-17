namespace IntelChan.OpenAI
{
    public class SerializableChatMessage
    {
        public MessageType MessageType { get; set; }

        public string Message { get; set; }

        public SerializableChatMessage(MessageType messageType, string message)
        {
            MessageType = messageType;
            Message = message;
        }
    }
}