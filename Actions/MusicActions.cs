using System;
using System.ComponentModel;

namespace CannockAutomation.Actions
{
    [Flags]
    public enum MusicActions
    {
        [Description("None")]
        None = 0,
        [Description("Play")]
        Play = 1 << 0,
        [Description("Enable")]
        Enable = 1 << 1,
        [Description("Disable")]
        Disable = 1 << 2,
        [Description("Pause")]
        Pause = 1 << 3,
        [Description("Skip")]
        Skip = 1 << 4,
        [Description("Volume Up")]
        VolumeUp = 1 << 5,
        [Description("Volume Down")]
        VolumeDown = 1 << 6,
        [Description("Mute")]
        Mute = 1 << 7,
        [Description("Back")]
        Back = 1 << 8,
        [Description("Next")]
        Next = 1 << 9,
        [Description("Shuffle")]
        Shuffle = 1 << 10,
        [Description("Play Pause")]
        PlayPause = 1 << 11,
        [Description("Previous")]
        Previous = 1 << 12,
        [Description("Stop")]
        Stop = 1 << 13,
        [Description("Resume")]
        Resume = 1 << 14,
        [Description("Speaker Actions")]
        Speaker = VolumeUp | VolumeDown | Mute | Enable | Disable,
        [Description("Player Actions")]
        Player = Play | Pause | Skip | Back | Next | Shuffle | PlayPause | Previous | Stop | Resume,
        [Description("Player Actions")]
        Playlist = Play | Shuffle,
    }
}