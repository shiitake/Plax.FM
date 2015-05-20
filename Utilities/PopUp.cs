using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Excel;


namespace PlexScrobble.Utilities
{
    public class PopUp
    {
        public bool Message(string api_key, string token)
        {
            var message = "When you click OK your internet browser should open at the Last.FM website and prompt you to authorize PlexScrobble to scrobble your music (login my be required).\n\nOnce completed you should see another pop-up to confirm you've completed authorization.";
            var result = MessageBox.Show(message, "Last.FM Authorization Required", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result == DialogResult.OK)
            {
                var url = "http://www.last.fm/api/auth/?api_key=" + api_key + "&token=" + token;
                ProcessStartInfo sInfo = new ProcessStartInfo(url);
                Process.Start(sInfo);
                Thread.Sleep(5000);
                var followUp = "Click OK once you've completed the Authorization";
                var finished = MessageBox.Show(followUp, "Pending confirmation", MessageBoxButtons.OK,MessageBoxIcon.None);
                if (finished == DialogResult.OK)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
