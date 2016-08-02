using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CannockAutomation.Net;
using CannockAutomation.Properties;

namespace CannockAutomation.Notifications
{
    public static class Ifttt
    {
        private static readonly Object TriggerLocker = new Object();

        public static void Text(String message)
        {
            Trigger("sms", message);
        }

        public static void Call(String message)
        {
            Trigger("phone", message);
        }

        public static void Trigger(String name, String value1 = null, String value2 = null, String value3 = null)
        {
            Trigger(name, new NameValueCollection { { "value1", value1 }, { "value2", value2 }, { "value3", value3 } });
        }

        private static void Trigger(String name, NameValueCollection parameters)
        {
            lock (TriggerLocker)
            {
                try
                {
                    using (var client = new TimedWebClient(5000, method: "POST"))
                    {
                        client.UploadValues(String.Format(Configuration.IftttTriggerUri, name), parameters);
                    }
                }
                catch (Exception e)
                {
                    Pushover.Log($"{e.GetType()}: {e.Message}");
                }
            }
        }
    }
        
}
