using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileHelpers;

namespace PlaxFm.Models
{
    [DelimitedRecord("[")]
    [IgnoreEmptyLines()]
    [ConditionalRecord(RecordCondition.IncludeIfMatchRegex, @"[aA-zZ]{3}\s[0-9]{2}\,\s")]
    public class PlexMediaServerLog: IComparableRecord
    {
        public string DateAdded;
        public string LogEntry;

        public bool IsEqualRecord(object record)
        {
            PlexMediaServerLog rec = (PlexMediaServerLog)record;
            return this.DateAdded == rec.DateAdded;
        }
    }

    public class SongEntry
    {
        public int UserId { get; set; }
        public int MediaId { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public DateTime TimePlayed { get; set; }
    }
}
