using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClubManagementSystem.App.Models;
using ClubManagementSystem.App.Services;

namespace ClubManagementSystem.App.ViewModels
{
    public class MemberManageViewModel : ViewModelBase
    {
        private readonly ClubService _clubService = new ClubService();
        private readonly MemberService _memberService = new MemberService();
        private int _lastMemberCount = 0;

        private ObservableCollection<Club> _clubs;
        public ObservableCollection<Club> Clubs
        {
            get => _clubs;
            set => SetProperty(ref _clubs, value);
        }

        private Club _selectedClub;
        public Club SelectedClub
        {
            get => _selectedClub;
            set { SetProperty(ref _selectedClub, value); LoadMembers(); LoadApplications(); }
        }

        private ObservableCollection<ClubMember> _members;
        public ObservableCollection<ClubMember> Members
        {
            get => _members;
            set => SetProperty(ref _members, value);
        }

        private ClubMember _selectedMember;
        public ClubMember SelectedMember
        {
            get => _selectedMember;
            set => SetProperty(ref _selectedMember, value);
        }

        private ObservableCollection<JoinApplication> _applications;
        public ObservableCollection<JoinApplication> Applications
        {
            get => _applications;
            set => SetProperty(ref _applications, value);
        }

        private JoinApplication _selectedApplication;
        public JoinApplication SelectedApplication
        {
            get => _selectedApplication;
            set => SetProperty(ref _selectedApplication, value);
        }

        private string _newPosition;
        public string NewPosition { get => _newPosition; set => SetProperty(ref _newPosition, value); }

        // 权限
        public bool CanManageMembers => SelectedClub != null &&
            (Session.IsAdmin || Session.MyManagedClubIds.Contains(SelectedClub.ClubId));

        // 是否显示待审批区域
        public bool CanSeeApplications => Session.IsAdmin || Session.MyManagedClubIds.Count > 0;

        // 只有管理员和该社团的社长才能设置/转让社长
        public bool CanSetPresident => SelectedMember != null
            && SelectedMember.Role != "社长"
            && (Session.IsAdmin || (SelectedClub != null && Session.IsPresidentOf(SelectedClub.ClubId)));

        public ICommand PromoteCommand { get; }
        public ICommand DemoteCommand { get; }
        public ICommand SetPresidentCommand { get; }
        public ICommand RemoveMemberCommand { get; }
        public ICommand ApproveApplicationCommand { get; }
        public ICommand RejectApplicationCommand { get; }

        public MemberManageViewModel()
        {
            PromoteCommand = new RelayCommand(PromoteMember, _ => CanManageMembers);
            DemoteCommand = new RelayCommand(DemoteMember, _ => CanManageMembers);
            SetPresidentCommand = new RelayCommand(SetPresident, _ => CanSetPresident);
            RemoveMemberCommand = new RelayCommand(RemoveMember, _ => CanManageMembers);
            ApproveApplicationCommand = new RelayCommand(ApproveApplication);
            RejectApplicationCommand = new RelayCommand(RejectApplication);
            LoadData();
        }

        private void LoadData()
        {
            if (Session.IsAdmin)
                Clubs = new ObservableCollection<Club>(_clubService.GetAllClubs().Where(c => c.Status != "已注销"));
            else
                Clubs = new ObservableCollection<Club>(_clubService.GetAllClubs()
                    .Where(c => Session.MyManagedClubIds.Contains(c.ClubId)));
        }

        private void LoadMembers()
        {
            if (SelectedClub == null) { Members = null; return; }
            Members = new ObservableCollection<ClubMember>(_memberService.GetMembers(SelectedClub.ClubId));
            _lastMemberCount = Members.Count;
            OnPropertyChanged(nameof(CanManageMembers));
            OnPropertyChanged(nameof(CanSetPresident));
        }

