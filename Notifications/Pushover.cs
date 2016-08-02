using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading;
using CannockAutomation.Devices;
using CannockAutomation.Extensions;
using CannockAutomation.Logging;
using CannockAutomation.Net;
using CannockAutomation.Properties;

namespace CannockAutomation.Notifications
{

    public static class Pushover
    {

        private static readonly Object PushLocker = new Object();

        public static void Debug(String message)
        {
            Push(message, priority: Priority.Debug);
        }

        public static void Write(String message)
        {
            Push(message, priority: Priority.Local);
        }

        public static void Log(String message)
        {
            Push(message, priority: Priority.Log);
        }

        public static void Send(String message, String title = null)
        {
            Push(message, title, Priority.Silent);
        }

        public static void Show(String message, String title = null)
        {
            Push(message, title, Priority.Quiet);
        }

        public static void Notify(String message, String title = null)
        {
            Push(message, title);
        }

        public static void Alert(String message, String title = null)
        {
            Push(message, title, Priority.Alert);
        }

        public static void Emergency(String message, String title = null, int expire = 86400)
        {
            Push(message, title, Priority.Emergency, expire);
        }

        public static void Push(String message, String title = null, Priority priority = Priority.Default, int expire = 86400, String sound = null, String user = null, String url = null, String urlTitle = null, PushoverDevice device = PushoverDevice.All)
        {
            if (priority < Priority.Debug) return;

            // Debug
            Console.WriteLine(message);

            if (priority < Priority.Local) return;

            // Log
            ThreadPool.QueueUserWorkItem(o => Logger.Log(message));

            if (priority == Priority.Log)
            {
                device = PushoverDevice.Desktop;
                priority = Priority.Silent;
            }
            else if (priority < Priority.Silent)
            {
                return;
            }

            // Silent - Emergency
            var priorityInt = (int)priority;

            var parameters = new NameValueCollection {
                { "token", Configuration.PushoverApiToken },
                { "user", user ?? Configuration.PushoverApiUser },
                { "message", message },
                { "priority", priorityInt.ToString(CultureInfo.InvariantCulture) },
            };

            if (!String.IsNullOrWhiteSpace(title))
            {
                parameters.Add("title", title);
            }

            AddOptionalParameters(ref parameters, priority, expire, sound, user, url, urlTitle, device);
            
            ThreadPool.QueueUserWorkItem(o => Push(parameters));
        }

        private static void AddOptionalParameters(ref NameValueCollection parameters, Priority priority, int expire, String sound, String user, String url, String urlTitle, PushoverDevice device)
        {
            if (!String.IsNullOrWhiteSpace(url))
            {
                parameters.Add("url", url);
            }
            if (!String.IsNullOrWhiteSpace(urlTitle))
            {
                parameters.Add("url_title", urlTitle);
            }
            if (device != PushoverDevice.All && device != PushoverDevice.None)
            {
                parameters.Add("device", device.GetName());
            }
            if (!String.IsNullOrWhiteSpace(sound))
            {
                parameters.Add("sound", sound);
            }
            else if (priority == Priority.Default)
            {
                parameters.Add("sound", "pianobar");
            }

            if (priority > Priority.High)
            {
                parameters["priority"] = "2";
                parameters.Add("retry", Math.Max((int)priority, 30).ToString(CultureInfo.InvariantCulture));
                parameters.Add("expire", Math.Min(expire, 86400).ToString(CultureInfo.InvariantCulture));
                parameters.Add("callback", Configuration.PushoverCallbackUri);
            }
        }

        private static void Push(NameValueCollection parameters)
        {
            lock (PushLocker)
            {
                try
                {
                    using (var client = new TimedWebClient(5000, method: "POST"))
                    {
                        client.UploadValues(Configuration.PushoverApiUri, parameters);
                    }
                }
                catch (Exception e)
                {
                    Log($"{e.GetType()}: {e.Message}");
                }
            }
        }
    }
}
