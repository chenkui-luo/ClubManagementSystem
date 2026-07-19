using System;
using System.Data.Entity;
using System.Linq;
using System.Windows;
using ClubManagementSystem.App.Models;
using ClubManagementSystem.App.Services;
using ClubManagementSystem.App.ViewModels;
using MahApps.Metro.Controls;

namespace ClubManagementSystem.App.Views
{
    public partial class MainWindow : MetroWindow
    {
        private readonly MainViewModel _viewModel;
        public static event Action RequestMenuRebuild;

        public static void TriggerMenuRebuild()
        {
            RequestMenuRebuild?.Invoke();
        }

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = DataContext as MainViewModel;
            RequestMenuRebuild += OnMenuRebuild;
            BuildMenuItems();
            ShowDashboard();
        }

        private void OnMenuRebuild()
        {
            BuildMenuItems();
        }

        private void BuildMenuItems()
        {
            var items = new HamburgerMenuItemCollection();

            // 控制台 - 所有人可见
            items.Add(new HamburgerMenuGlyphItem { Label = "控制台", Glyph = "" });

            if (Session.IsAdmin)
            {
                // 管理员: 全部功能
                items.Add(new HamburgerMenuGlyphItem { Label = "活动广场", Glyph = "" });
                items.Add(new HamburgerMenuGlyphItem { Label = "社团管理", Glyph = "" });
                items.Add(new HamburgerMenuGlyphItem { Label = "成员管理", Glyph = "" });
                items.Add(new HamburgerMenuGlyphItem { Label = "活动管理", Glyph = "" });
                items.Add(new HamburgerMenuGlyphItem { Label = "财务管理", Glyph = "" });
                items.Add(new HamburgerMenuGlyphItem { Label = "通知公告", Glyph = "" });
                items.Add(new HamburgerMenuGlyphItem { Label = "场地申请", Glyph = "" });
            }
            else if (!Session.IsVisitor)
            {
                bool isManager = Session.MyManagedClubIds.Count > 0;

                items.Add(new HamburgerMenuGlyphItem { Label = "活动广场", Glyph = "" });
                items.Add(new HamburgerMenuGlyphItem { Label = "我的社团", Glyph = "" });
                items.Add(new HamburgerMenuGlyphItem { Label = "社团列表", Glyph = "" });

                if (isManager)
                {
                    // 社长/干事: 完整管理功能
                    items.Add(new HamburgerMenuGlyphItem { Label = "成员管理", Glyph = "" });
                    items.Add(new HamburgerMenuGlyphItem { Label = "活动管理", Glyph = "" });
                    items.Add(new HamburgerMenuGlyphItem { Label = "财务管理", Glyph = "" });
                    items.Add(new HamburgerMenuGlyphItem { Label = "通知公告", Glyph = "" });
                    items.Add(new HamburgerMenuGlyphItem { Label = "场地申请", Glyph = "" });
                }
            }
            else
            {
                // 游客
                items.Add(new HamburgerMenuGlyphItem { Label = "活动广场", Glyph = "" });
                items.Add(new HamburgerMenuGlyphItem { Label = "我的社团", Glyph = "" });
                items.Add(new HamburgerMenuGlyphItem { Label = "社团列表", Glyph = "" });
            }

            HamburgerMenu.ItemsSource = items;
        }

        private void HamburgerMenu_OnItemInvoked(object sender, HamburgerMenuItemInvokedEventArgs e)
        {
            if (_viewModel == null) return;

            var item = e.InvokedItem as HamburgerMenuGlyphItem;
            if (item == null) return;

            switch (item.Label)
            {
                case "控制台":
                    ShowDashboard();
                    break;
                case "活动广场":
                    _viewModel.ShowActivityBoardCommand.Execute(null);
                    HideDashboard();
                    break;
                case "我的社团":
                    _viewModel.ShowMyClubsCommand.Execute(null);
                    HideDashboard();
                    break;
                case "社团管理":
                case "社团列表":
                    _viewModel.ShowClubManageCommand.Execute(null);
                    HideDashboard();
                    break;
                case "成员管理":
                    _viewModel.ShowMemberManageCommand.Execute(null);
                    HideDashboard();
                    break;
                case "活动管理":
                    _viewModel.ShowActivityManageCommand.Execute(null);
                    HideDashboard();
                    break;
                case "财务管理":
                    _viewModel.ShowFinanceManageCommand.Execute(null);
                    HideDashboard();
                    break;
                case "通知公告":
                    _viewModel.ShowNoticeManageCommand.Execute(null);
                    HideDashboard();
                    break;
                case "场地申请":
                    _viewModel.ShowAssetManageCommand.Execute(null);
                    HideDashboard();
                    break;
            }
        }

        private void ShowDashboard()
        {
            BuildMenuItems();
            _viewModel.ShowDashboardCommand.Execute(null);
            DashboardPanel.Visibility = Visibility.Visible;
            PageContent.Visibility = Visibility.Collapsed;
            HamburgerMenu.SelectedIndex = 0;
        }

        private void HideDashboard()
        {
            DashboardPanel.Visibility = Visibility.Collapsed;
            PageContent.Visibility = Visibility.Visible;
        }

