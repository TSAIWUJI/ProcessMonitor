using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MonitorProcess
{
    public class EnmuTest
    {
        [Flags]
        public enum MonitorTimes 
        {
            One = 0,
            Two = 1,
            Three = 2,
            Four = 3,
            Five = 4,
            Six = 5,
            Seven = 6,
            Eight = 7,
            Nine = 8,
            Ten = 9
        };
        public static int GetValue()
        {
            return 0;
        }
    }
}
