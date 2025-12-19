using System.Windows;
using System.Windows.Controls;

namespace Tune.Frontend
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            MainFrame.Navigate(new Pages.LoginPage());
        }
    }
}
