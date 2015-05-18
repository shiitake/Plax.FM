using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlexScrobble.Models;

namespace PlexScrobble.Configuration
{
    class InitialConfig
    {
        //This should contain all of the stuff that needs to happen before the service will run correctly
        //1. get plex username from server and store it in config - possibly from logs?
        //2. 
        public void FindPlexUsers()
        {
            var newLog = @"C:\Users\E002796\AppData\Local\Plex Media Server\Logs\Plex Media Server.log";
            //var log = new LogReader();
            //log.ReadLog(newLog);
        }
    }
}
