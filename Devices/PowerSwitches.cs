using System;
using System.ComponentModel;

namespace CannockAutomation.Devices
{
    [Flags]
    public enum PowerSwitches
    {
        [Description("No Power Switches")]
        None = 0,
        [Description("Toaster")]
        Toaster = 1 << 0,
		[Description("Doorbell")]
		Doorbell = 1 << 1,
        [Description("Fan")]
        Fan = 1 << 2,
        [Description("Fourth Switch")]
        Fourth = 1 << 3,
        [Description("Fifth Switch")]
        Fifth = 1 << 4,
        [Description("Sixth Switch")]
        Sixth = 1 << 5,
        [Description("Seventh Switch")]
        Seventh = 1 << 5,
        [Description("All Power Switches")]
		All = Toaster | Doorbell | Fan | Fourth | Fifth | Sixth | Seventh,
		[Description("Active Power Switches")]
		Active = Toaster | Doorbell | Fan,
    }
}