using System;
using System.Windows;
using System.Windows.Controls;
using Tune.Frontend.Pages;

namespace Tune.Frontend.Pages
{
    public partial class MainPage : Page
    {
        public MainPage()
        {   
            InitializeComponent();
            int userId = CurrentUser.loggedInUserId;
            string username = CurrentUser.loggedInUser;
            UsernameDisplay.Text = username;
            UserIdDisplay.Text = userId.ToString();
        }

    }
}