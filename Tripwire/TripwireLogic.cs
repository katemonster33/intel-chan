using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;


namespace Tripwire
{
    public class TripwireLogic
    {
        ITripwireDataProvider DataProvider { get; }
        public TripwireLogic(ITripwireDataProvider dataProvier)
        {
            DataProvider = dataProvier;
        }
        public DateTime SyncTime { get => _syncTime; }
        DateTime _syncTime;

        public WormholeSystem CreateSystem(Wormhole connection, string systemId, IList<Signature> signatures, IList<Wormhole> wormholes, int level)
        {
            WormholeSystem output = new WormholeSystem()
            {
                SystemId = systemId,
                Children = new List<KeyValuePair<Signature, WormholeSystem>>()
            };
            List<Signature> systemSigs = signatures.Where(ss => ss.SystemID == systemId).ToList();
            foreach (var sig in systemSigs)
            {
                var hole = wormholes.FirstOrDefault(h => h.InitialID == sig.ID || h.SecondaryID == sig.ID);
                if (hole != null)
                {
                    var otherSigId = hole.InitialID == sig.ID ? hole.SecondaryID : hole.InitialID;
                    var childSig = signatures.FirstOrDefault(child => child.ID == otherSigId);
                    if (childSig != null)
                    {
                        signatures.Remove(childSig);
                        wormholes.Remove(hole);
                        output.Children.Add(new KeyValuePair<Signature, WormholeSystem>(sig, CreateSystem(hole, childSig.SystemID, signatures, wormholes, level + 1)));
                    }
                }
            }
            return output;
        }

        public void GetChainSystemIds(WormholeSystem chain, ref List<string> systemIds)
        {
            systemIds.Add(chain.SystemId);
            foreach (var wh in chain.Children)
            {
                if (wh.Value != null)
                {
                    GetChainSystemIds(wh.Value, ref systemIds);
                }
            }
        }

        public async Task<IList<WormholeSystem>> GetChains()
        {

            var tripwireSigs = await DataProvider.GetSigs();
            var tripwireHoles = await DataProvider.GetHoles();
            _syncTime = DataProvider.SyncTime;
            return GetTheChains(tripwireSigs, tripwireHoles);

        }

        private IList<WormholeSystem> GetTheChains(IList<Signature> tripwireSigs, IList<Wormhole> tripwireHoles)
        {
            var chains = new List<WormholeSystem>();
            foreach (var id in DataProvider.SystemIds)
            {
                chains.Add(CreateSystem(null, id, tripwireSigs, tripwireHoles, 0));
            }
            return chains;
        }
    }
}