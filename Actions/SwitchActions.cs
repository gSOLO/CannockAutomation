using System;
using System.ComponentModel;

namespace CannockAutomation.Actions
{
    [Flags]
    public enum SwitchActions
    {
        [Description("None")]
        None = 0,
        [Description("On")]
        On = 1 << 0,
        [Description("Off")]
        Off = 1 << 1,
        [Description("Switch On")]
        TurnOn = 1 << 2,
        [Description("Switch Off")]
        TurnOff = 1 << 3,
        [Description("Toggle")]
        Toggle = 1 << 4,
        [Description("Blink")]
		Blink = 1 << 5,
        [Description("Long Press")]
        LongPress = 1 << 6,
        [Description("Error")]
        Error = 1 << 7,
        [Description("Pending")]
        Pending = 1 << 8,
        [Description("Standby")]
        Standby = 1 << 9,
    }
}