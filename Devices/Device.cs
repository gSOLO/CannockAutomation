using System;
using SystemColor = System.Drawing.Color;
using CannockAutomation.Extensions;

namespace CannockAutomation.Devices
{
    public class Device
    {
        public static Device Empty
        {
            get { return new Device(DeviceId.Null); }
        }

        private enum DeviceId { Null }

        public event EventHandler<EventArgs> Update;
        public event EventHandler<EventArgs> Off;
        public event EventHandler<EventArgs> On;
        public event EventHandler<EventArgs> Toggle;

        private Boolean _isOn;
		private DateTime _lastUpdate;

		public Enum Id { get; set; }
		public String Udn { get; set; }
        public String Name { get; set; }
        public Boolean IsOn
        {
            get { return _isOn; }
            set
            {
                var oldValue = _isOn;
                if (oldValue == value) return;

                _isOn = value;

                var handler = _isOn ? On : Off;
                handler?.Invoke(this, EventArgs.Empty);

                Toggle?.Invoke(this, EventArgs.Empty);
            }
        }
        public Boolean IsReachable { get; set; }
        public Boolean IsFlashing { get; set; }
        public byte Brightness { get; set; }
        public SystemColor Color { get; set; }
        public int Volume { get; set; }
        public int Channel { get; set; }
		public DateTime LastUpdate {
			get { return _lastUpdate; } 
			set
			{
			    var oldValue = _lastUpdate;
                if (oldValue == value) return;

                _lastUpdate = value;

                Update?.Invoke(this, EventArgs.Empty);
			}
		}

        public Device(Enum id)
        {
			Id = id;
			Udn = $"{id.GetType()}.{id}";
            Name = id.GetName();
            IsOn = false;
			IsReachable = false;
            Brightness = 255;
            Color = SystemColor.White;
            Volume = 100;
            Channel = -1;
			LastUpdate = DateTime.MinValue;
        }
    }
}
