using System;
using System.Collections.Generic;
using System.Text;

namespace CTU60GLib
{
    public class FixedStationInfo : WirellesUnitInfo
    {
        private int rsn;

        public int RSN { get { return rsn; } }
        public FixedStationInfo(string name, string serialNumber, string MAC, string longitude, string latitude, string volume, string channelWidth, string power, string frequency,string rsn) :base(name, serialNumber, MAC, longitude, latitude, volume, channelWidth, power, frequency)
        {
            if (rsn == null || rsn == string.Empty) throw new Exceptions.MissingParameterException("Missing rsn value.");
            int toParsei;
            if (!int.TryParse(rsn, out toParsei) ||
                !((toParsei == 12) ||
                  (toParsei == 18) ||
                  (toParsei == 21) ||
                  (toParsei == 25) ||
                  (toParsei == 28) ||
                  (toParsei == 31) ||
                  (toParsei == 34))) throw new Exceptions.InvalidPropertyValueException("rsn has to be one of those {12;18;21;25;28;31;34", rsn);
            this.rsn = toParsei;

            if (!int.TryParse(volume, out toParsei) ||
                (toParsei < 30 || toParsei > 60)) throw new Exceptions.InvalidPropertyValueException("Volume has to be integer in range <30;60>", volume);
        }
    }
}
