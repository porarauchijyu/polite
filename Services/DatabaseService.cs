using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace RssGenerator.Services
{
    public class TargetConfig
    {
        public int Id { get; set; }
        public string Name { get; set; } // サイト表示名
        public string Url { get; set; }
        public string TitleSelector { get; set; }
        public string LinkSelector { get; set; }
        public string ContainerSelector { get; set; } // [NEW] アイテム一覧を包むコンテナ
        public string DescriptionSelector { get; set; } // [NEW] 説明文
        public string DateSelector { get; set; } // [NEW] 公表日
        public int WaitMs { get; set; } = 3000; // [NEW] 待機時間（ミリ秒）
    }

    public class DatabaseService
    {
        private readonly string dbPath;
        private readonly string connectionString;

        public DatabaseService()
        {
            this.dbPath = PathHelper.ResolvePath("feed.sqlite");
            connectionString = $"Data Source={dbPath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }

            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                string createTargetsTable = @"
                    CREATE TABLE IF NOT EXISTS Targets (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT,
                        Url TEXT NOT NULL UNIQUE,
                        TitleSelector TEXT NOT NULL,
                        LinkSelector TEXT NOT NULL,
                        ContainerSelector TEXT,
                        DescriptionSelector TEXT,
                        DateSelector TEXT
                    );";

                string createFeedItemsTable = @"
                    CREATE TABLE IF NOT EXISTS FeedItems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        TargetId INTEGER,
                        Title TEXT,
                        Link TEXT UNIQUE,
                        Description TEXT,
                        PublishedDate DATETIME,
                        FOREIGN KEY(TargetId) REFERENCES Targets(Id)
                    );";

                using (var cmd = new SQLiteCommand(createTargetsTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // スキーマ移行: Name カラムがない場合は追加
                try
                {
                    using (var cmd = new SQLiteCommand("ALTER TABLE Targets ADD COLUMN Name TEXT", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { /* 既にある場合は無視 */ }
                
                try
                {
                    using (var cmd = new SQLiteCommand("ALTER TABLE Targets ADD COLUMN ContainerSelector TEXT", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { /* 既にある場合は無視 */ }

                try
                {
                    using (var cmd = new SQLiteCommand("ALTER TABLE Targets ADD COLUMN DescriptionSelector TEXT", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { /* 既にある場合は無視 */ }

                try
                {
                    using (var cmd = new SQLiteCommand("ALTER TABLE FeedItems ADD COLUMN Description TEXT", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { /* 既にある場合は無視 */ }

                try
                {
                    using (var cmd = new SQLiteCommand("ALTER TABLE Targets ADD COLUMN DateSelector TEXT", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { /* 既にある場合は無視 */ }

                try
                {
                    using (var cmd = new SQLiteCommand("ALTER TABLE Targets ADD COLUMN WaitMs INTEGER DEFAULT 3000", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { /* 既にある場合は無視 */ }

                using (var cmd = new SQLiteCommand(createFeedItemsTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool IsNewItem(string link)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM FeedItems WHERE Link = @link";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@link", link);
                    long count = (long)cmd.ExecuteScalar();
                    return count == 0;
                }
            }
        }

        public void SaveItem(int targetId, string title, string link, DateTime pubDate, string description = null)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string insert = "INSERT OR IGNORE INTO FeedItems (TargetId, Title, Link, PublishedDate, Description) VALUES (@targetId, @title, @link, @pubDate, @description)";
                using (var cmd = new SQLiteCommand(insert, conn))
                {
                    cmd.Parameters.AddWithValue("@targetId", targetId);
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@link", link);
                    cmd.Parameters.AddWithValue("@pubDate", pubDate);
                    cmd.Parameters.AddWithValue("@description", (object)description ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateTarget(int id, string url, string titleSelector, string linkSelector, string name, string containerSelector, string descriptionSelector, string dateSelector, int waitMs)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    UPDATE Targets 
                    SET Url = @url, Name = @name, TitleSelector = @titleSelector, LinkSelector = @linkSelector, 
                        ContainerSelector = @containerSelector, DescriptionSelector = @descriptionSelector, DateSelector = @dateSelector,
                        WaitMs = @waitMs
                    WHERE Id = @id";
                
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@url", url);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.Parameters.AddWithValue("@titleSelector", titleSelector);
                    cmd.Parameters.AddWithValue("@linkSelector", linkSelector);
                    cmd.Parameters.AddWithValue("@containerSelector", (object)containerSelector ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@descriptionSelector", (object)descriptionSelector ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@dateSelector", (object)dateSelector ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@waitMs", waitMs);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteTarget(int id)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM Targets WHERE Id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void RegisterTarget(string url, string titleSelector, string linkSelector, string name = null, string containerSelector = null, string descriptionSelector = null, string dateSelector = null, int waitMs = 3000)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();

                // 存在確認
                string checkQuery = "SELECT COUNT(*) FROM Targets WHERE Url = @url";
                long count = 0;
                using (var checkCmd = new SQLiteCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@url", url);
                    count = (long)checkCmd.ExecuteScalar();
                }

                if (count > 0)
                {
                    // 更新 
                    string update = "UPDATE Targets SET TitleSelector = @titleSelector, LinkSelector = @linkSelector, ContainerSelector = @containerSelector, DescriptionSelector = @descriptionSelector, DateSelector = @dateSelector, Name = COALESCE(@name, Name), WaitMs = @waitMs WHERE Url = @url";
                    using (var updateCmd = new SQLiteCommand(update, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@url", url);
                        updateCmd.Parameters.AddWithValue("@titleSelector", titleSelector);
                        updateCmd.Parameters.AddWithValue("@linkSelector", linkSelector);
                        updateCmd.Parameters.AddWithValue("@containerSelector", (object)containerSelector ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@descriptionSelector", (object)descriptionSelector ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@dateSelector", (object)dateSelector ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@name", (object)name ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@waitMs", waitMs);
                        updateCmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    // 追加
                    string insert = "INSERT INTO Targets (Name, Url, TitleSelector, LinkSelector, ContainerSelector, DescriptionSelector, DateSelector, WaitMs) VALUES (@name, @url, @titleSelector, @linkSelector, @containerSelector, @descriptionSelector, @dateSelector, @waitMs)";
                    using (var insertCmd = new SQLiteCommand(insert, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@name", (object)name ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@url", url);
                        insertCmd.Parameters.AddWithValue("@titleSelector", titleSelector);
                        insertCmd.Parameters.AddWithValue("@linkSelector", linkSelector);
                        insertCmd.Parameters.AddWithValue("@containerSelector", (object)containerSelector ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@descriptionSelector", (object)descriptionSelector ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@dateSelector", (object)dateSelector ?? DBNull.Value);
                        insertCmd.Parameters.AddWithValue("@waitMs", waitMs);
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<FeedItem> GetLatestItems(int count = 50)
        {
            var items = new List<FeedItem>();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT f.Title, f.Link, f.Description, f.PublishedDate, COALESCE(t.Name, t.Url) as SourceTitle 
                    FROM FeedItems f
                    JOIN Targets t ON f.TargetId = t.Id
                    ORDER BY f.PublishedDate DESC LIMIT @count";
                
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@count", count);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new FeedItem
                            {
                                Title = reader["Title"]?.ToString() ?? "Untitled",
                                Link = reader["Link"]?.ToString() ?? "",
                                Description = reader["Description"]?.ToString() ?? "",
                                PublishedDate = reader["PublishedDate"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["PublishedDate"]),
                                SourceTitle = reader["SourceTitle"]?.ToString() ?? "Unknown"
                            });
                        }
                    }
                }
            }
            return items;
        }

        public List<FeedItem> GetLatestItemsByTarget(int targetId, int count = 100)
        {
            var items = new List<FeedItem>();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = @"
                    SELECT f.Title, f.Link, f.Description, f.PublishedDate, COALESCE(t.Name, t.Url) as SourceTitle 
                    FROM FeedItems f
                    JOIN Targets t ON f.TargetId = t.Id
                    WHERE f.TargetId = @targetId
                    ORDER BY f.PublishedDate DESC LIMIT @count";
                
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@targetId", targetId);
                    cmd.Parameters.AddWithValue("@count", count);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            items.Add(new FeedItem
                            {
                                Title = reader["Title"]?.ToString() ?? "Untitled",
                                Link = reader["Link"]?.ToString() ?? "",
                                Description = reader["Description"]?.ToString() ?? "",
                                PublishedDate = reader["PublishedDate"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["PublishedDate"]),
                                SourceTitle = reader["SourceTitle"]?.ToString() ?? "Unknown"
                            });
                        }
                    }
                }
            }
            return items;
        }

        public List<TargetConfig> GetTargets()
        {
            var targets = new List<TargetConfig>();
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT Id, Name, Url, TitleSelector, LinkSelector, ContainerSelector, DescriptionSelector, DateSelector, WaitMs FROM Targets";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            targets.Add(new TargetConfig
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Name = reader["Name"]?.ToString(),
                                Url = reader["Url"].ToString(),
                                TitleSelector = reader["TitleSelector"].ToString(),
                                LinkSelector = reader["LinkSelector"].ToString(),
                                ContainerSelector = reader["ContainerSelector"]?.ToString(),
                                DescriptionSelector = reader["DescriptionSelector"]?.ToString(),
                                DateSelector = reader["DateSelector"]?.ToString(),
                                WaitMs = reader["WaitMs"] == DBNull.Value ? 3000 : Convert.ToInt32(reader["WaitMs"])
                            });
                        }
                    }
                }
            }
            return targets;
        }
        public bool HasHistory(int targetId)
        {
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM FeedItems WHERE TargetId = @targetId";
                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@targetId", targetId);
                    long count = (long)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }
    }
}

