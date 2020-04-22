using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CTU60GLib
{
    public abstract class WirellesUnitInfo
    {
        protected string mac;
        protected string serialNumber;
        protected string name;
        protected float latitude;
        protected float longitude;
        protected int volume;
        protected int channelWidth;
        protected int power;
        protected int frequency;


        public string Name
        {
            get { return name; }
        }
        public string SerialNumber
        {
            get { return this.serialNumber; }
        }
        public string MAC
        {
            get { return this.mac; }
        }
        public float Longitude
        {
            get { return longitude; }
        }
        public float Latitude
        {
            get { return latitude; }
        }
        public int Volume
        {
            get { return this.volume; }
        }
        public int ChannelWidth
        {
            get { return this.channelWidth; }
        }
        public int Power
        {
            get { return this.power; }
        }
        public int Frequency
        {
            get { return this.frequency; }
        }

        public WirellesUnitInfo(string name, string serialNumber, string mac, string longitude, string latitude, string volume, string channelWidth, string power, string frequency)
        {
            if ((serialNumber == null || serialNumber == string.Empty) && 
                (mac == null || mac == string.Empty)) throw new Exceptions.MissingParameterException("both serialNumber and mac address were empty at least one has to have a value.");
            if (longitude == null || longitude == string.Empty) throw new Exceptions.MissingParameterException("Missing longtitude.");
            if (latitude == null || latitude == string.Empty) throw new Exceptions.MissingParameterException("Missing latitude");
            if (volume == null || volume == string.Empty) throw new Exceptions.MissingParameterException("Missing volume");
            if (channelWidth == null || channelWidth == string.Empty) throw new Exceptions.MissingParameterException("Missing channel width");
            if (power == null || power == string.Empty) throw new Exceptions.MissingParameterException("Missing power");
            if (frequency == null || frequency == string.Empty) throw new Exceptions.MissingParameterException("Missing frequency");

            float toParsef;
            int toParsei;
            Regex rxMacAddress = new Regex(@"^[0-9a-fA-F]{2}(((:[0-9a-fA-F]{2}){5})|((:[0-9a-fA-F]{2}){5}))$");

            if (mac != null && mac != string.Empty && 
                !rxMacAddress.IsMatch(mac)) throw new Exceptions.InvalidPropertyValueException("Mac address eg. 1A:2B:3C:4D:5E:6F", mac);

            if (!float.TryParse(longitude, out toParsef)) throw new Exceptions.InvalidPropertyValueException("Longitude should be angle in float number", longitude);
            this.longitude = toParsef;

            if (!float.TryParse(latitude, out toParsef)) throw new Exceptions.InvalidPropertyValueException("Latitude should be angle in float number", latitude);
            this.latitude = toParsef;

            //validation for volume is different for each type
            this.volume = int.Parse(volume);

            if (!int.TryParse(channelWidth, out toParsei) ||
                (toParsei < 50 || toParsei > 2200)) throw new Exceptions.InvalidPropertyValueException("channel width has to be integer in range <50;2200>", channelWidth);
            this.channelWidth = toParsei;

            if (!int.TryParse(power, out toParsei)) throw new Exceptions.InvalidPropertyValueException("Power has to be integer", power);
            this.power = toParsei;

            if (!int.TryParse(frequency, out toParsei) ||
                (toParsei < 57000 || toParsei > 66000)) throw new Exceptions.InvalidPropertyValueException("Frequency has to be integer in range <57000;66000>", frequency);
            this.frequency = toParsei;

            this.name = (name == null)? string.Empty:name;
            this.mac = mac;
            this.serialNumber = (serialNumber == null)? "": serialNumber;
        }
    }
}
