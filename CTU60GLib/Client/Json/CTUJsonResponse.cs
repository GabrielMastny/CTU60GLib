namespace Client.Json
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class CtuWirelessUnit
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("lat")]
        public string Lat { get; set; }

        [JsonProperty("lng")]
        public string Lng { get; set; }

        [JsonProperty("id_station_pair")]
        public string IdStationPair { get; set; }

        [JsonProperty("id_master")]
        public string IdMaster { get; set; }

        [JsonProperty("id_m")]
        public string IdM { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id_user")]
        public string IdUser { get; set; }

        [JsonProperty("pair_position")]
        public string PairPosition { get; set; }

        [JsonProperty("typeName")]
        public string TypeName { get; set; }
    }

    public partial class CtuWirelessUnit
    {
        public static CtuWirelessUnit FromJson(string json) => JsonConvert.DeserializeObject<CtuWirelessUnit>(json, Client.Json.Converter.Settings);
    }

    public static class Serialize
    {
        public static string ToJson(this CtuWirelessUnit self) => JsonConvert.SerializeObject(self, Client.Json.Converter.Settings);
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
            },
        };
    }
}
