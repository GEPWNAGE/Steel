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

namespace Steel_2._0
{
    /// <summary>
    /// Interaction logic for GameInfo.xaml
    /// </summary>
    public partial class GameInfo : Window
    {
   

        public GameInfo(string gameName, string information)
        {
            InitializeComponent();
            gameNameLabel.Content = gameName;
            infoBlock.Text = information;
        }
    }
}
