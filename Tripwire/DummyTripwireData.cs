using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tripwire
{
    public class DummyTripwireData : ITripwireDataProvider
    {
        public DateTime SyncTime => DateTime.Now;

        public IList<string> SystemIds => new List<string>{"31000732"};

        public Task<IList<Wormhole>> GetHoles()
        {
            var retList = new List<Wormhole>{
                new Wormhole{InitialID="27900",MaskID="98277602.2",Parent="initial",SecondaryID="27902"}
            };
            return Task.FromResult<IList<Wormhole>>(retList);
        }

        public Task<IList<OccupiedSystem>> GetOccupiedSystems()
        {
            return Task.FromResult<IList<OccupiedSystem>>(new List<OccupiedSystem>());
        }

        public Task<IList<Occupant>> GetOccupants(string systemId)
        {
            return Task.FromResult<IList<Occupant>>(new List<Occupant>());
        }

        public Task RefreshData(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        public Task<IList<Signature>> GetSigs()
        {
            return Task.FromResult<IList<Signature>>(new List<Signature>{
                new Signature{
                    Bookmark="NULL",
                    CreatedByID="92265747",
                    CreatedByName="Miho Yvormes",
                    ID="27900",
                    LifeLeft="2021-03-14 16:43:56",
                    LifeLength="57600",
                    LifeTime="2021-03-14 00:43:56",
                    MaskID="98277602.2",
                    ModifiedByID="92265747",
                    ModifiedByName="Miho Yvormes",
                    ModifiedTime="2021-03-14 00:48:53",
                    Name=string.Empty,
                    SignatureID="yqw568",
                    SystemID="31000540",
                    SystemName="J105342",
                    Type="wormhole"
            }});
        }

        public Task<bool> Start(CancellationToken token)
        {
            return Task.FromResult(true);
        }
    }
}