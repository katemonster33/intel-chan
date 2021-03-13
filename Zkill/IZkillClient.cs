using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zkill
{
    public interface IZkillClient
    {
        bool Connected { get; }

        event EventHandler<string> KillReceived;

        Task ConnectAsync();
        Task DisconnectAsync();
        void Dispose();
        Task ReadAsync();
        Task SubscribeAll();
        Task SubscribeSystems(List<string> systemIds);
        Task UnsubscribeAll();
        Task UnsubscribeSystems(List<string> systemIds);
    }
}