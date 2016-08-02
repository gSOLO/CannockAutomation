using System;
using CannockAutomation.Devices;
using SharpHue;

namespace CannockAutomation.Extensions
{
    public static class LightExtensions
    {
        public static Boolean GetStateChanged(this Light light)
        {
            Boolean lightStateChanged;
            var haveLightStateInfo = LightCollectionExtensions.LightStateInfo.TryGetValue(light.Name, out lightStateChanged);
            return haveLightStateInfo && lightStateChanged;
        }

        public static void Switched(this Light light, Boolean on)
        {
            if (on)
            {
                light.Name.GetEnumValue<Lights>().SwitchedOn();
            }
            else
            {
                light.Name.GetEnumValue<Lights>().SwitchedOff();
            }
        }
    }
}
