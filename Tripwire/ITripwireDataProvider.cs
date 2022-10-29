using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tripwire
{
    public interface ITripwireDataProvider
    {
        public DateTime SyncTime { get; }

        public IList<string> SystemIds { get; }

        Task<IList<Wormhole>> GetHoles();

        Task<IList<Signature>> GetSigs();

        Task<bool> Start(CancellationToken token);
    }
    
}