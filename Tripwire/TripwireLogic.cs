using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace Tripwire
{
    public class TripwireLogic
    {
        List<WormholeSystem> cachedChains = new List<WormholeSystem>();

        ITripwireDataProvider DataProvider { get; }

        public bool Connected { get; set; }

        ILogger<TripwireLogic> Logger { get; }

        public TripwireLogic(ITripwireDataProvider dataProvier, ILogger<TripwireLogic> logger)
        {
            DataProvider = dataProvier;
            Logger = logger;
        }

        public DateTime SyncTime { get => _syncTime; }
        DateTime _syncTime;

        public WormholeSystem CreateSystem(Wormhole connection, Signature signature, IList<Signature> signatures, IList<Wormhole> wormholes, WormholeSystem parent, int level)
        {
            WormholeSystem output = new WormholeSystem()
            {
                SystemId = signature.SystemID,
                SystemName = signature.SystemName,
                Parent = parent,
                Children = new List<WormholeSystem>()
            };
            List<Signature> systemSigs = signatures.Where(ss => ss.SystemID == signature.SystemID).ToList();
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
                        output.Children.Add(CreateSystem(hole, childSig, signatures, wormholes, output, level + 1));
                    }
                }
            }
            return output;
        }

        public async Task StartAsync(CancellationToken token)
        {
            Logger.LogInformation("Starting Tripwire Connection");
            Connected = await DataProvider.Start(token);
        }

        public void FlattenList(WormholeSystem chain, ref List<WormholeSystem> systems)
        {
            systems.Add(chain);
            foreach (var wh in chain.Children)
            {
                FlattenList(wh, ref systems);
            }
        }

        public async Task<WormholeSystem> FindCharacter(string name)
        {
            IList<OccupiedSystem> occupiedSystems = await DataProvider.GetOccupiedSystems();
            foreach(var sys in occupiedSystems)
            {
                if(sys.count != "0")
                {
                    var occupants = DataProvider.GetOccupants(sys.systemID);
                }
            }
            return null;
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
                chains.Add(CreateSystem(null, new Signature{SystemID=id,SystemName="Home"}, tripwireSigs, tripwireHoles, null, 0));
            }
            lock(cachedChains)
            {
                cachedChains.Clear();
                cachedChains.AddRange(chains);
            }
            return chains;
        }
    }
}