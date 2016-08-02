using System;
using System.Drawing;
using CannockAutomation.Actions;
using CannockAutomation.Devices;

namespace CannockAutomation.Events
{
    public class DataArgs
    {
        public String Name { get; set; }
        public String Value { get; set; }
        public String OldValue { get; set; }
        public String NewValue
        {
            get { return Value; }
            set { Value = value; }
        }

        public DataActions Action { get; set; }

        public override string ToString()
        {
            return $"{Name}.{Action}";
        }
    }
}