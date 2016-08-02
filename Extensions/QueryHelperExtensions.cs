using System;
using System.Collections.Generic;
using System.Linq;
using CannockAutomation.Actions;
using CannockAutomation.Devices;
using CannockAutomation.Helpers;

namespace CannockAutomation.Extensions
{
    public static class QueryHelperExtensions
    {
        public static Boolean AllMatch(this IEnumerable<QueryHelper> helpers, String query)
        {
            return helpers.All(helper => helper.HaveMatch(query));
        }

        public static Boolean HaveMatch(this IEnumerable<QueryHelper> helpers, String query)
        {
            return helpers.Any(helper => helper.HaveMatch(query));
        }
        
        public static void Run(this QueryHelper helper, String query, ref Lights lights)
        {
            if (helper.HaveMatch(query)) lights |= (Lights)helper.MatchedItem;
        }

        public static void Run(this QueryHelper helper, String query, ref LightSwitches lightSwitches)
        {
            if (helper.HaveMatch(query)) lightSwitches |= (LightSwitches)helper.MatchedItem;
        }

        public static void Run(this QueryHelper helper, String query, ref PowerSwitches powerSwitches)
        {
            if (helper.HaveMatch(query)) powerSwitches |= (PowerSwitches)helper.MatchedItem;
        }

        public static void Run(this QueryHelper helper, String query, ref Sensors sensors)
        {
            if (helper.HaveMatch(query)) sensors |= (Sensors)helper.MatchedItem;
        }

        public static void Run(this QueryHelper helper, String query, ref Speakers speakers)
        {
            if (helper.HaveMatch(query)) speakers |= (Speakers)helper.MatchedItem;
        }

        public static void Run(this QueryHelper helper, String query, ref LightActions actions)
        {
            if (helper.HaveMatch(query)) actions |= (LightActions)helper.MatchedItem;
        }

        public static void Run(this QueryHelper helper, String query, ref MotionActions actions)
        {
            if (helper.HaveMatch(query)) actions |= (MotionActions)helper.MatchedItem;
        }

        public static void Run(this QueryHelper helper, String query, ref MusicActions actions)
        {
            if (helper.HaveMatch(query)) actions |= (MusicActions)helper.MatchedItem;
        }

        public static void Run(this QueryHelper helper, String query, ref SwitchActions actions)
        {
            if (helper.HaveMatch(query)) actions |= (SwitchActions)helper.MatchedItem;
        }
    }
}
