﻿using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using PlaxFm.Core.CustomExceptions;

namespace PlaxFm.Core.Utilities
{
    public class PopUp
    {
        public bool Message(string api_key, string token)
        {
            var message = "When you click OK your internet browser should open at the Last.FM website and prompt you to authorize PlexScrobble to scrobble your music (login my be required).\n\nOnce completed you should see another pop-up to confirm you've completed authorization.";
            var result = MessageBox.Show(message, "Last.FM Authorization Required", MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            if (result == DialogResult.OK)
            {
                var url = "http://www.last.fm/api/auth/?api_key=" + api_key + "&token=" + token;
                ProcessStartInfo sInfo = new ProcessStartInfo(url);
                Process.Start(sInfo);
                Thread.Sleep(10000);
                var followUp = "Click OK once you've completed the Authorization";
                var finished = MessageBox.Show(followUp, "Pending confirmation", MessageBoxButtons.OK,MessageBoxIcon.None);
                if (finished == DialogResult.OK)
                {
                    return true;
                }
                throw new IncompleteAuthorization();
            }
            throw new IncompleteAuthorization();
        }

        public void Message(string message)
        {
            var result = MessageBox.Show(message, "Status");
        }
    }
}
