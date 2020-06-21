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
using System.Globalization;
using System.Text.RegularExpressions;
using CTU60GLib.Exceptions;

namespace CTU60GLib.Client
{
    public class CTUClient : IDisposable
    {
        
        private HttpClient httpClient;
        private string frontEndToken ="";
        private bool isolationConsent = false;
        /// <summary>
        /// webclient for http comunication with web 60ghz.ctu.cz, automatically calls login method.
        /// </summary>
        /// <param name="login">username for login form</param>
        /// <param name="pass">password for login form</param>
        /// <param name="isolationConsent">if true, client sends automaticaly declaration of isolation for wirelless site in collission, otherwise is sent email with collision table</param>
        public CTUClient(string login, string pass,bool isolationConsent = false) : this()
        {
            Task.Run(() => this.LoginAsync(login, pass)).Wait();
            this.isolationConsent = isolationConsent;
        }
        /// <summary>
        /// webclient for http comunication with web 60ghz.ctu.cz
        /// </summary>
        public CTUClient()
        {
            httpClient = ConfigureHttpClient();
        }

        /// <summary>
        /// Configures httpClient for comunication with ctu web.
        /// </summary>
        /// <returns>configured httpClient</returns>
        private HttpClient ConfigureHttpClient()
        {
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            HttpClient httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri(CtuUrlConstants.BaseUrl);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
            //httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("cs", 0.9));
            httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("en", 0.6));
            return httpClient;
        }
        /// <summary>
        /// Logs in to ctu web via given credentials and save cookies to httpclient.
        /// </summary>
        /// <param name="login">username for login form</param>
        /// <param name="pass">password for login form</param>
        public async Task LoginAsync(string login,string pass)
        {
            HttpResponseMessage response = await httpClient.GetAsync(CtuUrlConstants.LoginUrl);
            if(response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exceptions.WebServerException("Error Ocured while logingin.",response.StatusCode);
            }
            frontEndToken = ObtainFrontEndToken(response.Content.ReadAsStreamAsync()).Result;
            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend",frontEndToken },
                {"LoginForm[email]",login },
                {"LoginForm[password]",pass },
                {"login-button","" }
            };

