using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaxFm.Core.Models
{
    public class Configuration
    {
        public bool IsInitialized { get; set; }
        public List<PlexUser> UserList { get; set; }
    }
    public class PlexUser
    {
        public int PlexId { get; set; }
        public string PlexUserName { get; set; }
        public bool IsAuthorized { get; set; }
        public string LastFmUserName { get; set; }
        public string SessionId { get; set; }
        public string Token { get; set; }
        public string PlexToken { get; set; }
    }
}
