using System;
using System.ComponentModel;

namespace CannockAutomation.Devices
{
    [Flags]
    public enum Lights
    {
        [Description("No Lights")]
        None = 0,
        [Description("Entryway")]
        Entryway = 1 << 0,
        [Description("Second Floor Landing")]
        SecondFloorLanding = 1 << 1,
        [Description("Living Room")]
        LivingRoom = 1 << 2,
        [Description("Dining Room")]
        DiningRoom = 1 << 3,
        [Description("Kitchen")]
        Kitchen = 1 << 4,
        [Description("Quarter Landing")]
        QuarterLanding = 1 << 5,
        [Description("Third Floor Landing")]
        ThirdFloorLanding = 1 << 6,
        [Description("Bedroom")]
        Bedroom = 1 << 7,
        [Description("Hallway")]
        Hallway = 1 << 8,
        [Description("All Lights")]
        All = Entryway | SecondFloorLanding | LivingRoom | DiningRoom | Kitchen | QuarterLanding | ThirdFloorLanding | Bedroom | Hallway,
        [Description("Basement")]
        Basement = Entryway | SecondFloorLanding,
        [Description("Living Area")]
        LivingArea = SecondFloorLanding | LivingRoom | DiningRoom,
        [Description("Second Floor")]
        SecondFloor = SecondFloorLanding | LivingRoom | DiningRoom | Kitchen | QuarterLanding,
        [Description("Third Floor")]
        ThirdFloor = ThirdFloorLanding | Bedroom | Hallway,
    }
}
