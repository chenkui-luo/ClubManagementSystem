using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClubManagementSystem.App.Models;
using ClubManagementSystem.App.Services;

namespace ClubManagementSystem.App.ViewModels
{
    public class ActivityManageViewModel : ViewModelBase
    {
        private readonly ClubService _clubService = new ClubService();
        private readonly ActivityService _activityService = new ActivityService();

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
            set { SetProperty(ref _selectedClub, value); LoadActivities(); }
        }

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

        // Form
        private string _newTitle;
        public string NewTitle { get => _newTitle; set => SetProperty(ref _newTitle, value); }

        private string _newDescription;
        public string NewDescription { get => _newDescription; set => SetProperty(ref _newDescription, value); }

        private string _newLocation;
        public string NewLocation { get => _newLocation; set => SetProperty(ref _newLocation, value); }

        private DateTime _newStartTime = DateTime.Now.AddDays(1);
        public DateTime NewStartTime { get => _newStartTime; set => SetProperty(ref _newStartTime, value); }

        private DateTime _newEndTime = DateTime.Now.AddDays(1).AddHours(2);
        public DateTime NewEndTime { get => _newEndTime; set => SetProperty(ref _newEndTime, value); }

        private int? _newMaxPeople;
        public int? NewMaxPeople { get => _newMaxPeople; set => SetProperty(ref _newMaxPeople, value); }

        private string _summaryText;
        public string SummaryText { get => _summaryText; set => SetProperty(ref _summaryText, value); }

        private bool _isCreating;
        public bool IsCreating { get => _isCreating; set => SetProperty(ref _isCreating, value); }

        // 权限
        public bool CanCreateActivity => SelectedClub != null &&
            (Session.IsAdmin || Session.MyManagedClubIds.Contains(SelectedClub.ClubId));
        public bool CanManageActivity => SelectedActivity != null &&
            (Session.IsAdmin || Session.MyManagedClubIds.Contains(SelectedActivity.ClubId));

        public ICommand ShowCreateCommand { get; }
        public ICommand SaveActivityCommand { get; }
        public ICommand CancelActivityCommand { get; }
        public ICommand FinishActivityCommand { get; }
        public ICommand SignInCommand { get; }
        public ICommand CancelCreateCommand { get; }

        public ActivityManageViewModel()
        {
            ShowCreateCommand = new RelayCommand(ShowCreate, _ => CanCreateActivity);
            SaveActivityCommand = new RelayCommand(SaveActivity);
            CancelActivityCommand = new RelayCommand(CancelActivity, _ => CanManageActivity);
            FinishActivityCommand = new RelayCommand(FinishActivity, _ => CanManageActivity);
            SignInCommand = new RelayCommand(SignIn);
            CancelCreateCommand = new RelayCommand(CancelCreate);
            LoadData();
        }

        private void LoadData()
        {
            if (Session.IsAdmin)
                Clubs = new ObservableCollection<Club>(_clubService.GetAllClubs().Where(c => c.Status != "已注销"));
            else
                Clubs = new ObservableCollection<Club>(_clubService.GetAllClubs()
                    .Where(c => Session.MyAllClubIds.Contains(c.ClubId)));
        }

        private void LoadActivities()
        {
            if (SelectedClub == null) { Activities = null; return; }
            Activities = new ObservableCollection<Activity>(
                _activityService.GetClubActivities(SelectedClub.ClubId));
            OnPropertyChanged(nameof(CanCreateActivity));
        }

        private void LoadRegistrations()
        {
            if (SelectedActivity == null) { Registrations = null; return; }
            Registrations = new ObservableCollection<ActivityRegistration>(
                _activityService.GetRegistrations(SelectedActivity.Id));
        }

        private void ShowCreate(object obj)
        {
            if (!CanCreateActivity)
            {
                MessageBox.Show("您没有权限为该社团创建活动。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            IsCreating = true;
        }

        private void SaveActivity(object obj)
        {
            if (SelectedClub == null || string.IsNullOrWhiteSpace(NewTitle)) return;
            _activityService.CreateActivity(new Activity
            {
                ClubId = SelectedClub.ClubId,
                Title = NewTitle,
                Description = NewDescription,
                Location = NewLocation,
                StartTime = NewStartTime,
                EndTime = NewEndTime,
                MaxPeople = NewMaxPeople
            });
            CancelCreate(null);
            LoadActivities();
        }

        private void CancelActivity(object obj)
        {
            if (SelectedActivity == null) return;
            _activityService.CancelActivity(SelectedActivity.Id);
            LoadActivities();
        }

        private void FinishActivity(object obj)
        {
            if (SelectedActivity == null) return;
            _activityService.FinishActivity(SelectedActivity.Id, SummaryText);
            LoadActivities();
        }

        private void SignIn(object obj)
        {
            if (SelectedActivity == null) return;
            // 找到当前用户的报名记录
            var reg = Registrations.FirstOrDefault(r => r.UserId == Session.CurrentUser.UserId);
            if (reg == null)
            {
                // 自动报名
                _activityService.RegisterForActivity(SelectedActivity.Id, Session.CurrentUser.UserId);
                LoadRegistrations();
            }
            reg = Registrations.FirstOrDefault(r => r.UserId == Session.CurrentUser.UserId);
            if (reg != null)
                _activityService.SignIn(reg.Id);
            LoadRegistrations();
        }

        private void CancelCreate(object obj)
        {
            IsCreating = false;
            NewTitle = "";
            NewDescription = "";
            NewLocation = "";
            NewMaxPeople = null;
        }
    }
}
