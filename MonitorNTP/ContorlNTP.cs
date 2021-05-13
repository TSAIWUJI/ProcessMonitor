using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Timers;

namespace MonitorNTP
{
    public class ContorlNTP
    {
        private static log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public void CheckNTPAppliction()
        {
            while (true)
            {
                Process ntpProcess = Process.GetProcesses().Where(a => a.ProcessName.Contains("NTPClock")).FirstOrDefault();
                if (ntpProcess == null)
                {
                    Process.Start(@"C:\Gorilla\NTPClock.exe");
                }
                Thread.Sleep(5 * 60 * 1000);
            }
        }

        public void CheckNTPClock()
        {
            System.Timers.Timer t = new System.Timers.Timer(5 * 60 * 1000);
            t.Elapsed += OnTimeEvent;
            t.AutoReset = true;
            t.Enabled = true;
        }

        private void OnTimeEvent(object sender, ElapsedEventArgs e)
        {
            Process ntpProcess = Process.GetProcesses().Where(a => a.ProcessName.Contains("NTPClock")).FirstOrDefault();
            if (ntpProcess == null)
            {
                _log.Debug("Start NTPClock");
                Process.Start(@"C:\Gorilla\NTPClock.exe");
            }
        }
    }
}