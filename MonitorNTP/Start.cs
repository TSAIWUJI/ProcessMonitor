using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MonitorNTP
{
    public static class Start
    {
        public static void StartMonitorNTP()
        {
            ContorlNTP cont = new ContorlNTP();
            Thread t = new Thread(cont.CheckNTPAppliction);
            t.Start();
        }
        public static void StartMonitorNTP2()
        {
            ContorlNTP ctrlNTP = new ContorlNTP();
            ctrlNTP.CheckNTPClock();
        }
    }
}
