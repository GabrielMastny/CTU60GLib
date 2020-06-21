using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CTU60GLib
{
    public class P2PSite
    {
        private const double maxDistance = 10;
        public readonly FixedStationInfo StationA;
        public readonly FixedStationInfo StationB;

        public P2PSite(FixedStationInfo staA, FixedStationInfo staB)
        {
            if (staA == null || staB == null) throw new Exceptions.MissingParameterException("Missing pair station");
            double distance = CalculateCoordinatesDistance(staA, staB);
            if (Math.Abs(distance) > maxDistance) throw new Exceptions.InvalidPropertyValueException("Distance between stations is too far, should be less than 10 [Km]", distance.ToString());
            StationA = staA;
            StationB = staB;
        }
        private double CalculateCoordinatesDistance(FixedStationInfo staA, FixedStationInfo staB)
        {
            double ToRadians(double degrees)
            {
                return (degrees * (Math.PI / 180));
            }

            double lat1 = ToRadians(staA.Latitude);
            double lat2 = ToRadians(staB.Latitude);

            double deltaLat = ToRadians(lat2 - lat1);
            double deltalong = ToRadians(staB.Longitude - staA.Longitude);

            double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltalong / 2) * Math.Sin(deltalong / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return 6371 * c; 
        }
    }
}
