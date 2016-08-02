using System;
using System.ComponentModel;

namespace CannockAutomation.Devices
{
    [Flags]
    public enum PushoverDevice
    {
        [Description("No Pushover Devices")]
        None = 0,
        [Description("GiP3GS")]
        AppleIPhone3Gs = 1 << 0,
        [Description("GiP4S")]
        AppleIPhone4S = 1 << 1,
        [Description("GiP5S")]
        AppleIPhone5S = 1 << 2,
        [Description("HiP6S")]
        AppleIPhone6Plus = 1 << 3,
        [Description("GiPrMini")]
        AppleIPadRetinaMini = 1 << 4,
        [Description("GPC")]
        Desktop = 1 << 5,
        [Description("GWork")]
        DellPrecision = 1 << 6,
        [Description("GZen")]
        Zen = 1 << 7,
        [Description("All Pushover Devices")]
        All = AppleIPhone3Gs | AppleIPhone4S | AppleIPhone5S | AppleIPhone6Plus | AppleIPadRetinaMini | Desktop | DellPrecision | Zen,
    }
}