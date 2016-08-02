using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using SystemColor = System.Drawing.Color;
using System.Linq;
using System.Text;
using System.Threading;
using CannockAutomation.Actions;
using CannockAutomation.Devices;
using CannockAutomation.Extensions;
using CannockAutomation.Net;
using CannockAutomation.Notifications;
using CannockAutomation.Properties;
using Newtonsoft.Json;
using SharpHue;
using SystemTimer = System.Timers.Timer;

namespace CannockAutomation.Helpers
{
    public static class DeviceHelper
    {
        private static LightCollection _hueLights;
        public static readonly List<Device> Devices;

        static DeviceHelper ()
        {
            Devices = new List<Device>();
            Devices.AddRange(Lights.All.ToList<Lights>(Lights.None).Select(light => new Device(light)));
            Devices.AddRange(LightSwitches.All.ToList<LightSwitches>(LightSwitches.None).Select(lightswitch => new Device(lightswitch)));
            Devices.AddRange(PowerSwitches.All.ToList<PowerSwitches>(PowerSwitches.None).Select(powerswitch => new Device(powerswitch)));
            Devices.AddRange(Sensors.All.ToList<Sensors>(Sensors.None).Select(sensor => new Device(sensor)));
            Devices.AddRange(Speakers.All.ToList<Speakers>(Speakers.None).Select(speaker => new Device(speaker)));
            
            var hueLightRefreshTimer = new SystemTimer(2 * 60 * 1000);
            hueLightRefreshTimer.Elapsed += (sender, args) => ThreadPool.QueueUserWorkItem(o => RefreshDevices());
            hueLightRefreshTimer.AutoReset = true;
            hueLightRefreshTimer.Start();

            LightHelper.LightStateChange += (sender, args) => ThreadPool.QueueUserWorkItem(o => RefreshHueLights(false));

            RefreshDevices();
        }
        
        public static Boolean HueInitalized
        {
            get { return SharpHue.Configuration.DeviceIP != null && SharpHue.Configuration.Username != null; }
        }

        public static LightCollection HueLights
        {
            get
            {
                if (_hueLights != null) return _hueLights;
                try
                {
                    if (!HueInitalized) SharpHue.Configuration.Initialize(Configuration.HueUserName);
                    _hueLights = new LightCollection();
                }
                catch (HubIPAddressNotFoundException e)
                {
                    Pushover.Alert(e.Message, $"Lilly DeviceHelper HueLights {e.GetType()}");
                }
                return _hueLights;
            }
        }

        public static void RefreshHueLights(Boolean updated=true)
        {
            try
            {
                var now = DateTime.Now;

                HueLights?.RefreshState();

                if (HueLights == null) { return; }

                foreach (var light in HueLights)
                {
                    var devices = Devices.Where(device => device.Name == light.Name).ToList();
                    foreach (var device in devices)
                    {
                        device.IsOn = light.State.IsOn;
                        device.IsReachable = light.State.IsReachable;
                        device.Brightness = light.State.Brightness;
                        device.Color = light.State.GetColor();
                        if (updated) { device.LastUpdate = now; }
                    }
                }
                var deviceGroups = Devices.Where(device => device.Id is Lights && device.Id.Count() > 1).ToList();
                foreach (var device in deviceGroups)
                {
                    var deviceNames = device.Id.ToList<Lights>().Select(deviceID => deviceID.GetName());
                    var lights = HueLights.Where(light => deviceNames.Contains(light.Name)).ToList();
                    device.IsOn = lights.AreOn();
                    device.IsReachable = lights.AreReachable();
                    if (lights.Any())
                    {
                        device.Brightness = (byte) lights.Average(light => light.State.Brightness);
                        device.Color = SystemColor.FromArgb(
                            (int) lights.Average(light => light.State.GetColor().R),
                            (int) lights.Average(light => light.State.GetColor().G),
                            (int) lights.Average(light => light.State.GetColor().B)
                            );
                    }
                    else
                    {
                        device.Brightness = 0;
                        device.Color = SystemColor.Black;
                    }
                    if (updated) { device.LastUpdate = now; }
                }
            }
            catch (Exception e)
            {
                Pushover.Alert(e.Message, $"Lilly DeviceHelper RefreshHueLights {e.GetType()}");
            }
        }

