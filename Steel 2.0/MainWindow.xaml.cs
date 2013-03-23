using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Threading;
using System.IO;
using Steel_2._0.Properties;

namespace Steel_2._0
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}
        GameList gameList;


		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
            // start torrent engine
            TorrentEngine.init();

            // check if first-run
			if (!Settings.Default.SettingsShown) {
				SettingsWindow settings = new SettingsWindow();
				settings.ShowDialog();
				Settings.Default.SettingsShown = true;
			}

            // create directories (if not exists)
			Directory.CreateDirectory(Settings.Default.Directory);
			Directory.CreateDirectory(Settings.Default.downloadPath);
			Directory.CreateDirectory(Settings.Default.installPath);
			Directory.CreateDirectory(Settings.Default.torrentPath);
			Directory.CreateDirectory(Path.Combine(Settings.Default.Directory,"icons"));


            // start asynchrone thread to load initial gamelist
            Thread loadGamelistThread = new Thread(new ThreadStart(loadGamelist_t));
            loadGamelistThread.Start();
            
            // thread to update the total speed
			Thread updateNetwork = new Thread(updateNetwork_t);
			updateNetwork.IsBackground = true;
			updateNetwork.Start();

            // start thread that updates the gamelist every 5 minutes
            Thread statusThread = new Thread(new ThreadStart(updateGamelist_t));
            statusThread.Start();
		}

        private void loadGamelist_t()
        {
            // load the gamelist
             this.Dispatcher.Invoke((Action)(() => {
                gameList = new GameList(Settings.Default.steelServerURL + "xml.php", true, true);
                dgrGames.ItemsSource = gameList.list;

                // remove "splash" labelC:
                LoadingLabel.Visibility = System.Windows.Visibility.Hidden;

                // set offline modus in the title if in offline modus
                if (gameList.offlineModus) {
                    this.Title += " (Offline Modus)";
                }
            }));
        }
        
        private void updateGamelist_t()
        {
            while (true) {
                Thread.Sleep(1000 * 5 * 60);
                this.Dispatcher.Invoke((Action)(() => {
                    btnRefresh_Click(null, null);
                }));
            }

        }

        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            Game g = ((FrameworkElement)sender).DataContext as Game;

            GameInfo gameInfo = new GameInfo(g.title,g.message);
            gameInfo.ShowDialog();
        }

		private void btnInstall_Click(object sender, RoutedEventArgs e)
		{
			Game g = ((FrameworkElement)sender).DataContext as Game;
			g.downloadAndInstall();
		}

		private Game selectedGame;

		private void btnPlay_Click(object sender, RoutedEventArgs e)
		{
			Game g = ((FrameworkElement)sender).DataContext as Game;
			selectedGame = g;

			if (g._isInstalling)
			{
				return;
			}

            if (g._isDownloading) {
                if (g._isPaused) {
                    // resume torrent
                    g.resumeTorrent();
                }
                else {
                    // pause torrent
                    g.pauseTorrent();
                }

                return;
            }

			if (!g.installed)
			{
				g.downloadAndInstall();
				return;
			}

			if (g.exeCount > 1) {
				Button b = ((FrameworkElement)sender) as Button;

				if (b.ContextMenu == null) {
					b.ContextMenu = new ContextMenu();
					b.ContextMenu.Background = Brushes.DarkSlateGray;
					b.ContextMenu.Foreground = Brushes.LightGray;

					MenuItem m0 = new MenuItem();
					m0.Header = g.executables[0].title;
					m0.Click += new RoutedEventHandler(m0_Click);

					m0.Icon = new System.Windows.Controls.Image {
						Source = new BitmapImage(new Uri(g.icon(0))),
						Height = 20,
						Width = 20
					};
					m0.Background = Brushes.DarkSlateGray;
					b.ContextMenu.Items.Add(m0);

					MenuItem m1 = new MenuItem();
					m1.Header = g.executables[1].title;
					m1.Click += new RoutedEventHandler(m1_Click);

					m1.Icon = new System.Windows.Controls.Image {
						Source = new BitmapImage(new Uri(g.icon(1))),
						Height = 20,
						Width = 20
					};

					m1.Background = Brushes.DarkSlateGray;
					b.ContextMenu.Items.Add(m1);
				}
				b.ContextMenu.PlacementTarget = b;
				b.ContextMenu.IsOpen = true;
			} else {
				this.WindowState = WindowState.Minimized;
				g.play(0);
				this.WindowState = WindowState.Normal;
			}
		}

		void m1_Click(object sender, RoutedEventArgs e)
		{
			this.WindowState = WindowState.Minimized;
			selectedGame.play(1);
			this.WindowState = WindowState.Normal;
		}

		void m0_Click(object sender, RoutedEventArgs e)
		{
			this.WindowState = WindowState.Minimized;
			selectedGame.play(0);
			this.WindowState = WindowState.Normal;
		}

		private void btnSettings_Click(object sender, RoutedEventArgs e)
		{
			SettingsWindow settings = new SettingsWindow();
			settings.ShowDialog();
		}

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            // fetch a new xml file
            gameList.fetchGameList(Settings.Default.steelServerURL + "xml.php");
            List<Game> tmpGameList = gameList.generateGameList();

            // update the game information
            foreach (Game g in tmpGameList) {
                foreach (Game g2 in gameList.list) {
                    if (g.title == g2.title) {
                        g2.title = g.title;
                        g2._size = g._size;
                        g2.players = g.players;
                        g2._executables = g._executables;
                        g2.message = g.message;
                    }
                }   
            }

            // reload the game information in the view
            dgrGames.Items.Refresh();
        }

		
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
            // save settings and stuff
            Console.WriteLine("Closing Steel...");
			Settings.Default.Save();
            Console.WriteLine("Settings saved");
            TorrentEngine.close();
            Console.WriteLine("Torrents engine stopped");

            // stop the application
            System.Environment.Exit(0);
		}

		private void btnDownload_Click(object sender , RoutedEventArgs e)
		{
			Game g = ((FrameworkElement)sender).DataContext as Game;
			g.startTorrent();
		}

		delegate void updateCallback(string txt);


		private void UpdateNetworkLabel(string txt)
		{
			if (lblNetwork.Dispatcher.CheckAccess() == false) {
				updateCallback uCallBack = new updateCallback(UpdateNetworkLabel);
				this.Dispatcher.Invoke(uCallBack, txt);
			} else {
				lblNetwork.Content = txt;
			}
		}

		private void updateNetwork_t()
		{
			while (true) {
				Thread.Sleep(1000);
				UpdateNetworkLabel("Down: " + TorrentEngine.getTotalDownSpeed().ToString() + " KBps Up: " + TorrentEngine.getTotalUpSpeed().ToString()+" KBps");
			}
		}

		private void btnDownload_Click_1(object sender, RoutedEventArgs e)
		{
			Game g = ((FrameworkElement)sender).DataContext as Game;
			g.downloadAndInstall();
		}


		/*
		private void btnRemove_Click(object sender, RoutedEventArgs e)
		{
			Game g = ((FrameworkElement)sender).DataContext as Game;
			selectedGame = g;

			Button b = ((FrameworkElement)sender) as Button;

			if (b.ContextMenu == null) {
				b.ContextMenu = new ContextMenu();
				b.ContextMenu.Background = Brushes.DarkSlateGray;
				b.ContextMenu.Foreground = Brushes.LightGray;

				MenuItem m0 = new MenuItem();
				m0.Header = "Remove Installer";
				m0.Click += new RoutedEventHandler(RemInstall_Click);

				m0.Background = Brushes.DarkSlateGray;
				b.ContextMenu.Items.Add(m0);

				MenuItem m1 = new MenuItem();
				m1.Header = "Remove All Data";
				m1.Click += new RoutedEventHandler(RemAll_Click);

				m1.Background = Brushes.DarkSlateGray;
				b.ContextMenu.Items.Add(m1);
			}
			b.ContextMenu.PlacementTarget = b;
			b.ContextMenu.IsOpen = true;
		}*/

		private void btnUninstall_Click(object sender, RoutedEventArgs e)
		{
			Game g = ((FrameworkElement)sender).DataContext as Game;
			selectedGame = g;

			g.uninstall();
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			
		}

        private void dgrGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

	}
}
