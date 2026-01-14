using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Tune.Frontend.Helpers;
using Tune.Frontend.Services;

namespace Tune.Frontend.Pages
{
    public partial class SignUpPage : Page
    {
        public SignUpPage()
        {
            InitializeComponent();
        }

        // Sign Up button click
        private async void SignUp_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text;
            string password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;
            string email = EmailBox.Text;

            // Basic validation
            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword) ||
                string.IsNullOrWhiteSpace(email))
            {
                ShowNotification("All fields must be filled!", isError: true);
                return;
            }

            if (password != confirmPassword)
            {
                ShowNotification("Passwords do not match!", isError: true);
                return;
            }

            // Call backend register
            var baseUrl = AppConfig.Configuration["BackendApiBaseUrl"];
            Tune.Frontend.Services.ApiClient.SetBaseUrl(baseUrl);

            var payload = new { Username = username, Email = email, Password = password };
            try
            {
                var resp = await Tune.Frontend.Services.ApiClient.Client.PostAsJsonAsync("/auth/register", payload);
                if (!resp.IsSuccessStatusCode)
                {
                    ShowNotification("Sign up failed: username or email may already be in use", isError: true);
                    return;
                }

                var data = await resp.Content.ReadFromJsonAsync<RegisterResponse>();
                if (data == null)
                {
                    ShowNotification("Unexpected server response", isError: true);
                    return;
                }

                // Store token and navigate
                Tune.Frontend.Pages.CurrentUser.Token = data.token;
                Tune.Frontend.Pages.CurrentUser.loggedInUserId = data.userId;
                Tune.Frontend.Pages.CurrentUser.loggedInUser = data.username;
                Tune.Frontend.Services.ApiClient.SetToken(data.token);

                ShowNotification("Account created and logged in", isError: false);
                await Task.Delay(800);
                NavigationService?.Navigate(new MainPage());

            }
            catch (Exception ex)
            {
                ShowNotification($"Error creating account: {ex.Message}", isError: true);
            }
        }

        // Show notification in the notification bar
        private void ShowNotification(string message, bool isError)
        {
            NotificationText.Text = message;
            NotificationText.Foreground = isError ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.OrangeRed) : new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#8AF3FF"));
            NotificationBar.Background = isError ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 20, 20)) : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(35, 39, 47));
            NotificationBar.Visibility = Visibility.Visible;
        }

private async Task<bool> IsUsernameTaken(string username)
{
    var baseUrl = AppConfig.Configuration["BackendApiBaseUrl"];
    Tune.Frontend.Services.ApiClient.SetBaseUrl(baseUrl);

    var users = await Tune.Frontend.Services.ApiClient.Client.GetFromJsonAsync<UserDto[]>("/users");
    if (users == null) return false;
    foreach (var u in users)
        if (string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase))
            return true;
    return false;
}

private record UserDto(int UserID, string Username, string Email);


        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new LoginPage());
        }

        public record RegisterResponse(string token, int userId, string username);
    }
}