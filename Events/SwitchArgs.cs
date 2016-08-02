using System;
using CannockAutomation.Actions;

namespace CannockAutomation.Events
{
    public class SwitchArgs : RequestArgs
    {
        public Enum Switch { get; set; }
        public SwitchActions Action { get; set; }
        public Boolean? Retry { get; set; }

        public SwitchArgs(RequestArgs request)
        {
            Source = request.Source;
            Url = request.Url;
            Query = request.Query;
            Priority = request.Priority;
            Response = request.Response;
        }

        public override string ToString()
        {
            return $"{Switch}.{Action}";
        }
    }
}