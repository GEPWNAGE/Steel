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
using System.Windows.Shapes;
using System.Windows.Forms;
using Steel_2._0.Properties;
using System.IO;

namespace Steel_2._0
{
	/// <summary>
	/// Interaction logic for SettingsWindow.xaml
	/// </summary>
	public partial class SettingsWindow : Window
	{
		public SettingsWindow()
		{
			InitializeComponent();
			txtDir.Text = Settings.Default.Directory;
			txtLanServer.Text = Settings.Default.lanServerURL;
			txtSteelServer.Text = Settings.Default.steelServerURL;
			txtPort.Text = Settings.Default.torrentPort.ToString();
		}

		private void btnBrowse_Click(object sender, RoutedEventArgs e)
		{
			FolderBrowserDialog diag = new FolderBrowserDialog();
			diag.ShowNewFolderButton = true;
			diag.RootFolder = Environment.SpecialFolder.MyComputer;
			diag.ShowDialog();
			txtDir.Text = diag.SelectedPath;
		}

		private void btnSave_Click(object sender, RoutedEventArgs e)
		{
			int port = 0;
			if (int.TryParse(txtPort.Text, out port)) {
				if (port > 1024 && port <= 65535) {
					Settings.Default.torrentPort = port;
				} else {
					System.Windows.MessageBox.Show("Please pick a port between 1025 and 65535", "Port out of range", MessageBoxButton.OK);
					return;
				}
			} else {
				System.Windows.MessageBox.Show("Invalid torrent port", "Error", MessageBoxButton.OK);
				return;
			}

			Settings.Default.Directory = txtDir.Text.EndsWith(@"\") ? txtDir.Text : txtDir.Text + @"\";
			Settings.Default.downloadPath = txtDir.Text + "downloads\\";
			Settings.Default.installPath = txtDir.Text + "games\\";
			Settings.Default.torrentPath = txtDir.Text + "torrents\\";
			Settings.Default.xmlPath = txtDir.Text + "gamelist.xml";

			Settings.Default.lanServerURL = txtLanServer.Text.EndsWith(@"/", StringComparison.InvariantCulture) ? txtLanServer.Text : txtLanServer.Text + "/";
			Settings.Default.steelServerURL = txtSteelServer.Text.EndsWith(@"/" , StringComparison.InvariantCulture) ? txtSteelServer.Text : txtSteelServer.Text + "/"; ;

            if (!Directory.Exists(Settings.Default.Directory))
            {
                Directory.CreateDirectory(Settings.Default.Directory);
            }
			this.Close();
			
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
