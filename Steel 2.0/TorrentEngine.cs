using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoTorrent.Client;
using MonoTorrent.Common;
using Steel_2._0.Properties;
using MonoTorrent.Client.Encryption;
using System.Net;
using MonoTorrent.BEncoding;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace Steel_2._0
{
	static class TorrentEngine
	{
		private static ClientEngine _engine;
		private static TorrentSettings _torrentDefaults;
        private static List<TorrentManager> managers;

		public static void init()
		{
            managers = new List<TorrentManager>();

			EngineSettings engineSettings = new EngineSettings(Settings.Default.downloadPath, Settings.Default.torrentPort);

			engineSettings.PreferEncryption = false;
			engineSettings.AllowedEncryption = EncryptionTypes.All;

			_torrentDefaults = new TorrentSettings(40, 150, 0, 0);

			_engine = new ClientEngine(engineSettings);
			_engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, Settings.Default.torrentPort));
		}


        public static void saveStatus()
        {
            BEncodedList list = new BEncodedList();
            if (managers == null) { return; }
            foreach (TorrentManager manager in managers)
            {
                if (manager.HashChecked)
                {
                    FastResume data = manager.SaveFastResume();
                    BEncodedDictionary fastResume = data.Encode();
                    list.Add(fastResume);
                }
            }

            // Write all the fast resume data to disk
            File.WriteAllBytes(Path.Combine(Settings.Default.Directory,"torrent.data"), list.Encode());
        }

		public static int addTorrent(string pvURL)
		{
            Torrent torrent;
            TorrentManager manager;

            try {
                torrent = Torrent.Load(pvURL);
                manager = new TorrentManager(torrent, Settings.Default.downloadPath, _torrentDefaults);
            }  catch {
                return -1;
            }

            // make sure this torrent is new
            foreach (TorrentManager m in managers)
            {
                if (manager.InfoHash == m.InfoHash)
                {
                    // torrent already exists, abort!
                    return -1;
                }
            }

            managers.Add(manager);

            // check if there is resume-data available
            if (File.Exists(Settings.Default.Directory + "torrent.data"))
            {
                BEncodedList list = (BEncodedList)BEncodedValue.Decode(File.ReadAllBytes(Settings.Default.Directory + "torrent.data"));
                foreach (BEncodedDictionary fastResume in list)
                {

                    FastResume data = new FastResume(fastResume);
                    if (manager.InfoHash == data.Infohash)
                    {
                        manager.LoadFastResume(data);
                    }
                }
            }

            try
            {
                _engine.Register(manager);
                manager.Start();
                return _engine.Torrents.IndexOf(manager);
            }
            catch
            {
                return -1;
            }
		}

        public static void pauseTorrent(int pvId)
        {
            _engine.Torrents[pvId].Pause();
        }

        public static void resumeTorrent(int pvId)
        {
            _engine.Torrents[pvId].Start();
        }

		public static int getDownSpeed(int pvID)
		{
			try {
				return _engine.Torrents[pvID].Monitor.DownloadSpeed / 1000;
			} catch (Exception) {
				return 0;
			}
		}

        public static int getUpSpeed(int pvID)
        {
            try {
                return _engine.Torrents[pvID].Monitor.UploadSpeed / 1000;
            }
            catch (Exception) {
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

		public static void removeTorrent(int pvID)
		{
            if (pvID >= 0 && pvID < _engine.Torrents.Count)
            {
                _engine.Torrents[pvID].Stop();
                _engine.Torrents[pvID].Dispose();
            }

            
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
			try {
				return Math.Round(_engine.Torrents[pvID].Progress,1);
			} catch(ArgumentOutOfRangeException){
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
