using System.IO;

namespace WpfApp3.Services
{
    public class FileManagerService
    {
        private readonly string _defaultProgramFileDir;

        public string ProgramFileDir
        {
            get
            {
                if (App.Database.IsConnected)
                {
                    var dbPath = App.Database.GetFileLocation();
                    if (!string.IsNullOrEmpty(dbPath) && Directory.Exists(dbPath))
                        return dbPath;
                }
                return _defaultProgramFileDir;
            }
        }

        public string DefaultProgramFileDir => _defaultProgramFileDir;

        public FileManagerService()
        {
            string baseDir = App.AppDirectory;
            _defaultProgramFileDir = Path.Combine(baseDir, "ProgramFile");
            if (!Directory.Exists(_defaultProgramFileDir)) Directory.CreateDirectory(_defaultProgramFileDir);
        }

        public List<FileRecordInfo> GetAllFiles()
        {
            var results = new List<FileRecordInfo>();
            if (!Directory.Exists(ProgramFileDir)) return results;

            foreach (var filePath in Directory.GetFiles(ProgramFileDir))
            {
                try
                {
                    var fi = new FileInfo(filePath);
                    results.Add(new FileRecordInfo
                    {
                        FullPath = filePath,
                        FileName = Path.GetFileNameWithoutExtension(filePath),
                        Extension = fi.Extension.ToLowerInvariant(),
                        FileSize = fi.Length,
                        SaveTime = fi.LastWriteTime
                    });
                }
                catch { }
            }
            return results;
        }

        public FileRecordInfo? GetFileByPath(string fullPath)
        {
            if (!File.Exists(fullPath)) return null;
            var fi = new FileInfo(fullPath);
            return new FileRecordInfo
            {
                FullPath = fullPath,
                FileName = Path.GetFileNameWithoutExtension(fullPath),
                Extension = fi.Extension.ToLowerInvariant(),
                FileSize = fi.Length,
                SaveTime = fi.LastWriteTime
            };
        }

        public void ImportFile(string sourcePath)
        {
            var fi = new FileInfo(sourcePath);
            string destName = fi.Name;
            string destPath = Path.Combine(ProgramFileDir, destName);

            int counter = 1;
            string baseName = Path.GetFileNameWithoutExtension(sourcePath);
            string ext = fi.Extension;
            while (File.Exists(destPath))
            {
                destName = string.Format("{0}_{1}{2}", baseName, counter, ext);
                destPath = Path.Combine(ProgramFileDir, destName);
                counter++;
            }

            File.Copy(sourcePath, destPath, overwrite: false);
        }

        public void DeleteFile(string fullPath)
        {
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }

        public void RenameFile(string fullPath, string newName)
        {
            var dir = Path.GetDirectoryName(fullPath);
            var ext = Path.GetExtension(fullPath);
            var newPath = Path.Combine(dir ?? "", newName + ext);
            if (File.Exists(newPath)) throw new IOException("同名文件已存在");
            File.Move(fullPath, newPath);
        }

        public void ChangeExtension(string fullPath, string newExtension)
        {
            if (!newExtension.StartsWith(".")) newExtension = "." + newExtension;
            var dir = Path.GetDirectoryName(fullPath);
            var name = Path.GetFileNameWithoutExtension(fullPath);
            var newPath = Path.Combine(dir ?? "", name + newExtension);
            if (File.Exists(newPath)) throw new IOException("同名文件已存在");
            File.Move(fullPath, newPath);
        }

        public byte[]? ReadAllBytes(string fullPath)
        {
            return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
        }

        public void SaveFile(string fullPath, byte[] data)
        {
            File.WriteAllBytes(fullPath, data);
        }

        public void ExportFile(string sourcePath, string exportPath)
        {
            if (File.Exists(sourcePath)) File.Copy(sourcePath, exportPath, overwrite: true);
        }

        public void OpenInExplorer(string fullPath)
        {
            if (File.Exists(fullPath))
            {
                System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + fullPath + "\"");
            }
            else if (Directory.Exists(ProgramFileDir))
            {
                System.Diagnostics.Process.Start("explorer.exe", ProgramFileDir);
            }
        }

