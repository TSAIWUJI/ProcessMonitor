using SharpPcap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MonitorProcess
{
    public class CaptureList
    {
        private log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public ProcessPerformanceInfo ProcInfo;
        private static string MONITORPROCESSNAME;
        public CaptureList(ProcessPerformanceInfo pInfo, string monitorProcessName)
        {
            ProcInfo = pInfo;
            MONITORPROCESSNAME = monitorProcessName;
        }
        public List<CaptureDevice> DeviceList()
        {
            CaptureDeviceList devices = CaptureDeviceList.Instance;
            List<int> ports = new List<int>();
            List<CaptureDevice> result = new List<CaptureDevice>();
            int getPortTimes = 0;

            while (ports.Count() == 0 && getPortTimes < 3)
            {
                ports = GetLisetnPort(GetProcessIDList(MONITORPROCESSNAME));
                getPortTimes++;
                if (ports.Count() == 0)
                {
                    _log.DebugFormat("Get Port fail to sleep 10 sec and Times is : {0}", getPortTimes);
                    Thread.Sleep(10 * 1000);
                }
            }
            if (ports.Count() == 0)
            {
                return result;
            }
            string IP = Dns.GetHostByName(Dns.GetHostName()).AddressList.First().ToString();
            
            if (devices.Count < 1)
            {
                _log.DebugFormat("No device found on this machine");
                return result;
            }

            for (int i = 0; i < devices.Count; i++)
            {
                devices[i].Open();
                Thread.Sleep(1 * 1000); // 讓程式暫停 1 秒，確認網卡可以收集封包
                if (devices[i].Statistics.ReceivedPackets == 0)
                {
                    devices[i].Close();
                    continue;
                }
                _log.DebugFormat("Line 140 and ports.count {0}", ports.Count);
                for (int j = 0; j < ports.Count; j++)
                {
                    result.Add(new CaptureDevice
                    {
                        IP = IP,
                        PortID = ports[j],
                        DeviceID = i
                    });
                }
            }
            return result;
        }
        private List<int> GetLisetnPort(List<int> pids)
        {
            _log.Debug("Start Sleep 30 Sec");
            Thread.Sleep(3 * 10 * 1000);
            //Thread.Sleep(1000);
            _log.Debug("End Sleep 30 Sec");
            Process pro = new Process();
            pro.StartInfo.FileName = "cmd.exe";
            pro.StartInfo.UseShellExecute = false;
            pro.StartInfo.RedirectStandardInput = true;
            pro.StartInfo.RedirectStandardOutput = true;
            pro.StartInfo.RedirectStandardError = true;
            pro.StartInfo.CreateNoWindow = true;
            pro.Start();
            pro.StandardInput.WriteLine("netstat -ano");
            pro.StandardInput.WriteLine("exit");
            Regex reg = new Regex("\\s+", RegexOptions.Compiled);
            List<int> ports = new List<int>();
            string line;
            while ((line = pro.StandardOutput.ReadLine()) != null)
            {
                line = line.Trim();
                if (line.StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
                {
                    line = reg.Replace(line, ",");
                    string[] arr = line.Split(',');
                    int temp = 0;
                    if (int.TryParse(arr[4], out temp))
                    {
                        if (pids.Contains(temp))
                        {
                            string soc = arr[1];
                            int pos = soc.LastIndexOf(':');
                            int pot = int.Parse(soc.Substring(pos + 1));
                            soc = soc.Remove(pos);
                            if (!IPAddress.Parse(soc).Equals(IPAddress.Any))
                            {
                                ports.Add(pot);
                                ProcInfo.ProcessID = temp;
                            }
                        }
                    }
                }
            }
            
            pro.Dispose();
            _log.DebugFormat("Process port List Count : {0}", ports.Count);
            return ports;
        }
        private List<int> GetProcessIDList(string processName)
        {
            List<int> proList = new List<int>();

            Process[] pArray = Process.GetProcesses().Where(a => a.ProcessName.Contains(processName)).ToArray();
            if (pArray != null)
            {
                foreach (Process pro in pArray)
                {
                    Console.WriteLine("GetProcessIDList --> Process Name is : {0}. Process ID is : {1}", processName, pro.Id);
                    proList.Add(pro.Id);
                }
            }
            return proList;
        }
    }
}
