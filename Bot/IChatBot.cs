using System;
using System.Collections.Generic;
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

        event Func<string, byte[]?, Task<string>>? HandleDrawCommand;
        event Func<Task<List<string>>>? HandleGetModelsCommand;
        event Func<string, Task<bool>>? HandleSetModelCommand;
    }
}