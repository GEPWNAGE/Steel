using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Steel_2._0
{
	class Exe
	{
		public string file;
		public string icon;
		public string title;


        public void createShortcut(string gamePath)
        {
            string deskDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            try
            {
                StreamWriter writer = new StreamWriter(deskDir + "\\" + title + ".url");
                string app = gamePath + file;
                writer.WriteLine("[InternetShortcut]");
                writer.WriteLine("URL=file:///" + app);
                writer.WriteLine("IconIndex=0");
                string icon = app.Replace('\\', '/');
                writer.WriteLine("IconFile=" + icon);
                writer.Flush();
            }
            catch
            {
                // failed
            }
        }
	}

}