        public static void RefreshWemoDevices()
        {
            try
            {
                var client = new TimedWebClient(5000);
                client.QueuedRequestCompleted += RefreshWemoDevicesCompleted;
                client.QueuedRequest($"{Configuration.WemoServiceBaseUri}devices", false);
            }
            catch (Exception e)
            {
                //Pushover.Alert(e.Message, $"Lilly DeviceHelper RefreshWemoDevices {e.GetType()}");
            }
        }

        private static void RefreshWemoDevicesCompleted(Object sender, EventArgs args)
        {
            var now = DateTime.Now;
            var client = sender as TimedWebClient;
            try
            {
                var wemoDevices = JsonConvert.DeserializeObject<WemoManagerDevice[]>(client?.QueResult);
                foreach (var wemoDevice in wemoDevices)
                {
                    var devices = Devices.Where(device => device.Name == wemoDevice.NAME).ToList();
                    foreach (var device in devices)
                    {
                        device.IsOn = wemoDevice.STATE == "on";
                        device.IsReachable = wemoDevice.CONNECTED;
                        device.Udn = wemoDevice.UDN;
                        device.IsFlashing = wemoDevice.FLASHING;
                        device.LastUpdate = now;
                    }
                }
            }
            catch (Exception e)
            {
                //Pushover.Alert(e.Message, $"Lilly DeviceHelper RefreshWemoDevicesCompleted {e.GetType()}");
            }
            finally
            {
                client?.Dispose();
            }
        }

        public static void RefreshDevices()
        {
            try
            {
                RefreshHueLights();
                RefreshWemoDevices();
            }
            catch (Exception e)
            {
                Pushover.Push(e.Message, $"Lilly DeviceHelper RefreshDevices {e.GetType()}");
            }
        }

        public static Device GetDevice(String name)
        {
            return Devices?.FirstOrDefault(device => device.Name == name || device.Udn == name);
        }

        public static Device GetDevice(Enum id)
        {
            return Devices?.FirstOrDefault(device => device.Id.Equals(id));
        }

        public static String GetDeviceInfo(String name, DeviceActions info = DeviceActions.All)
        {
            var queryInfo = new NameValueCollection();
            foreach (var value in info.ToList<DeviceActions>().Select(infoKey => infoKey.ToString()))
            {
                queryInfo.Add(name, value);
            }
            return GetDeviceInfo(queryInfo);
        }

        public static String GetDeviceInfo(NameValueCollection queryInfo)
        {
            var responseStringBuilder = new StringBuilder();

            var deviceActions = DeviceActions.None;
            foreach (var key in queryInfo.AllKeys)
            {
                queryInfo[key] = Uri.UnescapeDataString(queryInfo[key]).Replace('+', ' ');
                DeviceActions deviceAction;
                var haveDeviceAction = Enum.TryParse(key, true, out deviceAction);
                if (haveDeviceAction)
                {
                    deviceActions |= deviceAction;
                }
            }

            foreach (var deviceAction in deviceActions.ToList<DeviceActions>())
            {
                var device = Devices.FirstOrDefault(aDevice => aDevice.Name == queryInfo[deviceAction.ToString()]) ?? Device.Empty;
                if (deviceActions.Count() > 1)
                {
                    responseStringBuilder.Append(device.Id);
                    responseStringBuilder.Append(".");
                    responseStringBuilder.Append(deviceAction);
                    responseStringBuilder.Append("=");
                }
                try
                {
                    responseStringBuilder.Append(device.GetValue(deviceAction));
                }
                catch
                {
                    responseStringBuilder.Append("null");
                }
                if (deviceActions.Count() > 1)
                {
                    responseStringBuilder.Append("&");
                }
            }

            return responseStringBuilder.ToString();
        }

        public static String GetDeviceInfo(NameValueCollection queryInfo, out Boolean haveDeviceInfo)
        {
            var deviceInfo = GetDeviceInfo(queryInfo);
            haveDeviceInfo = !String.IsNullOrWhiteSpace(deviceInfo);
            return deviceInfo;
        }

    }
}
