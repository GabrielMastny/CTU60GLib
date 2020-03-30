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

            response = await httpClient.PostAsync("prihlaseni", new FormUrlEncodedContent(postContent));

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

        public async Task<List<CtuWirelessUnit>> GetMyStationsAsync()
        {
            HttpResponseMessage response = await httpClient.GetAsync("/api/v1/station/my-stations");
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


    }
}
