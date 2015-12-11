using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PlaxFm.Core.Utilities;

namespace PlaxFm.Models
{
    public class SongEntry
    {
        public int UserId { get; set; }
        public int MediaId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public DateTime TimePlayed { get; set; }

        public SongEntry()
        {
        }

        public SongEntry(PlexMediaServerLog log)
        {
            Regex rgx = new Regex(@".*\sDEBUG\s-\sLibrary\sitem\s(\d+)\s'.*'\sgot\splayed\sby\saccount\s(\d+).*");
            if (rgx.IsMatch(log.LogEntry))
            {
                var line = rgx.Replace(log.LogEntry, "$1,$2");
                if (line.Length > 0)
                {
                    var lineArray = line.Split(',');
                    MediaId = Int32.Parse(lineArray[0]);
                    UserId = Int32.Parse(lineArray[1]);
                    var date = log.DateAdded.Trim();
                    var format = "MMM dd, yyyy HH:mm:ss:fff";
                    TimePlayed = DateTime.ParseExact(date, format, CultureInfo.InvariantCulture).ToUniversalTime();
                }
            }
        }

        public SongEntry PopulateSongData(SongEntry song, XDocument docResponse)
        {
            if (docResponse != null)
            {
                song.Artist = StringHelper.EncodeToUtf8(docResponse.ElementOrEmpty("MediaContainer")
                    .ElementOrEmpty("Track")
                    .AttributeOrEmpty("originalTitle")
                    .Value);
                if (song.Artist == "")
                {
                    song.Artist = StringHelper.EncodeToUtf8(docResponse.ElementOrEmpty("MediaContainer")
                            .ElementOrEmpty("Track")
                            .AttributeOrEmpty("grandparentTitle")
                            .Value);
                }
                song.Title = StringHelper.EncodeToUtf8(docResponse.ElementOrEmpty("MediaContainer")
                        .ElementOrEmpty("Track")
                        .AttributeOrEmpty("title")
                        .Value);
                song.Album = StringHelper.EncodeToUtf8(docResponse.ElementOrEmpty("MediaContainer")
                        .ElementOrEmpty("Track")
                        .AttributeOrEmpty("parentTitle")
                        .Value);
            }
            return song;
        }
    }
}