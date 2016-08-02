using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CannockAutomation.Actions;
using CannockAutomation.Devices;
using CannockAutomation.Extensions;
using CannockAutomation.Net;
using CannockAutomation.Properties;
using Timer = System.Timers.Timer;

namespace CannockAutomation.Timers
{
    public class SwitchOffTimer : Timer
    {

        public static event EventHandler<UnhandledExceptionEventArgs> Exception;

        public SwitchOffTimer(Double interval, LightSwitches switches) : base(interval)
        {
            Setup(switches.ToList<LightSwitches>().Select(lightSwitch => lightSwitch.GetDevice()));
        }

        public SwitchOffTimer(Double interval, PowerSwitches switches) : base(interval)
        {
            Setup(switches.ToList<PowerSwitches>().Select(powerSwitch => powerSwitch.GetDevice()));
        }

        protected void Setup(IEnumerable<Device> switches)
        {
            Elapsed += (sender, args) => ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    foreach (var device in switches)
                    {

                        TimedWebClient.Put($"{Configuration.WemoServiceBaseUri}devices/{device.Udn}", data: new { STATE = false });
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
