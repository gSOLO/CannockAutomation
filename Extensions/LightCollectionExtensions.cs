using System;
using System.Collections.Generic;
using System.Linq;
using CannockAutomation.Helpers;
using CannockAutomation.Notifications;
using SharpHue;

namespace CannockAutomation.Extensions
{
    public static class LightCollectionExtensions
    {
        internal static readonly Dictionary<String, Boolean> LightStateInfo = new Dictionary<String, Boolean>();
        private static readonly Object LightStateInfoLocker = new Object();

        private static void ApplyRefresh(this LightCollection lights, Light light = null, LightStateBuilder state = null)
        {
            try
            {
                if (state != null)
                {
                    state.Apply();
                }
                if (light != null)
                {
                    light.RefreshState();
                }
                if (light == null && state == null)
                {
                    lights.Refresh();
                }
            }
            catch (HueApiException e)
            {
                Pushover.Alert(e.Message, $"Lilly ApplyRefresh {e.GetType()}");
            }
        }

        private static void ResetStateInfo(this IEnumerable<Light> lights)
        {
            foreach (var hueLight in lights)
            {
                Boolean lightStateChanged;
                var haveLightState = LightStateInfo.TryGetValue(hueLight.Name, out lightStateChanged);
                if (haveLightState)
                {
                    LightStateInfo[hueLight.Name] = false;
                }
                else
                {
                    LightStateInfo.Add(hueLight.Name, false);
                }
            }
        }

        public static void RefreshState(this LightCollection lights, Light light = null, LightStateBuilder state = null)
        {
            var lightStateInfo = lights.Select(l => new { l.Name, l.State.IsOn, });

            lights.ApplyRefresh(light, state);

            lock (LightStateInfoLocker)
            {
                lights.ResetStateInfo();

                var aLightStateChanged = false;
                foreach (var lightInfo in lightStateInfo)
                {
                    var lightName = lightInfo.Name;
                    light = lights[lightName];
                    if (light == null) continue;
                    var lightStateChanged = light.State.IsOn != lightInfo.IsOn;
                    LightStateInfo[lightName] = lightStateChanged;
                    aLightStateChanged = aLightStateChanged || lightStateChanged;
                }
                if (aLightStateChanged)
                {
                    LightHelper.OnLightStateChange();
                }
            }
        }

        public static Boolean AreOn(this IEnumerable<Light> lights, Boolean refresh = false)
        {
            var lightArray = lights as Light[] ?? lights.ToArray();
            if (refresh)
            {
                foreach (var light in lightArray) DeviceHelper.HueLights.RefreshState(light); 
            }
            return lightArray.All(light => light.State.IsOn);
        }

        public static Boolean AreOff(this IEnumerable<Light> lights, Boolean refresh = false)
        {
            var lightArray = lights as Light[] ?? lights.ToArray();
            if (refresh)
            {
                foreach (var light in lightArray) DeviceHelper.HueLights.RefreshState(light);
            }
            return lightArray.All(light => !light.State.IsOn);
        }

        public static Boolean AreReachable(this IEnumerable<Light> lights, Boolean refresh = false)
        {
            var lightArray = lights as Light[] ?? lights.ToArray();
            if (refresh)
            {
                foreach (var light in lightArray) DeviceHelper.HueLights.RefreshState(light);
            }
            return lightArray.All(light => light.State.IsReachable);
        }
    }
}
