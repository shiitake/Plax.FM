using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlexScrobble.Configuration
{
    public interface IAppSettings
    {
        string CronSchedule { get;  }
        string LastFmApiKey { get; }
        string LastFmApiSecret { get; }
    }
}
