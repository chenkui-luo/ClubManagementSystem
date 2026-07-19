using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ClubManagementSystem.App.Models;
using ClubManagementSystem.App.Services;

namespace ClubManagementSystem.App.ViewModels
{
    public class AssetManageViewModel : ViewModelBase
    {
        private readonly ClubService _clubService = new ClubService();
        private readonly AssetService _assetService = new AssetService();

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
            set { SetProperty(ref _selectedClub, value); LoadData(); }
        }

        private ObservableCollection<VenueApplication> _venueApps;
        public ObservableCollection<VenueApplication> VenueApps
        {
            get => _venueApps;
            set => SetProperty(ref _venueApps, value);
        }

        private VenueApplication _selectedVenueApp;
        public VenueApplication SelectedVenueApp
        {
            get => _selectedVenueApp;
            set => SetProperty(ref _selectedVenueApp, value);
        }

        // Venue form
        private string _venueName;
        public string VenueName { get => _venueName; set => SetProperty(ref _venueName, value); }
        private string _venuePurpose;
        public string VenuePurpose { get => _venuePurpose; set => SetProperty(ref _venuePurpose, value); }
        private DateTime _venueStart = DateTime.Now.AddDays(1);
        public DateTime VenueStart { get => _venueStart; set => SetProperty(ref _venueStart, value); }
        private DateTime _venueEnd = DateTime.Now.AddDays(1).AddHours(3);
        public DateTime VenueEnd { get => _venueEnd; set => SetProperty(ref _venueEnd, value); }
        private bool _isApplyingVenue;
        public bool IsApplyingVenue { get => _isApplyingVenue; set => SetProperty(ref _isApplyingVenue, value); }

        // 权限
        public bool CanApplyVenue => SelectedClub != null &&
            (Session.IsAdmin || Session.MyManagedClubIds.Contains(SelectedClub.ClubId));
        public bool CanApproveVenue => Session.IsAdmin;

        public ICommand ShowApplyVenueCommand { get; }
        public ICommand SubmitVenueCommand { get; }
        public ICommand CancelVenueCommand { get; }
        public ICommand ApproveVenueCommand { get; }
        public ICommand RejectVenueCommand { get; }

        public AssetManageViewModel()
        {
            ShowApplyVenueCommand = new RelayCommand(ShowApplyVenue, _ => CanApplyVenue);
            SubmitVenueCommand = new RelayCommand(SubmitVenue);
            CancelVenueCommand = new RelayCommand(CancelVenue);
            ApproveVenueCommand = new RelayCommand(ApproveVenue, _ => CanApproveVenue);
            RejectVenueCommand = new RelayCommand(RejectVenue, _ => CanApproveVenue);
            LoadClubs();
        }

        private void LoadClubs()
        {
            if (Session.IsAdmin)
                Clubs = new ObservableCollection<Club>(_clubService.GetAllClubs().Where(c => c.Status != "已注销"));
            else
                Clubs = new ObservableCollection<Club>(_clubService.GetAllClubs()
                    .Where(c => Session.MyManagedClubIds.Contains(c.ClubId)));
        }

        private void LoadData()
        {
            if (SelectedClub == null) return;
            VenueApps = new ObservableCollection<VenueApplication>(
                _assetService.GetVenueApplications(SelectedClub.ClubId));
            OnPropertyChanged(nameof(CanApplyVenue));
        }

        private void ShowApplyVenue(object obj) => IsApplyingVenue = true;
        private void SubmitVenue(object obj)
        {
            if (SelectedClub == null || string.IsNullOrWhiteSpace(VenueName)) return;
            _assetService.ApplyVenue(new VenueApplication
            {
                ClubId = SelectedClub.ClubId,
                Venue = VenueName,
                Purpose = VenuePurpose,
                StartTime = VenueStart,
                EndTime = VenueEnd,
                ApplicantId = Session.CurrentUser.UserId
            });
            CancelVenue(null);
            LoadData();
        }
        private void CancelVenue(object obj) { IsApplyingVenue = false; VenueName = ""; VenuePurpose = ""; }

        private void ApproveVenue(object obj)
        {
            if (SelectedVenueApp == null) return;
            _assetService.ProcessVenueApplication(SelectedVenueApp.Id, true);
            LoadData();
        }

        private void RejectVenue(object obj)
        {
            if (SelectedVenueApp == null) return;
            _assetService.ProcessVenueApplication(SelectedVenueApp.Id, false);
            LoadData();
        }
    }
}
