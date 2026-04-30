using System;
using System.IO;

namespace RssGenerator.Services
{
    public static class PathHelper
    {
        private static string _cachedRoot;

        /// <summary>
        /// プロジェクトのルートディレクトリ（targets.json または .git が存在する場所）を特定します。
        /// </summary>
        public static string FindProjectRoot()
        {
            if (_cachedRoot != null) return _cachedRoot;

            // 1. カレントディレクトリに targets.json があればそこがルート
            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "targets.json")))
            {
                _cachedRoot = Directory.GetCurrentDirectory();
                return _cachedRoot;
            }

            // 2. 実行ファイルから最大5段階遡って探す
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 5; i++)
            {
                if (File.Exists(Path.Combine(currentDir, "targets.json")) || 
                    Directory.Exists(Path.Combine(currentDir, ".git")))
                {
                    _cachedRoot = currentDir;
                    return _cachedRoot;
                }
                var parent = Directory.GetParent(currentDir);
                if (parent == null) break;
                currentDir = parent.FullName;
            }
            
            // 見つからなければカレントディレクトリを返す
            _cachedRoot = Directory.GetCurrentDirectory();
            return _cachedRoot;
        }

        /// <summary>
        /// 指定されたファイルが存在する最適なパスを解決します。
        /// 存在しない場合は、プロジェクトルートを優先的な作成先として返します。
        /// </summary>
        public static string ResolvePath(string fileName)
        {
            if (Path.IsPathRooted(fileName)) return fileName;

            // 1. すでに存在する場合の検索（ファイルまたはディレクトリ）
            if (File.Exists(fileName) || Directory.Exists(fileName)) return Path.GetFullPath(fileName);

            // b. プロジェクトルート
            string root = FindProjectRoot();
            string rootPath = Path.Combine(root, fileName);
            if (File.Exists(rootPath) || Directory.Exists(rootPath)) return rootPath;

            // c. 実行ファイルディレクトリ
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string sameDir = Path.Combine(baseDir, fileName);
            if (File.Exists(sameDir) || Directory.Exists(sameDir)) return sameDir;

            // 2. 存在しない場合（新規作成用）
            // 主要な設定・データファイル・フォルダはプロジェクトルートを優先
            if (fileName == "feed.sqlite" || fileName == "feed.xml" || fileName == "targets.json" || fileName == "settings.json" || fileName == "feeds")
            {
                return rootPath;
            }

            // その他は実行ファイルディレクトリをデフォルトとする
            return sameDir;
        }
    }
}
