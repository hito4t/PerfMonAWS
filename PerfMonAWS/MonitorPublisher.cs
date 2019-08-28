using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace PerfMonAWS
{
    public class MonitorPublisher
    {
        private Encoding _encoding = Encoding.UTF8;
        private string _dataDir;
        private string _logPath;
        private bool _stopping = false;
        private bool _monitorStopped = false;
        private bool _publisherStopped = false;


        public void Start()
        {
            string exePath = Assembly.GetEntryAssembly().Location;
            string baseDir = Path.GetDirectoryName(exePath);
            _logPath = Path.Combine(baseDir, Path.GetFileNameWithoutExtension(exePath) + ".log");
            _dataDir = Path.Combine(baseDir, "data");
            Directory.CreateDirectory(_dataDir);

            log("Started.");

            Thread monitorThread = new Thread(new ThreadStart(monitor));
            monitorThread.Start();

            Thread publisherThread = new Thread(new ThreadStart(publish));
            publisherThread.Start();
        }

        public bool IsAlive
        {
            get
            {
                return !_monitorStopped && !_publisherStopped;
            }
        }


        public void Stop()
        {
            _stopping = true;

            // wait 10 seconds
            for (int i = 0; i < 20; i++)
            {
                if (_monitorStopped && _publisherStopped)
                {
                    break;
                }

                Thread.Sleep(500);
            }

            log("Stopped.");
        }

        private void monitor()
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
            finally
            {
                _monitorStopped = true;
            }
        }

        private void monitor(PerfMonLib perfMon)
        {
            int interval = 5; // seconds
            while (!_stopping)
            {
                PerfData data = perfMon.GetValues();

                DateTime now = DateTime.Now;
                string timestamp = now.ToString("yyyy-MM-ddTHH:mm:ss") + now.ToString("zzz").Replace(":", "");
                string message = "{"
                    + CreateJsonItem("device", Environment.MachineName)
                    + ","
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

                string path = Path.Combine(_dataDir, timestamp.Replace("-", "").Replace(":", "") + ".json");
                File.WriteAllText(path, message, _encoding);

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

        private void publish()
        {
            try
            {

                using (Publishers publishers = new Publishers())
                {
                    publish(publishers);
                }
            }
            catch (Exception e)
            {
                log(e);
            }
            finally
            {
                _publisherStopped = true;
            }
        }

        private void publish(Publishers publishers)
        {
            int initialInterval = 1000; // milliseconds
            int maxInterval = 1000 * 60; // milliseconds
            int interval = initialInterval;

            while (!_stopping)
            {
                try
                {
                    publishFiles(publishers);

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

        private void publishFiles(Publishers publishers)
        {
            List<string> paths = new List<string>(Directory.GetFiles(_dataDir));
            paths.Sort();

            // the last file may be being written.
            for (int i = 0; i < paths.Count - 1; i++)
            {
                string path = paths[i];
                string message = File.ReadAllText(path, _encoding);
                publishers.Publish(message);
                File.Delete(path);

                Console.WriteLine(Path.GetFileName(path) + " published.");
            }
        }

        private void log(Exception e)
        {
            log(e.Message + "\r\n" + e.StackTrace);
        }

        private void log(string message)
        {
            using (StreamWriter writer = new StreamWriter(_logPath, true, _encoding))
            {
                writer.Write(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                writer.Write(" ");
                writer.WriteLine(message);
            }

            Console.WriteLine(message);
        }
    }
}
