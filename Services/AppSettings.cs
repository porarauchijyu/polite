using System;
using System.IO;
using Newtonsoft.Json;

namespace RssGenerator.Services
{
    public class AppSettings
    {
        public string BrowserPath { get; set; }
        public bool Headless { get; set; } = true;
        public int DefaultWaitMs { get; set; } = 3000;

        private static readonly string SettingsFileName = "settings.json";

        public static AppSettings Load()
        {
            try
            {
                string path = PathHelper.ResolvePath(SettingsFileName);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] 設定ファイルの読み込みに失敗しました: {ex.Message}");
            }

            var defaultSettings = new AppSettings();
            defaultSettings.Save(); // 初期ファイルを作成
            return defaultSettings;
        }

        public void Save()
        {
            try
            {
                string path = PathHelper.ResolvePath(SettingsFileName);
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] 設定ファイルの保存に失敗しました: {ex.Message}");
            }
        }
    }
}
