using System.Windows;
using System.Windows.Controls;
using Tune.Frontend.Services;

namespace Tune.Frontend
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // Hook up the shared media player
            MediaPlayerService.Initialize(GlobalMediaPlayer);

            MainFrame.Navigate(new Pages.LoginPage());
        }
    }
}
