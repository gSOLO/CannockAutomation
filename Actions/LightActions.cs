using System;
using System.ComponentModel;

namespace CannockAutomation.Actions
{
    [Flags]
    public enum LightActions
    {
        [Description("None")]
        None = 0,
        [Description("Turn On")]
        TurnOn = 1 << 0,
        [Description("Turn Off")]
        TurnOff = 1 << 1,
        [Description("Toggle")]
        Toggle = 1 << 2,
        [Description("Dim")]
        Dim = 1 << 3,
        [Description("Brighten")]
        Brighten = 1 << 4,
        [Description("Color")]
        Color = 1 << 5,
        [Description("Turn on and Color")]
        TurnOnColor = TurnOn | Color,
        [Description("Turn on and Dim")]
        TurnOnDim = TurnOn | Dim,
        [Description("Turn on and Brighten")]
        TurnOnBright = TurnOn | Brighten,
        [Description("Turn on and Brighten Color")]
        TurnOnBrightColor = TurnOnBright | Color,
    }
}