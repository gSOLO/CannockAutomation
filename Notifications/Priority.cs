using System.ComponentModel;

namespace CannockAutomation.Notifications
{
    public enum Priority
    {
        [Description("Console Output")]
        Debug = -5,
        [Description("Local Log File Only")]
        Local = -4,
        [Description("Local Log File and Desktop Pushover")]
        Log = -3,

        Lowest = -2,
        Low = -1,
        Normal = 0,
        High = 1,
        Emergency = 2,

        Silent = Lowest,
        Quiet = Low,
        Default = Normal,
        Alert = High,
    }
}
