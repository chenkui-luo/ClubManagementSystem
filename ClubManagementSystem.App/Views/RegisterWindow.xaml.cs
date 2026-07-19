using System.Windows;
using ClubManagementSystem.App.Models;
using ClubManagementSystem.App.Services;
using MahApps.Metro.Controls;

namespace ClubManagementSystem.App.Views
{
    public partial class RegisterWindow : MetroWindow
    {
        private readonly UserService _userService = new UserService();

        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Text = "";

            var studentId = TxtStudentId.Text.Trim();
            var name = TxtName.Text.Trim();
            var password = TxtPassword.Password;
            var confirmPwd = TxtConfirmPwd.Password;

            if (string.IsNullOrWhiteSpace(studentId) ||
                string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(password))
            {
                TxtError.Text = "学号、姓名和密码不能为空";
                return;
            }

            if (password != confirmPwd)
            {
                TxtError.Text = "两次输入的密码不一致";
                return;
            }

            if (password.Length < 6)
            {
                TxtError.Text = "密码长度不能少于6位";
                return;
            }

            var user = new User
            {
                StudentId = studentId,
                Name = name,
                Password = password,
                Phone = TxtPhone.Text.Trim()
            };

            var result = _userService.Register(user);
            if (result == null)
            {
                TxtError.Text = "该学号已被注册";
                return;
            }

            MessageBox.Show("注册成功！请返回登录。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
    }
}