        public static int GetFileTypeByExtension(string extension)
        {
            var ext = extension.ToLowerInvariant().TrimStart('.');
            return ext switch
            {
                "txt" or "md" or "csv" or "log" or "json" or "xml"
                    or "yaml" or "yml" or "ini" or "cfg" or "conf"
                    or "cs" or "xaml" or "html" or "css" or "js"
                    or "py" or "java" or "cpp" or "h" or "sql"
                    or "bat" or "sh" or "ps1" or "toml" => 0,
                "pdf" or "doc" or "docx" or "xls" or "xlsx" or "ppt" or "pptx" => 1,
                "jpg" or "jpeg" or "heif" or "heic" or "png" or "gif"
                    or "bmp" or "webp" or "ico" => 2,
                "mp4" or "avi" or "mkv" or "mov" or "wmv"
                    or "flv" or "webm" or "m4v" or "mpg" or "mpeg" => 3,
                "mp3" or "wav" or "flac" or "aac"
                    or "ogg" or "wma" or "m4a" or "opus" => 4,
                _ => 5
            };
        }

        public static string GetFileTypeName(int fileType)
        {
            return fileType switch
            {
                0 => "文本", 1 => "文档", 2 => "图片", 3 => "视频", 4 => "音频",
                _ => "其他"
            };
        }

        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return string.Format("{0:0.##} {1}", size, sizes[order]);
        }

        public bool ChangeFolder(string newPath)
        {
            if (string.IsNullOrEmpty(newPath)) return false;
            if (!Directory.Exists(newPath))
            {
                try { Directory.CreateDirectory(newPath); }
                catch { return false; }
            }
            string oldDir = ProgramFileDir;
            App.Database.SetSetting("filelocation", newPath);
            return true;
        }

        public (bool Success, List<string> Errors) MigrateFiles(string oldDir, string newDir, bool deleteOldDir = true)
        {
            var errors = new List<string>();

            if (!Directory.Exists(oldDir))
            {
                return (false, new List<string> { "原文件夹不存在" });
            }

            if (!Directory.Exists(newDir))
            {
                try { Directory.CreateDirectory(newDir); }
                catch (Exception ex)
                {
                    errors.Add($"无法创建新文件夹: {ex.Message}");
                    return (false, errors);
                }
            }

            var files = Directory.GetFiles(oldDir);
            foreach (var file in files)
            {
                try
                {
                    string fileName = Path.GetFileName(file);
                    string destPath = Path.Combine(newDir, fileName);
                    int counter = 1;
                    while (File.Exists(destPath))
                    {
                        string nameOnly = Path.GetFileNameWithoutExtension(fileName);
                        string ext = Path.GetExtension(fileName);
                        destPath = Path.Combine(newDir, $"{nameOnly}_{counter}{ext}");
                        counter++;
                    }
                    File.Copy(file, destPath, overwrite: false);
                }
                catch (Exception ex)
                {
                    errors.Add($"复制失败 [{Path.GetFileName(file)}]: {ex.Message}");
                }
            }

            var subDirs = Directory.GetDirectories(oldDir);
            foreach (var subDir in subDirs)
            {
                try
                {
                    string dirName = Path.GetFileName(subDir);
                    string destSubDir = Path.Combine(newDir, dirName);
                    CopyDirectoryRecursive(subDir, destSubDir, errors);
                }
                catch (Exception ex)
                {
                    errors.Add($"复制子目录失败 [{Path.GetFileName(subDir)}]: {ex.Message}");
                }
            }

            if (deleteOldDir && errors.Count == 0)
            {
                try { Directory.Delete(oldDir, true); }
                catch (Exception ex)
                {
                    errors.Add($"文件已复制，但无法删除旧文件夹: {ex.Message}");
                }
            }

            return (errors.Count == 0, errors);
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destDir, List<string> errors)
        {
            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                try
                {
                    string fileName = Path.GetFileName(file);
                    string destPath = Path.Combine(destDir, fileName);
                    int counter = 1;
                    while (File.Exists(destPath))
                    {
                        string nameOnly = Path.GetFileNameWithoutExtension(fileName);
                        string ext = Path.GetExtension(fileName);
                        destPath = Path.Combine(destDir, $"{nameOnly}_{counter}{ext}");
                        counter++;
                    }
                    File.Copy(file, destPath);
                }
                catch (Exception ex)
                {
                    errors.Add($"复制失败 [{Path.GetFileName(file)}]: {ex.Message}");
                }
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                try
                {
                    string dirName = Path.GetFileName(subDir);
                    CopyDirectoryRecursive(subDir, Path.Combine(destDir, dirName), errors);
                }
                catch (Exception ex)
                {
                    errors.Add($"复制子目录失败 [{Path.GetFileName(subDir)}]: {ex.Message}");
                }
            }
        }
    }

    public class FileRecordInfo
    {
        public string FullPath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime SaveTime { get; set; }

        public string DisplayName => FileName + Extension;
    }
}
