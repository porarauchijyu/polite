using System;
using System.IO;

namespace RssGenerator.Services
{
    public static class PathHelper
    {
        /// <summary>
        /// 指定されたファイルが存在する最適なパスを解決します。
        /// 1. カレントディレクトリ
        /// 2. 実行ファイルのあるディレクトリ
        /// 3. プロジェクトルートとして推測される場所（binの一段上など）
        /// の順で検索し、見つからない場合は実行ファイルと同じディレクトリを返します。
        /// </summary>
        public static string ResolvePath(string fileName)
        {
            // 1. カレントディレクトリをチェック
            if (File.Exists(fileName)) return fileName;

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // 2. 実行ファイルのあるディレクトリをチェック
            string sameDir = Path.Combine(baseDir, fileName);
            if (File.Exists(sameDir)) return sameDir;

            // 3. 開発環境を考慮し、bin/Debug/net48 等から一段ずつ上を探す (最大3段階)
            string currentDir = baseDir;
            for (int i = 0; i < 3; i++)
            {
                var parent = Directory.GetParent(currentDir);
                if (parent == null) break;
                currentDir = parent.FullName;
                
                string parentDirFile = Path.Combine(currentDir, fileName);
                if (File.Exists(parentDirFile)) return parentDirFile;
            }

            // どこにもなければ、実行ファイルと同じ場所に作成されるようにする
            return Path.Combine(baseDir, fileName);
        }
    }
}
