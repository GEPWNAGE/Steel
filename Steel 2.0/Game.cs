using System;
using System.Collections.Generic;
using System.Net;
using Steel_2._0.Properties;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Text;
using System.Windows;


namespace Steel_2._0
{
	class Game : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string name)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) {
				handler(this, new PropertyChangedEventArgs(name));
			}
		}

		private string _id;
		private string _version;
		private string _title;
		private string _size;

		private List<Exe> _executables;
		private int _torrentID = -1;
		public bool _isDownloading;
		public bool _isInstalling;


		public bool installed = false;

		private StreamReader _installProgress;

		private WebClient _downloader = new WebClient();

		public List<Exe> executables
		{
			get { return _executables; }
		}

		public int exeCount
		{
			get { return _executables.Count; }
		}

		public string id
		{
			get { return _id; }
			set { _id = value; }
		}

		public string version
		{
			get { return _version; }
			set { _version = value; }
		}

		public string title
		{
			get { return _title; }
			set { _title = value; }
		}

        public string buttonText
        {
            get
            {
                if (_isInstalling)
                {
                    return "Installing...";
                }
                else if (_isDownloading)
                {
                    return "Downloading...";
                }
                else if (installed)
                {
                    return "Play";
                }
                else
                {
                    return "Installed";
                }
            }
        }

        bool buttonEnabled
        {
            get
            {
                return !_isInstalling;
            }
        }

		public string size
		{
			get {
				if(_size.Length > 9)
					return (Math.Round((Double.Parse(_size) / 1000000000), 2).ToString() + " GB"); 
				else if(_size.Length > 6)
					return ((UInt64.Parse(_size) / 1000000).ToString() + " MB") ; 
				else
					return ((UInt64.Parse(_size) / 1000).ToString() + " kB");
			}
			set { _size = value; }
		}

		public void updateStatus()
		{
			OnPropertyChanged("downloadButtonEnabled");
			OnPropertyChanged("installButtonEnabled");
			OnPropertyChanged("playButtonEnabled");
			OnPropertyChanged("status");
            OnPropertyChanged("buttonText");
            OnPropertyChanged("buttonEnabled");
		}

		public string status
		{
			get
			{
				if (_isDownloading && !installed)
					return "Downloading... (" + getDownloadProgress().ToString() + 
											"%, Down: " + getDownloadSpeed().ToString()  + 
											" Up: " + getUploadSpeed().ToString() +
											" Seeds: " + TorrentEngine.getSeeds(_torrentID).ToString() + 
											", Peers: " + TorrentEngine.getPeers(_torrentID).ToString() + ")";
				if (_isInstalling)
					return "Installing...";
				if (installed)
                    if (getDownloadProgress() == -1)
                    {
                        return "Ready";
                    }
                    else if (getDownloadProgress() < 100)
                    {
                        return "Ready, checking (" + getDownloadProgress() + "%)";
                    }
                    else
                    {
                        return "Ready, seeding";
                    }
                return "Not installed";
			
			}
		}

		public string icon(int index)
		{
			return Settings.Default.Directory + "icons\\" + _executables[index].icon; 
		}


		public string listIcon
		{
			get { return icon(0); }
		}

		private string torrentURL()
		{
			return Settings.Default.steelServerURL + "/torrent.php?id=" + _id;
		}

		public string torrentPath()
		{
			return Settings.Default.torrentPath + @"\" + _title + ".torrent";
		}

        public string gamePath()
        {
            return Settings.Default.installPath + _title + "\\";
        }

		private string downloadPath()
		{
			return Settings.Default.downloadPath + _title + ".arc";
		}

		private string iconURL(string pvIconFileName)
		{
			return Settings.Default.steelServerURL + "/icon/" + pvIconFileName;
		}

		public Game()
		{
			_isDownloading = false;
			_isInstalling = false;
			installed = false;
			_executables = new List<Exe>();
		}

		public void AddExe(Exe pvExe)
		{
            _executables.Add(pvExe);
            string iconFile = Settings.Default.Directory + "icons\\" + pvExe.icon;

            // check if we need to download the icon
            if (File.Exists(iconFile))
            {
                return;
            }
            
            try {
			    _downloader.DownloadFile(iconURL(pvExe.icon), iconFile);
			} catch (WebException e) {
			    return;
			    //todo
			}
		}

		public void install()
		{
			Thread extractThread = new Thread(extract_t);
			//extractThread.IsBackground = true;
			extractThread.Start();
		}

		public void extract_t()
		{		
			_isInstalling = true;
			ProcessStartInfo psi = new ProcessStartInfo(@"arc"); 
			psi.Arguments = " x -y \"" + downloadPath() + "\"";
			Directory.CreateDirectory(Settings.Default.installPath + title + @"\");
			psi.WorkingDirectory = Settings.Default.installPath  + title + @"\";
			//psi.RedirectStandardOutput = true;
			psi.WindowStyle = ProcessWindowStyle.Hidden;
			//psi.CreateNoWindow = true;
			psi.UseShellExecute = false;
			Process unArc;
			unArc = Process.Start(psi);
			//_installProgress = unArc.StandardOutput;

			// wait (non-blocking) until arc-process is ready
			while (!unArc.HasExited)
			{
			    Thread.Sleep(1000); // 1s
			}

			if(File.Exists(Settings.Default.installPath + @"\" + title + @"\" + "setup.bat"))
			{
				psi = new ProcessStartInfo(Settings.Default.installPath + @"\" + title + @"\" + "setup.bat");
				//psi.WorkingDirectory = Settings.Default.installPath + @"\" + title + @"\";
				psi.WindowStyle = ProcessWindowStyle.Hidden;
				psi.CreateNoWindow = true;
				psi.UseShellExecute = false;

				Process setup = Process.Start(psi);
				setup.WaitForExit();
			}
			_isInstalling = false;
			installed = true;

		}

		public int getInstallProgress()
		{
			int ret = 0;
			if (_isInstalling) {
				if (_installProgress != null) {
					string text = _installProgress.ReadToEnd();
					string[] lines = text.Split('\n');
					if (lines.Length > 1) {
						int progress_loc = lines[1].LastIndexOf('%');
						string progress = lines[1].Substring(progress_loc - 4, 3);
						if (progress.Contains(".")) {
							progress = progress.Substring(0, progress.IndexOf('.'));
						}
						int.TryParse(progress, out ret);
					}
				}
			}
			return ret;
		}

		public int startTorrent(bool seeding = false)
		{
            if (!seeding)
            {
                _isDownloading = true;
            }
			_downloader.DownloadFileCompleted += new AsyncCompletedEventHandler(downloader_DownloadFileCompleted);
			try {
				_downloader.DownloadFileAsync(new Uri(torrentURL()), torrentPath(), _id);
			} catch (WebException) {
				return 1;
			} catch (NotSupportedException) {
				return 2;
			}
			if(!Settings.Default.downloadedGames.Contains(_id + @":"))
				Settings.Default.downloadedGames += (_id + @":");
			return 0;
		}

		private void downloadAndInstall_t()
		{
			this.startTorrent();
			while (!this.downloadCompleted) {
				Thread.Sleep(100);
			}
			_isDownloading = false;
			this.install();
            installed = true;
		}

		public void downloadAndInstall()
		{
			System.IO.DriveInfo[] drives = System.IO.DriveInfo.GetDrives();
			bool noFreeSpace = true;
			foreach (DriveInfo d in drives) {
				if (d.Name.ToLower().StartsWith(Settings.Default.Directory.Substring(0, 1).ToLower())) {
					noFreeSpace = (d.TotalFreeSpace < long.Parse(this._size));
				}
			}
			if (noFreeSpace) {
				MessageBox.Show("Not enough free space, remove some stuff and try again");
				return;
			}

			Thread installerThread = new Thread(downloadAndInstall_t);
			installerThread.IsBackground = true;
			installerThread.Start();
		}

		public bool downloadCompleted
		{
			get { return TorrentEngine.downloadDone(_torrentID); }
		}

		private void downloadMonitor_t()
		{
			while (true) {
				if(TorrentEngine.downloadDone(_torrentID)){
					break;
				}
				Thread.Sleep(100);
			}
		}

		void downloader_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
		{
            if (_torrentID == -1)
            {
                _torrentID = TorrentEngine.addTorrent(torrentPath());
            }
			Thread downloadMonitor = new Thread(downloadMonitor_t);
			downloadMonitor.IsBackground = true;
			downloadMonitor.Start();
		}

		public double getDownloadProgress()
		{
			if (_torrentID >= 0) {
				return TorrentEngine.getProgress(_torrentID);
			} else {
				return -1;
			}
		}

		public int getDownloadSpeed()
		{
			if (_torrentID >= 0) {
				return TorrentEngine.getDownSpeed(_torrentID);
			} else {
				return 0;
			}
		}

		public int getUploadSpeed()
		{
			if (_torrentID >= 0) {
				return TorrentEngine.getUpSpeed(_torrentID);
			} else {
				return 0;
			}
		}


		public void play(int index)
		{
			TorrentEngine.pauseAll();

            string executable = Settings.Default.installPath + title + @"\" + _executables[index].file;


            if (!File.Exists(executable))
            {
                return;
            }

            Process gameProcess = new Process();
            gameProcess.StartInfo.FileName = executable;
            gameProcess.StartInfo.UseShellExecute = false;
            gameProcess.StartInfo.WorkingDirectory = Settings.Default.installPath + title;
            gameProcess.Start();

            gameProcess.WaitForExit();

			TorrentEngine.resumeAll();
		}

		void web_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
		{
			throw new NotImplementedException();
		}



		public void remAll()
		{
			if (Directory.Exists(Settings.Default.installPath + @"\" + title + @"\")) {
				Directory.Delete(Settings.Default.installPath + @"\" + title + @"\", true);
				installed = false;
				_isDownloading = false;
				
				string[] installedGameIDs = Settings.Default.installedGames.Split(':');
				Settings.Default.installedGames = "";
				
				foreach (string gameID in installedGameIDs) {
					if (gameID != _id) {
						Settings.Default.installedGames += (gameID + @":");
					}
				}

				string[] downloadedGameIDs = Settings.Default.downloadedGames.Split(':');
				Settings.Default.downloadedGames = "";

				foreach (string gameID in downloadedGameIDs) {
					if (gameID != _id) {
						Settings.Default.downloadedGames += (gameID + @":");
					}
				}
			}
			TorrentEngine.removeTorrent(_torrentID);
			if (File.Exists(downloadPath())) {
				File.Delete(downloadPath());
			}
			
		}

		public void remInstall()
		{
			if (File.Exists(downloadPath())) {
				File.Delete(downloadPath());
			}
			_isDownloading = false;

			string[] downloadedGameIDs = Settings.Default.downloadedGames.Split(':');
			Settings.Default.downloadedGames = "";

			foreach (string gameID in downloadedGameIDs) {
				if (gameID != _id) {
					Settings.Default.downloadedGames += (gameID + @":");
				}
			}
			TorrentEngine.removeTorrent(_torrentID);
		}
	}
}
