using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CannockAutomation.Extensions;

namespace CannockAutomation.Devices
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class WemoManagerDevice
    {
        public string UDN { get; set; }
        public string NAME { get; set; }
        public Boolean SWITCH { get; set; }
        public Boolean CONNECTED { get; set; }
        public Boolean FLASHING { get; set; }
        public string TYPE { get; set; }
        public string STATE { get; set; }
    }
}
