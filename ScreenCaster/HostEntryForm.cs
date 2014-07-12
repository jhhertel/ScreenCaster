using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ScreenCasterSystemTray
{
    public partial class HostEntryForm : Form
    {
        public HostEntryForm()
        {
            InitializeComponent();
            this.OKButton.Click += new EventHandler(OKButton_Click);
            this.CancelButton.Click += new EventHandler(CancelButton_Click);
        }

        void CancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        void OKButton_Click(object sender, EventArgs e)
        {
            ScreenCasterSystemTray.Program.hostName = this.textBox1.Text;
            ScreenCasterSystemTray.Program.writeHostName(this.textBox1.Text);
            this.Close();
        }
    }
}
