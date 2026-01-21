using System;
using System.IO;

namespace Utils
{
    public static class Paths
    {
        public static string Documents => System.IO.Path.Combine(GetParent(AppDomain.CurrentDomain.BaseDirectory, 2), "Documents");
        public static string Design => System.IO.Path.Combine(Library, "设计");
        public static string DesignData => System.IO.Path.Combine(Library, "Design");
        public static string Config => System.IO.Path.Combine(Library, "Config");
        public static string Logs => System.IO.Path.Combine(Library, "Logs");
        public static string CrashLogs => System.IO.Path.Combine(Logs, "Crash");
        public static string ServerLogs => System.IO.Path.Combine(Logs, "Server");

        public static string Library => GetParent(AppDomain.CurrentDomain.BaseDirectory, 1);

        public static string GetParent(string path, int level)
        {
            if (level < 1) throw new ArgumentException("层级必须大于等于1");

            DirectoryInfo current = new DirectoryInfo(path);
            for (int i = 0; i < level; i++)
            {
                if (current.Parent == null)
                {
                    throw new InvalidOperationException($"无法获取第 {level} 级父目录，路径层级不足。");
                }
                current = current.Parent;
            }

            return current.FullName;
        }
    }
}
