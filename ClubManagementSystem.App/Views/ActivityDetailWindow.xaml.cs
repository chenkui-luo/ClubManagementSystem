using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClubManagementSystem.App.Models;
using ClubManagementSystem.App.Services;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace ClubManagementSystem.App.Views
{
    public partial class ActivityDetailWindow : MetroWindow
    {
        private readonly int _activityId;
        private readonly int _clubId;
        private readonly bool _canEdit;
        private readonly string _baseDir;
        private string _originalTitle, _originalLocation;
        private DateTime _originalStart, _originalEnd;
        private List<ActivityFile> _images = new List<ActivityFile>();
        private List<ActivityFile> _documents = new List<ActivityFile>();
        private int _currentImageIndex = 0;

        private const int MaxTotalFiles = 10;
        private const int MaxImages = 5;
        private const long MaxFileSize = 10 * 1024 * 1024;

        public ActivityDetailWindow(int activityId, int clubId, string activityTitle,
            string activityInfo, string description, DateTime startTime, DateTime endTime,
            string location, bool canEdit)
        {
            InitializeComponent();
            _activityId = activityId;
            _clubId = clubId;
            _canEdit = canEdit;
            _originalTitle = activityTitle;
            _originalStart = startTime;
            _originalEnd = endTime;
            _originalLocation = location;
            _baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Files", "ActivityFiles", activityId.ToString());
            Directory.CreateDirectory(_baseDir);

            TxtActivityTitle.Text = activityTitle;
            TxtActivityInfo.Text = activityInfo;
            TxtDescription.Text = description;
            TxtEditDescription.Text = description;

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

        // ===== Edit All (title + location + time + description) =====

        private void EditAll_Click(object sender, RoutedEventArgs e)
        {
            EditAllPanel.Visibility = Visibility.Visible;
            BtnEditAll.Visibility = Visibility.Collapsed;
            TxtDescription.Visibility = Visibility.Collapsed;

            TxtEditTitle.Text = _originalTitle;
            TxtEditLocation.Text = _originalLocation;
            DpEditStart.SelectedDate = _originalStart;
            DpEditEnd.SelectedDate = _originalEnd;
            TxtEditDescription.Text = TxtDescription.Text;
        }

        private void CancelEditAll_Click(object sender, RoutedEventArgs e)
        {
            EditAllPanel.Visibility = Visibility.Collapsed;
            BtnEditAll.Visibility = Visibility.Visible;
            TxtDescription.Visibility = Visibility.Visible;
        }

        private void SaveEditAll_Click(object sender, RoutedEventArgs e)
        {
            var newTitle = TxtEditTitle.Text.Trim();
            var newLocation = TxtEditLocation.Text.Trim();
            var newStart = DpEditStart.SelectedDate;
            var newEnd = DpEditEnd.SelectedDate;
            var newDesc = TxtEditDescription.Text;

            if (string.IsNullOrWhiteSpace(newTitle)) { MessageBox.Show("活动名称不能为空。"); return; }
            if (newStart == null || newEnd == null) { MessageBox.Show("请选择开始和结束时间。"); return; }

            using (var db = new AppDbContext())
            {
                var a = db.Activities.Find(_activityId);
                if (a != null)
                {
                    a.Title = newTitle;
                    a.Location = newLocation;
                    a.StartTime = newStart.Value;
                    a.EndTime = newEnd.Value;
                    a.Description = newDesc;
                    db.SaveChanges();
                }
            }

            _originalTitle = newTitle;
            _originalLocation = newLocation;
            _originalStart = newStart.Value;
            _originalEnd = newEnd.Value;

            TxtActivityTitle.Text = newTitle;
            TxtActivityInfo.Text = $"时间：{newStart.Value:yyyy-MM-dd HH:mm}  |  地点：{newLocation}";
            TxtDescription.Text = newDesc;

            EditAllPanel.Visibility = Visibility.Collapsed;
            TxtDescription.Visibility = Visibility.Visible;
            BtnEditAll.Visibility = Visibility.Visible;
            MessageBox.Show("活动信息已更新。", "成功");
        }

        private void LoadFiles()
        {
            using (var db = new AppDbContext())
            {
                var files = db.ActivityFiles
                    .Where(f => f.ActivityId == _activityId)
                    .ToList();

                _images = files.Where(f => f.FileType == "image").ToList();
                _documents = files.Where(f => f.FileType == "document").ToList();

                DgFiles.ItemsSource = new ObservableCollection<ActivityFile>(_documents);
            }

            RefreshImageDisplay();
            RefreshNoDataLabels();
        }

        // ===== Image Display =====

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

                if (_currentImageIndex >= _images.Count)
                    _currentImageIndex = 0;

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

        private string GetFullPath(string relativePath)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
        }

        private void PrevImage_Click(object sender, RoutedEventArgs e)
        {
            if (_images.Count == 0) return;
            _currentImageIndex = (_currentImageIndex - 1 + _images.Count) % _images.Count;
            RefreshImageDisplay();
        }

        private void NextImage_Click(object sender, RoutedEventArgs e)
        {
            if (_images.Count == 0) return;
            _currentImageIndex = (_currentImageIndex + 1) % _images.Count;
            RefreshImageDisplay();
        }

        private void ImgMain_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_images.Count == 0) return;
            var path = GetFullPath(_images[_currentImageIndex].FilePath);
            if (!File.Exists(path)) return;
            ShowZoomWindow(path);
        }

        private void ZoomImage_Click(object sender, RoutedEventArgs e)
        {
            if (_images.Count == 0) return;
            var path = GetFullPath(_images[_currentImageIndex].FilePath);
            if (!File.Exists(path)) return;
            ShowZoomWindow(path);
        }

        private void ShowZoomWindow(string path)
        {
            var bmp = new System.Windows.Media.Imaging.BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bmp.UriSource = new Uri(path);
            bmp.EndInit();

            var img = new Image { Source = bmp, Stretch = System.Windows.Media.Stretch.Uniform };
            var grid = new Grid { Background = System.Windows.Media.Brushes.Black, Margin = new Thickness(10) };
            grid.Children.Add(img);

            new Window
            {
                Title = "图片预览 - " + (_images.Count > 0 ? _images[_currentImageIndex].OriginalName : ""),
                Width = System.Windows.SystemParameters.PrimaryScreenWidth * 0.85,
                Height = System.Windows.SystemParameters.PrimaryScreenHeight * 0.85,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.CanResizeWithGrip,
                Content = grid
            }.ShowDialog();
        }

        private void DownloadImage_Click(object sender, RoutedEventArgs e)
        {
            if (_images.Count == 0) { MessageBox.Show("没有可下载的图片。", "提示"); return; }
            var file = _images[_currentImageIndex];
            var srcPath = GetFullPath(file.FilePath);
            if (!File.Exists(srcPath)) { MessageBox.Show("文件不存在。", "提示"); return; }

            var dlg = new SaveFileDialog { Title = "保存图片", FileName = file.OriginalName, Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*" };
            if (dlg.ShowDialog() == true)
            {
                File.Copy(srcPath, dlg.FileName, true);
                MessageBox.Show("图片已保存。", "成功");
            }
        }

        private void UploadImage_Click(object sender, RoutedEventArgs e)
        {
            if (!_canEdit) return;
            if (_images.Count + _documents.Count >= MaxTotalFiles) { MessageBox.Show($"最多只能上传{MaxTotalFiles}个文件。"); return; }
            if (_images.Count >= MaxImages) { MessageBox.Show($"最多只能上传{MaxImages}张图片。"); return; }

            var dlg = new OpenFileDialog { Title = "选择图片", Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.gif", Multiselect = true };
            if (dlg.ShowDialog() != true) return;

            foreach (var filePath in dlg.FileNames)
            {
                if (_images.Count + _documents.Count >= MaxTotalFiles) break;
                if (_images.Count >= MaxImages) { MessageBox.Show($"超过{MaxImages}张图片限制，部分文件未上传。"); break; }
                var fi = new FileInfo(filePath);
                if (fi.Length > MaxFileSize) { MessageBox.Show($"\"{fi.Name}\" 超过10MB限制，已跳过。"); continue; }

                var savedName = $"{Guid.NewGuid()}{fi.Extension}";
                File.Copy(filePath, Path.Combine(_baseDir, savedName));
                using (var db = new AppDbContext())
                {
                    db.ActivityFiles.Add(new ActivityFile { ActivityId = _activityId, FileType = "image", FilePath = $"Files/ActivityFiles/{_activityId}/{savedName}", OriginalName = fi.Name, FileSize = fi.Length });
                    db.SaveChanges();
                }
            }
            LoadFiles();
        }

        private void UploadFile_Click(object sender, RoutedEventArgs e)
        {
            if (!_canEdit) return;
            if (_images.Count + _documents.Count >= MaxTotalFiles) { MessageBox.Show($"最多只能上传{MaxTotalFiles}个文件。"); return; }

            var dlg = new OpenFileDialog { Title = "选择文件", Filter = "文档文件|*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.ppt;*.pptx|所有文件|*.*", Multiselect = true };
            if (dlg.ShowDialog() != true) return;

            foreach (var filePath in dlg.FileNames)
            {
                if (_images.Count + _documents.Count >= MaxTotalFiles) break;
                var fi = new FileInfo(filePath);
                if (fi.Length > MaxFileSize) { MessageBox.Show($"\"{fi.Name}\" 超过10MB限制，已跳过。"); continue; }

                var savedName = $"{Guid.NewGuid()}{fi.Extension}";
                File.Copy(filePath, Path.Combine(_baseDir, savedName));
                using (var db = new AppDbContext())
                {
                    db.ActivityFiles.Add(new ActivityFile { ActivityId = _activityId, FileType = "document", FilePath = $"Files/ActivityFiles/{_activityId}/{savedName}", OriginalName = fi.Name, FileSize = fi.Length });
                    db.SaveChanges();
                }
            }
            LoadFiles();
        }

        private void DeleteCurrentImage_Click(object sender, RoutedEventArgs e)
        {
            if (_images.Count == 0) return;
            ImgMain.Source = null;
            DeleteFile(_images[_currentImageIndex]);
        }

        private void DeleteSelectedFile_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgFiles.SelectedItem as ActivityFile;
            if (selected == null) return;
            DeleteFile(selected);
        }

        private void DeleteFile(ActivityFile file)
        {
            if (MessageBox.Show($"确定要删除 \"{file.OriginalName}\" 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            var fullPath = GetFullPath(file.FilePath);
            if (File.Exists(fullPath)) File.Delete(fullPath);

            using (var db = new AppDbContext())
            {
                var toDelete = db.ActivityFiles.Find(file.Id);
                if (toDelete != null) { db.ActivityFiles.Remove(toDelete); db.SaveChanges(); }
            }
            LoadFiles();
        }

        private void DgFiles_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            e.Handled = true;
            var sv = FindParent<ScrollViewer>(sender as DependencyObject);
            sv?.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta / 3.0);
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T t) return t;
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        private void DgFiles_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selected = DgFiles.SelectedItem as ActivityFile;
            if (selected == null) return;
            OpenOrDownload(selected);
        }

        private void DownloadFile_Click(object sender, RoutedEventArgs e)
        {
            var selected = DgFiles.SelectedItem as ActivityFile;
            if (selected == null) { MessageBox.Show("请先选中一个文件。"); return; }
            OpenOrDownload(selected);
        }

        private void OpenOrDownload(ActivityFile file)
        {
            var fullPath = GetFullPath(file.FilePath);
            if (!File.Exists(fullPath)) { MessageBox.Show("文件不存在。"); return; }
            var dlg = new SaveFileDialog { Title = "保存文件", FileName = file.OriginalName, Filter = "所有文件|*.*" };
            if (dlg.ShowDialog() == true) { File.Copy(fullPath, dlg.FileName, true); MessageBox.Show("文件已保存。", "成功"); }
        }
    }
}
