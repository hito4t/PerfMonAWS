using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfMonAWS
{
    class Publishers : IDisposable
    {
        private Publisher _firstPublisher;
        private Publisher _secondPublisher;

        public Publishers()
        {
            _firstPublisher = new Publisher(false);
            if (_firstPublisher.UsingProxy)
            {
                _secondPublisher = new Publisher(true);
            }
        }

        public void Publish(string message)
        {
            try
            {
                _firstPublisher.Publish(message);
            }
            catch (Amazon.Runtime.AmazonServiceException e1)
            {
                if (_secondPublisher != null)
                {
                    try
                    {
                        // when failed with proxy, will retry without proxy,
                        // or when faield without proxy, will retry with proxy,
                        // supposing network settings were changed.

                        _secondPublisher.Publish(message);
                        // swap publishers
                        Publisher tempPublisher = _firstPublisher;
                        _firstPublisher = _secondPublisher;
                        _secondPublisher = tempPublisher;
                    } 
                    catch (Amazon.Runtime.AmazonServiceException)
                    {
                    }
                }
                throw e1;
            }
        }


        public void Dispose()
        {
            _firstPublisher.Dispose();
            if (_secondPublisher != null)
            {
                _secondPublisher.Dispose();
            }
        }
    }
}
