using System;
using System.Windows;
using System.Threading.Tasks;
using RssGenerator.Services;

namespace RssGenerator
{
    public partial class App : Application
    {
        private async void OnStartup(object sender, StartupEventArgs e)
        {
            // コマンドライン引数がある場合は、旧来の CLI 処理または特定のバッチ処理を実行
            if (e.Args.Length > 0)
            {
                // NOTE: ここで旧 Program.Main のロジックを呼び出すことも可能だが、
                // 今回は GUI への移行を主眼に置くため、MainWindow を表示。
                // 特殊なフラグ（--batch など）がある場合のみヘッドレス実行する例。
                // await RunCommandLineAsync(e.Args);
            }

            // GUI のメインウィンドウを起動
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        private async Task RunCommandLineAsync(string[] args)
        {
            // 必要に応じて CLI ロジックをここに移植または Program クラスを呼び出す
        }
    }
}