        protected override void OnClosed(EventArgs e)
        {
            RequestMenuRebuild -= OnMenuRebuild;
            base.OnClosed(e);
        }

        // ===== Club Detail =====

        private void DashboardClub_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as System.Windows.Controls.Border;
            if (border?.Tag == null) return;
            int clubId = Convert.ToInt32(border.Tag);
            string clubName;
            string clubInfo;
            string desc;
            string contact;
            int? catId;
            using (var db = new AppDbContext())
            {
                var club = db.Clubs.Include(c => c.Category).Include(c => c.Founder)
                    .Include(c => c.ClubMembers.Select(m => m.User))
                    .FirstOrDefault(c => c.ClubId == clubId);
                if (club == null) return;
                clubName = club.Name;
                clubInfo = $"类别：{club.Category?.Name ?? "未分类"}  |  社长：{club.PresidentName}  |  联系人：{club.Contact}";
                desc = club.Description ?? "";
                contact = club.Contact ?? "";
                catId = club.CategoryId;
            }
            var win = new ClubDetailWindow(clubId, clubName, clubInfo, desc, contact, catId, false);
            win.Owner = this;
            win.ShowDialog();
        }

        private void ClubManage_ViewDetail(object sender, RoutedEventArgs e)
        {
            var mgr = _viewModel.CurrentView as ClubManageViewModel;
            if (mgr?.SelectedClub == null) return;
            int clubId = mgr.SelectedClub.ClubId;
            bool canEdit = Session.IsAdmin || Session.MyManagedClubIds.Contains(clubId);

            string clubName, clubInfo, desc, contact;
            int? catId;
            using (var db = new AppDbContext())
            {
                var club = db.Clubs.Include(c => c.Category).Include(c => c.Founder)
                    .Include(c => c.ClubMembers.Select(m => m.User))
                    .FirstOrDefault(c => c.ClubId == clubId);
                if (club == null) return;
                clubName = club.Name;
                clubInfo = $"类别：{club.Category?.Name ?? "未分类"}  |  社长：{club.PresidentName}  |  联系人：{club.Contact}";
                desc = club.Description ?? "";
                contact = club.Contact ?? "";
                catId = club.CategoryId;
            }
            var win = new ClubDetailWindow(clubId, clubName, clubInfo, desc, contact, catId, canEdit);
            win.Owner = this;
            win.ShowDialog();
        }

        private void ActivityBoard_ViewDetail(object sender, RoutedEventArgs e)
        {
            var board = _viewModel.CurrentView as ActivityBoardViewModel;
            if (board?.SelectedActivity == null) return;
            var a = board.SelectedActivity;
            var win = new ActivityDetailWindow(a.Id, a.ClubId,
                a.Title,
                $"主办社团：{a.Club?.Name}  |  时间：{a.StartTime:yyyy-MM-dd HH:mm}  |  地点：{a.Location}",
                a.Description ?? "",
                a.StartTime, a.EndTime, a.Location ?? "", false);
            win.Owner = this;
            win.ShowDialog();
        }

        // ===== Notice Detail =====

        private void DashboardNotice_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as System.Windows.Controls.Border;
            if (border?.Tag == null) return;
            int noticeId = Convert.ToInt32(border.Tag);
            Notice notice;
            using (var db = new AppDbContext())
            {
                notice = db.Notices.Include("Publisher").Include("Club").FirstOrDefault(n => n.Id == noticeId);
            }
            if (notice == null) return;
            var win = new NoticeDetailWindow(notice.Id,
                notice.Title,
                $"发布人：{notice.Publisher?.Name}  |  所属：{notice.Club?.Name ?? "全局公告"}  |  时间：{notice.PublishTime:yyyy-MM-dd HH:mm}",
                notice.Content, false);
            win.Owner = this;
            win.ShowDialog();
        }

        private void NoticeManage_ViewDetail(object sender, RoutedEventArgs e)
        {
            var mgr = _viewModel.CurrentView as NoticeManageViewModel;
            if (mgr?.SelectedNotice == null) return;
            var n = mgr.SelectedNotice;
            bool canEdit = Session.IsAdmin || (n.ClubId.HasValue && Session.MyManagedClubIds.Contains(n.ClubId.Value));
            var win = new NoticeDetailWindow(n.Id,
                n.Title,
                $"发布人：{n.Publisher?.Name}  |  所属：{n.Club?.Name ?? "全局公告"}  |  时间：{n.PublishTime:yyyy-MM-dd HH:mm}",
                n.Content, canEdit);
            win.Owner = this;
            win.ShowDialog();
        }

        private void ActivityManage_ViewDetail(object sender, RoutedEventArgs e)
        {
            var mgr = _viewModel.CurrentView as ActivityManageViewModel;
            if (mgr?.SelectedActivity == null) return;
            var a = mgr.SelectedActivity;
            bool canEdit = Session.IsAdmin || Session.MyManagedClubIds.Contains(a.ClubId);
            var win = new ActivityDetailWindow(a.Id, a.ClubId,
                a.Title,
                $"时间：{a.StartTime:yyyy-MM-dd HH:mm}  |  地点：{a.Location}",
                a.Description ?? "",
                a.StartTime, a.EndTime, a.Location ?? "", canEdit);
            win.Owner = this;
            win.ShowDialog();
        }
    }
}
