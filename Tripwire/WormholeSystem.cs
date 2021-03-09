using System.Collections.Generic;

namespace Tripwire
{
    public class WormholeSystem
    {
        public Signature ParentSignature { get; set; }
        
        public string SystemId { get; set; }
        
        public List<KeyValuePair<Signature, WormholeSystem>> Children { get; set; }
    }
}