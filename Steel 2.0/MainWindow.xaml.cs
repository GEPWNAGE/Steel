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
using System.Windows.Shapes;
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

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (!Settings.Default.SettingsShown) {
				SettingsWindow settings = new SettingsWindow();
				settings.ShowDialog();
				Settings.Default.SettingsShown = true;
			}

			Directory.CreateDirectory(Settings.Default.Directory);
			Directory.CreateDirectory(Settings.Default.downloadPath);
			Directory.CreateDirectory(Settings.Default.installPath);
			Directory.CreateDirectory(Settings.Default.torrentPath);

			GameList lst = new GameList(Settings.Default.steelServerURL + "xml.php", true);
			TorrentEngine.init();
			dgrGames.ItemsSource = lst.list;

			Thread updateNetwork = new Thread(updateNetwork_t);
			updateNetwork.IsBackground = true;
			updateNetwork.Start();
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

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			Settings.Default.Save();
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
				UpdateNetworkLabel("Down: " + TorrentEngine.getTotalDownSpeed().ToString() + " Up: " + TorrentEngine.getTotalUpSpeed().ToString());
			}
		}

		private void btnDownload_Click_1(object sender, RoutedEventArgs e)
		{
			Game g = ((FrameworkElement)sender).DataContext as Game;
			g.downloadAndInstall();
		}

		void RemInstall_Click(object sender, RoutedEventArgs e)
		{
			selectedGame.remInstall();
		}

		void RemAll_Click(object sender, RoutedEventArgs e)
		{
			selectedGame.remAll();
		}

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
		}

	}
}
