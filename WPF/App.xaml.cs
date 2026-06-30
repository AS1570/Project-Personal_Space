using System.IO;
using System.Windows;
using SQLitePCL;
using WpfApp3.Services;

namespace WpfApp3
{
    public partial class App : Application
    {
        public static readonly string AppDirectory = GetAppDirectory();

        public static DatabaseService Database { get; } = new DatabaseService();

        public static FileManagerService FileManager { get; } = new FileManagerService();

        private static string GetAppDirectory()
        {
            var processPath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(processPath))
            {
                var dir = Path.GetDirectoryName(processPath);
                if (!string.IsNullOrEmpty(dir))
                    return dir;
            }
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Batteries_V2.Init();
            raw.SetProvider(new SQLite3Provider_e_sqlcipher());

            Database.Initialize();

            ThemeService.Instance.Initialize();

            GenerateReadmeFile();
        }

        private static void GenerateReadmeFile()
        {
            var readmePath = Path.Combine(AppDirectory, "appdata", "软件说明.txt");
            if (File.Exists(readmePath)) return;

            try
            {
                var dir = Path.GetDirectoryName(readmePath);
                if (dir != null && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var exePath = Environment.ProcessPath ?? "";
                var appdataDir = Path.Combine(AppDirectory, "appdata");
                var programFileDir = Path.Combine(AppDirectory, "ProgramFile");
                var tempDir = AppDomain.CurrentDomain.BaseDirectory;

                var content = $@"Personal Space 软件初始文件位置说明
生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}

Personal Space.exe 位置：
    {exePath}

appdata 文件夹位置：
    {appdataDir}

ProgramFile 文件夹位置：
    {programFileDir}

Personal Space 临时文件夹位置：
    {tempDir}
";

                File.WriteAllText(readmePath, content, System.Text.Encoding.UTF8);
            }
            catch
            {
            }
        }
    }
}
