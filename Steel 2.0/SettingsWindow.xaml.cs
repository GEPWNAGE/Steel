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
			txtSteelServer.Text = Settings.Default.steelServerURL;
			txtPort.Text = Settings.Default.torrentPort.ToString();
            txtNickname.Text = Settings.Default.nickname;
            CheckShortcut.IsChecked = Settings.Default.createShortcuts;
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


            // save settings
            Settings.Default.createShortcuts = (bool)CheckShortcut.IsChecked;
			Settings.Default.Directory = txtDir.Text.EndsWith(@"\") ? txtDir.Text : txtDir.Text + @"\";
            Settings.Default.downloadPath = Path.Combine(txtDir.Text, "downloads");
            Settings.Default.installPath = Path.Combine(txtDir.Text, "games");
            Settings.Default.torrentPath = Path.Combine(txtDir.Text, "torrents");
            Settings.Default.xmlPath = Path.Combine(txtDir.Text, "gamelist.xml");
            Settings.Default.nickname = txtNickname.Text;
            
			Settings.Default.steelServerURL = txtSteelServer.Text.EndsWith(@"/" , StringComparison.InvariantCulture) ? txtSteelServer.Text : txtSteelServer.Text + "/"; ;

            if (!Directory.Exists(Settings.Default.Directory))
            {
                Directory.CreateDirectory(Settings.Default.Directory);
            }

            // save the settings
            Settings.Default.Save();

			this.Close();
			
		}

		private void btnCancel_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

        private void checkBox1_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
	}
}
