using System.Net;
using Newtonsoft.Json;

namespace Domain.Administrator
{
    public class DebugControl
    {
        private static DebugControl instance;
        public static DebugControl Instance { get { if (instance == null) { instance = new DebugControl(); } return instance; } }



        public async void OnGetDebugLogs(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                var query = context.Request.QueryString;
                string category = query["category"];
                string keyword = query["keyword"];
                int limit = int.TryParse(query["limit"], out var l) ? l : 100;
                DateTime? startTime = DateTime.TryParse(query["startTime"], out var st) ? st : null;
                DateTime? endTime = DateTime.TryParse(query["endTime"], out var et) ? et : null;
                string playerId = query["playerId"];

                var logs = Utils.Debug.Log.GetLogs(category, keyword, limit * 2, startTime, endTime);

                if (!string.IsNullOrEmpty(playerId))
                {
                    logs = logs.Where(log =>
                    {
                        if (log.Details == null) return false;
                        var detailsJson = JsonConvert.SerializeObject(log.Details);
                        return detailsJson.Contains(playerId);
                    }).ToList();
                }

                logs = logs.Take(limit).ToList();

                var result = new
                {
                    logs = logs.Select(log => new
                    {
                        time = log.Time.ToString("o"),
                        category = log.Category,
                        level = log.Level.ToString().ToLower(),
                        message = log.Message,
                        details = log.Details
                    }),
                    total = Utils.Debug.Log.GetTotalCount(),
                    hasMore = logs.Count >= limit
                };

                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[DebugControl] GetLogs Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }


        public async void OnGetLogFiles(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                var files = Utils.Debug.Log.GetLogFiles()
                    .Select(f => new
                    {
                        name = f.Name,
                        path = f.FullName,
                        size = FormatFileSize(f.Length),
                        sizeBytes = f.Length,
                        lastModified = f.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                    })
                    .ToList();

                var result = new
                {
                    success = true,
                    files
                };

                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[DebugControl] GetLogFiles Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }

        public async void OnGetFileLogContent(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                var query = context.Request.QueryString;
                var filePath = query["path"];

                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    await Net.Http.Instance.SendError(context.Response, "文件不存在", 404);
                    return;
                }

                var limitStr = query["limit"];
                var limit = int.TryParse(limitStr, out var l) ? l : 1000;

                List<string> lines;
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8))
                {
                    var allLines = new List<string>();
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        allLines.Add(line);
                    }
                    
                    lines = allLines
                        .Skip(Math.Max(0, allLines.Count - limit))
                        .ToList();
                }

                var result = new
                {
                    success = true,
                    content = string.Join("\n", lines),
                    lineCount = lines.Count
                };

                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[DebugControl] GetFileLogContent Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}




