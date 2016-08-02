using System;
using System.ComponentModel;

namespace CannockAutomation.Devices
{
    [Flags]
    public enum Sensors
    {
        [Description("No Sensors")]
        None = 0,
        [Description("Front Door")]
        FrontDoor = 1 << 0,
        [Description("Garage")]
        Garage = 1 << 1,
        [Description("Basement")]
        Basement = 1 << 2,
        [Description("Entryway")]
        Entryway = 1 << 3,
        [Description("Second Floor Landing")]
        SecondFloorLanding = 1 << 4,
        [Description("Love Seat")]
        LoveSeat = 1 << 5,
        [Description("Sofa")]
        Sofa = 1 << 6,
        [Description("Kitchen")]
        Kitchen = 1 << 7,
        [Description("Kitchen Sink")]
        KitchenSink = 1 << 8,
        [Description("Quarter Landing")]
        QuarterLanding = 1 << 9,
        [Description("Third Floor Landing")]
        ThirdFloorLanding = 1 << 10,
        [Description("Bedroom")]
        Bedroom = 1 << 11,
        [Description("Bed")]
        Bed = 1 << 12,
        [Description("Hallway")]
        Hallway = 1 << 13,
        [Description("All Sensors")]
        All = FrontDoor | Garage | Basement | Entryway | SecondFloorLanding | LoveSeat | Sofa | Kitchen | KitchenSink | QuarterLanding | ThirdFloorLanding | Bedroom | Bed | Hallway,
        [Description("Outside")]
        Outside = FrontDoor | Garage,
        [Description("Inside")]
        Inside = Basement | Entryway | SecondFloorLanding | LoveSeat | Sofa | Kitchen | KitchenSink | QuarterLanding | ThirdFloorLanding | Bedroom | Bed | Hallway,
        [Description("Axius")]
        Axius = Garage | Basement | LoveSeat | Sofa | Kitchen | Bedroom | Bed,
		[Description("WeMo")]
		WeMo = FrontDoor | Entryway | SecondFloorLanding | KitchenSink | QuarterLanding | ThirdFloorLanding | Hallway,
        [Description("WeMo Basement")]
        WeMoBasement = Entryway | SecondFloorLanding,
        [Description("WeMo Second Floor")]
        WeMoSecondFloor = SecondFloorLanding | KitchenSink | QuarterLanding,
        [Description("WeMo Third Floor")]
        WeMoThirdFloor = ThirdFloorLanding | Hallway,
    }
}