using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlaxFm.SystemTray
{
    public partial class PlaxConfig : Form
    {
        public string[] UserInfo;
        public PlaxConfig()
        {
            InitializeComponent();
            UserInfo = new string[2];
        }

        private void submitButton_Click(object sender, EventArgs e)
        {
            UserInfo[0] = userNameBox.Text;
            UserInfo[1] = passwordBox.Text;
            this.DialogResult = DialogResult.OK;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void passwordBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
