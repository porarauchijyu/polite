using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using PuppeteerSharp;
using Microsoft.Win32;

namespace RssGenerator.Services
{
    public class CrawlerService
    {
        private IBrowser _browser;
        private string _executablePath;
        private List<string> _candidatePaths;
        private AppSettings _settings;

        public CrawlerService()
        {
            _settings = AppSettings.Load();
            this.Headless = _settings.Headless;

            // CI環境 (GitHub Actions 等) の検知
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS")) ||
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
            {
                this.Headless = true;
                Console.WriteLine("[Crawler] CI環境を検知しました。強制的にヘッドレスモードで起動します。");
            }
        }

        public async Task InitializeAsync()
        {
            if (_candidatePaths != null && _candidatePaths.Count > 0) return;
            _candidatePaths = new List<string>();

            // 1. 設定ファイルからのパスを最優先
            if (!string.IsNullOrEmpty(_settings.BrowserPath))
            {
                if (File.Exists(_settings.BrowserPath))
                {
                    Console.WriteLine($"[Crawler] 設定されたブラウザパスを使用します: {_settings.BrowserPath}");
                    _candidatePaths.Add(_settings.BrowserPath);
                }
                else
                {
                    Console.WriteLine($"[Warning] 設定されたブラウザパスが見つかりません: {_settings.BrowserPath}");
                }
            }

            // 2. システムのブラウザを検索
            Console.WriteLine("システムのブラウザ (Chrome/Edge) を検索中...");
            var systemPaths = GetBrowserPaths();
            foreach (var path in systemPaths)
            {
                if (!_candidatePaths.Contains(path)) _candidatePaths.Add(path);
            }

            if (_candidatePaths.Count == 0)
            {
                // システムブラウザが見つからない場合のフォールバック（ダウンロード）
                var runtimesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtimes");
                if (!Directory.Exists(runtimesPath)) Directory.CreateDirectory(runtimesPath);

                var browserFetcher = new BrowserFetcher(new BrowserFetcherOptions { Path = runtimesPath });
                var installed = browserFetcher.GetInstalledBrowsers();
                
                if (!installed.Any())
                {
                    Console.WriteLine("システムブラウザが見つかりません。Chromium をダウンロードします...");
                    try 
                    {
                        await browserFetcher.DownloadAsync();
                        var newInstalled = browserFetcher.GetInstalledBrowsers();
                        if (newInstalled.Any())
                        {
                            _candidatePaths.Add(newInstalled.First().GetExecutablePath());
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Error] Chromium のダウンロードに失敗しました: {ex.Message}");
                    }
                }
                else
                {
                    _candidatePaths.Add(installed.First().GetExecutablePath());
                }
            }
        }

