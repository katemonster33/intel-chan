using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zkill
{
    public class DummyZkillClient : IZkillClient
    {
        public bool Connected {get;set;}

        public event EventHandler<string> KillReceived;

        Task readThread = null;
        public async Task ConnectAsync()
        {
            Connected = true;
            if (readThread != null)
            {
                await readThread;
                readThread = null;
            }

            readThread = DoWork();
            
            
        }

        private Task DoWork()
        {

            KillReceived?.Invoke(this, "kill");

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
    }
}