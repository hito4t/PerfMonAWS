using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;

using Amazon.Runtime;
using Amazon.IotData;
using Amazon.IotData.Model;


namespace PerfMonAWS
{
    class Publisher : IDisposable
    {
        private AmazonIotDataClient client;
        private string topic;
        private bool _usingProxy;

        public Publisher(bool ignoreProxy)
        {
            string awsAccessKey = ConfigurationManager.AppSettings["AWSAccessKey"];
            string awsSecretKey = ConfigurationManager.AppSettings["AWSSecretKey"];

            AWSCredentials credentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
            AmazonIotDataConfig config = new AmazonIotDataConfig();
            string endPoint = ConfigurationManager.AppSettings["AWSIoTEndPoint"];
            config.ServiceURL = "https://" + endPoint;

            string proxyHost = ConfigurationManager.AppSettings["ProxyHost"];
            string proxyPort = ConfigurationManager.AppSettings["ProxyPort"];
            string proxyUser = ConfigurationManager.AppSettings["ProxyUser"];
            string proxyPassword = ConfigurationManager.AppSettings["ProxyPassword"];

            if (!ignoreProxy && !string.IsNullOrEmpty(proxyHost))
            {
                config.ProxyHost = proxyHost;
                config.ProxyPort = int.Parse(proxyPort);
                if (!string.IsNullOrEmpty(proxyUser))
                {
                    config.ProxyCredentials = new NetworkCredential(proxyUser, proxyPassword);
                }
                _usingProxy = true;
            }
            client = new AmazonIotDataClient(credentials, config);

            topic = ConfigurationManager.AppSettings["AWSIoTTopic"];
        }

        public bool UsingProxy
        {
            get
            {
                return _usingProxy;
            }
        }

        public void Publish(string message)
        {
            PublishRequest request = new PublishRequest();
            request.Topic = topic;
            request.Qos = 1;

            request.Payload = new MemoryStream(Encoding.UTF8.GetBytes(message));
            PublishResponse response = client.Publish(request);
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
