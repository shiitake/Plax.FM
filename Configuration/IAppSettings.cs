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
        string LogCache { get;  }
        string PlexLog { get; }
        string WorkingCopy { get;  }
        string ConfigFile { get;  }
        string SchemaFile { get;  }
        string PlexServer { get;  }
    }
}
