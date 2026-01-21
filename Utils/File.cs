using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Utils
{
    public class FileBase
    {
        public FileBase(FileInfo fileInfo)
        {
            info = fileInfo;
            using (StreamReader streamReader = new StreamReader(fileInfo.FullName, Encoding.Default))
            {
                string line;
                StringBuilder temLine = new StringBuilder();
                while ((line = streamReader.ReadLine()) != null)
                {
                    temLine.Append(line);
                    if (line.Contains(@";"))
                    {
                        texts.Add(temLine.ToString());
                        temLine.Clear();
                    }
                }
            }
        }
        public FileInfo info;
        public List<string> texts = new List<string>();
    }

    public class FileManager
    {
        private static FileManager instance;
        private FileManager() { }
        public static FileManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new FileManager();
                }
                return instance;
            }
        }

        private List<FileBase> files = new List<FileBase>();

        public void SyncFilesLastModifiedTime(params string[] filePaths)
        {
            if (filePaths == null || filePaths.Length == 0)
            {
                return;
            }

            DateTime currentTime = DateTime.Now;

            foreach (string filePath in filePaths)
            {
                if (File.Exists(filePath))
                {
                    File.SetLastWriteTime(filePath, currentTime);
                }
            }
        }

        public void Load(string folder)
        {
            DirectoryInfo theFolder = new DirectoryInfo(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folder));
            DirectoryInfo[] dirInfo = theFolder.GetDirectories();
            FileInfo[] file = theFolder.GetFiles();

            foreach (DirectoryInfo directoryInfo in dirInfo)
            {
                Load(directoryInfo.FullName);
            }

            foreach (FileInfo fileInfo in file)
            {
                files.Add(new FileBase(fileInfo));
            }
        }

        public DateTime GetLastModifiedTime(string path)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(path);
                return fileInfo.LastWriteTime;
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }

        public void Save(string content, string path)
        {
            try
            {
                File.WriteAllText(path, content);
            }
            catch (Exception)
            {
                // 记录日志，抛出异常
            }
        }

        public void SaveWithDirectory(string content, string path)
        {
            try
            {
                var directory = System.IO.Path.GetDirectoryName(path);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(path, content);
            }
            catch (Exception)
            {
                // 记录日志，抛出异常
            }
        }

        public T Load<T>(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return default(T);
                }

                string dataString = File.ReadAllText(path);
                T data = (T)Convert.ChangeType(dataString, typeof(T));

                return data;
            }
            catch (Exception)
            {
                // 记录日志，抛出异常
                return default(T);
            }
        }

        public void DeleteFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public void DeleteFiles(string folderPath, bool recursive = false)
        {
            if (Directory.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath);
                string[] directories = Directory.GetDirectories(folderPath);

                foreach (string file in files)
                {
                    DeleteFile(file);
                }

                if (recursive)
                {
                    foreach (string directory in directories)
                    {
                        Directory.Delete(directory, true);
                    }
                }
                else
                {
                    foreach (string directory in directories)
                    {
                        Directory.Delete(directory, false);
                    }
                }
            }

        }

        public static TimeSpan CompareModificationTime(string filePath1, string filePath2)
        {
            DateTime file1LastModified = FileManager.Instance.GetLastModifiedTime(filePath1);
            DateTime file2LastModified = FileManager.Instance.GetLastModifiedTime(filePath2);
            bool isModified = file1LastModified.Second != file2LastModified.Second ||
                              file1LastModified.Minute != file2LastModified.Minute ||
                              file1LastModified.Hour != file2LastModified.Hour;

            if (isModified)
            {
                TimeSpan timeDifference = file1LastModified - file2LastModified;
                return timeDifference;
            }
            return TimeSpan.Zero;
        }
    }
}