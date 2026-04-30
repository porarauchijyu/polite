using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using RssGenerator.Services;

namespace RssGenerator
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
            else
            {
                MainAsync(args).GetAwaiter().GetResult();
            }
        }

        static async Task MainAsync(string[] args)
        {
            // TLS 1.2 を有効化
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            try { Console.Title = "RSS Generator Utility (Premium Edition)"; } catch { }
            PrintHeader();
            
            // ... (既存の初期化)
            var db = new DatabaseService();
            var crawler = new CrawlerService();
            var extractor = new ExtractorService();
            var rss = new RssService();

            try
            {
                // コマンドライン引数の処理
                if (args.Length >= 1 && (args[0] == "--help" || args[0] == "/?"))
                {
                    PrintHelp();
                    return;
                }

                if (args.Length >= 1 && args[0] == "--read")
                {
                    await HandleReadCommandAsync(db);
                    return;
                }

                if (args.Length >= 2 && args[0] == "--add")
                {
                    string targetUrl = args[1];
                    await HandleAddCommandAsync(targetUrl, crawler, db);
                    return;
                }

                if (args.Length >= 2 && args[0] == "--repair")
                {
                    string targetUrl = args[1];
                    await HandleRepairAllCommandAsync(db, crawler, targetUrl); // 単体修復
                    return;
                }

                if (args.Length >= 1 && args[0] == "--repair-all")
                {
                    await HandleRepairAllCommandAsync(db, crawler, null);
                    return;
                }

                if (args.Length >= 2 && args[0] == "--import")
                {
                    await HandleImportCommandAsync(args[1], db);
                    return;
                }

                if (args.Length >= 1 && args[0] == "--sync")
                {
                    await HandleSyncCommandAsync(db);
                    return;
                }

                if (args.Length >= 1 && args[0] == "--run")
                {
                    // 巡回実行モード（--run）
                    // そのまま下の処理へ進む
                }
                else if (args.Length >= 1 && args[0] != "--run")
                {
                    // 未知の引数の場合はヘルプを表示して終了
                    PrintStatus($"未知の引数です: {args[0]}", ConsoleColor.Yellow);
                    PrintHelp();
                    return;
                }

                // 1. 設定の読み込みと同期
                await LoadConfigurationAsync(db);
                
                // ... (既存の巡回処理) ...
                // 2. ブラウザの初期化
                await crawler.InitializeAsync();

                // 3. 監視対象の取得と巡回
                var targets = db.GetTargets();
                bool hasGlobalUpdates = false;

                foreach (var target in targets)
                {
                    if (await ProcessTargetAsync(target, db, crawler, extractor))
                    {
                        hasGlobalUpdates = true;
                    }
                }

                // 4. RSSフィードの生成
                // 更新があった場合、または --run 指定（CI等）の場合は、常にフィードを再生成する
                if (hasGlobalUpdates || (args.Length > 0 && args[0] == "--run"))
                {
                    UpdateRssFeed(db, rss);
                }
                else
                {
                    PrintStatus("新しい更新は見つかりませんでした。フィードの更新をスキップします。", ConsoleColor.Gray);
                }
            }
            catch (Exception ex)
            {
                PrintError($"致命的なエラーが発生しました: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                await crawler.CloseAsync();
                PrintFooter();
            }

            if (args.Length == 0)
            {
                Console.WriteLine("\n[Enter] キーを押して終了します...");
                Console.ReadLine();
            }
        }

        private static async Task HandleAddCommandAsync(string url, CrawlerService crawler, DatabaseService db)
        {
            PrintStatus($"[Discovery] URL {url} の解析を開始します...", ConsoleColor.Cyan);
            await crawler.InitializeAsync();
            
            var inference = new InferenceService(crawler);
            var results = await inference.InferSelectorsAsync(url);

            if (results.Count == 0)
            {
                PrintError("ニュースリストの構造を自動特定できませんでした。手動で設定してください。");
                return;
            }

            InferenceResult selectedResult = null;
            if (results.Count == 1 && results[0].Score >= 50)
            {
                selectedResult = results[0];
                PrintStatus($"[Inference] 高い信頼度 ({selectedResult.Score}) でセレクタを特定しました。", ConsoleColor.Green);
            }
            else
            {
                selectedResult = SelectInferenceResult(results);
            }

            if (selectedResult == null)
            {
                PrintStatus("キャンセルされました。", ConsoleColor.Gray);
                return;
            }

            string defaultName = selectedResult.PageTitle?.Trim() ?? "New Site";
            Console.Write($"サイト名を入力してください (デフォルト: {defaultName}): ");
            string siteName = Console.ReadLine();
            if (string.IsNullOrEmpty(siteName)) siteName = defaultName;

            Console.Write("説明文のセレクタを入力してください (省略可): ");
            string descSel = Console.ReadLine();

            Console.Write("公表日のセレクタを入力してください (省略可): ");
            string dateSel = Console.ReadLine();

            Console.WriteLine();
            PrintStatus("=== 選択された設定 ===", ConsoleColor.Yellow);
            PrintStatus($"  Name:           {siteName}", ConsoleColor.White);
            PrintStatus($"  Container:      {selectedResult.ContainerSelector ?? "(なし)"}", ConsoleColor.Cyan);
            PrintStatus($"  TitleSelector:  {selectedResult.TitleSelector}", ConsoleColor.White);
            PrintStatus($"  DescSelector:   {descSel ?? "(なし)"}", ConsoleColor.Cyan);
            PrintStatus($"  DateSelector:   {dateSel ?? "(なし)"}", ConsoleColor.Cyan);
            PrintStatus($"  Sample Title:   {selectedResult.SampleTitle}", ConsoleColor.Gray);
            Console.WriteLine();

            Console.Write("この設定を targets.json に追加しますか？ (y/n): ");
            string input = Console.ReadLine();
            if (input?.ToLower() == "y")
            {
                List<TargetConfig> configs = new List<TargetConfig>();
                string configPath = PathHelper.ResolvePath("targets.json");
                if (File.Exists(configPath))
                {
                    string jsonContent = File.ReadAllText(configPath);
                    configs = (string.IsNullOrWhiteSpace(jsonContent) ? null : JsonConvert.DeserializeObject<List<TargetConfig>>(jsonContent)) ?? new List<TargetConfig>();
                }

                if (configs.Exists(c => c.Url == url))
                {
                    PrintStatus("既に登録済みのURLです。設定を更新します。", ConsoleColor.Yellow);
                    configs.RemoveAll(c => c.Url == url);
                }

                configs.Add(new TargetConfig { 
                    Name = siteName, 
                    Url = url, 
                    TitleSelector = selectedResult.TitleSelector, 
                    LinkSelector = selectedResult.LinkSelector,
                    ContainerSelector = selectedResult.ContainerSelector,
                    DescriptionSelector = descSel,
                    DateSelector = dateSel
                });
                File.WriteAllText(configPath, JsonConvert.SerializeObject(configs, Formatting.Indented));
                
                db.RegisterTarget(url, selectedResult.TitleSelector, selectedResult.LinkSelector, siteName, selectedResult.ContainerSelector, descSel, dateSel);
                PrintStatus("正常に targets.json を更新しました。", ConsoleColor.Green);
            }
            else
            {
                PrintStatus("キャンセルしました。", ConsoleColor.Gray);
            }
        }

        private static async Task HandleRepairAllCommandAsync(DatabaseService db, CrawlerService crawler, string specificUrl = null)
        {
            string configPath = PathHelper.ResolvePath("targets.json");
            if (!File.Exists(configPath))
            {
                PrintError("targets.json が見つかりません。");
                return;
            }

            string jsonContent = File.Exists(configPath) ? File.ReadAllText(configPath) : "[]";
            var configs = (string.IsNullOrWhiteSpace(jsonContent) ? null : JsonConvert.DeserializeObject<List<TargetConfig>>(jsonContent)) ?? new List<TargetConfig>();
            var targetsToRepair = string.IsNullOrEmpty(specificUrl) 
                ? configs 
                : configs.Where(c => c.Url == specificUrl).ToList();

            if (targetsToRepair.Count == 0)
            {
                PrintError("対象となるサイトが見つかりません。");
                return;
            }

            PrintStatus($"[Repair-All] {targetsToRepair.Count} 件のサイトを解析します...", ConsoleColor.Cyan);
            await crawler.InitializeAsync();
            var inference = new InferenceService(crawler);

            int successCount = 0;
            foreach (var target in targetsToRepair)
            {
                Console.WriteLine();
                PrintStatus($"--- 解析中: {target.Name ?? target.Url} ---", ConsoleColor.Yellow);
                var results = await inference.InferSelectorsAsync(target.Url);

                if (results.Count == 0)
                {
                    PrintError("  -> 解析失敗。有効なセレクタが見つかりませんでした。");
                    continue;
                }

                InferenceResult selected = null;
                if (results.Count == 1 && results[0].Score >= 50)
                {
                    PrintStatus($"  -> 信頼度高 ({results[0].Score}): 自動適用します。", ConsoleColor.Green);
                    selected = results[0];
                }
                else
                {
                    PrintStatus($"  -> 候補が {results.Count} 件あります。確認が必要です。", ConsoleColor.Yellow);
                    selected = SelectInferenceResult(results);
                }

                if (selected != null)
                {
                    target.TitleSelector = selected.TitleSelector;
                    target.LinkSelector = selected.LinkSelector;
                    target.ContainerSelector = selected.ContainerSelector;
                    // Note: DescriptionSelector and DateSelector will be kept if already exists, unless we prompt for it
                    db.RegisterTarget(target.Url, selected.TitleSelector, selected.LinkSelector, target.Name, selected.ContainerSelector, target.DescriptionSelector, target.DateSelector);
                    successCount++;
                }
            }

            File.WriteAllText(configPath, JsonConvert.SerializeObject(configs, Formatting.Indented));
            PrintStatus($"\n[Complete] {successCount} 件のサイトを修復・更新しました。", ConsoleColor.Green);
        }

        private static async Task HandleRepairCommandAsync(string url, CrawlerService crawler, DatabaseService db)
        {
            await HandleRepairAllCommandAsync(db, crawler, url);
        }

        private static async Task HandleReadCommandAsync(DatabaseService db)
        {
            await Task.Yield();
            
            while (true)
            {
                SafeClear();
                PrintHeader();
                
                var targets = db.GetTargets();
                if (targets.Count == 0)
                {
                    PrintStatus("登録されているサイトがありません。先に追加を行ってください。", ConsoleColor.Yellow);
                    System.Threading.Thread.Sleep(2000);
                    return;
                }

                PrintStatus("【 購読サイト一覧 】", ConsoleColor.Yellow);
                Console.WriteLine("----------------------------------------------------------------");
                for (int i = 0; i < targets.Count; i++)
                {
                    var t = targets[i];
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"[{i + 1:D2}] ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write((t.Name ?? "No Name").PadRight(30));
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($" ({t.Url})");
                }
                Console.WriteLine("----------------------------------------------------------------");
                PrintStatus("閲覧するサイト番号を入力、または 'q' で終了:", ConsoleColor.White);
                Console.Write("> ");

                string siteInput = Console.ReadLine();
                if (string.IsNullOrEmpty(siteInput) || siteInput.ToLower() == "q") break;

                if (int.TryParse(siteInput, out int siteIdx) && siteIdx > 0 && siteIdx <= targets.Count)
                {
                    var target = targets[siteIdx - 1];
                    await ShowArticlesMenuAsync(db, target);
                }
                else
                {
                    PrintStatus("無効な入力です。", ConsoleColor.Red);
                    System.Threading.Thread.Sleep(800);
                }
            }
        }

        private static async Task ShowArticlesMenuAsync(DatabaseService db, TargetConfig target)
        {
            PrintStatus($"[{target.Name ?? target.Url}] 記事を読み込んでいます...", ConsoleColor.Cyan);
            var items = db.GetLatestItemsByTarget(target.Id, 100);

            if (items.Count == 0)
            {
                PrintStatus("この記事にはまだ新着情報がありません。", ConsoleColor.Yellow);
                System.Threading.Thread.Sleep(1500);
                return;
            }

            while (true)
            {
                Console.Clear();
                PrintHeader();
                PrintStatus($"【 {target.Name ?? target.Url} - 最新記事 (最大100件) 】", ConsoleColor.Yellow);
                Console.WriteLine("----------------------------------------------------------------");

                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($"[{i + 1:D2}] ");
                    
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write($"[{item.PublishedDate:yyyy/MM/dd}] ");

                    Console.ForegroundColor = ConsoleColor.Green;
                    string displayTitle = item.Title.Trim();
                    if (displayTitle.Length > 55) displayTitle = displayTitle.Substring(0, 52) + "...";
                    Console.WriteLine(displayTitle);
                    
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.Write("     Link: ");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine(item.Link);
                    
                    if (!string.IsNullOrEmpty(item.Description))
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine($"     {item.Description}");
                    }
                    Console.WriteLine(); 
                }

                Console.WriteLine("----------------------------------------------------------------");
                PrintStatus("番号で記事を開く、'b' で戻る、'q' で終了:", ConsoleColor.White);
                Console.Write("> ");

                string input = Console.ReadLine();
                if (string.IsNullOrEmpty(input) || input.ToLower() == "b") break;
                if (input.ToLower() == "q") Environment.Exit(0);

                if (int.TryParse(input, out int index) && index > 0 && index <= items.Count)
                {
                    var selected = items[index - 1];
                    PrintStatus($"ブラウザで開きます: {selected.Title}", ConsoleColor.Cyan);
                    try
                    {
                        Process.Start(new ProcessStartInfo(selected.Link) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        PrintError($"ページを開けませんでした: {ex.Message}");
                    }
                    System.Threading.Thread.Sleep(1000);
                }
                else
                {
                    PrintStatus("無効な入力です。", ConsoleColor.Red);
                    System.Threading.Thread.Sleep(800);
                }
            }
        }

        private static Task LoadConfigurationAsync(DatabaseService db)
        {
            string configPath = PathHelper.ResolvePath("targets.json");
            if (File.Exists(configPath))
            {
                PrintStatus("targets.json から設定を読み込んでいます...", ConsoleColor.Cyan);
                try
                {
                    string json = File.ReadAllText(configPath);
                    var configs = (string.IsNullOrWhiteSpace(json) ? null : JsonConvert.DeserializeObject<List<TargetConfig>>(json)) ?? new List<TargetConfig>();
                    
                    if (configs.Count == 0)
                    {
                        // JSONが空だがDBにデータがある場合、DBから同期を試みる
                        var dbTargets = db.GetTargets();
                        if (dbTargets.Count > 0)
                        {
                            PrintStatus("  -> targets.json が空のため、DBの設定から復旧します...", ConsoleColor.Yellow);
                            File.WriteAllText(configPath, JsonConvert.SerializeObject(dbTargets, Formatting.Indented));
                            configs = dbTargets;
                        }
                    }

                    foreach (var config in configs)
                    {
                        // JSON側のセレクタが空の場合は、DB側を上書きしないようにする（手動同期時以外）
                        // ここでは、読み込み時に不利益がないよう、DBに既存データがあればそれを考慮したRegisterTargetを検討すべきだが、
                        // シンプルに、空でないフィールドがある場合のみ更新のトリガーとする
                        db.RegisterTarget(config.Url, config.TitleSelector, config.LinkSelector, config.Name, config.ContainerSelector, config.DescriptionSelector, config.DateSelector);
                        PrintStatus($"  -> 同期完了: {config.Name ?? config.Url}", ConsoleColor.Gray);
                    }
                    PrintStatus($"同期完了: 合計 {configs.Count} 件のターゲットを同期しました。", ConsoleColor.Green);
                }
                catch (Exception ex)
                {
                    PrintError($"設定ファイルの読み込みに失敗しました: {ex.Message}");
                }
            }
            else
            {
                PrintStatus("警告: targets.json が見つかりません。DB内の既存設定を使用します。", ConsoleColor.Yellow);
            }
            return Task.FromResult(0);
        }

        private static async Task<bool> ProcessTargetAsync(TargetConfig target, DatabaseService db, CrawlerService crawler, ExtractorService extractor)
        {
            Console.WriteLine();
            PrintStatus($"[Processing] {target.Url}", ConsoleColor.White);
            bool targetHasUpdates = false;

            try
            {
                string html = await crawler.GetHtmlAsync(target.Url);
                var items = extractor.ExtractItems(html, target.Url, target.TitleSelector, target.LinkSelector, target.ContainerSelector, target.DescriptionSelector, target.DateSelector);

                int newCount = 0;
                foreach (var item in items)
                {
                    if (db.IsNewItem(item.Link))
                    {
                        PrintStatus($"  [NEW] {item.Title}", ConsoleColor.Green);
                        db.SaveItem(target.Id, item.Title, item.Link, item.PublishedDate, item.Description);
                        newCount++;
                        targetHasUpdates = true;
                    }
                }

                if (newCount > 0)
                {
                    PrintStatus($"  -> {newCount} 件の新しい記事を追加しました。", ConsoleColor.Cyan);
                }
                else if (items.Count == 0 && db.HasHistory(target.Id))
                {
                    PrintStatus("  [WARNING] 記事が1件も見つかりませんでした。", ConsoleColor.Yellow);
                    PrintStatus("            サイト構造が変化した可能性があります。修復を検討してください：", ConsoleColor.Yellow);
                    PrintStatus($"            .\\RssGenerator.exe --repair \"{target.Url}\"", ConsoleColor.White);
                }
                else
                {
                    PrintStatus("  更新はありませんでした。", ConsoleColor.DarkGray);
                }
            }
            catch (Exception ex)
            {
                PrintError($"  処理中にエラーが発生しました: {ex.Message}");
            }

            return targetHasUpdates;
        }

        private static void UpdateRssFeed(DatabaseService db, RssService rss)
        {
            Console.WriteLine();
            PrintStatus("RSSフィードを巡回・生成しています...", ConsoleColor.Magenta);
            rss.GenerateAllFeeds(db);
            PrintStatus("完了: 全てのフィードを保存・更新しました。", ConsoleColor.Green);
        }

        #region UI Helpers
        private static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
   RSS GENERATOR UTILITY - CUSTOM FEED MAKER
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.ResetColor();
        }

        private static void PrintFooter()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.ResetColor();
        }

        private static void PrintStatus(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void PrintError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}");
            Console.ResetColor();
        }
        private static void PrintHelp()
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("利用可能なコマンド:");
            Console.WriteLine("  (なし)             登録されている全サイトを巡回し、RSSを更新します。");
            Console.WriteLine("  --read             現在登録されているサイトの一覧を表示します。");
            Console.WriteLine("  --add [URL]        新しいURLを解析し、監視対象に追加します。");
            Console.WriteLine("  --repair [URL]     指定したURLのセレクタを再解析し、修復します。");
            Console.WriteLine("  --repair-all       全サイトのセレクタを自動で再解析・修復します。");
            Console.WriteLine("  --import [CSV]     CSVファイルからサイト設定を一括インポートします。");
            Console.WriteLine("                     Layout: URL, Name, TitleSelector, LinkSelector, ContainerSelector, DescriptionSelector, DateSelector");
            Console.WriteLine("  --sync             データベース内の現在の設定を targets.json に書き出します。");
            Console.WriteLine("  --help, /?         このヘルプメッセージを表示します。");
            Console.WriteLine("\n使用例:");
            Console.WriteLine("  RssGenerator.exe --add https://example.com/news");
            Console.WriteLine("  RssGenerator.exe --import sites.csv");
            Console.ResetColor();
        }
        #endregion
        private static async Task HandleImportCommandAsync(string csvPath, DatabaseService db)
        {
            if (!File.Exists(csvPath))
            {
                PrintError($"ファイルが見つかりません: {csvPath}");
                return;
            }

            try
            {
                // UTF-8 BOM を除去するために文字列として読み込み、行に分割
                string csvContent = File.ReadAllText(csvPath).Trim('\uFEFF');
                var lines = csvContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                
                var configPath = PathHelper.ResolvePath("targets.json");
                var configs = new List<TargetConfig>();
                if (File.Exists(configPath))
                {
                    string jsonContent = File.ReadAllText(configPath);
                    configs = (string.IsNullOrWhiteSpace(jsonContent) ? null : JsonConvert.DeserializeObject<List<TargetConfig>>(jsonContent)) ?? new List<TargetConfig>();
                }
                
                // インポート時にJSONが空、あるいはDBより少ない場合を考慮しDBと同期（既存データを保護）
                var dbTargets = db.GetTargets();
                foreach (var dbT in dbTargets)
                {
                    if (!configs.Exists(c => c.Url == dbT.Url))
                    {
                        configs.Add(dbT);
                    }
                }

                int count = 0;
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(',').Select(p => p.Trim()).ToArray();
                    if (parts.Length < 1) continue;

                    string url = parts[0];
                    // BOM除去後でも念のため Trim。ヘッダースキップ。
                    url = url.Trim();
                    if (url.Equals("url", StringComparison.OrdinalIgnoreCase) || !url.StartsWith("http")) continue;

                    string name = parts.Length > 1 ? parts[1] : url;
                    string titleSel = parts.Length > 2 ? parts[2] : "";
                    string linkSel = parts.Length > 3 ? parts[3] : "";
                    string contSel = parts.Length > 4 ? parts[4] : "";
                    string descSel = parts.Length > 5 ? parts[5] : "";
                    string dateSel = parts.Length > 6 ? parts[6] : "";

                    var existing = configs.Find(c => c.Url == url);
                    if (existing == null)
                    {
                        // 新規追加
                        configs.Add(new TargetConfig { 
                            Url = url, 
                            Name = name, 
                            TitleSelector = titleSel, 
                            LinkSelector = linkSel,
                            ContainerSelector = contSel,
                            DescriptionSelector = descSel,
                            DateSelector = dateSel
                        });
                        db.RegisterTarget(url, titleSel, linkSel, name, contSel, descSel, dateSel);
                        count++;
                    }
                    else
                    {
                        // 既存の更新（CSV側に値がある場合のみマージし、countに含める）
                        bool updated = false;
                        if (!string.IsNullOrEmpty(name) && name != url && existing.Name != name) { existing.Name = name; updated = true; }
                        if (!string.IsNullOrEmpty(titleSel) && existing.TitleSelector != titleSel) { existing.TitleSelector = titleSel; updated = true; }
                        if (!string.IsNullOrEmpty(linkSel) && existing.LinkSelector != linkSel) { existing.LinkSelector = linkSel; updated = true; }
                        if (!string.IsNullOrEmpty(contSel) && existing.ContainerSelector != contSel) { existing.ContainerSelector = contSel; updated = true; }
                        if (!string.IsNullOrEmpty(descSel) && existing.DescriptionSelector != descSel) { existing.DescriptionSelector = descSel; updated = true; }
                        if (!string.IsNullOrEmpty(dateSel) && existing.DateSelector != dateSel) { existing.DateSelector = dateSel; updated = true; }
                        
                        if (updated)
                        {
                            // 更新があった場合のみDBに書き戻す
                            db.RegisterTarget(url, 
                                string.IsNullOrEmpty(titleSel) ? existing.TitleSelector : titleSel,
                                string.IsNullOrEmpty(linkSel) ? existing.LinkSelector : linkSel,
                                string.IsNullOrEmpty(name) || name == url ? existing.Name : name,
                                string.IsNullOrEmpty(contSel) ? existing.ContainerSelector : contSel,
                                string.IsNullOrEmpty(descSel) ? existing.DescriptionSelector : descSel,
                                string.IsNullOrEmpty(dateSel) ? existing.DateSelector : dateSel
                            );
                            count++;
                        }
                    }
                }

                if (count > 0 || !File.Exists(configPath))
                {
                    File.WriteAllText(configPath, JsonConvert.SerializeObject(configs, Formatting.Indented));
                    PrintStatus($"{count} 件のサイトをインポート・更新しました。", ConsoleColor.Green);
                }
                else
                {
                    PrintStatus("既に最新の状態か、インポート対象が見つかりませんでした。", ConsoleColor.Yellow);
                }
            }
            catch (Exception ex)
            {
                PrintError($"インポートエラー: {ex.Message}");
            }
        }

        private static InferenceResult SelectInferenceResult(List<InferenceResult> results)
        {
            while (true)
            {
                PrintStatus("\n【 抽出候補の選択 】", ConsoleColor.Yellow);
                for (int i = 0; i < results.Count; i++)
                {
                    var r = results[i];
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"[{i + 1}] スコア: {r.Score}");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"    Container: {r.ContainerSelector ?? "(なし)"}");
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"    Selector:  {r.TitleSelector}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    foreach (var sample in r.Samples)
                    {
                        Console.WriteLine($"    - {sample}");
                    }
                    Console.WriteLine();
                }

                PrintStatus("適用する候補の番号を入力、または 'q' でキャンセル: ", ConsoleColor.White);
                Console.Write("> ");
                string input = Console.ReadLine();
                if (input?.ToLower() == "q") return null;

                if (int.TryParse(input, out int idx) && idx > 0 && idx <= results.Count)
                {
                    return results[idx - 1];
                }
                PrintStatus("無効な入力です。", ConsoleColor.Red);
            }
        }
        private static async Task HandleSyncCommandAsync(DatabaseService db)
        {
            PrintStatus("データベースから targets.json を更新(上書き)しています...", ConsoleColor.Cyan);
            var targets = db.GetTargets();
            if (targets.Count == 0)
            {
                PrintError("データベースに登録されているサイトがありません。");
                return;
            }

            string configPath = PathHelper.ResolvePath("targets.json");
            try
            {
                File.WriteAllText(configPath, JsonConvert.SerializeObject(targets, Formatting.Indented));
                PrintStatus($"成功: {targets.Count} 件のターゲットを targets.json に書き出しました。", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                PrintError($"同期中にエラーが発生しました: {ex.Message}");
            }
        }
        private static void SafeClear()
        {
            try { Console.Clear(); } catch { }
        }
    }
}

