using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zkill
{
    public interface IZkillClient
    {
        bool Connected { get; }

        event EventHandler<string> KillReceived;

        Task ConnectAsync(CancellationToken cancellationToken);
        Task DisconnectAsync();
        void Dispose();
        Task SubscribeSystems(List<string> systemIds);
        Task UnsubscribeSystems(List<string> systemIds);
        Task SubscribeCorps(List<string> corpIds);
        Task UnsubscribeCorps(List<string> corpIds);
    }
}