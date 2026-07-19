using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ClubManagementSystem.App.Models;
using ClubManagementSystem.App.Services;

namespace ClubManagementSystem.App.ViewModels
{
    public class NoticeManageViewModel : ViewModelBase
    {
        private readonly ClubService _clubService = new ClubService();
        private readonly NoticeService _noticeService = new NoticeService();

        private ObservableCollection<Notice> _allNotices;
        public ObservableCollection<Notice> AllNotices
        {
            get => _allNotices;
            set => SetProperty(ref _allNotices, value);
        }

        private Notice _selectedNotice;
        public Notice SelectedNotice
        {
            get => _selectedNotice;
            set => SetProperty(ref _selectedNotice, value);
        }

        private string _newTitle;
        public string NewTitle { get => _newTitle; set => SetProperty(ref _newTitle, value); }

        private string _newContent;
        public string NewContent { get => _newContent; set => SetProperty(ref _newContent, value); }

        private Club _selectedTargetClub;
        public Club SelectedTargetClub { get => _selectedTargetClub; set => SetProperty(ref _selectedTargetClub, value); }

        private ObservableCollection<Club> _targetClubs;
        public ObservableCollection<Club> TargetClubs
        {
            get => _targetClubs;
            set => SetProperty(ref _targetClubs, value);
        }

        private bool _isPublishing;
        public bool IsPublishing { get => _isPublishing; set => SetProperty(ref _isPublishing, value); }

        // 权限
        public bool CanPublish => Session.IsAdmin || Session.MyManagedClubIds.Count > 0;
        public bool CanDelete(Notice notice) =>
            Session.IsAdmin || (notice != null && Session.MyManagedClubIds.Contains(notice.ClubId ?? 0));

        public ICommand ShowPublishCommand { get; }
        public ICommand PublishCommand { get; }
        public ICommand DeleteNoticeCommand { get; }
        public ICommand CancelPublishCommand { get; }

        public NoticeManageViewModel()
        {
            ShowPublishCommand = new RelayCommand(ShowPublish, _ => CanPublish);
            PublishCommand = new RelayCommand(Publish);
            DeleteNoticeCommand = new RelayCommand(DeleteNotice);
            CancelPublishCommand = new RelayCommand(CancelPublish);
            LoadData();
        }

        private void LoadData()
        {
            if (Session.IsAdmin)
            {
                TargetClubs = new ObservableCollection<Club>(_clubService.GetAllClubs().Where(c => c.Status != "已注销"));
                AllNotices = new ObservableCollection<Notice>(_noticeService.GetAllNotices());
            }
            else if (Session.MyManagedClubIds.Count > 0)
            {
                TargetClubs = new ObservableCollection<Club>(_clubService.GetAllClubs()
                    .Where(c => Session.MyManagedClubIds.Contains(c.ClubId)));
                // 显示全局公告 + 自己管理的社团公告
                var myNotices = _noticeService.GetAllNotices()
                    .Where(n => n.ClubId == null || Session.MyManagedClubIds.Contains(n.ClubId.Value)
                        || Session.MyAllClubIds.Contains(n.ClubId.Value))
                    .ToList();
                AllNotices = new ObservableCollection<Notice>(myNotices);
            }
            else
            {
                TargetClubs = new ObservableCollection<Club>();
                // 只能看全局公告和自己所在社团的公告
                var visibleNotices = _noticeService.GetAllNotices()
                    .Where(n => n.ClubId == null || Session.MyAllClubIds.Contains(n.ClubId.Value))
                    .ToList();
                AllNotices = new ObservableCollection<Notice>(visibleNotices);
            }
        }

        private void ShowPublish(object obj)
        {
            if (!CanPublish)
            {
                System.Windows.MessageBox.Show("您没有发布公告的权限。", "提示",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            IsPublishing = true;
        }

        private void Publish(object obj)
        {
            if (string.IsNullOrWhiteSpace(NewTitle) || string.IsNullOrWhiteSpace(NewContent)) return;

            _noticeService.Publish(new Notice
            {
                ClubId = SelectedTargetClub?.ClubId,
                Title = NewTitle,
                Content = NewContent,
                PublisherId = Session.CurrentUser.UserId
            });
            CancelPublish(null);
            LoadData();
        }

        private void DeleteNotice(object obj)
        {
            if (SelectedNotice == null) return;
            if (!CanDelete(SelectedNotice))
            {
                System.Windows.MessageBox.Show("您没有权限删除此公告。", "提示",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            _noticeService.DeleteNotice(SelectedNotice.Id);
            LoadData();
        }

        private void CancelPublish(object obj)
        {
            IsPublishing = false;
            NewTitle = "";
            NewContent = "";
            SelectedTargetClub = null;
        }
    }
}
