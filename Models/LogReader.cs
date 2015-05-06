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

        public string GetPlexToken()
        {
            //$BB = [System.Text.Encoding]::UTF8.GetBytes("myplexaccount:mypassword")
            //$EncodedPassword = [System.Convert]::ToBase64String($BB)
            //$headers = @{}
            //$headers.Add("Authorization","Basic $($EncodedPassword)") | out-null
            //$headers.Add("X-Plex-Client-Identifier","TESTSCRIPTV1") | Out-Null
            //$headers.Add("X-Plex-Product","Test script") | Out-Null
            //$headers.Add("X-Plex-Version","V1") | Out-Null
            //[xml]$res = Invoke-RestMethod -Headers:$headers -Method Post -Uri:$url
            //$token = $res.user.authenticationtoken
        }


        public async Task<string> GetPlexResponse(string uri)
        {
            using (HttpClient client = new HttpClient())
            using (HttpResponseMessage response = await client.GetAsync(uri))
            using (HttpContent content = response.Content)
            {
                string result = await content.ReadAsStringAsync();
                return result;
            }
        }
    }
}
