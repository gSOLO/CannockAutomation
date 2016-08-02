using System;
using System.ComponentModel;

namespace CannockAutomation.Actions
{
    [Flags]
    public enum MotionActions
    {
        [Description("None")]
        None = 0,
        [Description("Motion")]
        Motion = 1 << 0,
        [Description("Going Up")]
        GoingUp = 1 << 1,
        [Description("Going Down")]
        GoingDown = 1 << 2,
        [Description("Motion Stopped")]
        MotionStopped = 1 << 3,
        [Description("Going Nowhere")]
        GoingNowhere = 1 << 4,
        [Description("Idle")]
        Idle = 1 << 5,
        [Description("Inactive")]
        Inactive = 1 << 6,
        [Description("Suspicious")]
        Suspicious = 1 << 7,
        [Description("Error")]
        Error = 1 << 8,
        [Description("Pending")]
        Pending = 1 << 9,
        [Description("Standby")]
        Standby = 1 << 10,
        [Description("Idle Motion  ~ 1hr")]
        IdleMotion = Idle | Motion,
        [Description("Inactive Motion ~ 8hrs")]
        InactiveMotion = Inactive | Motion,
        [Description("Suspicious Motion ~ 24hrs")]
        SuspiciousMotion = Suspicious | Motion,
        [Description("Motion Going Up")]
        Up = Motion | GoingUp,
        [Description("Motion Going Down")]
        Down = Motion | GoingDown,
        [Description("Motion Going Nowhere")]
        ContinuousMotion = Motion | GoingNowhere,
        [Description("Motion Going Up Nowhere")]
        ContinuousUp = ContinuousMotion | GoingUp,
        [Description("Motion Going Down Nowhere")]
        ContinuousDown = ContinuousMotion | GoingDown,
    }
}