using System;
using System.Windows;
using System.Windows.Controls;
using Tune.Frontend.Pages;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Tune.Frontend.Pages
{
    public partial class MainPage : Page
    {
        public ObservableCollection<Song> Songs { get; set; } = new ObservableCollection<Song>();
        private Song? _currentSong;
        private bool _isPlaying = false;

        public MainPage()
        {
            InitializeComponent();
            int userId = CurrentUser.loggedInUserId;
            string username = CurrentUser.loggedInUser;
            UsernameDisplay.Text = username;
            LoadSongs();
            var mediaElement = Services.MediaPlayerService.GetMediaElement();
            if (mediaElement != null)
            {
                mediaElement.MediaEnded += MediaPlayer_MediaEnded;
            }
        }

        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            _isPlaying = false;
            UpdatePlayButtonIcon();
        }

        private async void LoadSongs()
        {
            try
            {
                var baseUrl = Tune.Frontend.Helpers.AppConfig.Configuration["BackendApiBaseUrl"];
                Tune.Frontend.Services.ApiClient.SetBaseUrl(baseUrl);

                var response = await Services.ApiClient.Client.GetAsync("/api/songs/my-songs");
                if (response.IsSuccessStatusCode)
                {
                    var songs = await response.Content.ReadFromJsonAsync<Song[]>();
                    Songs.Clear();
                    if (songs != null)
                    {
                        foreach (var song in songs)
                        {
                            Songs.Add(song);
                        }
                    }
                    SongsListBox.ItemsSource = Songs;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Failed to load songs: {errorBody}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading songs: {ex.Message}");
            }
        }

        private async void PlaySong_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.DataContext is Song song)
            {
                try
                {
                    var response = await Services.ApiClient.Client.GetAsync($"/api/songs/{song.Id}/play");
                    if (response.IsSuccessStatusCode)
                    {
                        var mp3Bytes = await response.Content.ReadAsByteArrayAsync();
                        var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{song.Id}_{song.NameOfSong}.mp3");
                        await System.IO.File.WriteAllBytesAsync(tempPath, mp3Bytes);
                        
                        _currentSong = song;
                        Services.MediaPlayerService.PlaySong(tempPath);
                        _isPlaying = true;
                        UpdatePlayButtonIcon();
                        NowPlayingText.Text = $"Now Playing: {song.NameOfSong}";
                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Failed to play song: {errorBody}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error playing song: {ex.Message}");
                }
            }
        }

        private async void DeleteSong_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as System.Windows.Controls.Button;
            if (button?.DataContext is Song song)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete '{song.NameOfSong}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await Services.ApiClient.Client.DeleteAsync($"/api/songs/{song.Id}");
                        if (response.IsSuccessStatusCode)
                        {
                            Songs.Remove(song);
                            MessageBox.Show("Song deleted successfully!");
                        }
                        else
                        {
                            var errorBody = await response.Content.ReadAsStringAsync();
                            MessageBox.Show($"Failed to delete song: {errorBody}");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting song: {ex.Message}");
                    }
                }
            }
        }

        private void FooterPlayPause_Click(object sender, RoutedEventArgs e)
        {
            var mediaElement = Services.MediaPlayerService.GetMediaElement();
            if (mediaElement?.Source == null)
            {
                MessageBox.Show("Please select a song to play first.");
                return;
            }

            if (_isPlaying)
            {
                Services.MediaPlayerService.Pause();
                _isPlaying = false;
            }
            else
            {
                Services.MediaPlayerService.Play();
                _isPlaying = true;
            }
            UpdatePlayButtonIcon();
        }

        private void FooterStop_Click(object sender, RoutedEventArgs e)
        {
            Services.MediaPlayerService.Stop();
            _isPlaying = false;
            UpdatePlayButtonIcon();
            NowPlayingText.Text = "";
        }

        private void UpdatePlayButtonIcon()
        {
            if (FooterPlayPauseImage != null)
            {
                FooterPlayPauseImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri(_isPlaying ? "../imgs/pause.png" : "../imgs/play.png", UriKind.Relative));
            }
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
    }
}