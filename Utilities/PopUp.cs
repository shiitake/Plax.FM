using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Excel;


namespace PlexScrobble.Utilities
{
    public class PopUp
    {
        public void Message(string api_key, string token)
        {
            var message = "You need to authorize PlexScrobble to scrobble songs for you.";
            var result = MessageBox.Show(message, "Last.FM Authorization Required", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (result == DialogResult.OK)
            {
                var url = "http://www.last.fm/api/auth/?api_key=" + api_key + "&token=" + token;
                ProcessStartInfo sInfo = new ProcessStartInfo(url);
                Process.Start(sInfo);
            }
        }
    }
}
