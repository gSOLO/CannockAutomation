using System;
using System.ComponentModel;

namespace CannockAutomation.Actions
{
    [Flags]
    public enum DeviceActions
    {
        [Description("None")]
        None = 0,
        [Description("Get ID")]
        Id = 1 << 0,
        [Description("Get UDN")]
        Udn = 1 << 1,
        [Description("Get Name")]
        Name = 1 << 2,
        [Description("Get Is On")]
		IsOn = 1 << 3,
		[Description("Get Is Reachable")]
		IsReachable = 1 << 4,
		[Description("Get Brightness")]
		Brightness = 1 << 5,
        [Description("Get Color")]
		Color = 1 << 6,
        [Description("Get Volume")]
		Volume = 1 << 7,
		[Description("Get Channel")]
		Channel = 1 << 8,
        [Description("Get Last Update")]
		LastUpdate = 1 << 9,
        [Description("Get All Info")]
        All = Id | Udn | Name | IsOn | IsReachable | Brightness | Color | Volume | Channel | LastUpdate,
    }
}
