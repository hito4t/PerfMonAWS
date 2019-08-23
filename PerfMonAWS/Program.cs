using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;


namespace PerfMonAWS
{
    class Program
    {
        static void Main(string[] args)
        {
            using (PerfMonLib perfMon = new PerfMonLib())
            {
                using (Publisher publisher = new Publisher())
                {
                    int interval = 5;
                    while (true)
                    {
                        PerfData data = perfMon.GetValues();

                        DateTime now = DateTime.Now;
                        string timestamp = now.ToString("yyyy-MM-ddTHH:mm:ss") + now.ToString("zzz").Replace(":", "");
                        string message = "{"
                            + CreateJsonItem("timestamp", timestamp)
                            + ","
                            + CreateJsonItem("cpu", data.ProcessorUtilization)
                            + ","
                            + CreateJsonItem("memory", data.AvailableMemoryMB)
                            + ","
                            + CreateJsonItem("process", data.ActiveProcess)
                            + ","
                            + CreateJsonItem("interval", interval)
                            + "}";

                        Console.WriteLine(message);

                        publisher.Publish(message);

                        Thread.Sleep(interval * 1000);
                    }
                }
            }

        }

        private static string CreateJsonItem(string key, object value)
        {
            string item = "\"" + key + "\":";
            if (value == null)
            {
                item += "null";
            }
            else if (value is string)
            {
                item += "\"" + value + "\"";
            }
            else if (value is double)
            {
                item += string.Format("{0:0.##}", value);
            }
            else
            {
                item += value;
            }
            return item;
        }

    }
}
