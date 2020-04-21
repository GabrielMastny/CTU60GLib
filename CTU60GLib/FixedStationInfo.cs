using System;
using System.Collections.Generic;
using System.Text;

namespace CTU60GLib
{
    public class FixedStationInfo : WirellesUnitInfo
    {
        public FixedStationInfo(string name, string serialNumber, string MAC, string longitude, string latitude, string volume, string channelWidth, string power, string frequency) :base(name, serialNumber, MAC, longitude, latitude, volume, channelWidth, power, frequency)
        {

        }
    }
}
