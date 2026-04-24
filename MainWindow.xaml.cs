using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using RssGenerator.Services;

namespace RssGenerator
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _db;
        private readonly CrawlerService _crawler;
        private readonly ExtractorService _extractor;
        private readonly RssService _rss;
        
        public ObservableCollection<TargetConfig> Targets { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            
            _db = new DatabaseService();
            _crawler = new CrawlerService();
            _extractor = new ExtractorService();
            _rss = new RssService();
            
            Targets = new ObservableCollection<TargetConfig>();
            TargetsList.ItemsSource = Targets;

            // 初回同期
            Task.Run(async () => {
                await LoadConfigurationAsync();
                Dispatcher.Invoke(() => LoadTargets());
            });
        }

        private async Task LoadConfigurationAsync()
        {
            try 
            {
                string configPath = PathHelper.ResolvePath("targets.json");
                if (System.IO.File.Exists(configPath))
                {
                    Log("targets.json から設定を同期中...");
                    string json = System.IO.File.ReadAllText(configPath);
                    var configs = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<TargetConfig>>(json);
                    foreach (var c in configs)
                    {
                        _db.RegisterTarget(c.Url, c.TitleSelector, c.LinkSelector, c.Name, c.ContainerSelector, c.DescriptionSelector, c.DateSelector);
                    }
                }
            } catch (Exception ex) { Log($"同期失敗: {ex.Message}", true); }
        }

        private void LoadTargets()
        {
            Targets.Clear();
            var targets = _db.GetTargets();
            foreach (var t in targets)
            {
                Targets.Add(t);
            }
            TargetCountText.Text = Targets.Count.ToString();
            
            // 最新更新件数の簡易集計（直近24時間など）
            var latest = _db.GetLatestItems(100);
            UpdateCountText.Text = latest.Count(i => i.PublishedDate > DateTime.Now.AddDays(-1)).ToString();
        }

        private void Log(string message, bool isError = false)
        {
            Dispatcher.Invoke(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                LogText.Text += $"\n[{timestamp}] {(isError ? "[ERROR] " : "")}{message}";
                LogScrollViewer.ScrollToEnd();
            });
        }

        private void OnAddSiteClick(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddSiteWindow(_crawler, _db);
            addWindow.Owner = this;
            if (addWindow.ShowDialog() == true)
            {
                Log("新しいサイトを登録しました。");
                LoadTargets();
                SyncToJson();
            }
        }

        private void SyncToJson()
        {
            try
            {
                string configPath = PathHelper.ResolvePath("targets.json");
                var targets = _db.GetTargets();
                System.IO.File.WriteAllText(configPath, Newtonsoft.Json.JsonConvert.SerializeObject(targets, Newtonsoft.Json.Formatting.Indented));
                Log("targets.json を更新しました。");
            } catch (Exception ex) { Log($"JSON更新失敗: {ex.Message}", true); }
        }

        private async void OnRunAllClick(object sender, RoutedEventArgs e)
        {
            Log("全サイトの巡回を開始します...");
            
            try 
            {
                await _crawler.InitializeAsync();
            }
            catch (Exception ex)
            {
                Log($"初期化失敗: {ex.Message}", true);
                MessageBox.Show(ex.Message, "ブラウザ初期化エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            int totalNew = 0;
            foreach (var target in Targets.ToList())
            {
                try 
                {
                    Log($"{target.Name} を処理中...");
                    string html = await _crawler.GetHtmlAsync(target.Url, target.WaitMs);
                    var items = _extractor.ExtractItems(html, target.Url, target.TitleSelector, target.LinkSelector, target.ContainerSelector, target.DescriptionSelector, target.DateSelector);
                    
                    int newCount = 0;
                    foreach (var item in items)
                    {
                        if (_db.IsNewItem(item.Link))
                        {
                            _db.SaveItem(target.Id, item.Title, item.Link, item.PublishedDate, item.Description);
                            newCount++;
                        }
                    }
                    totalNew += newCount;
                    Log($"  -> {newCount} 件の新しい記事を追加しました。");
                }
                catch (Exception ex)
                {
                    Log($"{target.Name} の処理中にエラー: {ex.Message}", true);
                }
            }
            
            if (totalNew > 0)
            {
                _rss.GenerateAllFeeds(_db);
                Log($"完了: {totalNew} 件の記事を追加し、全ての RSS フィードを更新しました。");
            }
            else
            {
                Log("更新はありませんでした。");
            }
            
            await _crawler.CloseAsync();
            LoadTargets();
        }

        private async void OnRunTargetClick(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag is TargetConfig target)
            {
                Log($"{target.Name} の単独巡回を開始します...");
                
                try 
                {
                    await _crawler.InitializeAsync();
                }
                catch (Exception ex)
                {
                    Log($"初期化失敗: {ex.Message}", true);
                    MessageBox.Show(ex.Message, "ブラウザ初期化エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                try 
                {
                    string html = await _crawler.GetHtmlAsync(target.Url, target.WaitMs);
                    var items = _extractor.ExtractItems(html, target.Url, target.TitleSelector, target.LinkSelector, target.ContainerSelector, target.DescriptionSelector, target.DateSelector);
                    
                    int newCount = 0;
                    foreach (var item in items)
                    {
                        if (_db.IsNewItem(item.Link))
                        {
                            _db.SaveItem(target.Id, item.Title, item.Link, item.PublishedDate, item.Description);
                            newCount++;
                        }
                    }
                    
                    if (newCount > 0)
                    {
                        _rss.GenerateAllFeeds(_db);
                        Log($"完了: {newCount} 件の記事を追加しました。個別フィードも更新済みです。");
                    }
                    else Log("更新はありませんでした。");
                }
                catch (Exception ex)
                {
                    Log($"エラー: {ex.Message}", true);
                }
                finally { await _crawler.CloseAsync(); }
            }
        }

        private async void OnEditClick(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag is TargetConfig target)
            {
                var editWindow = new AddSiteWindow(_crawler, _db, target);
                editWindow.Owner = this;
                if (editWindow.ShowDialog() == true)
                {
                    Log($"{target.Name} の設定を更新しました。");
                    LoadTargets();
                    SyncToJson();
                }
            }
        }

        private void OnReadAllArticlesClick(object sender, RoutedEventArgs e)
        {
            var reader = new ArticleListWindow(_db);
            reader.Owner = this;
            reader.Show();
        }

        private void OnViewTargetArticlesClick(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag is TargetConfig target)
            {
                var reader = new ArticleListWindow(_db, target.Id);
                reader.Owner = this;
                reader.Show();
            }
        }

        private void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).Tag is TargetConfig target)
            {
                var result = MessageBox.Show($"{target.Name} を削除しますか？\n(URL: {target.Url})", "削除の確認", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    _db.DeleteTarget(target.Id);
                    Log($"{target.Name} を削除しました。");
                    LoadTargets();
                    SyncToJson();
                }
            }
        }
    }
}
