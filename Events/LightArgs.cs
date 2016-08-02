using System;
using System.Collections.Generic;
using SystemColor = System.Drawing.Color;
using CannockAutomation.Actions;
using CannockAutomation.Devices;

namespace CannockAutomation.Events
{
    public class LightArgs : RequestArgs
    {
        public Lights Light { get; set; }
        public LightActions Action { get; set; }
        public Boolean? Retry { get; set; }
        public ushort TransitionTime { get; set; }
        public SystemColor Color { get; set; }
        public byte? Brightness { get; set; }

        public LightArgs(RequestArgs request)
        {
            Source = request.Source;
            Url = request.Url;
            Query = request.Query;
            Priority = request.Priority;
            Response = request.Response;
        }
        public override string ToString()
        {
            return $"{Light}.{Action}";
        }
    }
}