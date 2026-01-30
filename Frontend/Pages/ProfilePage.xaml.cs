using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using Tune.Frontend.Helpers;
using Tune.Frontend.Models;
using Tune.Frontend.Services;

namespace Tune.Frontend.Pages
{
    public partial class ProfilePage : Page
    {
        public ObservableCollection<Song> Songs { get; } = new ObservableCollection<Song>();
        private Song? _currentSong;

        public ProfilePage()
        {
            InitializeComponent();
            UsernameDisplay.Text = CurrentUser.loggedInUser;
            LoadMySongs();
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

        private async void LoadMySongs()
        {
            try
            {
                var baseUrl = AppConfig.GetBackendBaseUrl();
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
                    MySongsListBox.ItemsSource = Songs;
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
            var button = sender as Button;
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
            var button = sender as Button;
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
    }
}