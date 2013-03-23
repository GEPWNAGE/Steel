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
		public List<Game> list = new List<Game>();
		private WebClient _downloader = new WebClient();

        public bool offlineModus = false;

        
        public void fetchGameList(string xmlURL)
        {
            try {
                _downloader.DownloadFile(xmlURL, Settings.Default.xmlPath + ".tmp");

                // download succesfully, update gamelist.xml
                if (File.Exists(Settings.Default.xmlPath)) {
                    File.Delete(Settings.Default.xmlPath);
                }
                File.Move(Settings.Default.xmlPath + ".tmp", Settings.Default.xmlPath);
            }
            catch {
                offlineModus = true;
                // download failed
            }
        }

        public List<Game> generateGameList()
        {
            List<Game> generatedList = new List<Game>();

            if(!File.Exists(Settings.Default.xmlPath)){
                return generatedList;
            }

            // parse the XML file
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
                    if (game_element.Name == "players") {
                        g.players = game_element.InnerText;
                    }

                    if (game_element.Name == "message") {
                        g.message = game_element.InnerText.Trim();
                    }

                    if (game_element.Name == "exes") {
                        foreach (XmlNode exe in game_element.ChildNodes) {
                            Exe newExe = new Exe();
                            foreach (XmlNode exe_element in exe.ChildNodes) {

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

                // new list? Add!
                generatedList.Add(g);
            }

            return generatedList;
        }


		public GameList(string xmlURL, bool refresh, bool initList)
		{
            // fetch game xml if not available
			if (refresh || !File.Exists(Settings.Default.xmlPath)) {
                fetchGameList(xmlURL);
			}

            // generate the game list
            list = generateGameList();		
     
            // check the status of the games
            checkGames();

            // start a thread to update the status of the games
			Thread _statusUpdate = new Thread(UpdateStatus_t);
			_statusUpdate.IsBackground = true;
			_statusUpdate.Start();
		}

		private void UpdateStatus_t()
		{
            // update the gameStatus for every game
			while (true) {
				foreach (Game g in list) {
					g.updateStatus();
				}
				Thread.Sleep(1000);
			}
		}

		private void checkGames()
		{
			foreach (Game game in list)
			{
				if (Directory.Exists(game.gamePath()))
				{
                    // if directory exists, it is installed
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
