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
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace CTU60GLib.Client
{
    public class CTUClient
    {
        private const string baseUrl = "https://60ghz.ctu.cz";
        private const string createFSPTPUrl = "/en/create-fs";
        private const string createWIGIGUrl = "/en/create-wigig";
        private const string stationUrl = "/en/station";
        private const string publishUrl = "/en/publish";
        private HttpClient httpClient;
        private string frontEndToken ="";

        public CTUClient()
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri(baseUrl);
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
            if(response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exceptions.WebServerException();
            }
            frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
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

        #region P2P

        public async Task<RegistrationJournal> AddPTPConnectionAsync(FixedP2PPair fpP2P)
        {
            RegistrationJournal regJournal = new RegistrationJournal();
            if (fpP2P == null)
            {
                regJournal.ThrownException = new Exceptions.MissingParameterException("Missing fixed p2p pair");
                return regJournal;
            }
            regJournal.NextPhase();
            await PTPLocalisation(fpP2P, regJournal);
            if (regJournal.SuccessfullRegistration != RegistrationSuccesEnum.Pending) return regJournal;
            regJournal.NextPhase();
            await PTPTechnicalSpec(fpP2P, regJournal);
            if (regJournal.SuccessfullRegistration != RegistrationSuccesEnum.Pending) return regJournal;
            regJournal.NextPhase();
            await PTPCollisionAndPublishing(fpP2P, regJournal);
            if (regJournal.SuccessfullRegistration != RegistrationSuccesEnum.Pending) return regJournal;
            regJournal.NextPhase();
            return regJournal;
        }
        private async Task PTPLocalisation(FixedP2PPair fpP2P, RegistrationJournal regJournal)
        {
            HttpResponseMessage response = await httpClient.GetAsync(createFSPTPUrl);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException();
                return;
            }
            frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend", frontEndToken },
                {"list-stations","my" },
                {"Station[a][name]", fpP2P.StationA.Name},
                {"Station[b][name]",fpP2P.StationB.Name},
                {"Station[a][lng]", fpP2P.StationA.Longitude.ToString() },
                {"Station[a][lat]",fpP2P.StationA.Latitude.ToString() },
                {"Station[b][lng]",fpP2P.StationB.Longitude.ToString() },
                {"Station[b][lat]",fpP2P.StationB.Latitude.ToString() }
            };

            response = await httpClient.PostAsync(createFSPTPUrl, new FormUrlEncodedContent(postContent));

            if (response.StatusCode != HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException();
                return;
            }
            regJournal.RegistrationId = response.RequestMessage.RequestUri.ToString().Split('/')[5];
        }
        private async Task PTPTechnicalSpec(FixedP2PPair fpP2P, RegistrationJournal regJournal)
        {
            string techSpecUrl = $"{stationUrl}/{regJournal.RegistrationId}/2";
            HttpResponseMessage response = await httpClient.GetAsync(techSpecUrl);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException();
                return;
            }

            frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend", frontEndToken },
                {"Station[a][updateStep]","2" },
                {"Station[a][antenna_volume]",fpP2P.StationA.Volume.ToString() },
                {"Station[a][channel_width]",fpP2P.StationA.ChannelWidth.ToString()},
                {"Station[a][power]",fpP2P.StationA.Power.ToString()},
                {"StationFs[a][frequency]",fpP2P.StationA.Frequency.ToString()},
                {"StationFs[a][ratio_signal_interference]",fpP2P.StationA.RSN.ToString() },
                {"Station[a][hardware_identifier]",(fpP2P.StationA.SerialNumber == string.Empty)?"0":"1"},
                {String.Format("Station[a][{0}]",(fpP2P.StationA.SerialNumber == string.Empty)?"macAddress":"serialNumber"),(fpP2P.StationA.SerialNumber == string.Empty)?fpP2P.StationA.MAC:fpP2P.StationA.SerialNumber},
                {"Station[b][updateStep]","2" },
                {"Station[b][antenna_volume]",fpP2P.StationB.Volume.ToString() },
                {"Station[b][channel_width]",fpP2P.StationB.ChannelWidth.ToString()},
                {"Station[b][power]",fpP2P.StationB.Power.ToString()},
                {"StationFs[b][frequency]",fpP2P.StationB.Frequency.ToString()},
                {"StationFs[b][ratio_signal_interference]",fpP2P.StationB.RSN.ToString() },
                {"Station[b][hardware_identifier]",(fpP2P.StationB.SerialNumber == string.Empty)?"0":"1"},
                {String.Format("Station[b][{0}]",(fpP2P.StationB.SerialNumber == string.Empty)?"macAddress":"serialNumber"),(fpP2P.StationB.SerialNumber == string.Empty)?fpP2P.StationB.MAC:fpP2P.StationB.SerialNumber}
            };

            response = await httpClient.PostAsync(response.RequestMessage.RequestUri, new FormUrlEncodedContent(postContent));

            if (response.StatusCode != HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException();
            }
        }
        private async Task PTPCollisionAndPublishing(FixedP2PPair fpP2P, RegistrationJournal regJournal)
        {
            string techSpecUrl = $"{stationUrl}/{regJournal.RegistrationId}/3";
            HttpResponseMessage response = await httpClient.GetAsync(techSpecUrl);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException();
                return;
            }
            frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());

            List<CollisionTableItem> closeStations = await GetFixedPTPStationsFromCollisionTable(await response.Content.ReadAsStringAsync());
            regJournal.CollisionStations = closeStations.Where(x => x.Collision == true).ToList();
            regJournal.CloseStations = closeStations.Where(x => x.Collision == false).ToList();

            if (regJournal.CollisionStations.Count > 0)
            {
                regJournal.ThrownException = new Exceptions.CollisionDetectedException();
                return;
            }

            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend", frontEndToken }
            };
            string publish = $"{publishUrl}/{regJournal.RegistrationId}"; 
            response = await httpClient.PostAsync(publish, new FormUrlEncodedContent(postContent));

            if (response.StatusCode != HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException();
            }

        }
        private async Task<List<CollisionTableItem>> GetFixedPTPStationsFromCollisionTable(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            List<CollisionTableItem> collisionItems = new List<CollisionTableItem>();
            var closeStations = from table in doc.DocumentNode.SelectNodes("//*[@id=\"stations-grid-container\"]/table").Cast<HtmlNode>()
                                from tbody in table.SelectNodes("tbody").Cast<HtmlNode>()
                                from rows in tbody.SelectNodes("tr").Cast<HtmlNode>()
                                select rows;
            foreach (var item in closeStations)
            {
                var columns = item.SelectNodes("td");
                if (item.InnerText == "No results found.") break;
                string id = columns[1].ChildNodes[0].InnerText;

                bool owned = (columns[1].ChildNodes.Count == 2); // another element symbolizes users ownership.
                string name = columns[2].InnerText;
                string link = baseUrl + columns[2].SelectSingleNode("a").GetAttributeValue("href", "");
                bool collision = columns[3].InnerText == "Yes";
                string type = columns[4].InnerText;
                collisionItems.Add(new CollisionTableItem(id, owned, name, collision, type, link));
            }

            return collisionItems;

        }

        #endregion

        #region WIGIG
        public async Task<RegistrationJournal> AddWIGIG_PTP_PTMPConnectionAsync(WigigPTMPUnitInfo wigig)
        {
            RegistrationJournal regJournal = new RegistrationJournal();
            if (wigig == null)
            {
                regJournal.ThrownException = new Exceptions.MissingParameterException("Missing wigig station");
                return regJournal;
            }
            regJournal.NextPhase();
            await WigigLocalisation(wigig, regJournal);
            if (regJournal.SuccessfullRegistration != RegistrationSuccesEnum.Pending) return regJournal;
            regJournal.NextPhase();
            await WigigTechnicalSpec(wigig, regJournal);
            if (regJournal.SuccessfullRegistration != RegistrationSuccesEnum.Pending) return regJournal;
            regJournal.NextPhase();
            await WigigCollisionAndPublishing(wigig, regJournal);
            if (regJournal.SuccessfullRegistration != RegistrationSuccesEnum.Pending) return regJournal;
            regJournal.NextPhase();
            return regJournal;
        }
        private async Task WigigLocalisation(WigigPTMPUnitInfo wigig, RegistrationJournal regJournal)
        {
            HttpResponseMessage response = await httpClient.GetAsync(createWIGIGUrl);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException();
                return;
            }

            frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend", frontEndToken },
                {"list-stations","my" },
                {"Station[name]",wigig.Name },
                {"Station[lng]",wigig.Longitude.ToString() },
                {"Station[lat]",wigig.Latitude.ToString() },
                {"StationWigig[direction]",wigig.Orientation.ToString() }
            };

            response = await httpClient.PostAsync(createWIGIGUrl, new FormUrlEncodedContent(postContent));

            if (response.StatusCode != HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException();
            }
            regJournal.RegistrationId = response.RequestMessage.RequestUri.ToString().Split('/')[5];
        }
        private async Task WigigTechnicalSpec(WigigPTMPUnitInfo wigig, RegistrationJournal regJournal)
        {
            string techSpecUrl = $"{stationUrl}/{regJournal.RegistrationId}/2";
            HttpResponseMessage response = await httpClient.GetAsync(techSpecUrl);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException();
                return;
            }
            frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend", frontEndToken },
                {"Station[updateStep]","2" },
                {"StationWigig[eirp_method]",(wigig.Eirp == null)?"auto":"manual"},
                {"StationWigig[eirp]", wigig.Eirp.ToString() },
                {"Station[antenna_volume]",wigig.Volume.ToString() },
                {"Station[power]",wigig.Power.ToString() },
                {"Station[channel_width]",wigig.ChannelWidth.ToString() },
                {"StationWigig[is_ptmp]",(int.Parse(wigig.Volume.ToString()) > 25)?"0":"1" },
                {"Station[hardware_identifier]",(wigig.SerialNumber == string.Empty)?"0":"1" },
                {String.Format("Station[{0}]",(wigig.SerialNumber == string.Empty)?"macAddress":"serialNumber"),(wigig.SerialNumber == string.Empty)?wigig.MAC:wigig.SerialNumber}
            };

            response = await httpClient.PostAsync(response.RequestMessage.RequestUri, new FormUrlEncodedContent(postContent));

            if (response.StatusCode != HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException();
                return;
            }
            string pp = "pp";
            string kk = "pp";

            var q = pp.GetHashCode();
            var qq = kk.GetHashCode();
        }
        private async Task WigigCollisionAndPublishing(WigigPTMPUnitInfo wigig, RegistrationJournal regJournal)
        {
            string collUrl = $"{stationUrl}/{regJournal.RegistrationId}/3";
            HttpResponseMessage response = await httpClient.GetAsync(collUrl);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException();
            }
            frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            List<CollisionTableItem> collisions = await GetWigigStationsFromCollisionTable(await response.Content.ReadAsStringAsync());
            regJournal.CollisionStations = collisions.Where(x => x.Collision == true).ToList();
            regJournal.CloseStations = collisions.Where(x => x.Collision == false).ToList();

            if (regJournal.CollisionStations.Count > 0)
            {
                regJournal.ThrownException = new Exceptions.CollisionDetectedException(); 
                return;
            }

            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend", frontEndToken }
            };
            string publish = $"{publishUrl}/{regJournal.RegistrationId}";
            response = await httpClient.PostAsync(publish, new FormUrlEncodedContent(postContent));

            if (response.StatusCode != HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException();
            }
        }
        private async Task<List<CollisionTableItem>> GetWigigStationsFromCollisionTable(string html)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(html);

            List<CollisionTableItem> collisionItems = new List<CollisionTableItem>();
            var closeStations = from table in doc.DocumentNode.SelectNodes("//*[@id=\"stations-grid-container\"]/table").Cast<HtmlNode>()
                                from tbody in table.SelectNodes("tbody").Cast<HtmlNode>()
                                from rows in tbody.SelectNodes("tr").Cast<HtmlNode>()
                                select rows;
            foreach (var item in closeStations)
            {
                if (item.InnerText == "No results found.") break;
                var columns = item.SelectNodes("td");
                string id = columns[0].ChildNodes[0].InnerText;
                bool owned = (columns[0].ChildNodes.Count == 2); // another element symbolizes users ownership.
                string name = columns[2].InnerText;
                string link = baseUrl + columns[2].SelectSingleNode("a").GetAttributeValue("href", "");
                bool collision = columns[3].InnerText == "Yes";
                string type = columns[1].InnerText;
                collisionItems.Add(new CollisionTableItem(id, owned, name, collision, type, link));
            }

            return collisionItems;
        }
        #endregion
        public async Task DeleteConnectionAsync(string id)
        {
            HttpResponseMessage response = await httpClient.GetAsync("");
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exceptions.WebServerException();
            }
            frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
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
