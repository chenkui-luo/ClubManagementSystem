using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ClubManagementSystem.App.Models;
using ClubManagementSystem.App.Services;

namespace ClubManagementSystem.App.ViewModels
{
    public class ClubManageViewModel : ViewModelBase
    {
        private readonly ClubService _clubService = new ClubService();
        private readonly MemberService _memberService = new MemberService();
        private ObservableCollection<Club> _clubs;
        private ObservableCollection<Category> _categories;
        private string _searchKeyword;
        private Category _selectedCategoryFilter;

        private Club _selectedClub;
        public Club SelectedClub
        {
            get => _selectedClub;
            set => SetProperty(ref _selectedClub, value);
        }

        public ObservableCollection<Club> Clubs
        {
            get => _clubs;
            set => SetProperty(ref _clubs, value);
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public string SearchKeyword
        {
            get => _searchKeyword;
            set { SetProperty(ref _searchKeyword, value); Search(); }
        }

        public Category SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set { SetProperty(ref _selectedCategoryFilter, value); Search(); }
        }

        // Form fields
        private string _newClubName;
        public string NewClubName { get => _newClubName; set => SetProperty(ref _newClubName, value); }

        private Category _newClubCategory;
        public Category NewClubCategory { get => _newClubCategory; set => SetProperty(ref _newClubCategory, value); }

        private string _newClubDescription;
        public string NewClubDescription { get => _newClubDescription; set => SetProperty(ref _newClubDescription, value); }

        private string _newClubContact;
        public string NewClubContact { get => _newClubContact; set => SetProperty(ref _newClubContact, value); }

        private bool _isEditing;
        public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }

        // 权限判断
        public bool CanCreateClub => true;
        public bool CanEditClub => Session.IsAdmin || (SelectedClub != null && Session.MyManagedClubIds.Contains(SelectedClub.ClubId));
        public bool CanDeleteClub => Session.IsAdmin;
        public bool CanJoinClub => SelectedClub != null && !Session.IsAdmin
            && SelectedClub.Status == "正常"
            && !Session.MyAllClubIds.Contains(SelectedClub.ClubId);

        public ICommand SearchCommand { get; }
        public ICommand CreateClubCommand { get; }
        public ICommand EditClubCommand { get; }
        public ICommand SaveClubCommand { get; }
        public ICommand DeleteClubCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand JoinClubCommand { get; }

        public ClubManageViewModel()
        {
            SearchCommand = new RelayCommand(Search);
            CreateClubCommand = new RelayCommand(PrepareCreate, _ => CanCreateClub);
            EditClubCommand = new RelayCommand(PrepareEdit);
            SaveClubCommand = new RelayCommand(SaveClub);
            DeleteClubCommand = new RelayCommand(DeleteClub, _ => CanDeleteClub);
            CancelEditCommand = new RelayCommand(CancelEdit);
            JoinClubCommand = new RelayCommand(JoinClub, _ => CanJoinClub);

            LoadData();
        }

        private void LoadData()
        {
            Categories = new ObservableCollection<Category>(_clubService.GetAllCategories());
            Search();
        }

        private void Search(object obj = null)
        {
            int? categoryId = SelectedCategoryFilter?.Id;
            var allClubs = _clubService.SearchClubs(SearchKeyword, categoryId);

            if (Session.IsAdmin)
            {
                // 管理员看到所有社团(含已注销)
                Clubs = new ObservableCollection<Club>(allClubs);
            }
            else
            {
                // 非管理员只看到正常状态的社团
                var activeClubs = allClubs.Where(c => c.Status == "正常").ToList();
                Clubs = new ObservableCollection<Club>(activeClubs);
            }
        }

        private void PrepareCreate(object obj)
        {
            NewClubName = "";
            NewClubCategory = null;
            NewClubDescription = "";
            NewClubContact = "";
            IsEditing = true;
            SelectedClub = null;
        }

        private void PrepareEdit(object obj)
        {
            if (SelectedClub == null) return;
            if (!Session.IsAdmin && !Session.MyManagedClubIds.Contains(SelectedClub.ClubId))
            {
                MessageBox.Show("您没有权限编辑该社团。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            NewClubName = SelectedClub.Name;
            NewClubCategory = Categories.FirstOrDefault(c => c.Id == SelectedClub.CategoryId);
            NewClubDescription = SelectedClub.Description;
            NewClubContact = SelectedClub.Contact;
            IsEditing = true;
        }

        private void SaveClub(object obj)
        {
            if (string.IsNullOrWhiteSpace(NewClubName)) return;

            if (SelectedClub == null)
            {
                _clubService.CreateClub(new Club
                {
                    Name = NewClubName,
                    CategoryId = NewClubCategory?.Id,
                    Description = NewClubDescription,
                    Contact = NewClubContact
                }, Session.CurrentUser.UserId, Session.CurrentUser.Role);

                // 创建社团后刷新用户身份（成为社长），重建菜单
                RefreshSession();
                Views.MainWindow.TriggerMenuRebuild();
            }
            else
            {
                if (!Session.IsAdmin && !Session.MyManagedClubIds.Contains(SelectedClub.ClubId)) return;
                SelectedClub.Name = NewClubName;
                SelectedClub.CategoryId = NewClubCategory?.Id;
                SelectedClub.Description = NewClubDescription;
                SelectedClub.Contact = NewClubContact;
                _clubService.UpdateClub(SelectedClub);
            }

            CancelEdit(null);
            LoadData();
        }

        private void DeleteClub(object obj)
        {
            if (SelectedClub == null) return;
            if (!Session.IsAdmin)
            {
                MessageBox.Show("只有管理员可以删除社团。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var result = MessageBox.Show(
                $"确定要删除社团 \"{SelectedClub.Name}\" 吗？", "确认删除",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _clubService.DeleteClub(SelectedClub.ClubId);
                LoadData();
            }
        }

        private void CancelEdit(object obj)
        {
            IsEditing = false;
            NewClubName = "";
            NewClubCategory = null;
            NewClubDescription = "";
            NewClubContact = "";
        }

        private void JoinClub(object obj)
        {
            if (SelectedClub == null) return;
            if (Session.MyAllClubIds.Contains(SelectedClub.ClubId))
            {
                MessageBox.Show("您已经是该社团的成员。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确定要申请加入 \"{SelectedClub.Name}\" 吗？", "申请加入社团",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var success = _memberService.ApplyJoin(SelectedClub.ClubId, Session.CurrentUser.UserId);
            if (success)
                MessageBox.Show("申请已提交，请等待社团管理员审批。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("申请失败，您可能已经申请过该社团。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void RefreshSession()
        {
            var userService = new UserService();
            var user = userService.ReloadWithMemberships(Session.CurrentUser.UserId);
            if (user != null) Session.CurrentUser = user;
        }
    }
}
