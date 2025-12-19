using System;
using System.Windows;
using System.Windows.Controls;
using MySqlConnector;
using System.Threading.Tasks;
using BCrypt.Net;
using Tune.Frontend.Helpers;
using Microsoft.VisualBasic;
using System.Security.Cryptography.X509Certificates;

namespace Tune.Frontend.Pages
{
    public static class CurrentUser
    {
        public static int loggedInUserId{get;set;}=-1;
        public static string loggedInUser{get;set;}=string.Empty;
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
            string connStr = AppConfig.GetConnectionString("MariaDB");


            using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            string sqlGetAllHashes = "SELECT hashpassword, userID, username FROM users";
            using var cmd = new MySqlCommand(sqlGetAllHashes, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            bool passwordMatched = false;
            while (await reader.ReadAsync())
            {
                string storedHash = reader.GetString("hashpassword");
                int userID= reader.GetInt32("userID");
                string username=reader.GetString("username");

                if (BCrypt.Net.BCrypt.Verify(password, storedHash))
                {   
                    passwordMatched = true;
                    
                    CurrentUser.loggedInUserId = userID;
                    CurrentUser.loggedInUser = username;
                    break;
                }
            }
            if (passwordMatched == true)
            {
                MessageBox.Show("Login Successful");
                NavigationService?.Navigate(new MainPage());
            }
            else
            {
                MessageBox.Show("Password is incorrect or user does not exist");
            }
        }
        private void SignUp_click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new SignUpPage());
        }
    }
}
