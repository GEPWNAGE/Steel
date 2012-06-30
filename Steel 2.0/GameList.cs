using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Steel_2._0.Properties;
using System.Xml;
using System.IO;
using System.Threading;

namespace Steel_2._0
{
	class GameList
	{
		private List<Game> _list;
		private WebClient _downloader;

		public List<Game> list
		{
			get { return _list; }
		}

		public GameList(string xmlURL, bool refresh)
		{
			_list = new List<Game>();
			if (refresh || !File.Exists(Settings.Default.xmlPath)) {			   
			   _downloader = new WebClient();
			   _downloader.DownloadFile(xmlURL, Settings.Default.xmlPath);
			}
			
			XmlDocument parser = new XmlDocument();
			parser.Load(Settings.Default.xmlPath);

			foreach (XmlNode gameNode in parser.GetElementsByTagName("game")) {
				Game g = new Game();
				foreach (XmlNode game_element in gameNode.ChildNodes) {

					List<string> exes = new List<string>();
					if (game_element.Name == "id") {
						g.id = game_element.InnerText;
					}
					if (game_element.Name == "versie") {
						g.version = game_element.InnerText;
					}
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
			string[] downloadedGameIDs = Settings.Default.downloadedGames.Split(':');
			string[] installedGameIDs = Settings.Default.installedGames.Split(':');
			
			foreach(string d_id in downloadedGameIDs){
				foreach(Game g in _list){
					if(g.id == d_id){
						if(installedGameIDs.Contains(d_id)){
							//downloaded and installed;
							g.installed = true;
							g.playButtonEnabled = true;
							g.startTorrent();						
						}else{
							//downloaded, not installed
							g.installed = false;
							g.downloadAndInstall();
						}
					}
				}
			}
			foreach(string i_id in downloadedGameIDs){
				foreach (Game g in _list) {
					if(g.id == i_id){
						if(!downloadedGameIDs.Contains(i_id)){
							//not downloaded (removed), installed
							g.playButtonEnabled = true;
							g.installed = true;
						}
					}
				}				
			}
		}
	}
}
