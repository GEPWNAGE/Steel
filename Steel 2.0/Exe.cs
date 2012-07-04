using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IWshRuntimeLibrary;

namespace Steel_2._0
{
	class Exe
	{
		public string file;
		public string icon;
		public string title;
        private WshShellClass WshShell;

        public void createShortcut(string gamePath)
        {


            WshShell = new WshShellClass();
            IWshRuntimeLibrary.IWshShortcut GameShortcut;

            string deskDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            GameShortcut = (IWshRuntimeLibrary.IWshShortcut)WshShell.CreateShortcut(deskDir + "\\" + title + ".lnk");
            GameShortcut.TargetPath = gamePath + file;
            GameShortcut.Description = title;
            GameShortcut.IconLocation = gamePath + file;
            GameShortcut.WorkingDirectory = gamePath;
            GameShortcut.Save();
        }
	}

}
