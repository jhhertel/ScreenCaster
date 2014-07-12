using System;
using System.Diagnostics;
using System.Windows.Forms;
using ScreenCasterSystemTray.Properties;
using System.Drawing;
using System.Threading;

namespace ScreenCasterSystemTray
{
	/// <summary>
	/// 
	/// </summary>
	class ContextMenus
	{
		
		/// <summary>
		/// Creates this instance.
		/// </summary>
		/// <returns>ContextMenuStrip</returns>
		public ContextMenuStrip Create()
		{
			// Add the default menu options.
			ContextMenuStrip menu = new ContextMenuStrip();
			ToolStripMenuItem item;
			ToolStripSeparator sep;

			// screencast
			ScreenCasterSystemTray.Program.castItem = new ToolStripMenuItem();
            ScreenCasterSystemTray.Program.castItem.Text = "Screencast to " + ScreenCasterSystemTray.Program.hostName;
            ScreenCasterSystemTray.Program.castItem.Click += new EventHandler(ScreenCast_Click);
			//item.Image = Resources.Explorer;
            menu.Items.Add(ScreenCasterSystemTray.Program.castItem);


			// Separator.
			sep = new ToolStripSeparator();
			menu.Items.Add(sep);

            // set screencast destination
            item = new ToolStripMenuItem();
            item.Text = "Set Screencast Host";
            item.Click += new EventHandler(setScreencastHost_Click);
            //item.Image = Resources.Explorer;
            menu.Items.Add(item);

			// Exit.
			item = new ToolStripMenuItem();
			item.Text = "Exit";
			item.Click += new System.EventHandler(Exit_Click);
			item.Image = Resources.Exit;
			menu.Items.Add(item);

			return menu;
		}

       
        void setScreencastHost_Click(object sender, EventArgs e)
        {
            Form f = new HostEntryForm();
            
            f.ShowDialog();
            
        }

		/// <summary>
		/// Handles the Click event of the Explorer control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		void ScreenCast_Click(object sender, EventArgs e)
		{
            ScreenGrabThread newsgt = new ScreenGrabThread();
            Thread captureThread = new Thread(new ThreadStart(newsgt.run));
            captureThread.Start();
		}

		/// <summary>
		/// Processes a menu item.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		void Exit_Click(object sender, EventArgs e)
		{
			// Quit without further ado.
			Application.Exit();
		}
	}
}