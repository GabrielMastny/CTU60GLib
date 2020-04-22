using System;
using System.Collections.Generic;
using System.Text;

namespace CTU60GLib
{
    public class WigigPTMPUnitInfo : WirellesUnitInfo
    {
        private int? eirp;
        private int orientation;

        public int? Eirp
        {
            get{ return eirp; }
        }
        public int Orientation
        {
            get { return orientation; }
        }
        public WigigPTMPUnitInfo(string name, string serialNumber, string MAC, string longitude, string latitude, string volume, string channelWidth, string power, string frequency,string eirp, string orientation):base(name, serialNumber, MAC, longitude, latitude, volume, channelWidth, power, frequency)
        {
            if (eirp == "") eirp = null;
            //if (eirp == null || eirp == string.Empty) throw new Exceptions.MissingParameterException("Missing eirp"); eirp can be null
            if (orientation == null || orientation == string.Empty) throw new Exceptions.MissingParameterException("Missing orientation");

            int toParsei;
            if (!int.TryParse(volume, out toParsei) ||
                (toParsei < 0 || toParsei > 60)) throw new Exceptions.InvalidPropertyValueException("Volume has to be integer in range <0;60>", volume);
            if ((eirp != null && !int.TryParse(eirp, out toParsei)) ||
                (toParsei < -20 || toParsei > 40)) throw new Exceptions.InvalidPropertyValueException("EIRP has to be integer in range <-20;40>", eirp);
            this.eirp = toParsei;
            if (!int.TryParse(orientation, out toParsei) ||
                (toParsei < 0 || toParsei > 360)) throw new Exceptions.InvalidPropertyValueException("Orientation has to be integer in range <0;360>",orientation);
            this.orientation = toParsei;
        }
    }
}
