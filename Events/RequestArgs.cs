using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CannockAutomation.Devices;
using CannockAutomation.Notifications;

namespace CannockAutomation.Events
{
    public class RequestArgs : EventArgs
    {
        public RecognitionDevice Source { get; set; }
        public String Url { get; set; }
        public String Query { get; set; }
        public List<Object> Results { get; set; }
        public Priority Priority { get; set; }
        public String Response { get; set; }

        public RequestArgs()
        {
            Results = new List<Object>();
        }

        public void AddResult(Object result)
        {
            Results.Add(result);
        }

        public void AddResult(Object result, String query)
        {
            Results.Add($"{result}({query})");
        }

        public override String ToString()
        {
            var sb = new StringBuilder("");
            if (Results.Any()) sb.AppendFormat("{0}", Results.Count);
            if (Priority > Priority.Low) sb.Append("!");
            if (sb.Length > 0) sb.Append(": ");
            sb.Append(Results.Any() ? String.Join("; ", Results) : Query);
            return sb.ToString();
        }
    }
}