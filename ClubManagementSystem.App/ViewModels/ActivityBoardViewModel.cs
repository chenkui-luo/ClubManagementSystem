using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClubManagementSystem.App.Models;
using ClubManagementSystem.App.Services;

namespace ClubManagementSystem.App.ViewModels
{
    public class ActivityBoardViewModel : ViewModelBase
    {
        private readonly ActivityService _activityService = new ActivityService();

        private ObservableCollection<Activity> _activities;
        public ObservableCollection<Activity> Activities
        {
            get => _activities;
            set => SetProperty(ref _activities, value);
        }

        private Activity _selectedActivity;
        public Activity SelectedActivity
        {
            get => _selectedActivity;
            set { SetProperty(ref _selectedActivity, value); LoadRegistrations(); }
        }

        private ObservableCollection<ActivityRegistration> _registrations;
        public ObservableCollection<ActivityRegistration> Registrations
        {
            get => _registrations;
            set => SetProperty(ref _registrations, value);
        }

        private ObservableCollection<ActivityRegistration> _myRegistrations;
        public ObservableCollection<ActivityRegistration> MyRegistrations
        {
            get => _myRegistrations;
            set => SetProperty(ref _myRegistrations, value);
        }

        private ObservableCollection<ActivityRegistration> _pendingRegistrations;
        public ObservableCollection<ActivityRegistration> PendingRegistrations
        {
            get => _pendingRegistrations;
            set => SetProperty(ref _pendingRegistrations, value);
        }

        private ActivityRegistration _selectedPending;
        public ActivityRegistration SelectedPending
        {
            get => _selectedPending;
            set => SetProperty(ref _selectedPending, value);
        }

        public bool CanManageActivity => SelectedActivity != null &&
            (Session.IsAdmin || Session.MyManagedClubIds.Contains(SelectedActivity.ClubId));
        // 审批按钮权限：选中了待审批项 + 该活动属于当前用户管辖
        public bool CanApprove => SelectedPending != null &&
            (Session.IsAdmin || Session.MyManagedClubIds.Contains(SelectedPending.Activity.ClubId));
        // 是否显示待审批区域
        public bool CanSeeApproval => Session.IsAdmin || Session.MyManagedClubIds.Count > 0;

        public ICommand RegisterCommand { get; }
        public ICommand ApproveCommand { get; }
        public ICommand RejectCommand { get; }

        public ActivityBoardViewModel()
        {
            RegisterCommand = new RelayCommand(RegisterForActivity);
            ApproveCommand = new RelayCommand(ApproveRegistration);
            RejectCommand = new RelayCommand(RejectRegistration);
            LoadData();
        }

        private void LoadData()
        {
            var all = _activityService.GetAllActivities()
                .Where(a => a.Status == "报名中" || a.Status == "进行中")
                .OrderBy(a => a.StartTime)
                .ToList();

            Activities = new ObservableCollection<Activity>(all);
            MyRegistrations = new ObservableCollection<ActivityRegistration>(
                _activityService.GetMyRegistrations(Session.CurrentUser.UserId));
            LoadAllPending();
        }

        private void LoadRegistrations()
        {
            if (SelectedActivity == null) { Registrations = null; return; }

            Registrations = new ObservableCollection<ActivityRegistration>(
                _activityService.GetRegistrations(SelectedActivity.Id));
            OnPropertyChanged(nameof(CanManageActivity));
        }

        private void LoadAllPending()
        {
            if (Session.IsAdmin)
                PendingRegistrations = new ObservableCollection<ActivityRegistration>(
                    _activityService.GetAllPendingRegistrations());
            else if (Session.MyManagedClubIds.Count > 0)
                PendingRegistrations = new ObservableCollection<ActivityRegistration>(
                    _activityService.GetAllPendingRegistrations(Session.MyManagedClubIds));
            else
                PendingRegistrations = new ObservableCollection<ActivityRegistration>();
        }

        private void RegisterForActivity(object obj)
        {
            if (SelectedActivity == null) return;
            if (Session.IsAdmin)
            {
                MessageBox.Show("管理员无需报名活动。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SelectedActivity.Status != "报名中")
            {
                MessageBox.Show("该活动已截止报名。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedActivity.StartTime <= DateTime.Now)
            {
                MessageBox.Show("活动已经开始，无法报名。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (SelectedActivity.MaxPeople.HasValue)
            {
                var approvedCount = Registrations?.Count(r => r.Status == "已通过") ?? 0;
                if (approvedCount >= SelectedActivity.MaxPeople.Value)
                {
                    MessageBox.Show("报名人数已满。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var result = MessageBox.Show(
                $"确定要报名 \"{SelectedActivity.Title}\" 吗？\n\n主办社团：{SelectedActivity.Club?.Name}\n时间：{SelectedActivity.StartTime:yyyy-MM-dd HH:mm}\n地点：{SelectedActivity.Location}",
                "报名活动", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var success = _activityService.RegisterForActivity(SelectedActivity.Id, Session.CurrentUser.UserId);
            if (success)
                MessageBox.Show("报名成功，请等待审批。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("报名失败，可能已报过名或名额已满。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);

            LoadData();
            if (SelectedActivity != null) LoadRegistrations();
        }

        private void ApproveRegistration(object obj)
        {
            if (SelectedPending == null) return;
            if (!CanApprove)
            {
                MessageBox.Show("您没有审批该活动的权限。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var activity = SelectedPending.Activity;
            if (activity != null && activity.MaxPeople.HasValue)
            {
                var approvedCount = _activityService.GetRegistrations(activity.Id)
                    .Count(r => r.Status == "已通过");
                if (approvedCount >= activity.MaxPeople.Value)
                {
                    MessageBox.Show("名额已满，无法通过。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            _activityService.ProcessRegistration(SelectedPending.Id, true);
            MyRegistrations = new ObservableCollection<ActivityRegistration>(
                _activityService.GetMyRegistrations(Session.CurrentUser.UserId));
            LoadAllPending();
            if (SelectedActivity != null) LoadRegistrations();
        }

        private void RejectRegistration(object obj)
        {
            if (SelectedPending == null) return;
            if (!CanApprove)
            {
                MessageBox.Show("您没有审批该活动的权限。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _activityService.ProcessRegistration(SelectedPending.Id, false);
            MyRegistrations = new ObservableCollection<ActivityRegistration>(
                _activityService.GetMyRegistrations(Session.CurrentUser.UserId));
            LoadAllPending();
            if (SelectedActivity != null) LoadRegistrations();
        }
    }
}
