using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using RssGenerator.Services;

namespace RssGenerator
{
    public partial class ArticleListWindow : Window
    {
        private readonly DatabaseService _db;

        public ArticleListWindow(DatabaseService db, int? targetId = null)
        {
            InitializeComponent();
            _db = db;
            LoadArticles(targetId);
        }

        private void LoadArticles(int? targetId)
        {
            List<FeedItem> items;
            if (targetId.HasValue)
            {
                items = _db.GetLatestItemsByTarget(targetId.Value, 100);
            }
            else
            {
                items = _db.GetLatestItems(100);
            }
            ArticleListView.ItemsSource = items;
        }

        private void OnArticleClick(object sender, MouseButtonEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is FeedItem item && !string.IsNullOrEmpty(item.Link))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = item.Link,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("リンクを開けませんでした: " + ex.Message);
                }
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
