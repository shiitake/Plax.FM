using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FileHelpers;

namespace PlexScrobble.Models
{
    public class LogReader
    {
        public static string _newLog;
        public static string _oldLog;

        public LogReader(string NewLog, string OldLog)
        {
            _newLog = NewLog;
            _oldLog = OldLog;
        }

        public void ReadLog()
        {
            FileDiffEngine diff = new FileDiffEngine(typeof(PlexMediaServerLog));
            PlexMediaServerLog[] entry = diff.OnlyNewRecords(_oldLog, _newLog) as PlexMediaServerLog[];
            ParseLogs(entry);
            System.IO.File.Copy(_newLog, _oldLog);
        }

        public void ParseLogs(PlexMediaServerLog[] logs)
        {
            var songList = new List<SongEntry>();
            var regEx = @"*\sDEBUG\s-\sLibrary\sitem\s(\d+)\s\'.*\'\sgot\splayed\sby\saccount\s(\d+).*')";
            foreach (PlexMediaServerLog log in logs)
            {
                var line = Regex.Replace(log.LogEntry, regEx, "$1,$2");
                if (line.Length > 0)
                {
                    var lineArray = line.Split(',');
                    var song = new SongEntry();
                    song.MediaId = Int16.Parse(lineArray[0]);
                    song.UserId = Int16.Parse(lineArray[1]);
                    var date = log.DateAdded.Trim();
                    song.TimePlayed = DateTime.Parse(date);
                    songList.Add(song);
                }
            }
            if (songList.Count > 0)
            {
                GetSongData(songList);
            }
        }

        public void GetSongData(List<SongEntry> songList)
        {
            //ToDo: Make this work
        }
    }
}
