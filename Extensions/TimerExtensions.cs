using System.Timers;

namespace CannockAutomation.Extensions
{
    public static class TimerExtensions
    {
        public static void Restart(this Timer timer)
        {
            timer.Stop();
            timer.Start();
        }

        public static void Reset(this Timer timer, double? interval = null)
        {
            if (interval.HasValue)
            {
                timer.Interval = interval.Value;
            }
            else if (timer.Enabled)
            {
                timer.Restart();
            }
        }

    }
}
