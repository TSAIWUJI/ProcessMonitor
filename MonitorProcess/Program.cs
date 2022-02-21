using System;
using System.Collections.Generic;
using System.Linq;
using SharpPcap;
using System.Diagnostics;
using System.Threading;
using System.Net;
using static MonitorProcess.EnmuTest;
using HttpClientTest;

namespace MonitorProcess
{
    public class Program
    {
        public static List<ICaptureDevice> ICAPTURELIST { get; set; }
        public static MonitorTimes FLAG { get; set; }
        private static List<long> TENTIMESNETRECVDATA = new List<long>()
        {
            0,0,0,0,0,0,0,0,0,0
        };
        public static ProcessPerformanceInfo ProcInfo;
        private static int MONITORPROCESSTIMES { get; set; }
        private static string MONITORPROCESSNAME = "IVAD";
        private static log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void Main(string[] args)
        {
            // 新增 NTP Clock 檢查 & 自動啟用
            MonitorNTP.Start.StartMonitorNTP2();

            _log.InfoFormat("IVAD Monitor Version : {0}", "1.1.0");
            MONITORPROCESSTIMES = 0;
            FLAG = EnmuTest.MonitorTimes.One;
            try
            {
                string ip = Dns.GetHostByName(Dns.GetHostName()).AddressList.First().ToString();
                while (MONITORPROCESSTIMES < 3)
                {
                    //SendHttpRequest send = new SendHttpRequest();
                    //send.SendInfoEvent(ip, "開始監控 IVAD 程式");
                    MonitorProcessFunction();
                    MONITORPROCESSTIMES++;
                }
                _log.WarnFormat("=================== MonitorProcess Fail ===================");
                //SendHttpRequest sendWarnEvent = new SendHttpRequest();
                //sendWarnEvent.SendInfoEvent(ip, "監測 IVAD Process Fail 請重開 IVAD Monitor Process", true, "高");
            }
            catch (Exception ex)
            {
                foreach (ICaptureDevice device in ICAPTURELIST)
                {
                    device.StopCapture();
                    device.Close();
                }
                _log.WarnFormat("Exception ex.Message : {0} \r\n ex.StackTrace : {1} \r\n ex.InnerEx : {2}", ex.Message, ex.StackTrace, ex.InnerException);
            }
        }
        private static void MonitorProcessFunction()
        {
            GetICAPTURELIST();
            if (ICAPTURELIST.Count == 0)
            {
                return;
            }
            int checkTimes = 0;
            bool isProcessFail = false;

            while (!isProcessFail)
            {
                RefershInfo();
                _log.DebugFormat("proc NetTotal usage : {0} MB", ProcInfo.NetRecvBytes / 1024 / 1024);
                UpdateNetFlow(ProcInfo.NetRecvBytes);
                if (ProcInfo.NetRecvBytes != 0 && MONITORPROCESSTIMES != 0)
                {
                    MONITORPROCESSTIMES = 0;
                }
                long tenTimesDataAverage = TenTimesNetRecvAverage();
                if (tenTimesDataAverage == 0 || (double)ProcInfo.NetRecvBytes < (double)tenTimesDataAverage * 0.5)
                {
                    if (checkTimes < 3)
                    {
                        _log.DebugFormat("Check ProcessID : {0} Fail,\r\n CheckTimes {1} , \r\n Process Net Recv : {2} \r\n", ProcInfo.ProcessID, checkTimes, ProcInfo.NetRecvBytes);
                        _log.DebugFormat("Is Ten Times Data Average = {0} & Is Recv Data < Data Average * 0.5 = {1}", tenTimesDataAverage == 0, (double)ProcInfo.NetRecvBytes < (double)tenTimesDataAverage * 0.8);
                        checkTimes++;
                        continue;
                    }
                    KillProcess(ProcInfo.ProcessID);
                    KillSmartProcess();
                    foreach (ICaptureDevice device in ICAPTURELIST)
                    {
                        int i = 1;
                        device.StopCapture();
                        device.Close();
                        _log.DebugFormat("StopCapture Time : {0}", i++);
                    }
                    _log.DebugFormat("=============== ICAPTURELIST Stop , wait 10 Sec to Start Process ===============");
                    if (GetProcessID(MONITORPROCESSNAME) == 0)
                    {
                        Thread.Sleep(3 * 60 * 1000); // 程式 sleep 3 分鐘，透過原本的機制啟用 SmartGate & IVAD
                    }
                    _log.DebugFormat("=============== ReStart StartMonitor ===============");
                    isProcessFail = true;
                    checkTimes = 0;
                }
                else
                {
                    checkTimes = 0;
                }
            }
            ICAPTURELIST.Clear();
            Console.WriteLine("ICAPTURELIST.Clear() 數量 : {0}", ICAPTURELIST.Count());
        }
        private static void GetICAPTURELIST()
        {
            _log.DebugFormat("============### Start StartMonitor ###=============");
            int pid = GetProcessID(MONITORPROCESSNAME);
            
            ProcInfo = new ProcessPerformanceInfo
            {
                ProcessID = pid
            };
            
            ICAPTURELIST = new List<ICaptureDevice>();
            
            CaptureList p = new CaptureList(ProcInfo, MONITORPROCESSNAME);
            
            foreach (var item in p.DeviceList())
            {
                ICAPTURELIST.Add(CaptureFlowRecv(item.IP, item.PortID, item.DeviceID));
            }
        }
        private static int GetProcessID(string processName)
        {
            int result = 0;
            Process monitorProcess = Process.GetProcesses().Where(a => a.ProcessName.Contains(processName)).FirstOrDefault();
            if (monitorProcess != null)
            {
                _log.DebugFormat("GetProcessID --> Process Name is : {0}. Process ID is : {1}", processName, monitorProcess.Id);
                result = monitorProcess.Id;
            }
            return result;
        }
        public static void KillProcess(int processID)
        {
            Process process = null;
            try
            {
                process = Process.GetProcessById(processID);
                string processPath = "";
                if (process != null)
                {
                    processPath = process.MainModule.FileName;
                    _log.DebugFormat("KillProcess --> Process Name is : {0}. Process ID is : {1} \r\n Process Path : {2}", process.ProcessName, process.Id, processPath);
                    while (!IsProcessKillSusses(process.Id))
                    {
                        process.Kill();
                        Thread.Sleep(5000);
                    }
                    SendHttpRequest send = new SendHttpRequest();
                    string ip = Dns.GetHostByName(Dns.GetHostName()).AddressList.First().ToString();
                    send.SendInfoEvent(ip, "關閉 IVAD 程式");
                }
            }
            catch (Exception)
            {
                _log.WarnFormat("Process 不存在");
            }
        }
        private static void KillSmartProcess()
        {
            IEnumerable<Process> newProcess = null;
            try
            {
                newProcess = Process.GetProcesses().Where(a => a.ProcessName.Contains("Smart"));
                foreach (var item in newProcess)
                {
                    item.Kill();
                }
                //SendHttpRequest send = new SendHttpRequest();
                //string ip = Dns.GetHostByName(Dns.GetHostName()).AddressList.First().ToString();
                //send.SendInfoEvent(ip, "關閉 SmartGate 程式");
            }
            catch (Exception ex)
            {
                _log.WarnFormat("Kill Smart Process fail ex.message : {0}", ex.Message);
            }
        }
        private static bool IsProcessKillSusses(int processID)
        {
            try
            {
                Process pro = Process.GetProcessById(processID);
                return false;
            }
            catch
            {
                return true;
            }
        }
        private static ICaptureDevice CaptureFlowRecv(string IP, int portID, int deviceID)
        {
            ICaptureDevice device = CaptureDeviceList.New()[deviceID];
            device.OnPacketArrival += new PacketArrivalEventHandler(device_OnPacketArrivalRecv);
            int readTimeoutMilliseconds = 1000;
            device.Open(DeviceMode.Promiscuous, readTimeoutMilliseconds);

            //string filter = "dst host " + IP + " and dst port " + portID;
            string filter = string.Format("(src host {0} and src port {1}) or (dst host {0} and dst port {1})", IP, portID);
            device.Filter = filter;
            device.StartCapture();
            _log.DebugFormat("StartCapture deviceID : {0} \r\n IP : {1} \r\n Port ID {2}", deviceID, IP, portID);
            return device;
        }
        private static void device_OnPacketArrivalRecv(object sender, CaptureEventArgs e)
        {
            var len = e.Packet.Data.Length;
            ProcInfo.NetRecvBytes += len;
        }
        private static void RefershInfo()
        {
            ProcInfo.NetRecvBytes = 0;
            ProcInfo.NetTotalBytes = 0;
            // 收集每 5 秒的 RecvBytes
            Thread.Sleep(5000);
            ProcInfo.NetTotalBytes = ProcInfo.NetRecvBytes;
            ProcInfo.NetRecvBytes = ProcInfo.NetRecvBytes;
        }
        private static void UpdateNetFlow(long netRecvBytes)
        {
            TENTIMESNETRECVDATA[FLAG.ToInt32()] = netRecvBytes;
            FLAG = FLAG.Next();
        }
        private static long TenTimesNetRecvAverage()
        {
            int i = 0;
            long allRecvData = 0;
            foreach (var recvData in TENTIMESNETRECVDATA)
            {
                if (recvData != 0)
                {
                    i++;
                    allRecvData += recvData;
                }
            }
            if (allRecvData == 0)
            {
                return 0;
            }
            return allRecvData / i;
        }
    }
}
