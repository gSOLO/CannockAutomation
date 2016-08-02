using System;

namespace CannockAutomation.Helpers
{
    public class SmartQueryHelper : QueryHelper
    {
        public String Exclude { get; set; }
        public String Start { get; set; }
        public String End { get; set; }

        public override Boolean HaveMatch(String query)
        {
            if (String.IsNullOrWhiteSpace(query)) return false;

            var haveMatch = base.HaveMatch(query);

            if (!String.IsNullOrWhiteSpace(Exclude))
            {
                haveMatch = haveMatch && !query.Contains(Exclude);
            }
            if (!String.IsNullOrWhiteSpace(Start))
            {
                haveMatch = haveMatch && query.StartsWith(Start);
            }
            if (!String.IsNullOrWhiteSpace(End))
            {
                haveMatch = haveMatch && query.EndsWith(End);
            }

            return haveMatch;
        }

        public SmartQueryHelper(Enum item, String match, String exclude = null) : base (item, match)
        {
           Exclude = exclude;
        }
    }
}
