using System;
using System.Windows;
using System.Windows.Controls;
using Tune.Frontend.Pages;
using Tune.Frontend.Models;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Tune.Frontend.Helpers;
using System.Windows.Threading;

namespace Tune.Frontend.Pages
{
    public partial class MainPage : Page
    {
        public ObservableCollection<Song> Songs { get; set; } = new ObservableCollection<Song>();
        private Song? _currentSong;
        private bool _isPlaying = false;
        private DispatcherTimer? _progressTimer;
        private bool _isSliderDragging = false;

        public MainPage()
        {
            InitializeComponent();
            int userId = CurrentUser.loggedInUserId;
            string username = CurrentUser.loggedInUser;
            UsernameDisplay.Text = username;
            LoadAllSongs();
            var mediaElement = Services.MediaPlayerService.GetMediaElement();
            if (mediaElement != null)
            {
                mediaElement.MediaEnded += MediaPlayer_MediaEnded;
                mediaElement.MediaOpened += MediaElement_MediaOpened;
            }
            
            // Initialize progress timer
            _progressTimer = new DispatcherTimer();
            _progressTimer.Interval = TimeSpan.FromMilliseconds(100);
            _progressTimer.Tick += UpdateProgress;
            _progressTimer.Start();
            
            // Set initial volume
            if (mediaElement != null)
            {
                mediaElement.Volume = 0.5;
            }
        }

        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            var mediaElement = Services.MediaPlayerService.GetMediaElement();
            if (mediaElement != null)
            {
                ProgressSlider.Maximum = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;
                TotalTimeText.Text = FormatTime(mediaElement.NaturalDuration.TimeSpan);
            }
        }

        private void UpdateProgress(object? sender, EventArgs e)
        {
            var mediaElement = Services.MediaPlayerService.GetMediaElement();
            if (mediaElement != null && !_isSliderDragging)
            {
                ProgressSlider.Value = mediaElement.Position.TotalSeconds;
                CurrentTimeText.Text = FormatTime(mediaElement.Position);
            }
        }

        private string FormatTime(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes}:{time.Seconds:D2}";
        }

        private void MediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            _isPlaying = false;
            UpdatePlayButtonIcon();
        }

        private async void LoadAllSongs()
        {
            try
            {
                var baseUrl = AppConfig.GetBackendBaseUrl();
                Tune.Frontend.Services.ApiClient.SetBaseUrl(baseUrl);

                var response = await Services.ApiClient.Client.GetAsync("/api/songs");
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
                    SongsItemsControl.ItemsSource = Songs;
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Failed to load all songs: {errorBody}");
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

        private void FooterPlayPause_Click(object sender, RoutedEventArgs e)
        {
            var mediaElement = Services.MediaPlayerService.GetMediaElement();
            if (mediaElement?.Source == null)
            {
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

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
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

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (Songs.Count == 0 || _currentSong == null) return;
            
            int currentIndex = Songs.IndexOf(_currentSong);
            if (currentIndex > 0)
            {
                PlaySong_Click(PlayPauseButton, new RoutedEventArgs());
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (Songs.Count == 0 || _currentSong == null) return;
            
            int currentIndex = Songs.IndexOf(_currentSong);
            if (currentIndex < Songs.Count - 1)
            {
                PlaySong_Click(PlayPauseButton, new RoutedEventArgs());
            }
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


        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as Tune.Frontend.MainWindow;
            mainWindow?.MainFrame.Navigate(new UploadPage());
        }

        private void ProgressSlider_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isSliderDragging = true;
        }

        private void ProgressSlider_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isSliderDragging = false;
            var mediaElement = Services.MediaPlayerService.GetMediaElement();
            if (mediaElement != null)
            {
                mediaElement.Position = TimeSpan.FromSeconds(ProgressSlider.Value);
            }
        }

        private void VolumeSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var mediaElement = Services.MediaPlayerService.GetMediaElement();
            if (mediaElement != null)
            {
                mediaElement.Volume = VolumeSlider.Value / 100.0;
            }
        }
    }
}