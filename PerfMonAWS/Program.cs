using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace PerfMonAWS
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1 && args[0] == "-background")
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = Assembly.GetEntryAssembly().Location;
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                Process p = Process.Start(psi);
                Console.WriteLine(Path.GetFileName(psi.FileName) + " is started in the background.");
            }
            else
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
}
