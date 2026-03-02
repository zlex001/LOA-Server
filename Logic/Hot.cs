using Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Logic
{
    public class Hot
    {
        #region Singleton

        private static Hot instance;
        public static Hot Instance { get { if (instance == null) { instance = new Hot(); } return instance; } }

        #endregion

        #region Initialization

        public void Init()
        {
            Net.Http.Instance.RegisterRoutes(8880,
                ("/api/hot/version", Net.Http.Event.HotVersion, OnVersion),
                ("/api/hot/check", Net.Http.Event.HotCheck, OnCheck)
            );
            Net.Http.Instance.RegisterPatternRoutes(8880, Net.Http.Event.Hot, OnHot, "/windows/", "/android/", "/ios/");
        }

        #endregion

        #region API Handlers

        private async void OnVersion(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            var language = Net.Http.Instance.GetLanguage(context);

            var response = new
            {
                version = string.Join(".", global::Data.Config.Agent.Instance.ClientVersion),
                texts = new
                {
                    checking = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.CheckingHotVersion, language),
                    check_failed = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.VersionCheckFailed, language)
                }
            };

            await Net.Http.Instance.SendJson(context.Response, response);
        }

        private async void OnCheck(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            var language = Net.Http.Instance.GetLanguage(context);

            string platform = context.Request.QueryString["platform"] ?? "android";

            string filesContent = ScanPlatformFiles(platform);

            var response = new
            {
                files = filesContent,
                texts = new
                {
                    comparing = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.ComparingResources, language),
                    resources_updated = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.NoUpdateNeeded, language),
                    start_downloading = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.StartDownloading, language),
                    download_complete = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.UpdateComplete, language),
                    download_failed = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.DownloadFailed, language),
                    loading_metadata = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.LoadingMetadata, language),
                    launching_game = Logic.Text.Agent.Instance.Get(global::Data.Text.Labels.LaunchingGame, language)
                }
            };

            await Net.Http.Instance.SendJson(context.Response, response);
        }

        #endregion

        #region 热更新处理
        private void OnHot(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            string path = context.Request.Url.LocalPath.TrimStart('/');

            System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    await Net.Http.Instance.SendFile(context.Request, context.Response, path);
                }
                catch (Exception)
                {
                    await Net.Http.Instance.SendError(context.Response, "服务器错误", 500);
                }
            });
        }

        #endregion

        #region File Scanning

        private string ScanPlatformFiles(string platform)
        {
            try
            {
                string baseDirectory = Utils.Paths.GetParent(AppDomain.CurrentDomain.BaseDirectory, 1);
                string platformPath = Path.Combine(baseDirectory, "Artifacts", platform);

                if (!Directory.Exists(platformPath))
                {
                    return string.Empty;
                }

                var fileEntries = new List<string>();
                ScanDirectory(platformPath, platformPath, fileEntries);

                return string.Join("\n", fileEntries);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("HOT", $"Failed to scan platform files: {ex.Message}");
                return string.Empty;
            }
        }

        private void ScanDirectory(string directory, string rootDirectory, List<string> fileEntries)
        {
            try
            {
                var files = Directory.GetFiles(directory);
                foreach (var file in files)
                {
                    string relativePath = Path.GetRelativePath(rootDirectory, file).Replace('\\', '/');
                    string md5 = CalculateMD5(file);
                    long size = new FileInfo(file).Length;
                    fileEntries.Add($"{relativePath}|{md5}|{size}");
                }

                var subdirectories = Directory.GetDirectories(directory);
                foreach (var subdirectory in subdirectories)
                {
                    ScanDirectory(subdirectory, rootDirectory, fileEntries);
                }
            }
            catch
            {
            }
        }

        private string CalculateMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        #endregion
    }
}

