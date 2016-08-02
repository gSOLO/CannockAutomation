using System;
using SystemColor = System.Drawing.Color;
using SharpHue;

namespace CannockAutomation.Extensions
{
    public static class LightStateExtensions
    {
        public static SystemColor GetColor(this LightState state)
        {
            if (String.Compare(state.CurrentColorMode, "xy", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return SystemColor.Black;
            }
            return state.Color;
        }
    }
}
