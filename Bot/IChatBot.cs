using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace IntelChan.Bot
{
    public interface IChatBot : IDisposable
    {
        bool IsConnected { get; }

        Task ConnectAsync(CancellationToken token);

        Task DisconnectAsync();

        Task Post(string message);

        event Func<string, Task<string>> HandlePathCommand;

        event Func<string, byte[], Task<string>> HandleDrawCommand;
        
    }
}