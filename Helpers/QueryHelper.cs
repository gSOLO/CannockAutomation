using System;
using System.Linq;
using System.Collections.Generic;
using CannockAutomation.Extensions;

namespace CannockAutomation.Helpers
{
    public class QueryHelper
    {
        private static readonly Dictionary<String, Int64> Numbers = new Dictionary<String, Int64> {
            {"zero", 0},
            {"one", 1},
            {"two", 2},
            {"three", 3},
            {"four", 4},
            {"five", 5},
            {"six", 6},
            {"seven", 7},
            {"eight", 8},
            {"nine", 9},
            {"ten", 10},
            {"eleven", 11},
            {"twelve", 12},
            {"thirteen", 13},
            {"fourteen", 14},
            {"fifteen", 15},
            {"sixteen", 16},
            {"seventeen", 17},
            {"eighteen", 18},
            {"nineteen", 19},
            {"twenty", 20},
            {"thirty", 30},
            {"forty", 40},
            {"fifty", 50},
            {"sixty", 60},
            {"seventy", 70},
            {"eighty", 80},
            {"ninety", 90},
            {"hundred", 100},
        };

        public static Int64 GetNumber(String query)
        {
            if (query == null) { return 0; }
            return query.Split(' ').Where(word => Numbers.ContainsKey(word)).Sum(word => Numbers[word]);
        }
        
        public static QueryHelper Create(Enum item, String match)
        {
            return item.GetQuery(match);
        }

        public String Match { get; set; }

        public Enum MatchedItem { get; internal set; }

        public QueryHelper(Enum item, String match)
        {
            MatchedItem = item;
            Match = match;
        }

        public virtual Boolean HaveMatch(String query)
        {
            if (String.IsNullOrWhiteSpace(query)) return false;

            var haveMatch = true;
            if (!String.IsNullOrWhiteSpace(Match))
            {
                haveMatch = query.Contains(Match);
            }

            return haveMatch;
        }

    }
}
