using System;
using System.Windows.Forms;
using System.IO;


namespace ScreenCasterSystemTray
{
	/// <summary>
	/// 
	/// </summary>
    
    
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
        
        public static string hostName = "";
        public static ToolStripMenuItem castItem = null;
        [STAThread]
        
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

            // first read the current casting host name


            hostName = readHostName();
			// Show the system tray icon.					
			using (ProcessIcon pi = new ProcessIcon())
			{
				pi.Display();

				// Make sure the application runs!
				Application.Run();
                
			}
            System.Environment.Exit(0);
		}

        public static string readHostName() {
            string file = Path.Combine(Environment.GetFolderPath
                      (Environment.SpecialFolder.ApplicationData),
                      "ScreenCast.txt");

            if (File.Exists(file))
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    string host = sr.ReadLine();
                    return host;

                }
            }
            return "NoHostSet";
        }
        public static void writeHostName(string host)
        {
            string file = Path.Combine(Environment.GetFolderPath
                      (Environment.SpecialFolder.ApplicationData),
                      "ScreenCast.txt");

            
             using (StreamWriter sw = new StreamWriter(file, false))
             {
                 sw.WriteLine(host);

             }
        }
	}
}