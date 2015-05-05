using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Excel;
using FileHelpers;
using System.IO;

namespace PlexScrobble.Models
{
    public class LogReader
    {
        public static string _newLog;
        public static string _oldLog;
        public static string LogCopy = "LogCopy.txt";
       
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

        public void GetSongData(List<SongEntry> songList)
        {
            //ToDo: Make this work
        }
    }
}
