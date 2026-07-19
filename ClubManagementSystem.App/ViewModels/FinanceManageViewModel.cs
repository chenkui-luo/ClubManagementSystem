using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using ClubManagementSystem.App.Models;
using ClubManagementSystem.App.Services;

namespace ClubManagementSystem.App.ViewModels
{
    public class FinanceManageViewModel : ViewModelBase
    {
        private readonly ClubService _clubService = new ClubService();
        private readonly FinanceService _financeService = new FinanceService();

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
            set { SetProperty(ref _selectedClub, value); LoadRecords(); }
        }

        private ObservableCollection<FinanceRecord> _records;
        public ObservableCollection<FinanceRecord> Records
        {
            get => _records;
            set => SetProperty(ref _records, value);
        }

        private FinanceRecord _selectedRecord;
        public FinanceRecord SelectedRecord
        {
            get => _selectedRecord;
            set => SetProperty(ref _selectedRecord, value);
        }

        private decimal _balance;
        public decimal Balance { get => _balance; set { SetProperty(ref _balance, value); OnPropertyChanged(nameof(BalanceDisplay)); } }
        public string BalanceDisplay => Balance >= 0 ? $"¥{Balance:N2}" : $"¥-{(-Balance):N2}";

        private string _newType = "支出";
        public string NewType { get => _newType; set => SetProperty(ref _newType, value); }

        private decimal _newAmount;
        public decimal NewAmount { get => _newAmount; set => SetProperty(ref _newAmount, value); }

        private string _newDescription;
        public string NewDescription { get => _newDescription; set => SetProperty(ref _newDescription, value); }

        private bool _isAdding;
        public bool IsAdding { get => _isAdding; set => SetProperty(ref _isAdding, value); }

        // 权限
        public bool CanManageFinance => SelectedClub != null &&
            (Session.IsAdmin || Session.MyManagedClubIds.Contains(SelectedClub.ClubId));
        public bool CanViewFinance => !Session.IsVisitor;

        public ICommand ShowAddCommand { get; }
        public ICommand SaveRecordCommand { get; }
        public ICommand DeleteRecordCommand { get; }
        public ICommand CancelAddCommand { get; }

        public FinanceManageViewModel()
        {
            ShowAddCommand = new RelayCommand(ShowAdd, _ => CanManageFinance);
            SaveRecordCommand = new RelayCommand(SaveRecord);
            DeleteRecordCommand = new RelayCommand(DeleteRecord, _ => CanManageFinance);
            CancelAddCommand = new RelayCommand(CancelAdd);
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

        private void LoadRecords()
        {
            if (SelectedClub == null) { Records = null; return; }
            Records = new ObservableCollection<FinanceRecord>(
                _financeService.GetRecords(SelectedClub.ClubId));
            Balance = _financeService.GetBalance(SelectedClub.ClubId);
            OnPropertyChanged(nameof(CanManageFinance));
        }

        private void ShowAdd(object obj)
        {
            if (!CanManageFinance)
            {
                System.Windows.MessageBox.Show("您没有权限管理该社团的财务。", "提示",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            IsAdding = true;
        }

        private void SaveRecord(object obj)
        {
            if (SelectedClub == null || NewAmount <= 0) return;
            if (string.IsNullOrWhiteSpace(NewType))
            {
                System.Windows.MessageBox.Show("请选择收入或支出类型。", "提示",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            _financeService.AddRecord(new FinanceRecord
            {
                ClubId = SelectedClub.ClubId,
                Type = NewType,
                Amount = NewAmount,
                Description = NewDescription,
                HandlerId = Session.CurrentUser.UserId
            });
            CancelAdd(null);
            LoadRecords();
        }

        private void DeleteRecord(object obj)
        {
            if (SelectedRecord == null) return;
            _financeService.DeleteRecord(SelectedRecord.Id);
            LoadRecords();
        }

        private void CancelAdd(object obj)
        {
            IsAdding = false;
            NewType = "支出";
            NewAmount = 0;
            NewDescription = "";
        }
    }
}
