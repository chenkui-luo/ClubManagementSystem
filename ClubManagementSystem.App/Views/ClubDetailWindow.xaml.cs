using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClubManagementSystem.App.Models;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace ClubManagementSystem.App.Views
{
    public partial class ClubDetailWindow : MetroWindow
    {
        private readonly int _clubId;
        private readonly int? _categoryId;
        private readonly bool _canEdit;
        private readonly bool _isAdmin;
        private readonly string _baseDir;
        private List<ClubFile> _images = new List<ClubFile>();
        private List<ClubFile> _documents = new List<ClubFile>();
        private int _currentImageIndex = 0;
        private List<Category> _categories;

        private const int MaxTotalFiles = 10;
        private const int MaxImages = 5;
        private const long MaxFileSize = 10 * 1024 * 1024;

        public ClubDetailWindow(int clubId, string name, string info, string description,
            string contact, int? categoryId, bool canEdit)
        {
            InitializeComponent();
            _clubId = clubId;
            _categoryId = categoryId;
            _canEdit = canEdit;
            _isAdmin = Services.Session.IsAdmin;
            _baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", "ClubFiles", clubId.ToString());
            Directory.CreateDirectory(_baseDir);

            TxtClubName.Text = name;
            TxtClubInfo.Text = info;
            TxtDescription.Text = description;

            LoadCategories();
            ApplyPermissions();
            LoadFiles();
            LoadPendingEdits();
        }

        private void LoadCategories()
        {
            using (var db = new AppDbContext())
                _categories = db.Categories.OrderBy(c => c.Name).ToList();
            CmbEditCategory.ItemsSource = _categories;
        }

        private void ApplyPermissions()
        {
            if (!_canEdit)
            {
                BtnUploadImage.Visibility = Visibility.Collapsed;
                BtnDeleteImage.Visibility = Visibility.Collapsed;
                BtnUploadFile.Visibility = Visibility.Collapsed;
                BtnDeleteFile.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnUploadImage.Visibility = Visibility.Visible;
                BtnDeleteImage.Visibility = Visibility.Visible;
                BtnUploadFile.Visibility = Visibility.Visible;
                BtnDeleteFile.Visibility = Visibility.Visible;
                BtnEditAll.Visibility = Visibility.Visible;
            }
        }

        // ===== Pending Edits (Admin Approval) =====

        private void LoadPendingEdits()
        {
            if (!_isAdmin || !_canEdit) return;
            using (var db = new AppDbContext())
            {
                var pending = db.ClubPendingEdits
                    .Where(e => e.ClubId == _clubId && e.Status == "待审批")
                    .Include(e => e.Submitter)
                    .OrderByDescending(e => e.SubmittedAt)
                    .ToList();

                DgPendingEdits.ItemsSource = new ObservableCollection<ClubPendingEdit>(pending);
                ApprovalSection.Visibility = pending.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void ApproveEdit_Click(object sender, RoutedEventArgs e)
        {
            var edit = DgPendingEdits.SelectedItem as ClubPendingEdit;
            if (edit == null) { MessageBox.Show("请先选中一条待审批的修改。"); return; }

            using (var db = new AppDbContext())
            {
                var club = db.Clubs.Find(_clubId);
                if (club != null)
                {
                    if (!string.IsNullOrWhiteSpace(edit.NewName)) club.Name = edit.NewName;
                    if (edit.CategoryId.HasValue) club.CategoryId = edit.CategoryId;
                    if (edit.NewDescription != null) club.Description = edit.NewDescription;
                    if (edit.NewContact != null) club.Contact = edit.NewContact;
                }

                var pe = db.ClubPendingEdits.Find(edit.Id);
                if (pe != null)
                {
                    pe.Status = "已通过";
                    pe.ProcessedAt = DateTime.Now;
                }
                db.SaveChanges();
            }

            RefreshClubInfo();
            LoadPendingEdits();
            MessageBox.Show("修改已通过并生效。", "成功");
        }

        private void RejectEdit_Click(object sender, RoutedEventArgs e)
        {
            var edit = DgPendingEdits.SelectedItem as ClubPendingEdit;
            if (edit == null) { MessageBox.Show("请先选中一条待审批的修改。"); return; }

            using (var db = new AppDbContext())
            {
                var pe = db.ClubPendingEdits.Find(edit.Id);
                if (pe != null)
                {
                    pe.Status = "已拒绝";
                    pe.ProcessedAt = DateTime.Now;
                }
                db.SaveChanges();
            }
            LoadPendingEdits();
            MessageBox.Show("修改已拒绝。", "成功");
        }

        private void RefreshClubInfo()
        {
            using (var db = new AppDbContext())
            {
                var club = db.Clubs.Include(c => c.Category).Include(c => c.Founder).FirstOrDefault(c => c.ClubId == _clubId);
                if (club != null)
                {
                    TxtClubName.Text = club.Name;
                    TxtClubInfo.Text = $"类别：{club.Category?.Name}  |  联系人：{club.Contact}";
                    TxtDescription.Text = club.Description;
                }
            }
        }

        // ===== Edit =====

        private void EditAll_Click(object sender, RoutedEventArgs e)
        {
            EditAllPanel.Visibility = Visibility.Visible;
            BtnEditAll.Visibility = Visibility.Collapsed;

            using (var db = new AppDbContext())
            {
                var club = db.Clubs.Find(_clubId);
                if (club != null)
                {
                    TxtEditName.Text = club.Name;
                    TxtEditContact.Text = club.Contact;
                    TxtEditDescription.Text = club.Description;
                    CmbEditCategory.SelectedItem = _categories.FirstOrDefault(c => c.Id == club.CategoryId);
                }
            }
        }

        private void CancelEditAll_Click(object sender, RoutedEventArgs e)
        {
            EditAllPanel.Visibility = Visibility.Collapsed;
            BtnEditAll.Visibility = Visibility.Visible;
        }

        private void SubmitEdit_Click(object sender, RoutedEventArgs e)
        {
            var newName = TxtEditName.Text.Trim();
            var newContact = TxtEditContact.Text.Trim();
            var newDesc = TxtEditDescription.Text;
            var newCat = CmbEditCategory.SelectedItem as Category;

            if (string.IsNullOrWhiteSpace(newName)) { MessageBox.Show("社团名称不能为空。"); return; }

            using (var db = new AppDbContext())
            {
                db.ClubPendingEdits.Add(new ClubPendingEdit
                {
                    ClubId = _clubId,
                    SubmittedBy = Services.Session.CurrentUser.UserId,
                    NewName = newName,
                    CategoryId = newCat?.Id,
                    NewDescription = newDesc,
                    NewContact = newContact,
                    Status = "待审批",
                    SubmittedAt = DateTime.Now
                });
                db.SaveChanges();
            }

            EditAllPanel.Visibility = Visibility.Collapsed;
            BtnEditAll.Visibility = Visibility.Visible;
            MessageBox.Show("修改已提交，请等待管理员审批。", "成功");
        }

        // ===== File Management =====

        private void LoadFiles()
        {
            using (var db = new AppDbContext())
            {
                var files = db.ClubFiles.Where(f => f.ClubId == _clubId).ToList();
                _images = files.Where(f => f.FileType == "image").ToList();
                _documents = files.Where(f => f.FileType == "document").ToList();
                DgFiles.ItemsSource = new ObservableCollection<ClubFile>(_documents);
            }
            RefreshImageDisplay();
            RefreshNoDataLabels();
        }

        private void RefreshImageDisplay()
        {
            if (_images.Count == 0)
            {
                ImgMain.Visibility = Visibility.Collapsed;
                TxtImageIndex.Visibility = Visibility.Collapsed;
                TxtNoImages.Visibility = Visibility.Visible;
            }
            else
            {
                ImgMain.Visibility = Visibility.Visible;
                TxtImageIndex.Visibility = Visibility.Visible;
                TxtNoImages.Visibility = Visibility.Collapsed;
                if (_currentImageIndex >= _images.Count) _currentImageIndex = 0;
                var path = GetFullPath(_images[_currentImageIndex].FilePath);
                if (File.Exists(path))
                {
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(path);
                    bmp.EndInit();
                    bmp.Freeze();
                    ImgMain.Source = bmp;
                }
                TxtImageIndex.Text = $"{_currentImageIndex + 1} / {_images.Count}";
            }
        }

        private void RefreshNoDataLabels()
        {
            TxtNoFiles.Visibility = _documents.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            DgFiles.Visibility = _documents.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private string GetFullPath(string p) => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p);

        private void PrevImage_Click(object sender, RoutedEventArgs e)
        { if (_images.Count == 0) return; _currentImageIndex = (_currentImageIndex - 1 + _images.Count) % _images.Count; RefreshImageDisplay(); }
        private void NextImage_Click(object sender, RoutedEventArgs e)
        { if (_images.Count == 0) return; _currentImageIndex = (_currentImageIndex + 1) % _images.Count; RefreshImageDisplay(); }

        private void ImgMain_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        { if (_images.Count == 0) return; var p = GetFullPath(_images[_currentImageIndex].FilePath); if (File.Exists(p)) ShowZoomWindow(p); }
        private void ZoomImage_Click(object sender, RoutedEventArgs e)
        { if (_images.Count == 0) return; var p = GetFullPath(_images[_currentImageIndex].FilePath); if (File.Exists(p)) ShowZoomWindow(p); }

        private void ShowZoomWindow(string path)
        {
            var bmp = new System.Windows.Media.Imaging.BitmapImage();
            bmp.BeginInit(); bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; bmp.UriSource = new Uri(path); bmp.EndInit();
            var img = new Image { Source = bmp, Stretch = System.Windows.Media.Stretch.Uniform };
            var grid = new Grid { Background = System.Windows.Media.Brushes.Black, Margin = new Thickness(10) }; grid.Children.Add(img);
            new Window { Title = "图片预览", Width = System.Windows.SystemParameters.PrimaryScreenWidth * 0.85, Height = System.Windows.SystemParameters.PrimaryScreenHeight * 0.85, WindowStartupLocation = WindowStartupLocation.CenterOwner, Owner = this, WindowStyle = WindowStyle.ToolWindow, ResizeMode = ResizeMode.CanResizeWithGrip, Content = grid }.ShowDialog();
        }

        private void DownloadImage_Click(object sender, RoutedEventArgs e)
        {
            if (_images.Count == 0) { MessageBox.Show("没有可下载的图片。"); return; }
            var f = _images[_currentImageIndex]; var src = GetFullPath(f.FilePath);
            if (!File.Exists(src)) { MessageBox.Show("文件不存在。"); return; }
            var dlg = new SaveFileDialog { Title = "保存图片", FileName = f.OriginalName, Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*" };
            if (dlg.ShowDialog() == true) { File.Copy(src, dlg.FileName, true); MessageBox.Show("图片已保存。"); }
        }

        private void UploadImage_Click(object sender, RoutedEventArgs e)
        {
            if (!_canEdit) return;
            if (_images.Count + _documents.Count >= MaxTotalFiles) { MessageBox.Show($"最多{MaxTotalFiles}个文件。"); return; }
            if (_images.Count >= MaxImages) { MessageBox.Show($"最多{MaxImages}张图片。"); return; }
            var dlg = new OpenFileDialog { Title = "选择图片", Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif", Multiselect = true };
            if (dlg.ShowDialog() != true) return;
            foreach (var fp in dlg.FileNames)
            {
                if (_images.Count + _documents.Count >= MaxTotalFiles) break;
                if (_images.Count >= MaxImages) { MessageBox.Show($"超过{MaxImages}张限制。"); break; }
                var fi = new FileInfo(fp); if (fi.Length > MaxFileSize) { MessageBox.Show($"\"{fi.Name}\" 超过10MB。"); continue; }
                var sn = $"{Guid.NewGuid()}{fi.Extension}"; File.Copy(fp, Path.Combine(_baseDir, sn));
                using (var db = new AppDbContext()) { db.ClubFiles.Add(new ClubFile { ClubId = _clubId, FileType = "image", FilePath = $"Files/ClubFiles/{_clubId}/{sn}", OriginalName = fi.Name, FileSize = fi.Length }); db.SaveChanges(); }
            }
            LoadFiles();
        }
        private void UploadFile_Click(object sender, RoutedEventArgs e)
        {
            if (!_canEdit) return;
            if (_images.Count + _documents.Count >= MaxTotalFiles) { MessageBox.Show($"最多{MaxTotalFiles}个文件。"); return; }
            var dlg = new OpenFileDialog { Title = "选择文件", Filter = "文档文件|*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.ppt;*.pptx|所有文件|*.*", Multiselect = true };
            if (dlg.ShowDialog() != true) return;
            foreach (var fp in dlg.FileNames)
            {
                if (_images.Count + _documents.Count >= MaxTotalFiles) break;
                var fi = new FileInfo(fp); if (fi.Length > MaxFileSize) { MessageBox.Show($"\"{fi.Name}\" 超过10MB。"); continue; }
                var sn = $"{Guid.NewGuid()}{fi.Extension}"; File.Copy(fp, Path.Combine(_baseDir, sn));
                using (var db = new AppDbContext()) { db.ClubFiles.Add(new ClubFile { ClubId = _clubId, FileType = "document", FilePath = $"Files/ClubFiles/{_clubId}/{sn}", OriginalName = fi.Name, FileSize = fi.Length }); db.SaveChanges(); }
            }
            LoadFiles();
        }

        private void DeleteCurrentImage_Click(object sender, RoutedEventArgs e)
        { if (_images.Count == 0) return; ImgMain.Source = null; DeleteFile(_images[_currentImageIndex]); }
        private void DeleteSelectedFile_Click(object sender, RoutedEventArgs e)
        { var s = DgFiles.SelectedItem as ClubFile; if (s != null) DeleteFile(s); }
        private void DeleteFile(ClubFile file)
        {
            if (MessageBox.Show($"确定删除 \"{file.OriginalName}\" 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            var fp = GetFullPath(file.FilePath); if (File.Exists(fp)) File.Delete(fp);
            using (var db = new AppDbContext()) { var d = db.ClubFiles.Find(file.Id); if (d != null) { db.ClubFiles.Remove(d); db.SaveChanges(); } }
            LoadFiles();
        }

        private void DgFiles_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        { e.Handled = true; var sv = FindParent<ScrollViewer>(sender as DependencyObject); sv?.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta / 3.0); }
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        { var p = System.Windows.Media.VisualTreeHelper.GetParent(child); while (p != null) { if (p is T t) return t; p = System.Windows.Media.VisualTreeHelper.GetParent(p); } return null; }
        private void DgFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        { var s = DgFiles.SelectedItem as ClubFile; if (s != null) OpenOrDownload(s); }
        private void DownloadFile_Click(object sender, RoutedEventArgs e)
        { var s = DgFiles.SelectedItem as ClubFile; if (s == null) { MessageBox.Show("请先选中一个文件。"); return; } OpenOrDownload(s); }
        private void OpenOrDownload(ClubFile file)
        {
            var fp = GetFullPath(file.FilePath); if (!File.Exists(fp)) { MessageBox.Show("文件不存在。"); return; }
            var dlg = new SaveFileDialog { Title = "保存文件", FileName = file.OriginalName, Filter = "所有文件|*.*" };
            if (dlg.ShowDialog() == true) { File.Copy(fp, dlg.FileName, true); MessageBox.Show("文件已保存。"); }
        }
    }
}
