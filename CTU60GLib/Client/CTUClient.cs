using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Client.Json;
using Newtonsoft.Json.Linq;
using CTU60GLib.CollisionTable;

namespace CTU60GLib.Client
{
    public class CTUClient
    {
        private HttpClient httpClient;
        private Uri baseAddres = new Uri("https://60ghz.ctu.cz");
        private string frontEndToken ="";

        public CTUClient()
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            httpClient = new HttpClient(handler);
            httpClient.BaseAddress = baseAddres;
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("cs", 0.9));
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("en", 0.6));

        }
        public async Task LoginAsync(string login,string pass)
        {
            HttpResponseMessage response = await httpClient.GetAsync("prihlaseni");
            if(response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            }
            else
            {
                //todo
                throw new NotImplementedException();
            }

            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend",frontEndToken },
                {"LoginForm[email]",login },
                {"LoginForm[password]",pass },
                {"login-button","" }
            };

            response = await httpClient.PostAsync("/cs/prihlaseni", new FormUrlEncodedContent(postContent));

            if (response.StatusCode != HttpStatusCode.OK) { }//todo throw exception
        }
        private async Task<string> ObtainFrontEndToken(Task<Stream> stream)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.Load(await stream);
            HtmlNode mdnode = doc.DocumentNode.SelectNodes("//meta[@name='csrf-token']").FirstOrDefault();

            if (mdnode != null || mdnode == default) { }//todo throw exception

            return mdnode.Attributes["content"].Value;
        }
        private async Task<List<CollisionTableItem>> GetCloseStations(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            List<CollisionTableItem> collisionItems = new List<CollisionTableItem>();
            try
            {
                
                var closeStations = from table in doc.DocumentNode.SelectNodes("//*[@id=\"stations-grid-container\"]/table").Cast<HtmlNode>()
                            from tbody in table.SelectNodes("tbody").Cast<HtmlNode>()
                            from rows in tbody.SelectNodes("tr").Cast<HtmlNode>()
                            select rows;
                foreach (var item in closeStations)
                {
                    var columns = item.SelectNodes("td");
                    string id = columns[1].ChildNodes[0].InnerText;
                    bool owned = columns[1].ChildNodes[1].ChildNodes[0]?.InnerText == "face";
                    string name = columns[2].InnerText;
                    string link = baseAddres.ToString() + columns[2].SelectSingleNode("a").GetAttributeValue("href","");
                    bool collision = columns[3].InnerText == "Yes";
                    string type = columns[4].InnerText;
                    collisionItems.Add(new CollisionTableItem(id, owned, name, collision, type, link));
                }
            }
            catch (Exception e)
            {

                throw;
            }

            return collisionItems;

        }
        public async Task AddPTPConnectionAsync(FixedP2PPair fpP2P)
        {

            HttpResponseMessage response = await httpClient.GetAsync("/en/create-fs");
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            }
            else
            {
                //todo
                throw new NotImplementedException();
            }
            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend", frontEndToken },
                {"list-stations","my" },
                {"Station[a][name]", fpP2P.StationA.Name},
                {"Station[b][name]",fpP2P.StationB.Name},
                {"Station[a][lng]", fpP2P.StationA.Longitude },
                {"Station[a][lat]",fpP2P.StationA.Latitude },
                {"Station[b][lng]",fpP2P.StationB.Longitude },
                {"Station[b][lat]",fpP2P.StationB.Latitude }
            };

            response = await httpClient.PostAsync("/en/create-fs", new FormUrlEncodedContent(postContent));

            if (response.StatusCode != HttpStatusCode.OK) { }//todo throw exception


            response = await httpClient.GetAsync(response.RequestMessage.RequestUri);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            }
            else
            {
                //todo
                throw new NotImplementedException();
            }

            postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend", frontEndToken },
                {"Station[a][updateStep]","2" },
                {"Station[a][antenna_volume]",fpP2P.StationA.Volume },
                {"Station[a][channel_width]",fpP2P.StationA.ChannelWidth},
                {"Station[a][power]",fpP2P.StationA.Power},
                {"StationFs[a][frequency]",fpP2P.StationA.Frequency},
                {"StationFs[a][ratio_signal_interference]","12" }, ///todo
                {"Station[a][hardware_identifier]",(fpP2P.StationA.SerialNumber == string.Empty)?"0":"1"},
                {String.Format("Station[a][{0}]",(fpP2P.StationA.SerialNumber == string.Empty)?"macAddress":"serialNumber"),(fpP2P.StationA.SerialNumber == string.Empty)?fpP2P.StationA.MAC:fpP2P.StationA.SerialNumber},
                {"Station[b][updateStep]","2" },
                {"Station[b][antenna_volume]",fpP2P.StationB.Volume },
                {"Station[b][channel_width]",fpP2P.StationB.ChannelWidth},
                {"Station[b][power]",fpP2P.StationB.Power},
                {"StationFs[b][frequency]",fpP2P.StationB.Frequency},
                {"StationFs[b][ratio_signal_interference]","12" }, ///todo
                {"Station[b][hardware_identifier]",(fpP2P.StationB.SerialNumber == string.Empty)?"0":"1"},
                {String.Format("Station[b][{0}]",(fpP2P.StationB.SerialNumber == string.Empty)?"macAddress":"serialNumber"),(fpP2P.StationB.SerialNumber == string.Empty)?fpP2P.StationB.MAC:fpP2P.StationB.SerialNumber}
            };

                response = await httpClient.PostAsync(response.RequestMessage.RequestUri, new FormUrlEncodedContent(postContent));

            if (response.StatusCode != HttpStatusCode.OK) { }//todo throw exception

            response = await httpClient.GetAsync(response.RequestMessage.RequestUri);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            }
            else
            {
                //todo
                throw new NotImplementedException();
            }
            List<CollisionTableItem> collisions = await GetCloseStations(await response.Content.ReadAsStringAsync());
        }
        public async Task AddWIGIG_PTP_PTMPConnectionAsync(WigigPTMPUnitInfo wigig)
        {
            HttpResponseMessage response = await httpClient.GetAsync("/en/create-wigig");
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            }
            else
            {
                //todo
                throw new NotImplementedException();
            }
            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend", frontEndToken },
                {"list-stations","my" },
                {"Station[name]",wigig.Name },
                {"Station[lng]",wigig.Longitude },
                {"Station[lat]",wigig.Latitude },
                {"StationWigig[direction]",wigig.Orientation }
            };

            response = await httpClient.PostAsync("/en/create-wigig", new FormUrlEncodedContent(postContent));

            if (response.StatusCode != HttpStatusCode.OK) { }//todo throw exception

            response = await httpClient.GetAsync(response.RequestMessage.RequestUri);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            }
            else
            {
                //todo
                throw new NotImplementedException();
            }

            postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend", frontEndToken },
                {"Station[updateStep]","2" },
                {"StationWigig[eirp_method]",(wigig.Eirp == string.Empty)?"auto":"manual"},
                {"StationWigig[eirp]", wigig.Eirp },
                {"Station[antenna_volume]",wigig.Volume },
                {"Station[power]",wigig.Power },
                {"Station[channel_width]",wigig.ChannelWidth },
                {"StationWigig[is_ptmp]",(int.Parse(wigig.Volume) > 25)?"0":"1" },
                {"Station[hardware_identifier]",(wigig.SerialNumber == string.Empty)?"0":"1" },
                {String.Format("Station[{0}]",(wigig.SerialNumber == string.Empty)?"macAddress":"serialNumber"),(wigig.SerialNumber == string.Empty)?wigig.MAC:wigig.SerialNumber}
            };

            response = await httpClient.PostAsync(response.RequestMessage.RequestUri, new FormUrlEncodedContent(postContent));

            if (response.StatusCode != HttpStatusCode.OK) { }//todo throw exception

            response = await httpClient.GetAsync(response.RequestMessage.RequestUri);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            }
            else
            {
                //todo
                throw new NotImplementedException();
            }

            List<CollisionTableItem> collisions = await GetCloseStations(await response.Content.ReadAsStringAsync());
        }
        public async Task DeleteConnectionAsync(string id)
        {
            HttpResponseMessage response = await httpClient.GetAsync("");
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            }
            else
            {
                //todo
                throw new NotImplementedException();
            }

            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend",frontEndToken }
            };

            response = await httpClient.PostAsync($"/en/station/delete?id={id}", new FormUrlEncodedContent(postContent));

            if (response.StatusCode != HttpStatusCode.OK) { }//todo throw exception
        }
        private async Task<List<CtuWirelessUnit>> GetApiStationsAsync(bool onlyMy)
        {
            string param = (onlyMy) ? "my-stations" : "all-stations";

            HttpResponseMessage response = await httpClient.GetAsync($"/api/v1/station/{param}");
            try
            {
                var x = response.Content.ReadAsStringAsync().Result;

                var jObj = JObject.Parse(x).Children().Children().ToList()[1].ToList();

                List<CtuWirelessUnit> wirelessUnits = new List<CtuWirelessUnit>();

                foreach (var item in jObj)
                {
                    wirelessUnits.Add(CtuWirelessUnit.FromJson(item.Children().FirstOrDefault().ToString()));
                }

                return wirelessUnits;
            }
            catch (Exception e)
            {

                throw;
            }

        }
        public async Task<List<CtuWirelessUnit>> GetAllStationsAsync()
        {
            return await GetApiStationsAsync(false);
        }
        public async Task<List<CtuWirelessUnit>> GetMyStationsAsync()
        {
            return await GetApiStationsAsync(true);   
        }


    }
}
