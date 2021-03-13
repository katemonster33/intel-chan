using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zkill
{
    public class DummyZkillClient : IZkillClient
    {
        public bool Connected {get;set;}

        public event EventHandler<string> KillReceived;

        public Task ConnectAsync()
        {
            Connected = true;
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

        public Task ReadAsync()
        {
            throw new NotImplementedException();
        }

        public Task SubscribeAll()
        {
            throw new NotImplementedException();
        }

        public Task SubscribeSystems(List<string> systemIds)
        {
            throw new NotImplementedException();
        }

        public Task UnsubscribeAll()
        {
            throw new NotImplementedException();
        }

        public Task UnsubscribeSystems(List<string> systemIds)
        {
            throw new NotImplementedException();
        }
    }
}