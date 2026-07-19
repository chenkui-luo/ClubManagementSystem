using MahApps.Metro.Controls;

namespace ClubManagementSystem.App.Views
{
    public partial class LoginWindow : MetroWindow
    {
        public LoginWindow()
        {
            InitializeComponent();
            PasswordBox.Password = "admin123";
        }
    }
}
