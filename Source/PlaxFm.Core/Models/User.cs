using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlaxFm.Core.Models
{
    public class User
    {
        public int PlexId { get; set; }
        public string PlexUsername { get; set; }
        public string LastFmUsername { get; set; }
        public string SessionId { get; set; }
        public string Token { get; set; }
        public bool IsAuthorized { get; set; }
        public string PlexToken { get; set; }
    }
}
