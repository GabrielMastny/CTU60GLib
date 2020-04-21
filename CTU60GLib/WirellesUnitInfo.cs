using System;
using System.Collections.Generic;
using System.Text;

namespace CTU60GLib
{
    public abstract class WirellesUnitInfo
    {
        protected string mac;
        protected string serialNumber;
        protected string name;
        protected string latitude;
        protected string longitude;
        protected string volume;
        protected string channelWidth;
        protected string power;
        protected string frequency;

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
        public string Longitude
        {
            get { return longitude; }
        }
        public string Latitude
        {
            get { return latitude; }
        }
        public string Volume
        {
            get { return this.volume; }
        }
        public string ChannelWidth
        {
            get { return this.channelWidth; }
        }
        public string Power
        {
            get { return this.power; }
        }
        public string Frequency
        {
            get { return this.frequency; }
        }

        public WirellesUnitInfo(string name, string serialNumber, string MAC, string longitude, string latitude, string volume, string channelWidth, string power, string frequency)
        {
            this.name = name;
            this.mac = MAC;
            this.serialNumber = serialNumber;
            this.longitude = longitude;
            this.latitude = latitude;
            this.volume = volume;
            this.channelWidth = channelWidth;
            this.power = power;
            this.frequency = frequency;
        }
    }
}
