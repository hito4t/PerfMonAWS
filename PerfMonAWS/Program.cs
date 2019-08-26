using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;


namespace PerfMonAWS
{
    class Program
    {
        static void Main(string[] args)
        {
            MonitorPublisher monitorPublisher = new MonitorPublisher();
            monitorPublisher.Start();

            while (monitorPublisher.IsAlive)
            {
                Thread.Sleep(500);
            }

            monitorPublisher.Stop();
        }
    }
}
