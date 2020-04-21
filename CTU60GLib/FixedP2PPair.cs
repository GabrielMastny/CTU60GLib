using System;
using System.Collections.Generic;
using System.Text;

namespace CTU60GLib
{
    public class FixedP2PPair
    {
        private FixedStationInfo stationA;
        private FixedStationInfo stationB;

        public FixedStationInfo StationA
        {
            get { return stationA; }
        }

        public FixedStationInfo StationB
        {
            get { return stationB; }
        }

        public FixedP2PPair(FixedStationInfo a, FixedStationInfo b)
        {
            stationA = a;
            stationB = b;
        }
    }
}