            response = httpClient.PostAsync(CtuUrlConstants.LoginUrl, new FormUrlEncodedContent(postContent)).Result;
            if (response.RequestMessage.RequestUri.ToString() == (CtuUrlConstants.BaseUrl + CtuUrlConstants.LoginUrl))
            {
                throw new Exceptions.InvalidMailOrPasswordException();
            } else if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exceptions.WebServerException("Ctu web did not respond correctly", response.StatusCode);
            }
            else if(response.RequestMessage.RequestUri.ToString() != CtuUrlConstants.BaseUrl + CtuUrlConstants.MainPageUrl)
            {
                throw new Exceptions.WebServerException("Ctu web did not respond correctly", response.StatusCode);
            }
        }
        /// <summary>
        /// Logs out user from ctu web
        /// </summary>
        /// <returns></returns>
        public async Task Logout()
        {
            
            HttpResponseMessage response = await httpClient.GetAsync("");
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exceptions.WebServerException("ctu web did not respond correctly",response.StatusCode);
            }
            frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend",frontEndToken }
            };
            response = await httpClient.PostAsync(CtuUrlConstants.LogoutUrl, new FormUrlEncodedContent(postContent));
            if (response.StatusCode != HttpStatusCode.OK || response.RequestMessage.RequestUri.ToString() != CtuUrlConstants.BaseUrl + CtuUrlConstants.MainPageUrl)
            {
                throw new Exceptions.WebServerException("ctu web did not respond correctly", response.StatusCode);
            }
        }
        /// <summary>
        /// obtains cross site request forgery token from source code
        /// </summary>
        /// <param name="stream">stream with source code from ctu web</param>
        /// <returns>csrf token</returns>
        private async Task<string> ObtainFrontEndToken(Task<Stream> stream)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.Load(await stream);
            HtmlNode mdnode = doc.DocumentNode.SelectNodes("//meta[@name='csrf-token']").FirstOrDefault();
            if (mdnode == null || mdnode == default) { throw new WebServerException("missing csrf token");}
            return mdnode.Attributes["content"].Value;
        }

        #region P2P
        /// <summary>
        /// attempts to publish new p2p wirelless site.
        /// </summary>
        /// <param name="p2P">p2p data</param>
        /// <returns>publication journal, marks if publication was succesful or where and why publication stopped</returns>
        public async Task<RegistrationJournal> AddPTPSiteAsync(P2PSite p2P)
        {
            RegistrationJournal regJournal = new RegistrationJournal();
            if (p2P == null)
            {
                regJournal.ThrownException = new Exceptions.MissingParameterException("Missing fixed p2p pair");
                return regJournal;
            }
            regJournal.NextPhase();
            await PTPLocalisation(p2P, regJournal);
            if (regJournal.SuccessfullRegistration != RegistrationSuccesEnum.Pending) return regJournal;
            regJournal.NextPhase();
            await PTPTechnicalSpec(p2P, regJournal);
            if (regJournal.SuccessfullRegistration != RegistrationSuccesEnum.Pending) return regJournal;
            regJournal.NextPhase();
            await PTPCollisionAndPublishing(p2P, regJournal);
            if (regJournal.SuccessfullRegistration != RegistrationSuccesEnum.Pending) return regJournal;
            regJournal.NextPhase();
            return regJournal;
        }
        /// <summary>
        /// if succesfull, creates draft with name and gps cordinates.
        /// </summary>
        /// <param name="p2P">p2p data</param>
        /// <param name="regJournal"> registration journal with information abou registration process</param>
        /// <returns></returns>
        private async Task PTPLocalisation(P2PSite p2P, RegistrationJournal regJournal)
        {
            HttpResponseMessage response = await httpClient.GetAsync(CtuUrlConstants.CreateFSPTPUrl);
            if (response.StatusCode != System.Net.HttpStatusCode.OK || response.RequestMessage.RequestUri.LocalPath != CtuUrlConstants.CreateFSPTPUrl)
            {
                regJournal.ThrownException = new Exceptions.WebServerException("Web ctu did not respond properly.",response.StatusCode);
                return;
            }
            frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend", frontEndToken },
                {"list-stations","my" },
                {"Station[a][name]", p2P.StationA.Name},
                {"Station[b][name]",p2P.StationB.Name},
                {"Station[a][lng]", p2P.StationA.Longitude.ToString(CultureInfo.GetCultureInfo("en-US")) },
                {"Station[a][lat]",p2P.StationA.Latitude.ToString(CultureInfo.GetCultureInfo("en-US")) },
                {"Station[b][lng]",p2P.StationB.Longitude.ToString(CultureInfo.GetCultureInfo("en-US")) },
                {"Station[b][lat]",p2P.StationB.Latitude.ToString(CultureInfo.GetCultureInfo("en-US")) }
            };
            response = await httpClient.PostAsync(CtuUrlConstants.CreateFSPTPUrl, new FormUrlEncodedContent(postContent));
            if (response.StatusCode != HttpStatusCode.OK || !new Regex(@"^/en/station/[0-9]+/2$").IsMatch(response.RequestMessage.RequestUri.LocalPath))
            {
                regJournal.ThrownException = new Exceptions.WebServerException("web ctu did not respond properly.",response.StatusCode);
                return;
            }
            regJournal.RegistrationId = response.RequestMessage.RequestUri.ToString().Split('/')[5];
        }
        /// <summary>
        /// if succesfull, updates draft with technical specs
        /// </summary>
        /// <param name="fpP2P">fixed p2p data</param>
        /// <param name="regJournal"> registration journal with information abou registration process</param>
        /// <returns></returns>
        private async Task PTPTechnicalSpec(P2PSite fpP2P, RegistrationJournal regJournal)
        {
            string techSpecUrl = $"{CtuUrlConstants.StationUrl}/{regJournal.RegistrationId}/2";
            HttpResponseMessage response = await httpClient.GetAsync(techSpecUrl);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException("Web ctu did not respond properly.", response.StatusCode);
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
                regJournal.ThrownException = new Exceptions.WebServerException("Web ctu did not respond properly.", response.StatusCode);
            }
        }
        /// <summary>
        /// if succesfull, then p2p is officialy published on ctu, otherwise saved as draft
        /// </summary>
        /// <param name="fpP2P"></param>
        /// <param name="regJournal"></param>
        /// <returns></returns>
        private async Task PTPCollisionAndPublishing(P2PSite fpP2P, RegistrationJournal regJournal)
        {
            string techSpecUrl = $"{CtuUrlConstants.StationUrl}/{regJournal.RegistrationId}/3";
            HttpResponseMessage response = await httpClient.GetAsync(techSpecUrl);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException("Web ctu did not respond properly.", response.StatusCode);
                return;
            }
            frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());

            List<CollisionTableItem> closeStations = await GetFixedPTPStationsFromCollisionTable(await response.Content.ReadAsStringAsync());
            regJournal.CollisionStations = closeStations.Where(x => x.Collision == true).ToList();
            regJournal.CloseStations = closeStations;

            Dictionary<string, string> postContent = new Dictionary<string, string>()
                {
                    {"_csrf-frontend", frontEndToken }
                };
            if (regJournal.CollisionStations.Where((x) => x.Owned == true).Count() == regJournal.CollisionStations.Count && isolationConsent) //collision only with owned stations.
            {
                
                string declare = $"{CtuUrlConstants.DeclareUrl}/{regJournal.RegistrationId}";
                response = await httpClient.PostAsync(declare, new FormUrlEncodedContent(postContent));
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    regJournal.ThrownException = new Exceptions.WebServerException("Web ctu did not respond properly.", response.StatusCode);
                }
            }
            else if(regJournal.CollisionStations.Count > 0) // collision list contains unowned stations
            {
                regJournal.ThrownException = new Exceptions.CollisionDetectedException();
                return;
            }
            else // no collision ocured
            {
                string publish = $"{CtuUrlConstants.PublishUrl}/{regJournal.RegistrationId}";
                response = await httpClient.PostAsync(publish, new FormUrlEncodedContent(postContent));
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    regJournal.ThrownException = new Exceptions.WebServerException("Web ctu did not respond properly.", response.StatusCode);
                }
            }
        }
        /// <summary>
        /// updates already exisiting p2p record
        /// </summary>
        /// <param name="p2P">p2p data</param>
        /// <returns>publication journal, marks if publication was succesful or where and why publication stopped</returns>
        public async Task<RegistrationJournal> UpdatePTPConnectionAsync(P2PSite p2P)
        {
            RegistrationJournal regJournal = new RegistrationJournal();
            if (p2P == null)
            {
                regJournal.ThrownException = new Exceptions.MissingParameterException("Missing fixed p2p pair");
                return regJournal;
            }
            regJournal.RegistrationId = p2P.StationB.CTUId.ToString();
            regJournal.NextPhase();
            HttpResponseMessage response = await httpClient.GetAsync("");
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException("Web ctu did not respond properly.", response.StatusCode);
                return regJournal;
            }
            frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            await PTPTechnicalSpec(p2P, regJournal);
            if (regJournal.SuccessfullRegistration != RegistrationSuccesEnum.Pending) return regJournal;
            regJournal.NextPhase();
            await PTPCollisionAndPublishing(p2P, regJournal);
            if (regJournal.SuccessfullRegistration != RegistrationSuccesEnum.Pending) return regJournal;
            regJournal.NextPhase();
            return regJournal;
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
                string link = CtuUrlConstants.BaseUrl + columns[2].SelectSingleNode("a").GetAttributeValue("href", "");
                bool collision = columns[3].InnerText == "Yes";
                string type = columns[4].InnerText;
                collisionItems.Add(new CollisionTableItem(id, owned, name, collision, type, link));
            }

            return collisionItems;

        }

        #endregion

        #region WIGIG
        /// <summary>
        ///  Attempts to publish wigig site
        /// </summary>
        /// <param name="wigig">wigig data</param>
        /// <returns>publication journal, marks if publication was succesful or where and why publication stopped</returns>
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
        /// <summary>
        /// if succesfull, creates draft with name and gps cordinates.
        /// </summary>
        /// <param name="wigig">wigig data</param>
        /// <param name="regJournal"> registration journal with information abou registration process</param>
        /// <returns></returns>
        private async Task WigigLocalisation(WigigPTMPUnitInfo wigig, RegistrationJournal regJournal)
        {
            HttpResponseMessage response = await httpClient.GetAsync(CtuUrlConstants.CreateWIGIGUrl);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException("Web ctu did not respond properly.", response.StatusCode);
                return;
            }

            frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend", frontEndToken },
                {"list-stations","my" },
                {"Station[name]",wigig.Name },
                {"Station[lng]",wigig.Longitude.ToString(CultureInfo.GetCultureInfo("en-US")) },
                {"Station[lat]",wigig.Latitude.ToString(CultureInfo.GetCultureInfo("en-US")) },
                {"StationWigig[direction]",wigig.Orientation.ToString() }
            };

            response = await httpClient.PostAsync(CtuUrlConstants.CreateWIGIGUrl, new FormUrlEncodedContent(postContent));
            if (response.StatusCode != HttpStatusCode.OK || !new Regex(@"^/en/station/[0-9]+/2$").IsMatch(response.RequestMessage.RequestUri.LocalPath))
            {
                regJournal.ThrownException = new Exceptions.WebServerException("Web ctu did not respond properly.", response.StatusCode);
            }
            regJournal.RegistrationId = response.RequestMessage.RequestUri.ToString().Split('/')[5];
        }
        /// <summary>
        /// if succesfull, creates draft with name, gps cordinates and azimut.
        /// </summary>
        /// <param name="wigig">wigig data</param>
        /// <param name="regJournal"> registration journal with information abou registration process</param>
        /// <returns></returns>
        private async Task WigigTechnicalSpec(WigigPTMPUnitInfo wigig, RegistrationJournal regJournal)
        {
            string techSpecUrl = $"{CtuUrlConstants.StationUrl}/{regJournal.RegistrationId}/2";
            HttpResponseMessage response = await httpClient.GetAsync(techSpecUrl);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException("Web ctu did not respond properly.", response.StatusCode);
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
                regJournal.ThrownException = new Exceptions.WebServerException("Web ctu did not respond properly.", response.StatusCode);
                return;
            }
        }
        /// <summary>
        /// if succesfull, then wigig is officialy published on ctu, otherwise saved as draft
        /// </summary>
        /// <param name="wigig"></param>
        /// <param name="regJournal"></param>
        /// <returns></returns>
        private async Task WigigCollisionAndPublishing(WigigPTMPUnitInfo wigig, RegistrationJournal regJournal)
        {
            string collUrl = $"{CtuUrlConstants.StationUrl}/{regJournal.RegistrationId}/3";
            HttpResponseMessage response = await httpClient.GetAsync(collUrl);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                regJournal.ThrownException = new Exceptions.WebServerException("Web ctu did not respond properly.", response.StatusCode);
            }
            frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            List<CollisionTableItem> collisions = await GetWigigStationsFromCollisionTable(await response.Content.ReadAsStringAsync());
            regJournal.CollisionStations = collisions.Where(x => x.Collision == true).ToList();
            regJournal.CloseStations = collisions.Where(x => x.Collision == false).ToList();

            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend", frontEndToken }
            };
            if (regJournal.CollisionStations.Where((x) => x.Owned == true).Count() == regJournal.CollisionStations.Count && isolationConsent) //collision only with owned stations.
            {
                string declare = $"{CtuUrlConstants.DeclareUrl}/{regJournal.RegistrationId}";
                response = await httpClient.PostAsync(declare, new FormUrlEncodedContent(postContent));
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    regJournal.ThrownException = new Exceptions.WebServerException("Web ctu did not respond properly.", response.StatusCode);
                }
            }
            else if (regJournal.CollisionStations.Count > 0) // collision list contains unowned stations
            {
                regJournal.ThrownException = new Exceptions.CollisionDetectedException();
                return;
            }
            else // no collision ocured
            {
                string publish = $"{CtuUrlConstants.PublishUrl}/{regJournal.RegistrationId}";
                response = await httpClient.PostAsync(publish, new FormUrlEncodedContent(postContent));
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    regJournal.ThrownException = new Exceptions.WebServerException("Web ctu did not respond properly.", response.StatusCode);
                }
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
                string link = CtuUrlConstants.BaseUrl + columns[2].SelectSingleNode("a").GetAttributeValue("href", "");
                bool collision = columns[3].InnerText == "Yes";
                string type = columns[1].InnerText;
                collisionItems.Add(new CollisionTableItem(id, owned, name, collision, type, link));
            }

            return collisionItems;
        }
        #endregion
        /// <summary>
        /// deletes record from web
        /// </summary>
        /// <param name="id">id of record from ctu web</param>
        /// <returns></returns>
        public async Task DeleteConnectionAsync(string id)
        {
            HttpResponseMessage response = await httpClient.GetAsync("");
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exceptions.WebServerException("",response.StatusCode);
            }
            frontEndToken = await ObtainFrontEndToken(response.Content.ReadAsStreamAsync());
            Dictionary<string, string> postContent = new Dictionary<string, string>()
            {
                {"_csrf-frontend",frontEndToken }
            };

            response = await httpClient.PostAsync($"{CtuUrlConstants.DeleteUrl}?id={id}", new FormUrlEncodedContent(postContent));

            if (response.StatusCode != HttpStatusCode.OK) 
            {
                throw new Exceptions.WebServerException("Web ctu did not respond properly.", response.StatusCode);
            }
        }
        /// <summary>
        /// gets metadata about ctu sites
        /// </summary>
        /// <param name="onlyMy">if true return only users meta, otherwise returns all meta</param>
        /// <param name="id">id of record from ctu web</param>
        /// <returns>list of serialized json</returns>
        private async Task<List<CtuWirelessUnit>> GetApiStationsAsync(bool onlyMy = true, string id = default)
        {
            //needs review!!
            string param = default;
            if (id != default)
                param = CtuUrlConstants.ApiEndpointTypeConcreteStation + $"/{id}";
            else
            param = onlyMy ? CtuUrlConstants.ApiEndpointTypeMyStations : CtuUrlConstants.ApiEndpointTypeAllStations;

            var t = CtuUrlConstants.ApiBase + CtuUrlConstants.ApiVersion + CtuUrlConstants.ApiStationEndpoint + param;

            HttpResponseMessage response = await httpClient.GetAsync(CtuUrlConstants.ApiBase + CtuUrlConstants.ApiVersion + CtuUrlConstants.ApiStationEndpoint + param);
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

                throw e;
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
        public async Task<List<CtuWirelessUnit>> GetStationByIdAsync(string id)
        {
            return await GetApiStationsAsync(id: id);
        }
        public void Dispose()
        {
            Task.Run(() => this.Logout()).Wait();
        }
    }
}
