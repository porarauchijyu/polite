using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using RssGenerator.Services;

namespace RssGenerator
{
    public partial class AddSiteWindow : Window
    {
        private readonly CrawlerService _crawler;
        private readonly InferenceService _inference;
        private readonly VisualSelectorService _visualSelector;
        private readonly DatabaseService _db;

        private TargetConfig _editingTarget;
        public bool EditMode => _editingTarget != null;

        public AddSiteWindow(CrawlerService crawler, DatabaseService db, TargetConfig editTarget = null)
        {
            InitializeComponent();
            _crawler = crawler;
            _db = db;
            _inference = new InferenceService(crawler);
            _visualSelector = new VisualSelectorService(crawler);
            _editingTarget = editTarget;

            if (EditMode)
            {
                WindowTitle.Text = "Edit Site Settings";
                NameTextBox.Text = _editingTarget.Name;
                UrlTextBox.Text = _editingTarget.Url;
                WaitTimeTextBox.Text = (_editingTarget.WaitMs / 1000.0).ToString();
                SaveButton.Content = "Update Settings";
                SaveButton.IsEnabled = true;
            }
        }

        private async void OnAnalyzeClick(object sender, RoutedEventArgs e)
        {
            string url = UrlTextBox.Text;
            if (!int.TryParse(WaitTimeTextBox.Text, out int waitSec)) waitSec = 3;
            int waitMs = waitSec * 1000;

            if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("http"))
            {
                MessageBox.Show("有効な URL を入力してください。");
                return;
            }

            StatusLabel.Visibility = Visibility.Collapsed;
            LoadingBar.Visibility = Visibility.Visible;
            ResultsPanel.Visibility = Visibility.Collapsed;

            try
            {
                await _crawler.InitializeAsync();
                var results = await _inference.InferSelectorsAsync(url); // TODO: pass waitMs to InferenceService if needed
                
                if (results.Count > 0)
                {
                    // サイト名が未入力の場合のみ自動セット
                    if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                    {
                        NameTextBox.Text = results[0].PageTitle;
                    }

                    InferenceListView.ItemsSource = results;
                    ResultsPanel.Visibility = Visibility.Visible;
                    LoadingBar.Visibility = Visibility.Collapsed;
                    SaveButton.IsEnabled = true;
                }
                else
                {
                    StatusLabel.Text = "自動解析に失敗しました。ビジュアルモードを試してください。";
                    StatusLabel.Visibility = Visibility.Visible;
                    LoadingBar.Visibility = Visibility.Collapsed;
                    ResultsPanel.Visibility = Visibility.Visible; // 手動ボタンを表示するため
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解析エラー: {ex.Message}");
                LoadingBar.Visibility = Visibility.Collapsed;
                StatusLabel.Visibility = Visibility.Visible;
            }
        }

        private async void OnVisualSelectClick(object sender, RoutedEventArgs e)
        {
            string url = UrlTextBox.Text;
            this.Opacity = 0.5; // ウィンドウを半透明に
            
            try 
            {
                var result = await _visualSelector.SelectSelectorsAsync(url);
                if (!result.IsCancelled)
                {
                    // サイト名を自動セット
                    if (!string.IsNullOrEmpty(result.PageTitle))
                    {
                        NameTextBox.Text = result.PageTitle;
                    }

                    // 手動選択の結果を候補リストとして表示
                    var manualResult = new InferenceResult
                    {
                        SampleTitle = "[手動選択の結果]",
                        TitleSelector = result.TitleSelector,
                        LinkSelector = result.TitleSelector,
                        ContainerSelector = result.ContainerSelector,
                        DescriptionSelector = result.DescriptionSelector,
                        DateSelector = result.DateSelector,
                        Score = 1000,
                        PageTitle = result.PageTitle ?? NameTextBox.Text
                    };

                    var results = new List<InferenceResult> { manualResult };
                    InferenceListView.ItemsSource = results;
                    InferenceListView.SelectedIndex = 0;

                    ResultsPanel.Visibility = Visibility.Visible;
                    StatusLabel.Visibility = Visibility.Collapsed;
                    LoadingBar.Visibility = Visibility.Collapsed;
                    SaveButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラー: {ex.Message}");
            }
            finally
            {
                this.Opacity = 1.0;
            }
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            string url = UrlTextBox.Text;
            string name = NameTextBox.Text;
            if (!double.TryParse(WaitTimeTextBox.Text, out double waitSec)) waitSec = 3.0;
            int waitMs = (int)(waitSec * 1000);

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("サイト名を入力してください。");
                return;
            }

            if (InferenceListView.SelectedItem is InferenceResult result)
            {
                if (EditMode)
                {
                    _db.UpdateTarget(_editingTarget.Id, url, result.TitleSelector, result.LinkSelector, name, result.ContainerSelector, result.DescriptionSelector, result.DateSelector, waitMs);
                }
                else
                {
                    _db.RegisterTarget(url, result.TitleSelector, result.LinkSelector, name, result.ContainerSelector, result.DescriptionSelector, result.DateSelector, waitMs);
                }
                this.DialogResult = true;
                this.Close();
            }
            else if (EditMode)
            {
                // セレクタを変更せずに名前やURLだけ変更する場合
                _db.UpdateTarget(_editingTarget.Id, url, _editingTarget.TitleSelector, _editingTarget.LinkSelector, name, _editingTarget.ContainerSelector, _editingTarget.DescriptionSelector, _editingTarget.DateSelector, waitMs);
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("候補をリストから選択するか、ビジュアルモードで指定してください。");
            }
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        
        // Window Drag Support
        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
    }
}
