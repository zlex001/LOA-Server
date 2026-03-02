using System.Net;
using Newtonsoft.Json;

namespace Logic.Administrator
{
    public class VersionHistory
    {
        private static VersionHistory instance;
        public static VersionHistory Instance { get { if (instance == null) { instance = new VersionHistory(); } return instance; } }

        private readonly string _filePath = System.IO.Path.Combine(Utils.Paths.Config, "version_history.json");
        private List<VersionRecord> _versions = new List<VersionRecord>();
        private int _nextId = 1;

        public class VersionRecord
        {
            public string id { get; set; }
            public string date { get; set; }
            public string version { get; set; }
            public string description { get; set; }
            public VersionDetails details { get; set; }
        }

        public class VersionDetails
        {
            public string releaseDate { get; set; }
            public string developer { get; set; }
            public string svnRevision { get; set; }
            public List<string> changes { get; set; }
            public List<string> bugFixes { get; set; }
        }

        public VersionHistory()
        {
            LoadFromFile();
        }

        private void LoadFromFile()
        {
            try
            {
                if (System.IO.File.Exists(_filePath))
                {
                    var json = System.IO.File.ReadAllText(_filePath);
                    _versions = JsonConvert.DeserializeObject<List<VersionRecord>>(json) ?? new List<VersionRecord>();
                    
                    if (_versions.Count > 0)
                    {
                        var maxId = _versions.Max(v => int.TryParse(v.id, out var id) ? id : 0);
                        _nextId = maxId + 1;
                    }
                }
                else
                {
                    _versions = new List<VersionRecord>();
                }
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("VERSION", $"Failed to load version history: {ex.Message}");
                _versions = new List<VersionRecord>();
            }
        }

        private void SaveToFile()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_versions, Formatting.Indented);
                System.IO.File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("VERSION", $"Failed to save version history: {ex.Message}");
            }
        }

        public async void OnGetVersions(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                var queryString = context.Request.Url.Query;
                var queryParameters = System.Web.HttpUtility.ParseQueryString(queryString);

                var startDateParam = queryParameters["startDate"];
                var endDateParam = queryParameters["endDate"];

                var result = _versions.AsEnumerable();

                if (!string.IsNullOrEmpty(startDateParam) && !string.IsNullOrEmpty(endDateParam))
                {
                    if (DateTime.TryParse(startDateParam, out var startDate) && 
                        DateTime.TryParse(endDateParam, out var endDate))
                    {
                        result = result.Where(v => 
                        {
                            if (DateTime.TryParse(v.date, out var versionDate))
                            {
                                return versionDate >= startDate && versionDate <= endDate;
                            }
                            return false;
                        });
                    }
                }

                var sortedResult = result.OrderBy(v => v.date).ToList();
                await Net.Http.Instance.SendJson(context.Response, sortedResult);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("VERSION", $"OnGetVersions failed: {ex.Message}");
                await Net.Http.Instance.SendJson(context.Response, new List<VersionRecord>());
            }
        }

        public async void OnCreateVersion(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                string jsonData;
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    jsonData = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(jsonData))
                {
                    await Net.Http.Instance.SendError(context.Response, "Request body is empty", 400);
                    return;
                }

                var newVersion = JsonConvert.DeserializeObject<VersionRecord>(jsonData);
                newVersion.id = _nextId.ToString();
                _nextId++;

                _versions.Add(newVersion);
                SaveToFile();

                var result = new { code = 200, message = "Created successfully", data = newVersion };
                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("VERSION", $"OnCreateVersion failed: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }

        public async void OnUpdateVersion(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                var path = context.Request.Url.AbsolutePath;
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var id = segments.Last();

                string jsonData;
                using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                {
                    jsonData = await reader.ReadToEndAsync();
                }

                if (string.IsNullOrWhiteSpace(jsonData))
                {
                    await Net.Http.Instance.SendError(context.Response, "Request body is empty", 400);
                    return;
                }

                var existingVersion = _versions.FirstOrDefault(v => v.id == id);
                if (existingVersion == null)
                {
                    await Net.Http.Instance.SendError(context.Response, "Version not found", 404);
                    return;
                }

                var updatedVersion = JsonConvert.DeserializeObject<VersionRecord>(jsonData);
                updatedVersion.id = id;

                _versions.Remove(existingVersion);
                _versions.Add(updatedVersion);
                SaveToFile();

                var result = new { code = 200, message = "Updated successfully", data = updatedVersion };
                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("VERSION", $"OnUpdateVersion failed: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }

        public async void OnDeleteVersion(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                var path = context.Request.Url.AbsolutePath;
                var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                var id = segments.Last();

                var existingVersion = _versions.FirstOrDefault(v => v.id == id);
                if (existingVersion == null)
                {
                    await Net.Http.Instance.SendError(context.Response, "Version not found", 404);
                    return;
                }

                _versions.Remove(existingVersion);
                SaveToFile();

                var result = new { code = 200, message = "Deleted successfully" };
                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("VERSION", $"OnDeleteVersion failed: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }
    }
}

