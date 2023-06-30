using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zkill
{
    public class DummyZkillClient : IZkillClient
    {
        public bool Connected { get; set; }

        public CancellationToken CancelToken { get; set; }

        public event EventHandler<string>? KillReceived;

        Task? readThread = null;
        public async Task ConnectAsync(CancellationToken cancelToken)
        {
            Connected = true;
            CancelToken=cancelToken;
            if (readThread != null)
            {
                await readThread;
                readThread = null;
            }

            readThread = DoWork();
        }

        private Task DoWork()
        {
            while(true){
                Thread.Sleep(5000);
                KillReceived?.Invoke(this, "kill");
                if(CancelToken.IsCancellationRequested)
                    break;
                
            }
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            Connected = false;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            
        }

        public Task SubscribeSystems(List<string> systemIds)
        {
            return Task.CompletedTask;
        }

        public Task UnsubscribeSystems(List<string> systemIds)
        {
            return Task.CompletedTask;
        }

        public Task SubscribeCorps(List<string> systemIds)
        {
            return Task.CompletedTask;
        }

        public Task UnsubscribeCorps(List<string> systemIds)
        {
            return Task.CompletedTask;
        }
    }
}