        private void LoadApplications()
        {
            if (SelectedClub == null) { Applications = null; return; }
            Applications = new ObservableCollection<JoinApplication>(
                _memberService.GetPendingApplications(SelectedClub.ClubId));
        }

        private void PromoteMember(object obj)
        {
            if (!CanManageMembers) return;
            if (SelectedMember == null || SelectedMember.Role == "社长") return;
            _memberService.AssignRole(SelectedMember.Id, "干事", NewPosition);
            LoadMembers();
        }

        private void DemoteMember(object obj)
        {
            if (!CanManageMembers) return;
            if (SelectedMember == null || SelectedMember.Role == "社长") return;
            _memberService.AssignRole(SelectedMember.Id, "成员", null);
            LoadMembers();
        }

        private void SetPresident(object obj)
        {
            if (!CanManageMembers || SelectedMember == null) return;
            if (SelectedMember.Role == "社长") return;

            var result = MessageBox.Show(
                $"确定要将 \"{SelectedMember.User?.Name}\" 设为 \"{SelectedClub.Name}\" 的社长吗？",
                "设置社长", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            _memberService.TransferPresident(SelectedClub.ClubId, SelectedMember.UserId);

            // 刷新当前用户的 Session 数据（如果当前用户就是被降级的前社长）
            var userService = new UserService();
            var freshUser = userService.ReloadWithMemberships(Session.CurrentUser.UserId);
            if (freshUser != null) Session.CurrentUser = freshUser;
            Views.MainWindow.TriggerMenuRebuild();

            LoadMembers();
            MessageBox.Show("社长已变更。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RemoveMember(object obj)
        {
            if (!CanManageMembers) return;
            if (SelectedMember == null) return;

            // 任何人都不能在成员管理界面中把自己移除
            if (SelectedMember.UserId == Session.CurrentUser.UserId)
            {
                MessageBox.Show("不能在此处移除自己，如需退出社团请在\"我的社团\"页面中操作。",
                    "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isLastMember = (_lastMemberCount <= 1);

            // 社团只剩最后一人（社长），只有管理员能处理
            if (isLastMember && !Session.IsAdmin)
            {
                MessageBox.Show("不能删除社团的最后一人，请联系管理员处理。",
                    "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (isLastMember && Session.IsAdmin)
            {
                // 管理员删除最后一人：询问是否直接删除此社团
                var adminResult = MessageBox.Show(
                    $"\"{SelectedClub.Name}\" 仅剩 \"{SelectedMember.User?.Name}\" 一人。\n\n是否直接删除整个社团？\n（选「是」=删除社团，选「否」=取消操作）",
                    "删除社团最后一人",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (adminResult == MessageBoxResult.Yes)
                {
                    _clubService.DeleteClub(SelectedClub.ClubId);
                    SelectedClub = null;
                    LoadData();
                    MessageBox.Show("社团已删除。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("不能删除社团的最后一人，请先解散社团后再操作。",
                        "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            // 正常情况：移除成员
            var result = MessageBox.Show(
                $"确定要移除成员 \"{SelectedMember.User?.Name}\" 吗？", "确认",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                // 管理员可以强制移除（包括不是最后一人时的社长）
                if (Session.IsAdmin && SelectedMember.Role == "社长")
                    _memberService.ForceRemoveMember(SelectedMember.Id);
                else
                    _memberService.RemoveMember(SelectedMember.Id);

                LoadMembers();
            }
        }

        private void ApproveApplication(object obj)
        {
            if (SelectedApplication == null) return;
            if (!CanManageMembers)
            {
                MessageBox.Show("您没有审批权限。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _memberService.ProcessApplication(SelectedApplication.Id, true);
            LoadMembers();
            LoadApplications();
        }

        private void RejectApplication(object obj)
        {
            if (SelectedApplication == null) return;
            if (!CanManageMembers)
            {
                MessageBox.Show("您没有审批权限。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _memberService.ProcessApplication(SelectedApplication.Id, false);
            LoadApplications();
        }
    }
}
