using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClubManagementSystem.App.Models;
using ClubManagementSystem.App.Services;

namespace ClubManagementSystem.App.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly UserService _userService = new UserService();
        private string _studentId;
        private string _password;
        private string _errorMessage;

        public string StudentId
        {
            get => _studentId;
            set => SetProperty(ref _studentId, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }

        public static User CurrentUser { get; set; }

        public LoginViewModel()
        {
            StudentId = "admin";
            LoginCommand = new RelayCommand(ExecuteLogin);
            RegisterCommand = new RelayCommand(ExecuteRegister);
        }

        private void ExecuteLogin(object parameter)
        {
            ErrorMessage = string.Empty;

            // PasswordBox不支持直接绑定，需从控件参数中获取密码
            var passwordBox = parameter as PasswordBox;
            string pwd = passwordBox?.Password ?? "";

            if (string.IsNullOrWhiteSpace(StudentId) || string.IsNullOrWhiteSpace(pwd))
            {
                ErrorMessage = "请输入学号和密码";
                return;
            }

            var user = _userService.Login(StudentId, pwd);
            if (user == null)
            {
                ErrorMessage = "学号或密码错误";
                return;
            }

            CurrentUser = user;
            Session.CurrentUser = user;
            var mainWindow = new Views.MainWindow();

            // 先保存引用再关闭登录窗口
            var loginWindow = Application.Current.MainWindow;
            mainWindow.Show();
            Application.Current.MainWindow = mainWindow;
            loginWindow?.Close();
        }

        private void ExecuteRegister(object parameter)
        {
            var registerWindow = new Views.RegisterWindow();
            registerWindow.Owner = Application.Current.MainWindow;
            registerWindow.ShowDialog();
        }
    }
}
