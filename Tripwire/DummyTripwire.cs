using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tripwire
{
    public class DummyTripwire : ITripwire
    {
        public void ClearSavedSystemIds()
        {
            throw new NotImplementedException();
        }

        public WormholeSystem CreateSystem(Wormhole connection, string systemId, List<Signature> signatures, List<Wormhole> wormholes, int level)
        {
            throw new NotImplementedException();
        }

        public List<WormholeSystem> GetChains(out DateTime syncTime)
        {
            throw new NotImplementedException();
        }

        public void GetChainSystemIds(WormholeSystem chain, ref List<string> systemIds)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Login(string username, string password)
        {
            throw new NotImplementedException();
        }
    }
}