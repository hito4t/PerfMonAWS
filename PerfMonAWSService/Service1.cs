using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

using PerfMonAWS;

namespace PerfMonAWSService
{
    public partial class Service1 : ServiceBase
    {
        private MonitorPublisher _monitorPublisher;

        public Service1()
        {
            InitializeComponent();

            _monitorPublisher = new MonitorPublisher();
        }

        protected override void OnStart(string[] args)
        {
            _monitorPublisher.Start();
        }

        protected override void OnStop()
        {
            _monitorPublisher.Stop();
        }
    }
}
