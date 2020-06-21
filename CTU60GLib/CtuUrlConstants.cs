using System;
using System.Collections.Generic;
using System.Text;

namespace CTU60GLib
{
    internal static class CtuUrlConstants
    {
        internal const string BaseUrl = "https://60ghz.ctu.cz";
        internal const string MainPageUrl = "/en";
        internal const string LoginUrl = MainPageUrl + "/login";
        internal const string LogoutUrl = MainPageUrl + "/logout";
        internal const string CreateFSPTPUrl = MainPageUrl + "/create-fs";
        internal const string CreateWIGIGUrl = MainPageUrl + "/create-wigig";
        internal const string StationUrl = MainPageUrl + "/station";
        internal const string PublishUrl = MainPageUrl + "/publish";
        internal const string DeclareUrl = MainPageUrl + "/declare";
        internal const string DeleteUrl = StationUrl + "/delete";
        internal const string ApiBase = "/api";
        internal const string ApiVersion = "/v1";
        internal const string ApiStationEndpoint = "/station";
        internal const string ApiEndpointTypeConcreteStation = "/station";
        internal const string ApiEndpointTypeMyStations = "/my-stations";
        internal const string ApiEndpointTypeAllStations = "/all-stations";
    }
}
