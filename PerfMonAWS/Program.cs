using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;


namespace PerfMonAWS
{
    class Program
    {
        private static Encoding encoding = Encoding.UTF8;
        private static string dataDir;

        static void Main(string[] args)
        {
            dataDir = Path.Combine(Environment.CurrentDirectory, "data");
            Directory.CreateDirectory(dataDir);

            Thread monitorThread = new Thread(new ThreadStart(monitor));
            monitorThread.Start();

            Thread publisherThread = new Thread(new ThreadStart(publish));
            publisherThread.Start();

            while (true)
            {
                if (!monitorThread.IsAlive || !publisherThread.IsAlive)
                {
                    break;
                }

                Thread.Sleep(500);
            }
        }


        private static void monitor()
        {
            try
            {
                using (PerfMonLib perfMon = new PerfMonLib())
                {
                    monitor(perfMon);
                }
            } 
            catch (Exception e)
            {
                log(e);
            }
        }

        private static void monitor(PerfMonLib perfMon)
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

                string path = Path.Combine(dataDir, timestamp.Replace("-", "").Replace(":", "") + ".json");
                File.WriteAllText(path, message, encoding);

                //publisher.Publish(message);

                Thread.Sleep(interval * 1000);
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

        private static void publish()
        {
            try
            {
                using (Publisher publisher = new Publisher())
                {
                    publish(publisher);
                }
            }
            catch (Exception e)
            {
                log(e);
            }
        }

        private static void publish(Publisher publisher)
        {
            int initialInterval = 1000; // milliseconds
            int maxInterval = 1000 * 60; // milliseconds
            int interval = initialInterval;

            while (true)
            {
                try
                {
                    publishFiles(publisher);

                    interval = initialInterval;
                }
                catch (Amazon.Runtime.AmazonServiceException e)
                {
                    log(e);
                    if (interval < maxInterval)
                    {
                        interval *= 2;
                    }
                }

                Thread.Sleep(interval);
            }
        }

        private static void publishFiles(Publisher publisher)
        {
            List<string> paths = new List<string>(Directory.GetFiles(dataDir));
            paths.Sort();

            // the last file may be being written.
            for (int i = 0; i < paths.Count - 1; i++)
            {
                string path = paths[i];
                string message = File.ReadAllText(path, encoding);
                publisher.Publish(message);
                File.Delete(path);

                Console.WriteLine(Path.GetFileName(path) + " published.");
            }
        }

        private static void log(Exception e)
        {
            using (StreamWriter writer = new StreamWriter("PerfMonAWS.log", true, encoding))
            {
                writer.Write(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                writer.Write(" ");
                writer.WriteLine(e.Message);
                writer.WriteLine(e.StackTrace);
            }

            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }

    }
}
