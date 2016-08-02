using System;
using System.Collections.Generic;
using System.Linq;
using CannockAutomation.Actions;
using CannockAutomation.Devices;
using CannockAutomation.Extensions;
using CannockAutomation.Notifications;

namespace CannockAutomation.Helpers
{
    public static class MotionHelper
    {
        static MotionHelper()
        {
            SensorTimes = new Dictionary<Sensors, DateTime>();
        }

        public const String SensorDeviceType = "urn:belkin:device:sensor:1";
        public const String AxiusDevicePrefix = "prontotec:axius:7";

        public static Sensors GetSensors(String query, Sensors rest = Sensors.None)
        {
            var sensors = Sensors.None;

            foreach (var sensor in Sensors.All.ToList<Sensors>())
            {
                var name = sensor.GetName().ToLower();
                var addSensor = query.Contains("udn=sensor") && query.Contains($"name={name}");
                addSensor = addSensor || query.Contains($"{SensorDeviceType}:{name}:") || query.Contains($"{AxiusDevicePrefix}:{name}:");
                if (addSensor)
                {
                    sensors |= sensor;
                }
            }

            if (rest != Sensors.None)
            {
                sensors &= ~rest;
            }

            return sensors;
        }

        public static Sensors GetSensors(String query, List<Sensors> restList)
        {
            var rest = Sensors.None;
            foreach (var sensor in restList)
            {
                rest |= sensor;
            }
            return GetSensors(query, rest);
        }

        public static MotionActions GetActions(String query)
        {
            if (query.StartsWith(SensorDeviceType) || query.StartsWith(AxiusDevicePrefix))
            {
                return query.EndsWith(":1") ? MotionActions.Motion : MotionActions.MotionStopped;
            }

            if (!query.Contains("udn=sensor")) { return MotionActions.None; }

            if (query.Contains("state=on")) { return MotionActions.Motion; }
            if (query.Contains("state=off")) { return MotionActions.MotionStopped; }
            if (query.Contains("state=pending")) { return MotionActions.Pending; }
            if (query.Contains("state=error")) { return MotionActions.Error; }
            if (query.Contains("state=standby")) { return MotionActions.Standby; }

            return MotionActions.None;
        }

        public static DateTime CurrentSensorTime { get; set; }
        public static Sensors CurrentSensor  { get; set; }
        public static Sensors PreviousSensor { get; set; }
        public static readonly Dictionary<Sensors, DateTime> SensorTimes;
        
        public static MotionActions GetActions(String query, out Sensors sensors)
        {
            sensors = GetSensors(query);
            var action = GetActions(query);

            var now = DateTime.Now;
            var haveMotion = action.HasFlag(MotionActions.Motion);

            if (haveMotion && Sensors.WeMo.HasFlag(CurrentSensor) && !sensors.HasFlag(Sensors.FrontDoor) && !CurrentSensor.HasFlag(sensors))
            {
                PreviousSensor = CurrentSensor;
                CurrentSensor = sensors;

                var previousSensorTime = CurrentSensorTime;
                CurrentSensorTime = now;

                if (previousSensorTime.AddSeconds(10) > CurrentSensorTime)
                {
                    action |= PreviousSensor > CurrentSensor ? MotionActions.GoingUp : MotionActions.GoingDown;
                }
            }

            if (!haveMotion) { return action; }

            DateTime oldSensorTime;
            var haveOldSensorTime = SensorTimes.TryGetValue(sensors, out oldSensorTime);
            if (haveOldSensorTime)
            {
                SensorTimes.Remove(sensors);
                // If motion was within the last minute, we're not going anywhere.
                if (oldSensorTime.AddMinutes(1) > now)
                {
                    action |= MotionActions.GoingNowhere;
                }
                // If motion was not within the past hour, the censor has been idle.
                if (oldSensorTime.AddHours(1) < now)
                {
                    action |= MotionActions.Idle;
                }
                // If motion was not within the past 8 hours, the censor has been, for all intents and purposes, inactive.
                if (oldSensorTime.AddHours(8) < now)
                {
                    action |= MotionActions.Inactive;
                }
                // If motion was not within the past 24 hours, this motion is suspicious.
                if (oldSensorTime.AddHours(24) < now)
                {
                    action |= MotionActions.Suspicious;
                }
            }
            SensorTimes.Add(sensors, now);

            return action;
        }

    }
}
