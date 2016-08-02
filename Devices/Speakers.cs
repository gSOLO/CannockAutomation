using System;
using System.ComponentModel;

namespace CannockAutomation.Devices
{
    [Flags]
    public enum Speakers
    {
        [Description("No Speakers")]
        None = 0,
        [Description("G-PC")]
        PC = 1 << 0,
        [Description("G-ATV")]
        AppleTV  = 1 << 1,
        [Description("GiP3GS")]
        AirPlay3GS = 1 << 2,
        [Description("GiP4S")]
        AirPlay4S = 1 << 3,
        [Description("Television")]
        TV = 1 << 3,
        [Description("All Speakers")]
        All = PC | AppleTV | AirPlay3GS | AirPlay4S | TV,
        [Description("Second Floor")]
        LivingRoom = PC | AppleTV,
        [Description("Second Floor")]
        SecondFloor = PC | AppleTV | AirPlay4S | TV,
        [Description("Third Floor")]
        ThirdFloor = AirPlay3GS,
    }
}