using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ClubManagementSystem.App.Models;
using ClubManagementSystem.App.Services;

namespace ClubManagementSystem.App.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ClubService _clubService = new ClubService();
        private readonly NoticeService _noticeService = new NoticeService();
        private ViewModelBase _currentView;
        private string _pageTitle;

        public ViewModelBase CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string PageTitle
        {
            get => _pageTitle;
            set => SetProperty(ref _pageTitle, value);
        }

        // 身份信息
        public string UserDisplayName => Session.CurrentUser?.Name ?? "";
        public string UserRoleLabel => Session.EffectiveRoleLabel;
        public bool IsAdmin => Session.IsAdmin;
        public bool IsVisitor => Session.IsVisitor;

        // Dashboard data
        private int _totalClubs;
        public int TotalClubs { get => _totalClubs; set => SetProperty(ref _totalClubs, value); }

        private int _totalMembers;
        public int TotalMembers { get => _totalMembers; set => SetProperty(ref _totalMembers, value); }

        private int _totalActivities;
        public int TotalActivities { get => _totalActivities; set => SetProperty(ref _totalActivities, value); }

        private int _totalNotices;
        public int TotalNotices { get => _totalNotices; set => SetProperty(ref _totalNotices, value); }

        private ObservableCollection<Club> _recentClubs;
        public ObservableCollection<Club> RecentClubs
        {
            get => _recentClubs;
            set => SetProperty(ref _recentClubs, value);
        }

        private ObservableCollection<Notice> _recentNotices;
        public ObservableCollection<Notice> RecentNotices
        {
            get => _recentNotices;
            set => SetProperty(ref _recentNotices, value);
        }

        // Navigation commands
        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowClubManageCommand { get; }
        public ICommand ShowMemberManageCommand { get; }
        public ICommand ShowActivityManageCommand { get; }
        public ICommand ShowFinanceManageCommand { get; }
        public ICommand ShowNoticeManageCommand { get; }
        public ICommand ShowAssetManageCommand { get; }
        public ICommand ShowMyClubsCommand { get; }
        public ICommand ShowActivityBoardCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand RefreshCommand { get; }

        public MainViewModel()
        {
            ShowDashboardCommand = new RelayCommand(ShowDashboard);
            ShowClubManageCommand = new RelayCommand(ShowClubManage);
            ShowMemberManageCommand = new RelayCommand(ShowMemberManage);
            ShowActivityManageCommand = new RelayCommand(ShowActivityManage);
            ShowFinanceManageCommand = new RelayCommand(ShowFinanceManage);
            ShowNoticeManageCommand = new RelayCommand(ShowNoticeManage);
            ShowAssetManageCommand = new RelayCommand(ShowAssetManage);
            ShowMyClubsCommand = new RelayCommand(ShowMyClubs);
            ShowActivityBoardCommand = new RelayCommand(ShowActivityBoard);
            LogoutCommand = new RelayCommand(Logout);
            RefreshCommand = new RelayCommand(Refresh);

            ShowDashboard(null);
        }

        private void ShowDashboard(object obj)
        {
            PageTitle = "控制台首页";
            LoadDashboardData();
        }

        private void ShowClubManage(object obj)
        {
            PageTitle = "社团管理";
            CurrentView = new ClubManageViewModel();
        }

        private void ShowMemberManage(object obj)
        {
            PageTitle = "成员管理";
            CurrentView = new MemberManageViewModel();
        }

        private void ShowActivityManage(object obj)
        {
            PageTitle = "活动管理";
            CurrentView = new ActivityManageViewModel();
        }

        private void ShowFinanceManage(object obj)
        {
            PageTitle = "财务管理";
            CurrentView = new FinanceManageViewModel();
        }

        private void ShowNoticeManage(object obj)
        {
            PageTitle = "通知公告";
            CurrentView = new NoticeManageViewModel();
        }

        private void ShowAssetManage(object obj)
        {
            PageTitle = "场地申请";
            CurrentView = new AssetManageViewModel();
        }

        private void ShowMyClubs(object obj)
        {
            PageTitle = "我的社团";
            CurrentView = new MyClubsViewModel();
        }

        private void ShowActivityBoard(object obj)
        {
            PageTitle = "活动广场";
            CurrentView = new ActivityBoardViewModel();
        }

        private void LoadDashboardData()
        {
            CurrentView = null;
            try
            {
                var clubs = _clubService.GetAllClubs();
                TotalClubs = clubs.Count(c => c.Status != "已注销");
                TotalMembers = clubs.SelectMany(c => c.ClubMembers ?? Enumerable.Empty<ClubMember>())
                                     .Select(m => m.UserId).Distinct().Count();

                using (var db = new AppDbContext())
                {
                    TotalActivities = db.Activities.Count(a => a.Status != "已取消");
                    TotalNotices = db.Notices.Count();
                }

                RecentClubs = new ObservableCollection<Club>(clubs.Where(c => c.Status != "已注销").Take(5));
                RecentNotices = new ObservableCollection<Notice>(_noticeService.GetAllNotices()
                    .Where(n => n.ClubId == null || Session.IsAdmin || Session.MyAllClubIds.Contains(n.ClubId.Value))
                    .Take(5));
            }
            catch (Exception)
            {
                TotalClubs = 0;
                TotalMembers = 0;
                TotalActivities = 0;
                TotalNotices = 0;
                RecentClubs = new ObservableCollection<Club>();
                RecentNotices = new ObservableCollection<Notice>();
            }

            OnPropertyChanged(nameof(UserDisplayName));
            OnPropertyChanged(nameof(UserRoleLabel));
            OnPropertyChanged(nameof(IsAdmin));
            OnPropertyChanged(nameof(IsVisitor));
        }

        private void Refresh(object obj)
        {
            // 重新加载 ClubMembers
            var userService = new UserService();
            var freshUser = userService.ReloadWithMemberships(Session.CurrentUser.UserId);
            if (freshUser != null)
            {
                Session.CurrentUser = freshUser;
            }
            ShowDashboard(null);
            OnPropertyChanged(nameof(UserDisplayName));
            OnPropertyChanged(nameof(UserRoleLabel));
            OnPropertyChanged(nameof(IsAdmin));
            OnPropertyChanged(nameof(IsVisitor));
        }

        private void Logout(object obj)
        {
            LoginViewModel.CurrentUser = null;
            Session.CurrentUser = null;
            var loginWindow = new Views.LoginWindow();
            var currentMain = System.Windows.Application.Current.MainWindow;
            loginWindow.Show();
            System.Windows.Application.Current.MainWindow = loginWindow;
            currentMain?.Close();
        }
    }
}
