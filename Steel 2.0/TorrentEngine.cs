using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoTorrent.Client;
using MonoTorrent.Common;
using Steel_2._0.Properties;
using MonoTorrent.Client.Encryption;
using System.Net;

namespace Steel_2._0
{
	static class TorrentEngine
	{
		private static ClientEngine _engine;
		private static TorrentSettings _torrentDefaults;


		public static void init()
		{
			EngineSettings engineSettings = new EngineSettings(Settings.Default.downloadPath, Settings.Default.torrentPort);

			engineSettings.PreferEncryption = false;
			engineSettings.AllowedEncryption = EncryptionTypes.All;

			_torrentDefaults = new TorrentSettings(40, 150, 0, 0);

			_engine = new ClientEngine(engineSettings);
			_engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, Settings.Default.torrentPort));
		}

		public static int addTorrent(string pvURL)
		{
			Torrent torrent = Torrent.Load(pvURL);
			TorrentManager manager = new TorrentManager(torrent, Settings.Default.downloadPath, _torrentDefaults);
			_engine.Register(manager);
			manager.Start();
			return _engine.Torrents.IndexOf(manager);
		}

		public static int getDownSpeed(int pvID)
		{
			try {
				return _engine.Torrents[pvID].Monitor.DownloadSpeed / 1000;
			} catch (Exception) {
				return 0;
			}
		}

		public static bool downloadDone(int pvID)
		{
			try {
				return _engine.Torrents[pvID].Complete;
			} catch (Exception) {
				return false;
			}
		}

		public static int getUpSpeed(int pvID)
		{
			try {
				return _engine.Torrents[pvID].Monitor.UploadSpeed / 1000;
			} catch (Exception) {
				return 0;
			}
		}

		public static void removeTorrent(int pvID)
		{
			_engine.Torrents[pvID].Dispose();
		}

		public static int getTotalDownSpeed()
		{
			return _engine.TotalDownloadSpeed / 1000;
		}

		public static int getTotalUpSpeed()
		{
			return _engine.TotalUploadSpeed / 1000;
		}

		public static double getProgress(int pvID)
		{
			try{
				return Math.Round(_engine.Torrents[pvID].Progress);
			}catch(ArgumentOutOfRangeException){
				return 0;
			}
		}

		public static int getSeeds(int pvID)
		{
			try{
				return _engine.Torrents[pvID].Peers.Seeds;
			}catch(ArgumentOutOfRangeException){
				return 0;
			}
		}

		public static int getPeers(int pvID)
		{
			try{
				return _engine.Torrents[pvID].Peers.Leechs;
			}catch(ArgumentOutOfRangeException){
				return 0;
			}
		}

		public static void pauseAll()
		{
			_engine.PauseAll();
		}

		public static void resumeAll()
		{
			_engine.StartAll();
		}

	}
}
