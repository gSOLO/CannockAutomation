using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using CannockAutomation.Actions;
using CannockAutomation.Devices;
using CannockAutomation.Helpers;
using CannockAutomation.Net;
using CannockAutomation.Properties;
using CannockAutomation.Timers;
using SharpHue;

namespace CannockAutomation.Extensions
{
    public static class DeviceExtensions
    {
        public static readonly Dictionary<Enum, Timer> Timers = new Dictionary<Enum, Timer>();

        public static Device GetDevice(this Enum id)
        {
            return DeviceHelper.Devices.FirstOrDefault(d => Equals(d.Id, id)) ?? new Device(id);
        }

        public static Boolean IsOn(this Enum id, Boolean refresh = false)
        {
            return id.GetDevice().IsOn(refresh);
        }

        public static Boolean AreOn(this Enum id, Boolean refresh = false)
        {
            return id.ToList<Enum>().All(device => device.GetDevice().IsOn(refresh));
        }

        public static Boolean AreOff(this Enum id, Boolean refresh = false)
        {
            return id.ToList<Enum>().All(device => !device.GetDevice().IsOn(refresh));
        }

        public static Boolean AreReachable(this Enum id, Boolean refresh = false)
        {
            return id.ToList<Enum>().All(device => device.GetDevice().IsReachable(refresh));
        }

        public static Light GetHueLight(this Enum light)
        {
            return DeviceHelper.HueLights[light.GetName()];
        }

        public static Timer SetTimer(this Enum device, double? interval = null)
        {
            return device.GetTimer(interval);
        }

        public static Timer SetTimer(this Device device, double? interval = null)
        {
            return device.Id.SetTimer(interval);
        }

        public static Timer SetTimer(this Enum device, int milliseconds = 0, int seconds = 0, int minutes = 0, int hours = 0)
        {
            return device.GetTimer(milliseconds + seconds * 1000 + minutes * 1000 * 60 + hours * 1000 * 60 * 60);
        }

        public static Timer SetTimer(this Device device, int milliseconds = 0, int seconds = 0, int minutes = 0, int hours = 0)
        {
            return device.Id.SetTimer(milliseconds, seconds, minutes, hours);
        }

        public static Timer GetTimer(this Enum device, double? interval = null)
        {
            Timer timer;
            var haveTimer = Timers.TryGetValue(device, out timer);
            if (!haveTimer)
            {
                if (device is Lights)
                {
                    timer = new LightOffTimer(Int32.MaxValue, (Lights)device);
                }
                else if (device is LightSwitches)
                {
                    timer = new SwitchOffTimer(Int32.MaxValue, (LightSwitches)device);
                }
                else if (device is PowerSwitches)
                {
                    timer = new SwitchOffTimer(Int32.MaxValue, (PowerSwitches)device);
                }
                else
                {
                    timer = new Timer(Int32.MaxValue);
                }
                Timers.Add(device, timer);
            }
            if (interval.HasValue)
            {
                timer.Interval = interval.Value;
            }
            return timer;
        }

        public static Timer GetTimer(this Device device, double? interval = null)
        {
            return device.Id.GetTimer(interval);
        }

        public static void RestartTimer(this Enum device, double? interval = null)
        {
            device.GetTimer(interval).Restart();
        }

        public static void RestartTimer(this Device device, double? interval = null)
        {
            device.Id.RestartTimer(interval);
        }

        public static void ResetTimer(this Enum device, double? interval = null)
        {
            device.GetTimer(interval).Reset();
        }

        public static void ResetTimer(this Device device, double? interval = null)
        {
            device.Id.GetTimer(interval).Reset();
        }

        public static void StopTimer(this Enum device)
        {
            device.GetTimer().Stop();
        }

        public static void StopTimer(this Device device)
        {
            device.Id.GetTimer().Stop();
        }

        public static void TurnOn(this Enum device, Boolean apply = true)
        {
            device.RestartTimer();
            if (!apply) { return; }

            if (device is PowerSwitches || device is LightSwitches)
            {
                var wemoDevice = DeviceHelper.GetDevice(device);
                if (wemoDevice != null)
                {
                    TimedWebClient.Put($"{Configuration.WemoServiceBaseUri}devices/{wemoDevice.Udn}", data: new { STATE = true });
                }
            }

            if (!(device is Lights)) return;
            foreach (var light in device.ToList<Lights>())
            {
                var hueLight = light.GetHueLight();
                if (hueLight != null)
                {
                    new LightStateBuilder().For(hueLight).TurnOn().ApplyInQueue();
                }
            }
        }

        public static void TurnOn(this Device device, Boolean apply = true)
        {
            device.Id.TurnOn(apply);
        }

        public static void TurnOff(this Enum device, Boolean apply = true)
        {
            device.RestartTimer();
            if (!apply) { return; }

            if (device is PowerSwitches || device is LightSwitches)
            {
                var wemoDevice = DeviceHelper.GetDevice(device);
                if (wemoDevice != null)
                {
                    TimedWebClient.Put($"{Configuration.WemoServiceBaseUri}devices/{wemoDevice.Udn}", data: new { STATE = false });
                }
            }

            if (!(device is Lights)) return;

            foreach (var light in device.ToList<Lights>())
            {
                var hueLight = light.GetHueLight();
                if (hueLight != null)
                {
                    new LightStateBuilder().For(hueLight).TurnOff().ApplyInQueue();
                }
            }
        }

        public static void TurnOff(this Device device, Boolean apply = true)
        {
            device.Id.TurnOff(apply);
        }

        public static void SwitchedOn(this Lights light)
        {
            light.TurnOn(false);
        }

        public static void SwitchedOff(this Lights light)
        {
            light.TurnOff(false);
        }

        public static Boolean RefreshState(this Enum id)
        {
            return RefreshState(id.GetDevice());
        }

        public static Boolean RefreshState(this Device device)
        {
            var id = device.Id;
            if (id is Lights)
            {
                DeviceHelper.HueLights.RefreshState(id.GetHueLight());
            }
            else if (id is LightSwitches || id is PowerSwitches)
            {
                DeviceHelper.RefreshWemoDevices();
            }
            return true;
        }

        public static Boolean IsOn(this Device device, Boolean refresh = false)
        {
            if (refresh) device.RefreshState();
            return device.IsOn;
        }

        public static Boolean IsReachable(this Device device, Boolean refresh = false)
        {
            if (refresh) device.RefreshState();
            return device.IsReachable;
        }

        public static Boolean AreOn(this IEnumerable<Device> devices, Boolean refresh = false)
        {
            return devices.All(device => (!refresh || device.RefreshState()) && device.IsOn);
        }

        public static Boolean AreOff(this IEnumerable<Device> devices, Boolean refresh = false)
        {
            return devices.All(device => (!refresh || device.RefreshState()) && !device.IsOn);
        }

        public static Boolean AreReachable(this IEnumerable<Device> devices, Boolean refresh = false)
        {
            return devices.All(device => (!refresh || device.RefreshState()) && device.IsReachable);
        }

        public static String GetValue(this Device device, Object property)
        {
            return device.GetType().GetProperty(property.ToString()).GetValue(device, null).ToString();
        }
        
    }
}
