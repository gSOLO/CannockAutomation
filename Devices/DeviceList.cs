using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CannockAutomation.Extensions;

namespace CannockAutomation.Devices
{
    public class Collections
    {
        public static readonly ReadOnlyCollection<Lights> Lights = Devices.Lights.All.ToList<Lights>().AsReadOnly();
        public static readonly ReadOnlyCollection<LightSwitches> LightSwitches = Devices.LightSwitches.All.ToList<LightSwitches>().AsReadOnly();
        public static readonly ReadOnlyCollection<PowerSwitches> PowerSwitches = Devices.PowerSwitches.All.ToList<PowerSwitches>().AsReadOnly();
        public static readonly ReadOnlyCollection<Sensors> Sensors = Devices.Sensors.All.ToList<Sensors>().AsReadOnly();
        public static readonly ReadOnlyCollection<String> LightNames = Lights.Select(device => device.GetName()).ToList().AsReadOnly();
        public static readonly ReadOnlyCollection<String> LightSwitcheNames = LightSwitches.Select(device => device.GetName()).ToList().AsReadOnly();
        public static readonly ReadOnlyCollection<String> PowerSwitcheNames = PowerSwitches.Select(device => device.GetName()).ToList().AsReadOnly();
        public static readonly ReadOnlyCollection<String> SensorNames = Sensors.Select(device => device.GetName()).ToList().AsReadOnly();
    }
}
