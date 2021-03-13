using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tripwire
{
    public interface ITripwire
    {
        void ClearSavedSystemIds();
        WormholeSystem CreateSystem(Wormhole connection, string systemId, List<Signature> signatures, List<Wormhole> wormholes, int level);
        List<WormholeSystem> GetChains(out DateTime syncTime);
        void GetChainSystemIds(WormholeSystem chain, ref List<string> systemIds);
        Task<bool> Login(string username, string password);
    }
}