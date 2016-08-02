using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Configuration.ConfigurationManager;

namespace CannockAutomation
{
    public static class Configuration
    {
        public static readonly String AmazonUserName = AppSettings["AmazonUserName"];
        public static readonly String DataFilePath = AppSettings["DataFilePath"];
        public static readonly String HueUserName = AppSettings["HueUserName"];
        public static readonly String IftttTriggerUri = AppSettings["IftttTriggerUri"];
        public static readonly String IftttUserName = AppSettings["IftttUserName"];
        public static readonly String JsonFilePath = AppSettings["JsonFilePath"];
        public static readonly String ListenerPrefix = AppSettings["ListenerPrefix"];
        public static readonly String ListeningIconFilePath = AppSettings["ListeningIconFilePath"];
        public static readonly String LogFilePath = AppSettings["LogFilePath"];
        public static readonly String NetworkCredentialPassword = AppSettings["NetworkCredentialPassword"];
        public static readonly String NetworkCredentialUserName = AppSettings["NetworkCredentialUserName"];
        public static readonly String NotifyIconText = AppSettings["NotifyIconText"];
        public static readonly String PushoverApiToken = AppSettings["PushoverApiToken"];
        public static readonly String PushoverApiUri = AppSettings["PushoverApiUri"];
        public static readonly String PushoverApiUser = AppSettings["PushoverApiUser"];
        public static readonly String PushoverCallbackUri = AppSettings["PushoverCallbackUri"];
        public static readonly String PushoverUserName = AppSettings["PushoverUserName"];
        public static readonly String StoppedListeningIconFilePath = AppSettings["StoppedListeningIconFilePath"];
        public static readonly String WemoServiceBaseUri = AppSettings["WemoServiceBaseUri"];
    }
}
