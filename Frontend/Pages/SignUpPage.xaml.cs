using System;
using System.Windows;
using System.Windows.Controls;
using MySqlConnector;
using System.Threading.Tasks;
using BCrypt.Net;
using Tune.Frontend.Helpers;

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
        MessageBox.Show("All fields must be filled!");
        return;
    }

    if (password != confirmPassword)
    {
        MessageBox.Show("Passwords do not match!");
        return;
    }

    // Check if username already exists
    if (await IsUsernameTaken(username))
    {
        MessageBox.Show("Username is already taken. Please choose another.");
        return;
    }

    // Insert new user
    try
    {
        await CreateNewUser(username, password, email);
        MessageBox.Show("User created successfully!");
        NavigationService?.Navigate(new LoginPage());
    }
    catch (Exception ex)
    {
        MessageBox.Show($"Email is already in use, if this wasen't you please check ur email");
    }
}

private async Task<bool> IsUsernameTaken(string username)
{
    string connStr = AppConfig.GetConnectionString("MariaDB");

    using var conn = new MySqlConnection(connStr);
    await conn.OpenAsync();

    string sql = "SELECT COUNT(*) FROM users WHERE username = @username";
    using var cmd = new MySqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("@username", username);

    var result = await cmd.ExecuteScalarAsync();
    int count = Convert.ToInt32(result);

    return count > 0;
}


        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new LoginPage());
        }

        public async Task CreateNewUser(string username, string password, string email)
        {
            // Hash password
            string hashPassword = BCrypt.Net.BCrypt.HashPassword(password);

            string connStr = AppConfig.GetConnectionString("MariaDB");

            using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            // Insert user
            string sql = @"INSERT INTO users (username, email, hashpassword)
                           VALUES (@username, @email, @hashpassword)";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@email", email); 
            cmd.Parameters.AddWithValue("@hashpassword", hashPassword);

            await cmd.ExecuteNonQueryAsync();

        }
    }
}