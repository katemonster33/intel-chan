using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IntelChan.Bot
{
    public class DummyChatBot : IChatBot
    {
        public bool IsConnected { get; private set; }

        public event Func<string, Task<string>> HandlePathCommand;
        public event Func<string, byte[], Task<string>> HandleDrawCommand;

        public Task ConnectAsync(CancellationToken token)
        {
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }

        public Task Post(string message)
        {
            Console.WriteLine("message");
            return Task.CompletedTask;
        }
    }
}