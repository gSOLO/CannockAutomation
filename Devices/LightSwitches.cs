using System;
using System.ComponentModel;

namespace CannockAutomation.Devices
{
    [Flags]
    public enum LightSwitches
    {
        [Description("No Light Switches")]
        None = 0,
        [Description("Outside Garage")]
        OutsideGarage = 1 << 0,
        [Description("Garage")]
        Garage = 1 << 1,
        [Description("Front Door")]
        FrontDoor = 1 << 2,
        [Description("Laundry Room")]
        LaundryRoom = 1 << 3,
        [Description("Outside Living Room")]
        OutsideLivingRoom = 1 << 4,
        [Description("Kitchen Sink")]
        KitchenSink = 1 << 5,
        [Description("Seocnd Floor Bathroom")]
        SecondFloorBathroom = 1 << 6,
        [Description("Closet")]
        Closet = 1 << 7,
        [Description("Third Floor Bathroom")]
        ThirdFloorBathroom = 1 << 8,
        [Description("All Light Switches")]
        All = OutsideGarage | Garage | LaundryRoom | FrontDoor | OutsideLivingRoom | KitchenSink | SecondFloorBathroom | Closet | ThirdFloorBathroom,
        [Description("Outside")]
        Outside = OutsideGarage | Garage | FrontDoor | OutsideLivingRoom,
        [Description("Inside")]
        Inside = LaundryRoom | KitchenSink | SecondFloorBathroom | Closet | ThirdFloorBathroom,
    }
}
