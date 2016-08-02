using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using CannockAutomation.Properties;
using Newtonsoft.Json;

namespace CannockAutomation.Net
{
    public class QueuedWebException : WebException
    {
        public QueuedWebException(String address, WebException exception): base($"{exception.Message} ({address})", exception, exception.Status, exception.Response)
        {
            
        }
    }
    
    public class TimedWebClient : WebClient
    {
        private const int PingTimeout = 1234;
        private static readonly Object TimedWebClientLocker = new Object();
        
        public static event EventHandler<UnhandledExceptionEventArgs> Exception;

        public static void QueueRequest(String address, int timeout = 100000, Boolean keepAlive = false, String method = null, Boolean invokeException = true, Object data = null)
        {
            try
            {
                var client = new TimedWebClient(timeout, keepAlive, method, data);
                client.QueuedRequestCompleted += (sender, args) => client.Dispose();
                client.QueuedRequest(address, invokeException);
            }
            catch (WebException e)
            {
                if (invokeException)
                {
                    var qe = new QueuedWebException(address, e);
                    Exception?.Invoke(null, new UnhandledExceptionEventArgs(qe, false));
                }
            }
            catch (Exception e)
            {
                if (invokeException)
                {
                    Exception?.Invoke(null, new UnhandledExceptionEventArgs(e, false));
                } 
            }
        }

        public static void QueueRequest(Uri address, int timeout = 100000, Boolean keepAlive = false, String method = null, Boolean invokeException = true, Object data = null)
        {
            QueueRequest(address.ToString(), timeout, keepAlive, method, invokeException);
        }

        public static void Ping(String address, Boolean invokeException = true)
        {
            QueueRequest(address, PingTimeout, method: "HEAD", invokeException: invokeException);
        }

        public static void Put(String address, Boolean invokeException = true, Object data = null)
        {
            QueueRequest(address, PingTimeout, method: "PUT", invokeException: invokeException, data: data);
        }

        private readonly int _timeout;
        private readonly Boolean _keepAlive;
        private readonly String _method;
        private readonly String _data;

        public String QueResult { get; private set; }
        public event EventHandler<EventArgs> QueuedRequestCompleted;

        public TimedWebClient(int timeout = 100000, Boolean keepAlive = false, String method = null, Object data = null)
        {
            _timeout = timeout;
            _keepAlive = keepAlive;
            if (data != null)
            {
                _method = method ?? "PUT";
                _data = JsonConvert.SerializeObject(data);
            }
            _method = method ?? "GET";
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var webRequest = base.GetWebRequest(address);

            if (webRequest == null) return null;

            webRequest.Timeout = _timeout;
            webRequest.Proxy = null;
            webRequest.Method = _method;
            webRequest.Credentials = new NetworkCredential(Configuration.NetworkCredentialUserName, Configuration.NetworkCredentialPassword);
            
            if (!(webRequest is HttpWebRequest)) return webRequest;

            var httpRequest = webRequest as HttpWebRequest;
            httpRequest.KeepAlive = _keepAlive;

            if (String.IsNullOrWhiteSpace(_data)) { return httpRequest; }

            httpRequest.Accept = "application/json";
            httpRequest.ContentType = "application/json";

            var encoding = new ASCIIEncoding();
            var bytes = encoding.GetBytes(_data);

            var newStream = httpRequest.GetRequestStream();
            newStream.Write(bytes, 0, bytes.Length);
            newStream.Close();

            return httpRequest;
        }

        public void QueuedRequest(String address, Boolean invokeException=true)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                lock (TimedWebClientLocker)
                {
                    try
                    {
                        var data = DownloadData(address);
                        QueResult = System.Text.Encoding.UTF8.GetString(data);
                        var x = 1;
                    }
                    catch (WebException e)
                    {
                        if (invokeException)
                        {
                            var qe = new QueuedWebException(address, e);
                            Exception?.Invoke(null, new UnhandledExceptionEventArgs(qe, false));
                        }
                    }
                    catch (Exception e)
                    {
                        if (invokeException)
                        {
                            Exception?.Invoke(this, new UnhandledExceptionEventArgs(e, false));
                        }
                    }
                    finally
                    {
                        QueuedRequestCompleted?.Invoke(this, EventArgs.Empty);
                    }
                }
            });
        }

        public void QueuedRequest(Uri address, Boolean invokeException=true)
        {
            QueuedRequest(address.ToString(), invokeException);
        }
    }
}
