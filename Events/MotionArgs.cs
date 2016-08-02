using System;
using CannockAutomation.Actions;
using CannockAutomation.Devices;

namespace CannockAutomation.Events
{
    public class MotionArgs : RequestArgs
    {
        public Sensors Sensor { get; set; }
        public Boolean AtHome { get; set; }
        public Boolean Alert { get; set; }
        public MotionActions Action { get; set; }
        public Boolean? Retry { get; set; }

        public MotionArgs(RequestArgs request)
        {
            Source = request.Source;
            Url = request.Url;
            Query = request.Query;
            Priority = request.Priority;
            Response = request.Response;
        }

        public override string ToString()
        {
            return $"{Sensor}.{Action}";
        }
    }
}