using System;
using System.Collections.Generic;
using System.Text;

namespace CTU60GLib
{
    public class WigigPTMPUnitInfo : WirellesUnitInfo
    {
        private string eirp;
        private string orientation;

        public string Eirp
        {
            get{ return eirp; }
        }
        public string Orientation
        {
            get { return orientation; }
        }
        public WigigPTMPUnitInfo(string name, string serialNumber, string MAC, string longitude, string latitude, string volume, string channelWidth, string power, string frequency,string eirp, string orientation):base(name, serialNumber, MAC, longitude, latitude, volume, channelWidth, power, frequency)
        {
            this.eirp = eirp;
            this.orientation = orientation;
        }
    }
}