        private List<string> GetBrowserPaths()
        {
            var paths = new List<string>();
            
            // 1. レジストリからの検索
            string[] registryKeys = {
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe",
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe",
                @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe",
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe"
            };

            foreach (var key in registryKeys)
            {
                try 
                {
                    var path = Registry.GetValue(key, "", null) as string;
                    path = CleanPath(path);
                    if (!string.IsNullOrEmpty(path) && File.Exists(path) && !paths.Contains(path))
                    {
                        paths.Add(path);
                    }
                } catch { /* レジストリアクセスエラーは無視 */ }
            }

            // 2. 標準的なインストールパス
            string[] commonPaths = {
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\Application\chrome.exe"),
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\Application\msedge.exe")
            };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path) && !paths.Contains(path))
                {
                    paths.Add(path);
                }
            }

            // 3. PATH環境変数からの検索 (where コマンド相当)
            try 
            {
                foreach (var exe in new[] { "chrome.exe", "msedge.exe" })
                {
                    var path = FindInPath(exe);
                    if (!string.IsNullOrEmpty(path) && File.Exists(path) && !paths.Contains(path))
                    {
                        paths.Add(path);
                    }
                }
            } catch { }

            return paths;
        }

        private string FindInPath(string exeName)
        {
            var values = Environment.GetEnvironmentVariable("PATH");
            if (values == null) return null;

            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, exeName);
                if (File.Exists(fullPath)) return fullPath;
            }
            return null;
        }

        private string CleanPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            return path.Trim().Trim('\"', '\'');
        }

        private async Task<IPage> CreateStealthPageAsync()
        {
            var page = await _browser.NewPageAsync();
            
            // 標準的なブラウザヘッダーを設定
            await page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
            {
                { "Accept-Language", "ja-JP,ja;q=0.9,en-US;q=0.8,en;q=0.7" },
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7" },
                { "Upgrade-Insecure-Requests", "1" },
                { "sec-ch-ua", "\"Not_A Brand\";v=\"8\", \"Chromium\";v=\"120\", \"Google Chrome\";v=\"120\"" },
                { "sec-ch-ua-mobile", "?0" },
                { "sec-ch-ua-platform", "\"Windows\"" }
            });

            // navigator.webdriver を隠蔽し、Chrome 固有のオブジェクトを偽装
            await page.EvaluateFunctionOnNewDocumentAsync(@"
                () => {
                    Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                    window.navigator.chrome = { runtime: {} };
                    Object.defineProperty(navigator, 'languages', { get: () => ['ja-JP', 'ja', 'en-US', 'en'] });
                    Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
                }
            ");

            await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            await page.SetViewportAsync(new ViewPortOptions { Width = 1920, Height = 1080 });

            return page;
        }

        public async Task<string> GetHtmlAsync(string url, int waitMs = 3000)
        {
            await EnsureBrowserAsync();

            using (var page = await CreateStealthPageAsync())
            {
                Console.WriteLine($"[Crawler] {url} を取得中 (Stealthモード, wait={waitMs}ms)...");
                
                try 
                {
                    // 必要に応じてルートドメインを先に訪問して Cookie を確立
                    await WarmupDomainAsync(page, url);

                    await page.GoToAsync(url, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 }, Timeout = 30000 });
                    await Task.Delay(waitMs); 

                    return await page.GetContentAsync();
                }
                catch (Exception ex) when (ex.Message.Contains("ERR_CONNECTION_RESET"))
                {
                    Console.WriteLine("[Crawler] 接続リセットを検知。リファラを設定して再試行します...");
                    Uri uri = new Uri(url);
                    string root = $"{uri.Scheme}://{uri.Host}/";
                    
                    await page.SetExtraHttpHeadersAsync(new Dictionary<string, string> { { "Referer", root } });
                    await page.GoToAsync(root, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } });
                    await Task.Delay(2000);
                    
                    await page.GoToAsync(url, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } });
                    return await page.GetContentAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] コンテンツ取得失敗 ({url}): {ex.Message}");
                    throw;
                }
            }
        }

        public async Task<T> EvaluateAsync<T>(string url, string script, int waitMs = 3000)
        {
            await EnsureBrowserAsync();

            using (var page = await CreateStealthPageAsync())
            {
                try 
                {
                    await WarmupDomainAsync(page, url);
                    await page.GoToAsync(url, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 }, Timeout = 30000 });
                    await Task.Delay(waitMs);

                    return await page.EvaluateExpressionAsync<T>(script);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] スクリプト実行失敗 ({url}): {ex.Message}");
                    throw;
                }
            }
        }

        private async Task WarmupDomainAsync(IPage page, string targetUrl)
        {
            try
            {
                Uri uri = new Uri(targetUrl);
                string root = $"{uri.Scheme}://{uri.Host}/";
                
                // ルートを一度踏んでおくとボット対策を抜けやすい場合がある
                Console.WriteLine($"[Crawler] ドメインの正規アクセスをシミュレート中: {root}");
                await page.GoToAsync(root, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded }, Timeout = 10000 });
                await Task.Delay(1500);
            }
            catch { /* ウォームアップの失敗は無視して本題へ進む */ }
        }

        private async Task EnsureBrowserAsync()
        {
            if (_browser != null && !_browser.IsClosed) return;

            if (_candidatePaths == null || _candidatePaths.Count == 0)
            {
                await InitializeAsync();
            }

            // すでに成功したパスがある場合はそれを優先
            if (!string.IsNullOrEmpty(_executablePath))
            {
                try 
                {
                    _browser = await LaunchBrowserAsync(_executablePath);
                    return;
                }
                catch 
                {
                    Console.WriteLine($"[Warning] 以前成功したブラウザ ({_executablePath}) の再起動に失敗しました。再検索します。");
                    _executablePath = null;
                }
            }

            // 候補パスを順番に試行
            var errors = new List<string>();
            foreach (var path in _candidatePaths)
            {
                try 
                {
                    Console.WriteLine($"[Crawler] ブラウザを起動中: {path}");
                    _browser = await LaunchBrowserAsync(path);
                    _executablePath = path;
                    return;
                }
                 catch (Exception ex)
                {
                    string msg = $"ブラウザ ({path}) の起動に失敗しました:\n{ex.ToString()}";
                    Console.WriteLine($"[Warning] {msg}");
                    errors.Add(msg);
                }
            }

            Console.WriteLine("[Error] 利用可能なブラウザがすべて起動に失敗しました。");
            string detailedError = "有効なブラウザを起動できませんでした。\n";
            detailedError += "試行したパス:\n" + string.Join("\n", _candidatePaths.Select(p => "- " + p)) + "\n\n";
            detailedError += "エラー詳細:\n" + string.Join("\n", errors);
            detailedError += "\n\n解決方法: settings.json を作成し、\"BrowserPath\" に有効な chrome.exe または msedge.exe のパスを指定してください。";
            
            throw new Exception(detailedError);
        }

        public bool Headless { get; set; } = true;

        public async Task<IPage> NewPageAsync()
        {
            await EnsureBrowserAsync();
            return await CreateStealthPageAsync();
        }

        private async Task<IBrowser> LaunchBrowserAsync(string path)
        {
            var userDataDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "browser_data");
            try
            {
                if (!Directory.Exists(userDataDir)) Directory.CreateDirectory(userDataDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] データディレクトリの作成に失敗しました: {ex.Message}");
            }

            return await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = this.Headless,
                ExecutablePath = path,
                Pipe = true, // WebSocket の制限を回避
                UserDataDir = userDataDir, // デフォルトの一時フォルダ制限を回避
                Args = new[] { 
                    "--no-sandbox", 
                    "--disable-setuid-sandbox",
                    "--disable-dev-shm-usage",
                    "--disable-infobars",
                    "--window-position=0,0",
                    "--ignore-certifcate-errors",
                    "--ignore-certifcate-errors-spki-list",
                    "--disable-blink-features=AutomationControlled",
                    "--disable-gpu",
                    "--disable-software-rasterizer",
                    "--no-first-run",
                    "--no-default-browser-check",
                    "--disable-extensions",
                    "--password-store=basic",
                    "--use-mock-keychain"
                },
                DefaultViewport = (this.Headless ? new ViewPortOptions { Width = 1920, Height = 1080 } : null),
                IgnoreHTTPSErrors = true
            });
        }

        public async Task CloseAsync()
        {
            if (_browser != null)
            {
                Console.WriteLine("ブラウザを終了しています...");
                await _browser.DisposeAsync();
                _browser = null;
            }
        }
    }
}


