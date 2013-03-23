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
using MonoTorrent.Tracker.Listeners;
using MonoTorrent.Dht.Listeners;
using MonoTorrent.Dht;
using System.Net.Sockets;

namespace Steel_2._0
{
	static class TorrentEngine
	{
		private static ClientEngine _engine;
		private static TorrentSettings _torrentDefaults;
		private static List<TorrentManager> managers;

        public static void StartDht(ClientEngine engine, int port)
        {
            // Send/receive DHT messages on the specified port
            IPEndPoint listenAddress = new IPEndPoint(IPAddress.Any, port);

            // Create a listener which will process incoming/outgoing dht messages
            DhtListener listener = new DhtListener(listenAddress);

            // Create the dht engine
            DhtEngine dht = new DhtEngine(listener);

            // Connect the Dht engine to the MonoTorrent engine
            engine.RegisterDht(dht);

            // Start listening for dht messages and activate the DHT engine
            listener.Start();

            // If there are existing DHT nodes stored on disk, load them
            // into the DHT engine so we can try and avoid a (very slow)
            // full bootstrap
            byte[] nodes = null;
            if (File.Exists("udp"))
                nodes = File.ReadAllBytes("udp");
            dht.Start(nodes);
        }

		public static void init()
		{
			managers = new List<TorrentManager>();

			EngineSettings engineSettings = new EngineSettings(Settings.Default.downloadPath, Settings.Default.torrentPort);

			engineSettings.PreferEncryption = false;
			engineSettings.AllowedEncryption = EncryptionTypes.All;

			_torrentDefaults = new TorrentSettings(40, 150, 0, 0);
			_torrentDefaults.UseDht = true;
			_torrentDefaults.EnablePeerExchange = true;

			_engine = new ClientEngine(engineSettings);
			_engine.ChangeListenEndpoint(new IPEndPoint(IPAddress.Any, Settings.Default.torrentPort));
			_engine.LocalPeerSearchEnabled = true;

            //StartDht(_engine, 1800);
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

        public static void close()
        {
            saveStatus();
            _engine.Dispose();
            
        }

		public static int addTorrent(string pvURL)
		{
            // Load Torrent File
            Torrent torrent = Torrent.Load(pvURL);
            TorrentManager manager = new TorrentManager(torrent, Settings.Default.downloadPath, _torrentDefaults);

            // make sure this torrent is new
            var managersList = managers.ToList();
            foreach (TorrentManager m in managersList)
			{
				if (manager.InfoHash == m.InfoHash)
				{
					// torrent already exists, return position
                    return _engine.Torrents.IndexOf(manager);
				}
			}

            try {
                managers.Add(manager);
            }
            catch {

            }

			// check if there is resume-data available
			if (File.Exists(Path.Combine(Settings.Default.Directory,"torrent.data")))
			{
                BEncodedList list = null;

                try {
                    list = (BEncodedList)BEncodedValue.Decode(File.ReadAllBytes(Path.Combine(Settings.Default.Directory, "torrent.data")));
                } catch(Exception e){
                    Console.WriteLine("Unable to load torrent.data, ignoring...");
                }

                if (list != null) {
                    foreach (BEncodedDictionary fastResume in list) {
                        FastResume data = new FastResume(fastResume);
                        if (manager.InfoHash == data.Infohash) {
                            // load resume-data
                            manager.LoadFastResume(data);
                        }
                    }
                }
			}

			try
			{
				_engine.Register(manager);
				manager.Start();
				int torrentIndex = _engine.Torrents.IndexOf(manager);
				return torrentIndex;
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
            if (pvID < 0) {
                return false;
            }

		    return _engine.Torrents[pvID].Complete;

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
            if (pvID < 0) {
                return -1; // torrent not available
            }

			return _engine.Torrents[pvID].Peers.Seeds;
		}

		public static int getPeers(int pvID)
		{
            if (pvID < 0) {
                return -1;
            }

			return _engine.Torrents[pvID].Peers.Leechs;
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
