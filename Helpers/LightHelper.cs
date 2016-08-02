using System;
using System.Collections.Generic;
using SystemColor = System.Drawing.Color;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CannockAutomation.Actions;
using CannockAutomation.Color;
using CannockAutomation.Devices;
using CannockAutomation.Extensions;
using Newtonsoft.Json.Serialization;

namespace CannockAutomation.Helpers
{
    public static class LightHelper
    {
        public static event EventHandler<EventArgs> LightStateChange;

        private static readonly Dictionary<String, System.Drawing.Color> ColorDictionary;
        private static readonly List<QueryHelper> LightHelpers;
        private static readonly List<QueryHelper> ActionHelpers;
        private static readonly List<QueryHelper> DimHelpers;
        private static readonly List<QueryHelper> BrightenHelpers;

        static LightHelper()
        {
            ColorDictionary = new Dictionary<String, SystemColor>
            {
                {"concentrate", LightColor.Concentrate},
                {"relax", LightColor.Relax},
                {"reading", LightColor.Reading},
                {"energize", LightColor.Energize},
            };

            var colorType = typeof(SystemColor);
            var properties = colorType.GetProperties(BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.Public);
            foreach (var property in properties)
            {
                var colorName = Regex.Replace(property.Name, "(\\B[A-Z])", " $1").ToLower();
                Console.WriteLine(colorName);
                Console.WriteLine(property.GetValue(property.Name));
            }

            LightHelpers = new List<QueryHelper>
            {
                Lights.Entryway.GetQuery(),
                Lights.SecondFloorLanding.GetQuery("second floor", "bathroom"),
                Lights.LivingRoom.GetQuery("living", "outside"),
                Lights.DiningRoom.GetQuery("dining"),
                Lights.Kitchen.GetQuery("kitchen", "sink"),
                Lights.QuarterLanding.GetQuery(),
                Lights.ThirdFloorLanding.GetQuery("third floor", "bathroom"),
                Lights.Bedroom.GetQuery(),
                Lights.Hallway.GetQuery(),
                Lights.Basement.GetQuery(),
                Lights.SecondFloor.GetQuery("on the second floor"),
                Lights.SecondFloor.GetQuery("first floor"),
                Lights.SecondFloor.GetQuery("downstairs"),
                Lights.LivingArea.GetQuery(),
                Lights.ThirdFloor.GetQuery("on the third floor"),
                Lights.ThirdFloor.GetQuery("upstairs"),
            };

            ActionHelpers = new List<QueryHelper>
            {
                LightActions.TurnOn.GetQuery(),
                LightActions.TurnOn.GetQuery(" on "),
                LightActions.TurnOn.GetQuery(endsWith: " on"),
                LightActions.TurnOff.GetQuery(),
                LightActions.TurnOff.GetQuery(" off "),
                LightActions.TurnOff.GetQuery(endsWith: " off"),
                LightActions.TurnOff.GetQuery(" cut "),
            };

            DimHelpers = new List<QueryHelper>
            {
                LightActions.Dim.GetQuery("dim "),
                LightActions.Dim.GetQuery("darken "),
                LightActions.Dim.GetQuery(" bright ", "brighten"),
            };

            BrightenHelpers = new List<QueryHelper>
            {
                LightActions.Brighten.GetQuery("brightness "),
                LightActions.Brighten.GetQuery("brighten "),
                LightActions.Brighten.GetQuery(" dark ", "darken"),
            };

        }

        public static void OnLightStateChange()
        {
            LightStateChange?.Invoke(null, EventArgs.Empty);
        }

        public static void OnLightStateChange(Object sender, EventArgs e)
        {
            LightStateChange?.Invoke(sender, e);
        }

        public static Lights GetLights(String query, Lights rest = Lights.None)
        {
            var lights = Lights.None;
            
            foreach (var queryHelper in LightHelpers)
            {
                queryHelper.Run(query, ref lights);
            }

            return GetRemainingLights(query, lights, rest);
        }

        private static Lights GetRemainingLights(String query, Lights lights, Lights rest)
        {
            if (lights == Lights.None && !query.Contains(SwitchHelper.LightSwitchDeviceType) && !query.Contains("uid=") && (query.Contains("inside") || !query.Contains("outside")) && (query.Contains("lights") || query.Contains("all")))
            {
                lights = Lights.All;
            }

            if (query.Contains("except"))
            {
                lights = Lights.All & ~lights;
            }

            if (rest != Lights.None && query.Contains("rest"))
            {
                lights &= ~rest;
            }

            return lights;
        }

        public static Lights GetLights(String query, List<Lights> restList)
        {
            var rest = restList.Aggregate<Lights, Lights>(0, (current, light) => current | light);
            return GetLights(query, rest);
        }

        public static SystemColor GetColor(String query, Boolean exactMatch=false)
        {
            if (exactMatch)
            {
                SystemColor color;
                var haveColor = ColorDictionary.TryGetValue(query, out color);
                return haveColor ? color : SystemColor.Empty;
            }
            var colors = new List<SystemColor>();
            foreach (var colorKey in ColorDictionary.Keys)
            {
                var queryHasKey = query.Contains($" {colorKey} ") || query.EndsWith($" {colorKey}") || query.Equals(colorKey);
                if (queryHasKey) colors.Add(ColorDictionary[colorKey]);
            }
            return colors.Any() ? colors.OrderByDescending(color => color.Name.Length * (color.Name != query ? 1 : 10)).First() : SystemColor.Empty;
        }

        public static byte? GetBrightness(String query)
        {
            foreach (var word in query.Split(' '))
            {
                byte brightness;
                var haveBrightness = byte.TryParse(word, out brightness);
                if (haveBrightness)
                {
                    return brightness;
                }
            }
            var number = QueryHelper.GetNumber(query);
            if (number != 0)
            {
                return (byte)number;
            }
            return null;
        }

        public static LightActions GetActions(String query, out SystemColor color, out byte? brightness)
        {
            var action = LightActions.None;
            
            foreach (var queryHelper in ActionHelpers)
            {
                queryHelper.Run(query, ref action);
            }

            if (action == LightActions.None)
            {
                action = LightActions.Toggle;
            }

            color = GetColor(query);
            if (color != SystemColor.Empty)
            {
                if (action == LightActions.Toggle)
                {
                    action = LightActions.Color;
                }
                else
                {
                    action |= LightActions.Color;
                }
                query = query.Replace(color.Name, String.Empty);
            }

            brightness = null;
            if (DimHelpers.HaveMatch(query))
            {
                if (action == LightActions.Toggle)
                {
                    action = LightActions.Dim;
                }
                else
                {
                    action |= LightActions.Dim;
                }
                brightness = GetBrightness(query);
            }
            if (BrightenHelpers.HaveMatch(query))
            {
                if (action == LightActions.Toggle)
                {
                    action = LightActions.Brighten;
                }
                else
                {
                    action |= LightActions.Brighten;
                }
                brightness = GetBrightness(query);
            }
            return action;
        }

    }
}
