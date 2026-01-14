
using System.Windows;
using System.Windows.Controls;

namespace Tune.Frontend.Pages
{
    public class Song
    {
        public int Id { get; set; }
        public string NameOfSong { get; set; } = string.Empty;
        public double Length { get; set; }
        public string FormattedLength => System.TimeSpan.FromSeconds(Length).ToString(@"mm\:ss");
    }

    public partial class LibraryPage : Page
    {
        public LibraryPage()
        {
            InitializeComponent();
            UsernameDisplay.Text = CurrentUser.loggedInUser;
        }

        private void LogoButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as Tune.Frontend.MainWindow;
            mainWindow?.MainFrame.Navigate(new MainPage());
        }

        private void LibraryButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as Tune.Frontend.MainWindow;
            mainWindow?.MainFrame.Navigate(new LibraryPage());
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as Tune.Frontend.MainWindow;
            mainWindow?.MainFrame.Navigate(new UploadPage());
        }

        private void ProfileMenuButton_Click(object sender, RoutedEventArgs e)
        {
            ProfileMenuPopup.IsOpen = true;
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = System.Windows.Application.Current.MainWindow as Tune.Frontend.MainWindow;
            mainWindow?.MainFrame.Navigate(new ProfilePage());
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentUser.loggedInUserId = -1;
            CurrentUser.loggedInUser = string.Empty;
            CurrentUser.Token = string.Empty;
            Tune.Frontend.Services.ApiClient.SetToken("");
            var mainWindow = System.Windows.Application.Current.MainWindow as Tune.Frontend.MainWindow;
            mainWindow?.MainFrame.Navigate(new LoginPage());
        }
    }
}