using System;
using System.Windows;
using System.Windows.Controls;
using System.Net.Http.Json;
using Tune.Frontend.Helpers;
using Tune.Frontend.Services;

namespace Tune.Frontend.Pages
{
    public static class CurrentUser
    {
        public static int loggedInUserId{get;set;}=-1;
        public static string loggedInUser{get;set;}=string.Empty;
        public static string Token { get; set; } = string.Empty;
    }
    public partial class LoginPage : Page
    {
        private readonly string connStr;

        public LoginPage()
        {
            InitializeComponent();

            connStr = AppConfig.GetConnectionString("MariaDB");
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string password = PasswordBox.Password;
            string usernameOrEmail = UsernameBox.Text;

            // Set up the API URL
            var baseUrl = AppConfig.Configuration["BackendApiBaseUrl"];
            Tune.Frontend.Services.ApiClient.SetBaseUrl(baseUrl);

            var payload = new { UsernameOrEmail = usernameOrEmail, Password = password };
            try
            {
                var resp = await Tune.Frontend.Services.ApiClient.Client.PostAsJsonAsync("/auth/login", payload);
                if (!resp.IsSuccessStatusCode)
                {
                    ShowNotification("Login failed: check username/email and password", isError: true);
                    return;
                }

                var data = await resp.Content.ReadFromJsonAsync<LoginResponse>();
                if (data == null)
                {
                    ShowNotification("Unexpected response from server", isError: true);
                    return;
                }

                CurrentUser.loggedInUserId = data.userId;
                CurrentUser.loggedInUser = data.username;
                CurrentUser.Token = data.token;
                Tune.Frontend.Services.ApiClient.SetToken(CurrentUser.Token);

                ShowNotification("Login Successful", isError: false);
                await System.Threading.Tasks.Task.Delay(800);
                NavigationService?.Navigate(new MainPage());
            }
            catch (Exception ex)
            {
                ShowNotification($"Error logging in: {ex.Message}", isError: true);
            }
        }

        // Display a message to the user at the top
        private void ShowNotification(string message, bool isError)
        {
            NotificationText.Text = message;
            NotificationText.Foreground = isError ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.OrangeRed) : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#8AF3FF"));
            NotificationBar.Background = isError ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 20, 20)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(35, 39, 47));
            NotificationBar.Visibility = Visibility.Visible;
        }

        private void SignUp_click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SignUpPage());
        }

        private record LoginResponse(string token, int userId, string username);
    }
}
