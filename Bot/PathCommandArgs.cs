using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelChan.Bot
{
    public class PathCommandArgs : EventArgs
    {
        public string SourceUserName { get; set; }

        public string Character { get; set; }

        public string SystemName { get; set; }

        public string Response { get; set; }
    }
}
