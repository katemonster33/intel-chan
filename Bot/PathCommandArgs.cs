using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntelChan.Bot
{
    public class PathCommandArgs : EventArgs
    {
        public string Character { get; set; }

        public string SystemName { get; set; }
    }
}
