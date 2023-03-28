using System.Collections.Generic;

namespace Tripwire
{
    public class WormholeSystem
    {
        public WormholeSystem Parent { get; set; }
        
        public string ParentSignatureId { get; set; }

        public string SystemId { get; set; }
        public string SystemName { get; set; }
        public IList<WormholeSystem> Children { get; set; }
    }
}