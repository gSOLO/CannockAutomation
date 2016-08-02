using System.Threading;
using CannockAutomation.Helpers;
using SharpHue;

namespace CannockAutomation.Extensions
{
    public static class LightStateBuilderExtensions
    {
        public static void ApplyInQueue(this LightStateBuilder state)
        {
            ThreadPool.QueueUserWorkItem(o => DeviceHelper.HueLights.RefreshState(state: state));
        }
    }
}
