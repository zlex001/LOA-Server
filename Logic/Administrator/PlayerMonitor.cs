using System.Net;
using System.Linq;
using Newtonsoft.Json;

namespace Logic.Administrator
{
    public class PlayerMonitor
    {
        private static PlayerMonitor instance;
        public static PlayerMonitor Instance { get { if (instance == null) { instance = new PlayerMonitor(); } return instance; } }

        public async void OnGetPlayerList(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                Utils.Debug.Log.Info("ADMIN", "[PlayerList] Request received");
                var query = context.Request.QueryString;
                int page = int.TryParse(query["page"], out var p) ? p : 1;
                int pageSize = int.TryParse(query["pageSize"], out var ps) ? ps : 50;
                string sortBy = query["sortBy"] ?? "level";
                string order = query["order"] ?? "desc";
                string search = query["search"];
                string status = query["status"] ?? "all";

                pageSize = Math.Min(pageSize, 100);
                page = Math.Max(page, 1);

                var allPlayers = global::Data.Database.Agent.Instance.Content.Gets<global::Data.Database.Player>().ToList();
                var onlinePlayerIds = global::Data.Agent.Instance.Content.Gets<global::Data.Player>()
                    .Select(p => p.Id)
                    .ToHashSet();

                var playerList = allPlayers.Select(dbPlayer =>
                {
                    bool isOnline = onlinePlayerIds.Contains(dbPlayer.Id);
                    var onlinePlayer = isOnline ? global::Data.Agent.Instance.Content.Get<global::Data.Player>(p => p.Id == dbPlayer.Id) : null;

                    return new
                    {
                        id = dbPlayer.Id,
                        name = dbPlayer.GetText("Name"),
                        level = dbPlayer.GetRecord("Level"),
                        exp = dbPlayer.GetRecord("Exp"),
                        isOnline = isOnline,
                        signInTime = dbPlayer.GetTime("SignIn"),
                        signOutTime = dbPlayer.GetTime("SignOut"),
                        mapId = onlinePlayer?.Map?.Config?.Id ?? 0,
                        mapName = onlinePlayer?.Map?.Config?.Name.ToString() ?? string.Empty,
                        gem = dbPlayer.GetRecord("Gem"),
                        opvpScore = dbPlayer.GetRecord("OpvpScore"),
                        cumulativeGem = dbPlayer.GetRecord("CumulativeGem"),
                        registerTime = dbPlayer.GetTime("Register")
                    };
                }).ToList();

                if (status == "online")
                {
                    playerList = playerList.Where(p => p.isOnline).ToList();
                }
                else if (status == "offline")
                {
                    playerList = playerList.Where(p => !p.isOnline).ToList();
                }

                if (!string.IsNullOrEmpty(search))
                {
                    playerList = playerList.Where(p =>
                        p.id.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                        p.name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                playerList = sortBy.ToLower() switch
                {
                    "level" => order == "asc" ? playerList.OrderBy(p => p.level).ToList() : playerList.OrderByDescending(p => p.level).ToList(),
                    "signintime" => order == "asc" ? playerList.OrderBy(p => p.signInTime).ToList() : playerList.OrderByDescending(p => p.signInTime).ToList(),
                    "gem" => order == "asc" ? playerList.OrderBy(p => p.gem).ToList() : playerList.OrderByDescending(p => p.gem).ToList(),
                    "opvpscore" => order == "asc" ? playerList.OrderBy(p => p.opvpScore).ToList() : playerList.OrderByDescending(p => p.opvpScore).ToList(),
                    "cumulativegem" => order == "asc" ? playerList.OrderBy(p => p.cumulativeGem).ToList() : playerList.OrderByDescending(p => p.cumulativeGem).ToList(),
                    _ => playerList.OrderByDescending(p => p.level).ToList()
                };

                int total = playerList.Count();
                var pagedPlayers = playerList
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = new
                {
                    total = total,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling(total / (double)pageSize),
                    players = pagedPlayers
                };

                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[PlayerList] Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }

        public async void OnGetPlayerDetails(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                var path = context.Request.Url.LocalPath;
                
                if (path.EndsWith("/history"))
                {
                    return;
                }
                
                var playerId = path.Replace("/api/administrator/players/", "").Replace("/details", "").TrimEnd('/');
                Utils.Debug.Log.Info("ADMIN", $"[PlayerDetails] Request received for player: {playerId}");

                if (string.IsNullOrEmpty(playerId))
                {
                    await Net.Http.Instance.SendError(context.Response, "Player ID is required", 400);
                    return;
                }

                var onlinePlayer = global::Data.Agent.Instance.Content.Get<global::Data.Player>(p => p.Id == playerId);
                var dbPlayer = global::Data.Database.Agent.Instance.Content.Get<global::Data.Database.Player>(p => p.Id == playerId);

                if (dbPlayer == null)
                {
                    await Net.Http.Instance.SendError(context.Response, "Player not found", 404);
                    return;
                }

                bool isOnline = onlinePlayer != null;
                
                var basic = new
                {
                    id = dbPlayer.Id,
                    name = dbPlayer.GetText("Name"),
                    level = dbPlayer.GetRecord("Level"),
                    exp = dbPlayer.GetRecord("Exp"),
                    age = isOnline ? onlinePlayer.Age : dbPlayer.GetRecord("Age"),
                    gender = dbPlayer.grade.ContainsKey(global::Data.Life.Attributes.Con) ? "Unknown" : "Unknown"
                };

                var onlineParts = isOnline ? onlinePlayer.Content.Gets<global::Data.Part>().ToList() : new List<global::Data.Part>();
                var attributes = new
                {
                    hp = isOnline ? onlineParts.Sum(p => p.Hp) : dbPlayer.parts.Sum(p => p.Hp),
                    maxHp = isOnline ? onlineParts.Sum(p => p.MaxHp) : dbPlayer.parts.Sum(p => p.MaxHp),
                    mp = isOnline ? onlinePlayer.Mp : 0,
                    maxMp = isOnline ? (int)onlinePlayer.MaxMp : 0,
                    lp = isOnline ? onlinePlayer.Lp : 0.0,
                    atk = isOnline ? (int)onlinePlayer.Atk : dbPlayer.grade.GetValueOrDefault(global::Data.Life.Attributes.Atk, 0),
                    def = isOnline ? (int)onlinePlayer.Def : dbPlayer.grade.GetValueOrDefault(global::Data.Life.Attributes.Def, 0),
                    agi = isOnline ? (int)onlinePlayer.Agi : dbPlayer.grade.GetValueOrDefault(global::Data.Life.Attributes.Agi, 0),
                    ine = isOnline ? (int)onlinePlayer.Ine : dbPlayer.grade.GetValueOrDefault(global::Data.Life.Attributes.Ine, 0),
                    con = isOnline ? (int)onlinePlayer.Con : dbPlayer.grade.GetValueOrDefault(global::Data.Life.Attributes.Con, 0)
                };

                var currency = new
                {
                    gem = dbPlayer.GetRecord("Gem"),
                    credit = dbPlayer.GetRecord("Credit"),
                    cumulativeGem = dbPlayer.GetRecord("CumulativeGem"),
                    opvpScore = dbPlayer.GetRecord("OpvpScore"),
                    opvpRank = isOnline ? onlinePlayer.OpvpRank : 0,
                    towerLevel = dbPlayer.GetRecord("Tower"),
                    profitMoney = dbPlayer.GetRecord("ProfitMoney"),
                    profitToken = dbPlayer.GetRecord("ProfitToken")
                };

                var skills = dbPlayer.skills.Select(skill => new
                {
                    id = skill.Id,
                    level = skill.Level,
                    exp = skill.Exp
                }).ToList();

                var equipments = dbPlayer.equipments.Select(item => new
                {
                    id = item.Id,
                    count = item.Count,
                    properties = item.Properties
                }).ToList();

                var statusInfo = new
                {
                    state = isOnline ? onlinePlayer.State.CurrentKey.ToString() : "Offline",
                    isOnline = isOnline,
                    mapId = isOnline ? (onlinePlayer.Map?.Config?.Id ?? 0) : 0,
                    mapName = isOnline ? (onlinePlayer.Map?.Config?.Name.ToString() ?? string.Empty) : string.Empty,
                    position = isOnline && onlinePlayer.Map != null ? dbPlayer.pos : new int[0]
                };

                var timeline = new
                {
                    registerTime = dbPlayer.GetTime("Register"),
                    signInTime = dbPlayer.GetTime("SignIn"),
                    signOutTime = dbPlayer.GetTime("SignOut"),
                    dinerTime = dbPlayer.GetTime("Diner"),
                    eatTime = dbPlayer.GetTime("Eat")
                };

                var result = new
                {
                    basic = basic,
                    attributes = attributes,
                    currency = currency,
                    skills = skills,
                    equipments = equipments,
                    status = statusInfo,
                    timeline = timeline
                };

                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[PlayerDetails] Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }

        public async void OnGetPlayerHistory(params object[] args)
        {
            var context = (HttpListenerContext)args[0];
            try
            {
                var path = context.Request.Url.LocalPath;
                
                if (!path.EndsWith("/history"))
                {
                    return;
                }
                
                var playerId = path.Replace("/api/administrator/players/", "").Replace("/history", "").TrimEnd('/');
                Utils.Debug.Log.Info("ADMIN", $"[PlayerHistory] Request received for player: {playerId}");

                if (string.IsNullOrEmpty(playerId))
                {
                    await Net.Http.Instance.SendError(context.Response, "Player ID is required", 400);
                    return;
                }

                var query = context.Request.QueryString;
                DateTime startDate = DateTime.Now.AddDays(-30);
                DateTime endDate = DateTime.Now;

                if (!string.IsNullOrEmpty(query["startDate"]))
                {
                    if (!DateTime.TryParse(query["startDate"], out startDate))
                    {
                        await Net.Http.Instance.SendError(context.Response, "Invalid startDate format", 400);
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(query["endDate"]))
                {
                    if (!DateTime.TryParse(query["endDate"], out endDate))
                    {
                        await Net.Http.Instance.SendError(context.Response, "Invalid endDate format", 400);
                        return;
                    }
                }

                if ((endDate - startDate).TotalDays > 90)
                {
                    await Net.Http.Instance.SendError(context.Response, "Time range cannot exceed 90 days", 400);
                    return;
                }

                var historyData = global::Data.Database.Agent.Instance.QueryPlayerHistory(playerId, startDate, endDate);

                var result = new
                {
                    playerId = playerId,
                    startDate = startDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    endDate = endDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    total = historyData.Count,
                    data = historyData
                };

                await Net.Http.Instance.SendJson(context.Response, result);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("ADMIN", $"[PlayerHistory] Error: {ex.Message}");
                await Net.Http.Instance.SendError(context.Response, ex.Message, 500);
            }
        }
    }
}

