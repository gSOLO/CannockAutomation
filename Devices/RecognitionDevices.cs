using System;
using System.ComponentModel;

namespace CannockAutomation.Devices
{
    [Flags]
    public enum RecognitionDevice
    {
        [Description("No Recognition Device")]
        None = 0,
        [Description("Web Request")]
        Lilly = 1 << 0,
        [Description("Amazon Echo")]
        Amazon = 1 << 1,
        [Description("Alexa Skills Kit")]
        Alexa = 1 << 2,
        [Description("IFTTT")]
        Ifttt = 1 << 3,
        [Description("Axius Tablet")]
        Axius = 1 << 4,
        [Description("Pushover")]
        Pushover = 1 << 5,
        [Description("SMS")]
        Sms = 1 << 6,
        [Description("Text Message")]
        Text = Sms,
        [Description("Phone Call")]
        Phone = 1 << 7,
        [Description("Wemo Device")]
        WeMo = 1 << 8,
    }
}