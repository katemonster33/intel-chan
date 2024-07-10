using OpenAI;

namespace IntelChan.OpenAI
{
    public class OpenAIManager
    {
        OpenAIClient client { get; }
        public OpenAIManager(OpenAIClient client)
        {
            this.client = client;
        }

        public void SendChat(string? context, string chat)
        {

        }

    }
}