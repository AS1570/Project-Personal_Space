using System.IO;
using SQLite;
using WpfApp3.Models;

namespace WpfApp3.Services
{
    public class DatabaseService
    {
        private SQLiteConnection? _connection;
        private readonly string _dbPath;
        private bool _isConnected;

        public const string DefaultPassword = "admin123";
        public const string DefaultPasswordHint = "初始密码：admin123";

        public bool IsConnected => _isConnected;
        public string DbPath => _dbPath;

        public DatabaseService()
        {
            string baseDir = App.AppDirectory;
            string appDataDir = Path.Combine(baseDir, "appdata");
            if (!Directory.Exists(appDataDir)) Directory.CreateDirectory(appDataDir);
            _dbPath = Path.Combine(appDataDir, "data.db");
            _isConnected = false;
        }

        public void Initialize()
        {
            if (File.Exists(_dbPath))
            {
                System.Diagnostics.Debug.WriteLine($"[DB] 数据库已存在: {_dbPath}");
                return;
            }

            try
            {
                var connString = new SQLiteConnectionString(_dbPath, storeDateTimeAsTicks: false, key: DefaultPassword);
                var db = new SQLiteConnection(connString);

                CreateAllTables(db);
                SavePasswordHint(db);

                db.Close();
                db.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB] 数据库创建失败: {ex}");
                throw;
            }
        }

        private static void CreateAllTables(SQLiteConnection db)
        {
            db.CreateTable<DiaryEntry>();
            db.CreateTable<EncryptedSetting>();
        }

        private static void SavePasswordHint(SQLiteConnection db)
        {
            var existing = db.Find<EncryptedSetting>("PasswordHint");
            if (existing == null)
            {
                db.Insert(new EncryptedSetting { Key = "PasswordHint", Value = DefaultPasswordHint });
            }

            string baseDir = App.AppDirectory;
            string defaultFileLocation = Path.Combine(baseDir, "ProgramFile");
            var fileLoc = db.Find<EncryptedSetting>("filelocation");
            if (fileLoc == null)
            {
                db.Insert(new EncryptedSetting { Key = "filelocation", Value = defaultFileLocation });
            }
        }

        public bool TryConnect(string password)
        {
            if (!File.Exists(_dbPath)) return false;

            Disconnect();

            try
            {
                var options = new SQLiteConnectionString(_dbPath, storeDateTimeAsTicks: false, key: password);
                _connection = new SQLiteConnection(options);

                var tableCount = _connection.ExecuteScalar<int>("SELECT count(*) FROM sqlite_master");
                _isConnected = true;
                _currentPassword = password;
                return true;
            }
            catch (SQLiteException)
            {
                _connection?.Close();
                _connection?.Dispose();
                _connection = null;
                _isConnected = false;
                return false;
            }
            catch (Exception)
            {
                _connection?.Close();
                _connection?.Dispose();
                _connection = null;
                _isConnected = false;
                return false;
            }
        }

        public void Disconnect()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
            _isConnected = false;
        }

        public string? GetPasswordHint()
        {
            if (!_isConnected || _connection == null) return null;
            var setting = _connection.Find<EncryptedSetting>("PasswordHint");
            return setting?.Value;
        }

        private string? _currentPassword;

        public bool ChangePassword(string oldPassword, string newPassword)
        {
            if (!_isConnected || _connection == null) return false;
            if (string.IsNullOrEmpty(newPassword)) return false;

            try
            {
                var escaped = newPassword.Replace("'", "''");
                _connection.ExecuteScalar<string>($"PRAGMA rekey = '{escaped}'");
                _currentPassword = newPassword;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DB] 修改密码失败: {ex.Message}");
                return false;
            }
        }

        public void SetSetting(string key, string value)
        {
            if (!_isConnected || _connection == null) return;
            var existing = _connection.Find<EncryptedSetting>(key);
            if (existing != null)
            {
                existing.Value = value;
                _connection.Update(existing);
            }
            else
            {
                _connection.Insert(new EncryptedSetting { Key = key, Value = value });
            }
        }

        public string? GetSetting(string key)
        {
            if (!_isConnected || _connection == null) return null;
            var setting = _connection.Find<EncryptedSetting>(key);
            return setting?.Value;
        }

        public string? GetFileLocation()
        {
            var val = GetSetting("filelocation");
            if (string.IsNullOrEmpty(val)) return null;
            return val;
        }

        public SQLiteConnection? Connection => _connection;
    }
}
