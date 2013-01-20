using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Steel_2._0.Properties;
using System.Xml;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace Steel_2._0
{
	class GameList
	{
		private List<Game> _list;
		private WebClient _downloader = new WebClient();

        public bool offlineModus = false;

		public List<Game> list
		{
			get { return _list; }
		}

		public GameList(string xmlURL, bool refresh)
		{
			_list = new List<Game>();
			if (refresh || !File.Exists(Settings.Default.xmlPath)) {			   
			   try {
				_downloader.DownloadFile(xmlURL, Settings.Default.xmlPath+".tmp");

				// download succesfully, update gamelist.xml
                if (File.Exists(Settings.Default.xmlPath)) {
                    File.Delete(Settings.Default.xmlPath);
                }
				File.Move(Settings.Default.xmlPath + ".tmp", Settings.Default.xmlPath);
			   } catch {
                   offlineModus = true;
				   // download failed
			   }
			}
			
			XmlDocument parser = new XmlDocument();
			parser.Load(Settings.Default.xmlPath);

			foreach (XmlNode gameNode in parser.GetElementsByTagName("game")) {
				Game g = new Game();
				foreach (XmlNode game_element in gameNode.ChildNodes) {

					List<string> exes = new List<string>();
					if (game_element.Name == "title") {
						g.title = game_element.InnerText;
					}
					if (game_element.Name == "size") {
                        g.size = game_element.InnerText;
					}

					if (game_element.Name == "exes") {
						foreach (XmlNode exe in game_element.ChildNodes) {
							Exe newExe = new Exe();
							foreach(XmlNode exe_element in exe.ChildNodes){
								
								if (exe_element.Name == "file") {
									newExe.file = exe_element.InnerText;
								}
								if (exe_element.Name == "icon") {
									newExe.icon = exe_element.InnerText;
								}
								if (exe_element.Name == "name") {
									newExe.title = exe_element.InnerText;
								}	
							}
							g.AddExe(newExe);
						}
					}
				}
				_list.Add(g);	
			}
			checkGames();
			Thread _statusUpdate = new Thread(UpdateStatus_t);
			_statusUpdate.IsBackground = true;
			_statusUpdate.Start();

		}

		private void UpdateStatus_t()
		{
			while (true) {
				foreach (Game g in _list) {
					g.updateStatus();
				}
				Thread.Sleep(100);
			}
		}

		private void checkGames()
		{
			foreach (Game game in _list)
			{
				if (Directory.Exists(game.gamePath()))
				{
					game.installed = true;
				}

				if (File.Exists(game.torrentPath()))
				{
					if (!game.installed)
					{
						// resume downloading and install game
						game.downloadAndInstall();
					}
					else
					{
						// start seeding
						game.startTorrent(true);
					}
				}
			}
		}
		
	}
}
