using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32; 
using System.IO;                     
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Tune.Frontend.Helpers;
using Tune.Frontend.Services;

namespace Tune.Frontend.Pages
{
    public partial class UploadPage : Page
    {
        private byte[]? _selectedThumbnailBytes;

        public UploadPage()
        {
            InitializeComponent();
            UsernameDisplay.Text = CurrentUser.loggedInUser;
        }
        private async void UploadSong_Click(object sender, RoutedEventArgs e)
        {
            
            string songName = SongName.Text;
            string filePath = FilePathTextBox.Text;
            if (string.IsNullOrWhiteSpace(songName) || string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("Please select a song and enter a name.");
                return;
            }

            var baseUrl = AppConfig.GetBackendBaseUrl();
            Tune.Frontend.Services.ApiClient.SetBaseUrl(baseUrl);

            byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
            var content = new MultipartFormDataContent();
            content.Add(new StringContent(songName), "NameOfSong");
            content.Add(new ByteArrayContent(fileBytes), "mp3_file", Path.GetFileName(filePath));
            
            
            if (_selectedThumbnailBytes != null && _selectedThumbnailBytes.Length > 0)
            {
                content.Add(new ByteArrayContent(_selectedThumbnailBytes), "thumbnail_file", "thumbnail.png");
            }

            try
            {
                var response = await ApiClient.Client.PostAsync("/api/songs/upload", content);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Song uploaded successfully!");
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
                    
                    _selectedThumbnailBytes = CompressImage(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load image: {ex.Message}");
                }
            }
        }

        private byte[] CompressImage(string imagePath)
        {
            try
            {
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.DecodePixelWidth = 200;
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                var encoder = new System.Windows.Media.Imaging.JpegBitmapEncoder { QualityLevel = 80 };
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));

                using (var ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    return ms.ToArray();
                }
            }
            catch
            {
                return File.ReadAllBytes(imagePath);
            }
        }
    }
}
