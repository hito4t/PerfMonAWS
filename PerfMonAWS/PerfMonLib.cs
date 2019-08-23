using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace PerfMonAWS
{
    class PerfData
    {
        public double ProcessorUtilization
        {
            get;
            set;
        }

        public int AvailableMemoryMB
        {
            get;
            set;
        }

        public string ActiveProcess
        {
            get;
            set;
        }

        public override string ToString()
        {
            return string.Format("Processor Utilization = {0:0.##} (%), Available Memory = {1} (MB), Active Process = {2}", 
                ProcessorUtilization, AvailableMemoryMB, ActiveProcess);
        }
    }

    class PerfMonLib : IDisposable
    {
        public PerfMonLib()
        {
            if (!PdhOpen())
            {
                string error = PdhGetLastError();
                throw new Exception(error);
            }
        }

        public PerfData GetValues()
        {
            double processorUtilization;
            double availableMemoryMB;
            StringBuilder activeProcess = new StringBuilder(512);
            if (!PdhGetValues(out processorUtilization, out availableMemoryMB, activeProcess, activeProcess.Capacity))
            {
                string error = PdhGetLastError();
                throw new Exception(error);
            }

            PerfData data = new PerfData();
            data.ProcessorUtilization = processorUtilization;
            data.AvailableMemoryMB = (int)availableMemoryMB;
            if (activeProcess.ToString().Length > 0)
            {
                data.ActiveProcess = Path.GetFileName(activeProcess.ToString());
            }
            return data;
        }

        public void Dispose()
        {
            PdhClose();
        }

        private string PdhGetLastError()
        {
            StringBuilder builder = new StringBuilder(512);
            PdhGetLastError(builder, builder.Capacity);
            return builder.ToString();
        }

        [DllImport("PerfMonLib.Dll", CharSet = CharSet.Unicode)]
        public static extern void PdhGetLastError(StringBuilder s, int maxLength);

        [DllImport("PerfMonLib.Dll")]
        public static extern bool PdhOpen();

        [DllImport("PerfMonLib.Dll", CharSet = CharSet.Unicode)]
        public static extern bool PdhGetValues(out double processorUtilization, out double availableMemoryMB,
            StringBuilder activeProcess, int activeProcessSize);

        [DllImport("PerfMonLib.Dll")]
        public static extern void PdhClose();
    }
}
