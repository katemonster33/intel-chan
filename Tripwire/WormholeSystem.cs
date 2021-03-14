using System.Collections.Generic;

namespace Tripwire
{
    public class WormholeSystem
    {
        
        public string SystemId { get; set; }
        
        public List<KeyValuePair<Signature, WormholeSystem>> Children { get; set; }
    }
}