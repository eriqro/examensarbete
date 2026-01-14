using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32; 
using System.IO;                     
using System.Net.Http; // For MultipartFormDataContent, StringContent, ByteArrayContent
using System.Threading.Tasks;
using System.Net.Http.Json;
using Tune.Frontend.Helpers;
using Tune.Frontend.Services;

namespace Tune.Frontend.Pages
{
    public partial class UploadPage : Page
    {
        public UploadPage()
        {
            InitializeComponent();
            UsernameDisplay.Text = CurrentUser.loggedInUser;
        }
        private async void UploadSong_Click(object sender, RoutedEventArgs e)
        {
            // Get song name and file path
            string songName = SongName.Text;
            string filePath = FilePathTextBox.Text;
            if (string.IsNullOrWhiteSpace(songName) || string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("Please select a song and enter a name.");
                return;
            }

            // Ensure API base URL is set
            var baseUrl = Tune.Frontend.Helpers.AppConfig.Configuration["BackendApiBaseUrl"];
            Tune.Frontend.Services.ApiClient.SetBaseUrl(baseUrl);

            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(songName), "NameOfSong");
            content.Add(new ByteArrayContent(fileBytes), "mp3_file", Path.GetFileName(filePath));
            // Thumbnail can be added similarly if available

            try
            {
                var response = await ApiClient.Client.PostAsync("/api/songs/upload", content);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Song uploaded successfully!");
                    // Navigate to LibraryPage after upload
                    var mainWindow = Application.Current.MainWindow as Tune.Frontend.MainWindow;
                    mainWindow?.MainFrame.Navigate(new LibraryPage());
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Upload failed ({response.StatusCode}):\n{errorBody}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        private void LogoButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as Tune.Frontend.MainWindow;
            mainWindow?.MainFrame.Navigate(new MainPage());
        }

        private void LibraryButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as Tune.Frontend.MainWindow;
            mainWindow?.MainFrame.Navigate(new LibraryPage());
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as Tune.Frontend.MainWindow;
            mainWindow?.MainFrame.Navigate(new UploadPage());
        }

        private void ProfileMenuButton_Click(object sender, RoutedEventArgs e)
        {
            ProfileMenuPopup.IsOpen = true;
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as Tune.Frontend.MainWindow;
            mainWindow?.MainFrame.Navigate(new ProfilePage());
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentUser.loggedInUserId = -1;
            CurrentUser.loggedInUser = string.Empty;
            CurrentUser.Token = string.Empty;
            Tune.Frontend.Services.ApiClient.SetToken("");
            var mainWindow = Application.Current.MainWindow as Tune.Frontend.MainWindow;
            mainWindow?.MainFrame.Navigate(new LoginPage());
        }

        private void BrowseFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select a file",
                Filter = "All files (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = dialog.FileName;
            }
        }

        private void UploadThumbnail_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "Select a thumbnail image",
                Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(dialog.FileName);
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ThumbnailPreview.Source = bitmap;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image: {ex.Message}");
                }
            }
        }
    }
}
