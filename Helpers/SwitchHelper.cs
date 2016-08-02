using System;
using System.Collections.Generic;
using System.Linq;
using CannockAutomation.Actions;
using CannockAutomation.Devices;
using CannockAutomation.Extensions;

namespace CannockAutomation.Helpers
{
    public static class SwitchHelper
    {

        public const String LightSwitchDeviceType = "urn:belkin:device:lightswitch:1";
        public const String PowerSwitchDeviceType = "urn:belkin:device:controllee:1";

        public static void OnSwitchStateChange()
        {
            SwitchStateChange?.Invoke(null, EventArgs.Empty);
        }

        public static void OnSwitchStateChange(object sender, EventArgs e)
        {
            SwitchStateChange?.Invoke(sender, e);
        }

        public static event EventHandler<EventArgs> SwitchStateChange;
        
        public static LightSwitches GetLightSwitches(String query, LightSwitches rest = LightSwitches.None)
        {
            var lightswitches = LightSwitches.None;

            foreach (var lightswitch in LightSwitches.All.ToList<LightSwitches>())
            {
                var name = lightswitch.GetName().ToLower();
                var addSwitch = query.Contains("udn=lightswitch") && query.Contains($"name={name}");
                addSwitch = addSwitch || query.Contains($"{LightSwitchDeviceType}:{name}:");
                if (addSwitch)
                {
                    lightswitches |= lightswitch;
                }
            }

            if (query.Contains("outside garage") || query.Contains("outside the garage"))
            {
                lightswitches |= LightSwitches.OutsideGarage;
            }
            if (query.Contains("garage") && !query.Contains("outside"))
            {
                lightswitches |= LightSwitches.Garage;
            }
            if (query.Contains("front door")) lightswitches |= LightSwitches.FrontDoor;
            if (query.Contains("outside living") || query.Contains("outside the living"))
            {
                lightswitches |= LightSwitches.OutsideLivingRoom;
            }
            else if (query.Contains("outside"))
            {
                lightswitches |= LightSwitches.OutsideGarage | LightSwitches.FrontDoor | LightSwitches.OutsideLivingRoom;
            }

            if (query.Contains("kitchen sink"))
            {
                lightswitches |= LightSwitches.KitchenSink;
            }

            if (query.Contains("secoind floor bathroom"))
            {
                lightswitches |= LightSwitches.SecondFloorBathroom;
            }

            if (query.Contains("closet"))
            {
                lightswitches |= LightSwitches.Closet;
            }

            if (query.Contains("third floor bathroom"))
            {
                lightswitches |= LightSwitches.ThirdFloorBathroom;
            }
            else if (query.Contains("bathroom"))
            {
                lightswitches |= LightSwitches.SecondFloorBathroom | LightSwitches.ThirdFloorBathroom;
            }

            if (query.Contains("basement"))
            {
                lightswitches |= LightSwitches.Garage;
            }

            if (query.Contains("on the second floor") || query.Contains("downstairs"))
            {
                lightswitches |= LightSwitches.KitchenSink;
                if (query.Contains("all"))
                {
                    lightswitches |= LightSwitches.SecondFloorBathroom;
                    if (!query.Contains("inside"))
                    {
                        lightswitches |= LightSwitches.OutsideLivingRoom;
                    }
                }
            }

            if (query.Contains("on the third floor") || query.Contains("upstairs"))
            {
                lightswitches |= LightSwitches.Closet;
                if (query.Contains("all"))
                {
                    lightswitches |= LightSwitches.ThirdFloorBathroom;
                }
            }

            var exception = query.Contains("except");

            if (lightswitches == LightSwitches.None && (query.Contains("inside") || !query.Contains("outside")) && (query.Contains("lights") || query.Contains("all")))
            {
                lightswitches = exception ? LightSwitches.All : LightSwitches.Inside;
            }

            if (exception)
            {
                lightswitches = LightSwitches.All & ~lightswitches;
            }

            if (rest != LightSwitches.None)
            {
                lightswitches &= ~rest;
            }

            return lightswitches;
        }

        public static LightSwitches GetLightSwitches(String query, List<LightSwitches> restList)
        {
            var rest = LightSwitches.None;
            foreach (var lightswitch in restList)
            {
                rest |= lightswitch;
            }
            return GetLightSwitches(query, rest);
        }

        public static PowerSwitches GetPowerSwitches(String query, PowerSwitches rest = PowerSwitches.None)
        {
            var powerswitches = PowerSwitches.None;

            foreach (var powerswitch in PowerSwitches.All.ToList<PowerSwitches>())
            {
                var name = powerswitch.GetName().ToLower();
                var addSwitch = query.Contains("udn=socket") && query.Contains($"name={name}");
                addSwitch = addSwitch || query.Contains($"{PowerSwitchDeviceType}:{name}:");
                if (addSwitch)
                {
                    powerswitches |= powerswitch;
                }
            }

            if (query.Contains("toaster"))
            {
                powerswitches |= PowerSwitches.Toaster;
            }

            if (query.Contains("fan"))
            {
                powerswitches |= PowerSwitches.Fan;
            }

            if (query.Contains("except"))
            {
                powerswitches = PowerSwitches.All & ~powerswitches;
            }

            if (rest != PowerSwitches.None)
            {
                powerswitches &= ~rest;
            }

            return powerswitches;
        }

        public static PowerSwitches GetPowerwitches(String query, List<PowerSwitches> restList)
        {
            var rest = PowerSwitches.None;
            foreach (var powerswitch in restList)
            {
                rest |= powerswitch;
            }
            return GetPowerSwitches(query, rest);
        }

        public static SwitchActions GetActions(String query)
        {
            if (query.StartsWith(LightSwitchDeviceType) || query.StartsWith(PowerSwitchDeviceType))
            {
                return query.EndsWith(":1") ? SwitchActions.On : SwitchActions.Off;
            }

            if (!query.Contains("udn=socket") && !query.Contains("udn=lightswitch")) { return SwitchActions.None; }

            if (query.Contains("state=on")) { return SwitchActions.On; }
            if (query.Contains("state=off")) { return SwitchActions.Off; }
            if (query.Contains("state=pending")) { return SwitchActions.Pending; }
            if (query.Contains("state=error")) { return SwitchActions.Error; }
            if (query.Contains("state=standby")) { return SwitchActions.Standby; }

            return SwitchActions.None;
        }

        public static SwitchActions GetActions(String query, out LightSwitches lightswitches, out PowerSwitches powerswitches)
        {
            lightswitches = GetLightSwitches(query);
            powerswitches = GetPowerSwitches(query);
            var actions = GetActions(query);

            if (lightswitches != LightSwitches.None || powerswitches != PowerSwitches.None)
            {
                if (query.Contains(" on ") || query.EndsWith(" on")) actions = SwitchActions.TurnOn;
                if (query.Contains(" off ") || query.EndsWith(" off") || query.Contains("cut")) actions = SwitchActions.TurnOff;
            }

            return actions;
        }

    }
}