using System;
using System.ComponentModel;

namespace CannockAutomation.Actions
{
    [Flags]
    public enum DataActions
    {
        [Description("None")]
        None = 0,
        [Description("Set")]
        Set = 1 << 0,
        [Description("Get")]
        Get = 1 << 1,
        [Description("Change")]
        Change = 1 << 2,
        [Description("Clear")]
        Clear = 1 << 3,
        [Description("Check")]
        Check = 1 << 4,
        [Description("All Data Actions")]
        All = Set | Get | Change | Clear,
    }
}
