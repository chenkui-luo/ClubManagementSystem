using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ClubManagementSystem.App.Models;
using ClubManagementSystem.App.Services;

namespace ClubManagementSystem.App.ViewModels
{
    public class MyClubsViewModel : ViewModelBase
    {
        private readonly MemberService _memberService = new MemberService();
        private readonly UserService _userService = new UserService();

        private ObservableCollection<ClubMember> _myMemberships;
        public ObservableCollection<ClubMember> MyMemberships
        {
            get => _myMemberships;
            set => SetProperty(ref _myMemberships, value);
        }

        private ObservableCollection<JoinApplication> _myApplications;
        public ObservableCollection<JoinApplication> MyApplications
        {
            get => _myApplications;
            set => SetProperty(ref _myApplications, value);
        }

        private ClubMember _selectedMembership;
        public ClubMember SelectedMembership
        {
            get => _selectedMembership;
            set => SetProperty(ref _selectedMembership, value);
        }

        public bool CanLeaveClub => SelectedMembership != null && SelectedMembership.Role != "社长";

        public ICommand LeaveClubCommand { get; }

        public MyClubsViewModel()
        {
            LeaveClubCommand = new RelayCommand(LeaveClub);
            LoadData();
        }

        private void LoadData()
        {
            // 重新加载用户的社团成员信息
            var user = _userService.ReloadWithMemberships(Session.CurrentUser.UserId);
            if (user != null)
            {
                Session.CurrentUser = user;
                MyMemberships = new ObservableCollection<ClubMember>(user.ClubMembers);
            }
            else
            {
                MyMemberships = new ObservableCollection<ClubMember>();
            }

            MyApplications = new ObservableCollection<JoinApplication>(
                _memberService.GetMyApplications(Session.CurrentUser.UserId));
        }

        private void LeaveClub(object obj)
        {
            if (SelectedMembership == null) return;
            if (SelectedMembership.Role == "社长")
            {
                System.Windows.MessageBox.Show("社长不能直接退出社团，请先转让社长身份。", "提示",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show(
                $"确定要退出 \"{(SelectedMembership.Club != null ? SelectedMembership.Club.Name : "")}\" 吗？",
                "确认退出", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);

            if (result != System.Windows.MessageBoxResult.Yes) return;

            _memberService.RemoveMember(SelectedMembership.Id);
            LoadData();

            // 刷新菜单
            Views.MainWindow.TriggerMenuRebuild();
        }
    }
}
