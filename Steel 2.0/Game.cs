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
using System.Security.Principal;
using System.Globalization;
using System.Windows.Media;


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

		private string _title;
		public string _size;

		public List<Exe> _executables;
		private int _torrentID = -1;
		public bool _isDownloading;
        public bool _isPaused;
		public bool _isInstalling;
		public double progress = -1;


		public bool installed = false;
		public bool running = false;
        public string players = "";


		private WebClient _downloader = new WebClient();

		public List<Exe> executables
		{
			get { return _executables; }
		}

		public int exeCount
		{
			get { return _executables.Count; }
		}

		public string title
		{
			get { return _title; }
			set { _title = value; }
		}

        public int playerCount
        {
            get
            {
                if (this.players.Trim() == "") {
                    return 0;
                }
                return this.players.Split(',').Length;
            }
        }

        public string playerList
        {
            get
            {
                return this.players;
            }
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
                    if (!_isPaused) {
                        return "Pause";
                    }
                    else {
                        return "Resume";
                    }
				}
				else if (installed)
				{
					return "Play";
				}
				else
				{
					return "Install";
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
			OnPropertyChanged("status");
			OnPropertyChanged("buttonText");
			OnPropertyChanged("buttonEnabled");
			OnPropertyChanged("uninstallButtonVisibility");
            OnPropertyChanged("textColor");
		}

        public Brush textColor
        {
            get {
                if (_isDownloading || _isInstalling) {
                    return new SolidColorBrush(Colors.LightGreen);
                } else if(!installed){
                    return new SolidColorBrush(Color.FromRgb(180, 180, 180));
                }

                return new SolidColorBrush(Colors.White);
            }
        }

		public string status
		{
			get
			{
                if (_isDownloading && _isPaused && !installed) {
                    return "Download paused";
                }

				if (_isDownloading && !installed)
					return "Downloading... (" + getDownloadProgress().ToString() + "% Speed: " + getDownloadSpeed().ToString() + " KBps [" + TorrentEngine.getSeeds(_torrentID).ToString() + "/" + TorrentEngine.getPeers(_torrentID).ToString() + "])";
				if (_isInstalling)
					if (progress == -1)
					{
						return "Installing...";
					}
					else
					{
						return "Installing... (" + progress + "%)";
					}
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

		public string uninstallButtonVisibility
		{
			get
			{
				if ((installed || _isDownloading) && !_isInstalling)
				{
					return "visible";
				}
				else
				{
					return "hidden";
				}
			}
		}

        public string uninstallButtonText {
            get
            {
                if (installed) {
                    return "Uninstall";
                }
                else {
                    return "Cancel";
                }
            }
        }


		public string icon(int index)
		{
			return Path.Combine(Path.Combine(Settings.Default.Directory,"icons"),_executables[index].icon); 
		}


		public string listIcon
		{
			get { return icon(0); }
		}

		private string torrentURL()
		{
            return Settings.Default.steelServerURL + "/torrent.php?file=" + Uri.EscapeUriString(_title);
		}

		public string torrentPath()
		{
			return Path.Combine(Settings.Default.torrentPath,_title + ".torrent");
		}

		public string gamePath()
		{
			return Path.Combine(Settings.Default.installPath, _title);
		}

		private string downloadPath()
		{
			return Path.Combine(Settings.Default.downloadPath,_title + ".arc");
		}

		private string iconURL(string pvIconFileName)
		{
			return Settings.Default.steelServerURL + "/icon.php?file=" + pvIconFileName;
		}

		public Game()
		{
			_isDownloading = false;
            _isPaused = false;
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
			} catch {
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

			System.Diagnostics.Process proc0 = new System.Diagnostics.Process();
			proc0.StartInfo.FileName = "cmd";
			Directory.CreateDirectory(gamePath());
			proc0.StartInfo.WorkingDirectory = gamePath();
			
			string statusFile = proc0.StartInfo.WorkingDirectory+"status_"+title+".txt";
			string command = "arc x -y \"" + downloadPath() + "\" > \""+statusFile+"\"";
			proc0.StartInfo.Arguments = "/C " + command;
			proc0.StartInfo.CreateNoWindow = true;
			proc0.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			proc0.Start();

			while (!proc0.HasExited)
			{
				if (File.Exists(statusFile))
				{
					updateProgress(statusFile);
				}
			   
				Thread.Sleep(500);
			}

            // delete the status file
            File.Delete(statusFile);
			
			string setupFile = Settings.Default.installPath + title + @"\" + "setup.bat";
			if(File.Exists(setupFile))
			{
				ProcessStartInfo processInfo = new ProcessStartInfo();
				processInfo.Verb = "runas"; // administrator rights
				processInfo.FileName = setupFile;
				processInfo.WindowStyle = ProcessWindowStyle.Minimized;
				processInfo.WorkingDirectory = Settings.Default.installPath + title;
				Process.Start(processInfo);
			}
			_isInstalling = false;
			installed = true;

			// shortcuts
			if (Settings.Default.createShortcuts)
			{
				foreach (Exe exe in _executables)
				{
					exe.createShortcut(gamePath());
				}
			}
			 

		}

		static double parseProgressString(string progress)
		{
			// check for empty string
			if (progress == null || progress.Length == 0)
			{
				return 0;
			}

			// "... 49.5% ...." => "49.5"
			int endProgress = progress.LastIndexOf('%');
            if (endProgress == -1) {
                return 0;
            }

			int beginProgress = progress.Substring(0, endProgress).LastIndexOf(' ');
            if (beginProgress == -1) {
                return 0;
            }

			string stringProgress = progress.Substring(beginProgress + 1, endProgress - beginProgress - 1);

			// "49.5" => 49.5
			double returnValue = 0;
			double.TryParse(stringProgress, NumberStyles.Number, CultureInfo.CreateSpecificCulture ("en-US"), out returnValue);

			return returnValue;
		}

		public void updateProgress(string statusFile)
		{
			FileStream logFileStream = new FileStream(statusFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			StreamReader logFileReader = new StreamReader(logFileStream);

			while (!logFileReader.EndOfStream)
			{
				try
				{
					string line = logFileReader.ReadLine();
					progress = Math.Max(parseProgressString(line), progress);
				}
				catch
				{
					// todo
				}
			}

			// Clean up
			logFileReader.Close();
			logFileStream.Close();

		}

		public int startTorrent(bool seeding = false)
		{
			if (!seeding)
			{
				_isDownloading = true;
			}
			_downloader.DownloadFileCompleted += new AsyncCompletedEventHandler(downloader_DownloadFileCompleted);
			try {
				_downloader.DownloadFileAsync(new Uri(torrentURL()), torrentPath(), title);
			} catch (WebException) {
				return 1;
			} catch (NotSupportedException) {
				return 2;
			}

			return 0;
		}

        public void pauseTorrent()
        {
            TorrentEngine.pauseTorrent(_torrentID);
            _isPaused = true;
        }

        public void resumeTorrent()
        {
            TorrentEngine.resumeTorrent(_torrentID);
            _isPaused = false;
        }

		private void downloadAndInstall_t()
		{
			this.startTorrent();
			while (!this.downloadCompleted) {
				Thread.Sleep(1000);
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
				Thread.Sleep(1000);
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
				return -2;
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

		private void process_Exited(object sender, EventArgs e)
		{
			this.running = false;
			TorrentEngine.resumeAll();
		}

		private void sendPlayingStatus()
		{
			WebClient _downloader = new WebClient();
			try {
				_downloader.DownloadFile(Settings.Default.steelServerURL + "/playing.php?game=" + _title + "&nickname="+Settings.Default.nickname, "tmp");
			} catch {
				
			}
		}

		public void play(int index)
		{
			TorrentEngine.pauseAll();
			TorrentEngine.saveStatus();

			string executable = Path.Combine(Path.Combine(Settings.Default.installPath, _title), _executables[index].file);


			if (!File.Exists(executable))
			{
				return;
			}

			Process gameProcess = new Process();
			gameProcess.StartInfo.FileName = executable;
			gameProcess.StartInfo.UseShellExecute = false;
			gameProcess.StartInfo.WorkingDirectory = Path.Combine(Settings.Default.installPath, _title);
			gameProcess.Start();

			gameProcess.EnableRaisingEvents = true;
			gameProcess.Exited += new EventHandler(process_Exited);

			this.running = true;
			int counter = 0;

			sendPlayingStatus();
			while (this.running) {
				counter++;
				if (counter == 60*5) {
					sendPlayingStatus();
					counter = 0;
				}

				Thread.Sleep(1000);
				
			}

		}

		void web_UploadDataCompleted(object sender, UploadDataCompletedEventArgs e)
		{
			throw new NotImplementedException();
		}

		public void uninstall()
		{
			if (Directory.Exists(gamePath()))
			{
				try
				{
					Directory.Delete(gamePath(), true);
				}
				catch
				{
					// :(
				}

			}

			TorrentEngine.removeTorrent(_torrentID);

			if (File.Exists(downloadPath()))
			{
				File.Delete(downloadPath());
			}

			if (File.Exists(torrentPath()))
			{
                try {
                    File.Delete(torrentPath());
                }
                catch {
                    // keep torrent file
                }
			}

			installed = false;
			_isInstalling = false;
			_isDownloading = false;
			_torrentID = -1;
		}

	}
}
