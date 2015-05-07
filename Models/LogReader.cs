using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Excel;
using FileHelpers;
using System.IO;
using PlexScrobble.Utilities;

namespace PlexScrobble.Models
{
    public class LogReader
    {
        public static string _newLog;
        public static string _oldLog;
        public static string LogCopy = "LogCopy.txt";
        public string BaseUrl = @"http://localhost:32400";
       
        public LogReader(string newLog, string oldLog)
        {
            _newLog = CopyLog(newLog);
            _oldLog = oldLog;
        }

        public void ReadLog()
        {
            FileDiffEngine diff = new FileDiffEngine(typeof(PlexMediaServerLog));
            PlexMediaServerLog[] entry = diff.OnlyNewRecords(_oldLog, _newLog) as PlexMediaServerLog[];
            ParseLogs(entry);
            System.IO.File.Copy(_newLog, _oldLog);
        }

        private string CopyLog(string log)
        {
            try
            {
                File.Copy(log, LogCopy, true);
                return LogCopy;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }
        
        public void ParseLogs(PlexMediaServerLog[] logs)
        {
            var songList = new List<SongEntry>();
            Regex rgx = new Regex(@".*\sDEBUG\s-\sLibrary\sitem\s(\d+)\s'.*'\sgot\splayed\sby\saccount\s(\d+).*");
            foreach (PlexMediaServerLog log in logs)
            {
                if (rgx.IsMatch(log.LogEntry))
                {
                    var line = rgx.Replace(log.LogEntry, "$1,$2");
                    if (line.Length > 0)
                    {
                        var lineArray = line.Split(',');
                        var song = new SongEntry();
                        song.MediaId = Int16.Parse(lineArray[0]);
                        song.UserId = Int16.Parse(lineArray[1]);
                        var date = log.DateAdded.Trim();
                        var format = "MMM dd, yyyy HH:mm:ss:fff";
                        song.TimePlayed = DateTime.ParseExact(date, format, CultureInfo.InvariantCulture);
                        songList.Add(song);
                    }    
                }
                
            }
            if (songList.Count > 0)
            {
                GetSongData(songList);
            }
        }

        public async void GetSongData(List<SongEntry> songList)
        {
            //ToDo: Make this work
            foreach (SongEntry song in songList)
            {
                XDocument docResponse = null;
                var plexUri = BaseUrl + "/library/metadata/" + song.MediaId;
                Task<string> plexCall = GetPlexResponse(plexUri);
                string responseRaw = await plexCall;

                if (responseRaw != null)
                {
                    using (XmlReader reader = XmlReader.Create(new StringReader(responseRaw)))
                    {
                        docResponse = XDocument.Load(reader);
                    }
                }
                var artistName = "";

                if (docResponse != null)
                {
                    XNamespace aw = docResponse.Root.Name.NamespaceName;
                    var artistasin = "";
                    IEnumerable<XElement> items = null;
                    items = (
                        from item in
                            docResponse.ElementOrEmpty(aw, "ItemLookupResponse")
                                .ElementOrEmpty(aw, "Items")
                                .Elements(aw + "Item")
                        select item);
                    foreach (XElement item in items)
                    {
                        artistasin =
                            item.ElementOrEmpty(aw, "RelatedItems")
                                .ElementOrEmpty(aw, "RelatedItem")
                                .ElementOrEmpty(aw, "Item")
                                .ElementOrEmpty(aw, "ASIN").Value;
                        if (artistasin.Length > 0)
                        {
                            return artistasin;
                        }
                    }
                    return artistasin;
                }
            }
        }

        public async Task<string> GetPlexToken()
        {
            var url = "https://plex.tv/users/sign_in.xml";
            var myplexaccount = "sbarrett00";
            var mypassword = "123456";
            var token = "";
            byte[] accountBytes = Encoding.UTF8.GetBytes(myplexaccount +":"+ mypassword);
            var encodedPassword = Convert.ToBase64String(accountBytes);
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Plex-Client-Identifier","PlexScrobble");
                client.DefaultRequestHeaders.Add("Authorization", "Basic " + encodedPassword);
                HttpContent blankcontent = new StringContent("");
                using (HttpResponseMessage response = await client.PostAsync(url, blankcontent))
                using (HttpContent content = response.Content)
                {
                    string result = await content.ReadAsStringAsync();
                    if (result != null)
                    {
                        using (XmlReader reader = XmlReader.Create(new StringReader(result)))
                        {
                            var data = XDocument.Load(reader);
                            token = data.ElementOrEmpty("user").ElementOrEmpty("authentication-token").Value;
                        }
                        
                    }
                }
            }
            return token;
        }

        public async Task<string> GetPlexResponse(string uri)
        {
            using (HttpClient client = new HttpClient())
            {
                var request = new HttpRequestMessage();
                request.RequestUri = new Uri(uri);
                request.Method = HttpMethod.Get;
                request.Headers.Add();
                using (HttpResponseMessage response = await client.GetAsync(uri))
                using (HttpContent content = response.Content)
                {
                    string result = await content.ReadAsStringAsync();
                    return result;
                }
            }
        }
    }
}
