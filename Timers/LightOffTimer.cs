using System;
using System.Linq;
using System.Threading;
using CannockAutomation.Devices;
using CannockAutomation.Extensions;
using CannockAutomation.Helpers;
using SharpHue;
using Timer = System.Timers.Timer;

namespace CannockAutomation.Timers
{
    public class LightOffTimer : Timer
    {

        public static event EventHandler<UnhandledExceptionEventArgs> Exception;

        public LightOffTimer(Double interval, Lights lights, int transitionTime = 100) : base(interval)
        {
            var lightNames = lights.ToList<Lights>().Select(light => light.GetName());

            Elapsed += (sender, args) => ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    if (DeviceHelper.HueLights == null) { return; }

                    DeviceHelper.HueLights.RefreshState();
                    foreach (var state in DeviceHelper.HueLights.Where(light => light.State.IsOn && lightNames.Contains(light.Name)).Select(light => new LightStateBuilder().For(light)).ToList())
                    {
                        state.Brightness(0).TransitionTime((ushort)transitionTime).ApplyInQueue();
                    }
                    var count = 0;
                    while (!Enabled && ++count < transitionTime)
                    {
                        Thread.Sleep(transitionTime);
                    }
                    if (Enabled) { return; }
                    foreach (var state in DeviceHelper.HueLights.Where(light => light.State.IsOn && lightNames.Contains(light.Name)).Select(light => new LightStateBuilder().For(light)).ToList())
                    {
                        state.TurnOff().ApplyInQueue();
                    }
                }
                catch (Exception e)
                {
                    Exception?.Invoke(this, new UnhandledExceptionEventArgs(e, false));
                }
            });
            AutoReset = false;
            Start();
        }

    }
}
