using System;
using CannockAutomation.Actions;
using CannockAutomation.Devices;

namespace CannockAutomation.Events
{
    public class MusicArgs : RequestArgs
    {
        public Speakers Speaker { get; set; }
        public String Name { get; set; }
        public MusicActions Action { get; set; }
        public Boolean? Retry { get; set; }

        public MusicArgs(RequestArgs request)
        {
            Source = request.Source;
            Url = request.Url;
            Query = request.Query;
            Priority = request.Priority;
            Response = request.Response;
        }

        public override string ToString()
        {
            return $"{Name}.{Action}";
        }
    }
}