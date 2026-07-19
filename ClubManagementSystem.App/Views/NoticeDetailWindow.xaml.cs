using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClubManagementSystem.App.Models;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace ClubManagementSystem.App.Views
{
    public partial class NoticeDetailWindow : MetroWindow
    {
        private readonly int _noticeId;
        private readonly bool _canEdit;
        private readonly string _baseDir;
        private string _originalTitle, _originalContent;
        private List<NoticeFile> _images = new List<NoticeFile>();
        private List<NoticeFile> _documents = new List<NoticeFile>();
        private int _currentImageIndex = 0;

        private const int MaxTotalFiles = 10;
        private const int MaxImages = 5;
        private const long MaxFileSize = 10 * 1024 * 1024;

        public NoticeDetailWindow(int noticeId, string title, string info, string content, bool canEdit)
        {
            InitializeComponent();
            _noticeId = noticeId;
            _canEdit = canEdit;
            _originalTitle = title;
            _originalContent = content;
            _baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", "NoticeFiles", noticeId.ToString());
            Directory.CreateDirectory(_baseDir);

            TxtNoticeTitle.Text = title;
            TxtNoticeInfo.Text = info;
            TxtContent.Text = content;

            ApplyPermissions();
            LoadFiles();
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

        // ===== Edit All =====

        private void EditAll_Click(object sender, RoutedEventArgs e)
        {
            EditAllPanel.Visibility = Visibility.Visible;
            BtnEditAll.Visibility = Visibility.Collapsed;
            TxtContent.Visibility = Visibility.Collapsed;
            TxtEditTitle.Text = _originalTitle;
            TxtEditContent.Text = _originalContent;
        }

        private void CancelEditAll_Click(object sender, RoutedEventArgs e)
        {
            EditAllPanel.Visibility = Visibility.Collapsed;
            BtnEditAll.Visibility = Visibility.Visible;
            TxtContent.Visibility = Visibility.Visible;
        }

        private void SaveEditAll_Click(object sender, RoutedEventArgs e)
        {
            var newTitle = TxtEditTitle.Text.Trim();
            var newContent = TxtEditContent.Text;
            if (string.IsNullOrWhiteSpace(newTitle)) { MessageBox.Show("标题不能为空。"); return; }

            using (var db = new AppDbContext())
            {
                var n = db.Notices.Find(_noticeId);
                if (n != null) { n.Title = newTitle; n.Content = newContent; db.SaveChanges(); }
            }

            _originalTitle = newTitle;
            _originalContent = newContent;
            TxtNoticeTitle.Text = newTitle;
            TxtContent.Text = newContent;

            EditAllPanel.Visibility = Visibility.Collapsed;
            TxtContent.Visibility = Visibility.Visible;
            BtnEditAll.Visibility = Visibility.Visible;
            MessageBox.Show("公告信息已更新。", "成功");
        }

        private void LoadFiles()
        {
            using (var db = new AppDbContext())
            {
                var files = db.NoticeFiles.Where(f => f.NoticeId == _noticeId).ToList();
                _images = files.Where(f => f.FileType == "image").ToList();
                _documents = files.Where(f => f.FileType == "document").ToList();
                DgFiles.ItemsSource = new ObservableCollection<NoticeFile>(_documents);
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
                using (var db = new AppDbContext()) { db.NoticeFiles.Add(new NoticeFile { NoticeId = _noticeId, FileType = "image", FilePath = $"Files/NoticeFiles/{_noticeId}/{sn}", OriginalName = fi.Name, FileSize = fi.Length }); db.SaveChanges(); }
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
                using (var db = new AppDbContext()) { db.NoticeFiles.Add(new NoticeFile { NoticeId = _noticeId, FileType = "document", FilePath = $"Files/NoticeFiles/{_noticeId}/{sn}", OriginalName = fi.Name, FileSize = fi.Length }); db.SaveChanges(); }
            }
            LoadFiles();
        }

        private void DeleteCurrentImage_Click(object sender, RoutedEventArgs e)
        { if (_images.Count == 0) return; ImgMain.Source = null; DeleteFile(_images[_currentImageIndex]); }
        private void DeleteSelectedFile_Click(object sender, RoutedEventArgs e)
        { var s = DgFiles.SelectedItem as NoticeFile; if (s != null) DeleteFile(s); }
        private void DeleteFile(NoticeFile file)
        {
            if (MessageBox.Show($"确定删除 \"{file.OriginalName}\" 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            var fp = GetFullPath(file.FilePath); if (File.Exists(fp)) File.Delete(fp);
            using (var db = new AppDbContext()) { var d = db.NoticeFiles.Find(file.Id); if (d != null) { db.NoticeFiles.Remove(d); db.SaveChanges(); } }
            LoadFiles();
        }

        private void DgFiles_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        { e.Handled = true; var sv = FindParent<ScrollViewer>(sender as DependencyObject); sv?.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta / 3.0); }
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        { var p = System.Windows.Media.VisualTreeHelper.GetParent(child); while (p != null) { if (p is T t) return t; p = System.Windows.Media.VisualTreeHelper.GetParent(p); } return null; }
        private void DgFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        { var s = DgFiles.SelectedItem as NoticeFile; if (s != null) OpenOrDownload(s); }
        private void DownloadFile_Click(object sender, RoutedEventArgs e)
        { var s = DgFiles.SelectedItem as NoticeFile; if (s == null) { MessageBox.Show("请先选中一个文件。"); return; } OpenOrDownload(s); }
        private void OpenOrDownload(NoticeFile file)
        {
            var fp = GetFullPath(file.FilePath); if (!File.Exists(fp)) { MessageBox.Show("文件不存在。"); return; }
            var dlg = new SaveFileDialog { Title = "保存文件", FileName = file.OriginalName, Filter = "所有文件|*.*" };
            if (dlg.ShowDialog() == true) { File.Copy(fp, dlg.FileName, true); MessageBox.Show("文件已保存。"); }
        }
    }
}
