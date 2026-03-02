using System.Linq;
using System;

namespace Logic.Authentication
{
    public static class Logout
    {
        public static void Do(global::Data.Player player)
        {
            if (player == null) return;

            player.SignOutTime = DateTime.Now;
            player.Database.grade = player.Grade;
            if (Logic.Quest.Maze.IsIn(player))
            {
                player.Database.pos = Logic.Quest.Maze.Get(player).Last.Database.teleport;
            }
            else if (Logic.Quest.Copy.IsIn(player))
            {
                // Save current map position (same coordinates exist in world)
                player.Database.pos = player.Map?.Database.pos;
            }
            else
            {
                player.Database.pos = player.Map == null ? default : player.Map.Database.pos;
            }
            player.Database.text["Name"] = player.data.Get<string>(global::Data.Player.Data.Name);
            player.Database.text["Master"] = player.Master;

            player.Database.parts.Clear();
            foreach (var part in player.Content.Gets<global::Data.Part>())
            {
                player.Database.parts.Add(new global::Data.Database.Part
                {
                    Type = part.Type,
                    Hp = part.Hp,
                    MaxHp = part.MaxHp,
                });
            }

            player.Database.record["Mp"] = player.Mp;
            player.Database.record["Lp"] = (int)player.Lp;
            player.Database.record["Gender"] = (int)player.data.Get<global::Data.Life.Genders>(global::Data.Life.Data.Gender);
            player.Database.record["Exp"] = player.Exp;
            player.Database.record["Level"] = player.Level;
            player.Database.record["Gem"] = player.Gem;
            player.Database.record["Credit"] = player.Credit;
            player.Database.record["OpvpScore"] = player.OpvpScore;
            player.Database.record["OpvpWinningStreak"] = player.data.Get<int>(global::Data.Player.Data.OpvpWinningStreak);
            player.Database.record["OpvpLosingStreak"] = player.data.Get<int>(global::Data.Player.Data.OpvpLosingStreak);
            player.Database.record["CumulativeGem"] = player.CumulativeGem;
            player.Database.record["Tower"] = (int)player.TowerLevel;
            player.Database.record["TokenFund"] = player.data.Get<bool>(global::Data.Player.Data.TokenFund) ? 1 : 0;
            player.Database.record["ProfitToken"] = player.ProfitToken;
            player.Database.record["ProfitMoney"] = player.ProfitMoney;
            player.Database.record["TaskCycle"] = player.TaskCycle;
            player.Database.record["ScreenUIAdaptation"] = player.ScreenUIAdaptation;
            player.Database.record["Age"] = (int)player.Age;

            player.Database.time["SignOut"] = player.SignOutTime.ToString();
            player.Database.time["SignIn"] = player.SignIn.ToString();
            player.Database.time["Register"] = player.RegisterTime.ToString();
            player.Database.time["Diner"] = player.DinerTime.ToString();
            player.Database.time["Eat"] = player.EatTime.ToString();
            player.Database.time["TokenMonthBasicLast"] = player.TokenMonthBasicLastTime.ToString();
            player.Database.time["TokenMonthPremiumLast"] = player.TokenMonthPremiumLastTime.ToString();
            player.Database.time["MonthlyExpLastCalc"] = player.MonthlyExpLastCalcTime.ToString();
            player.Database.time["RankReward"] = player.RankRewardTime.ToString();
            player.Database.time["HostelTime"] = player.HostelTime.ToString();

            try
            {
                var skillsList = player.Content.Gets<global::Data.Skill>().ToList();
                var databaseSkills = new List<global::Data.Database.Skill>();
                foreach (var skill in skillsList)
                {
                    try
                    {
                        var dbSkill = new global::Data.Database.Skill(skill);
                        databaseSkills.Add(dbSkill);
                    }
                    catch (Exception ex)
                    {
                        Utils.Debug.Log.Error("LOGOUT", $"技能转换失败 - Player: {player.Id}, Skill.Config.Id: {skill.Config.Id}, Error: {ex.Message}");
                    }
                }
                player.Database.skills = databaseSkills;
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("LOGOUT", $"Skills保存失败 - Player: {player.Id}, Error: {ex.Message}");
                player.Database.skills = new List<global::Data.Database.Skill>();
            }

            player.Database.payments = player.Content.Gets<global::Data.Payment>().Select(p => new global::Data.Database.Payment(p)).ToList();
            player.Database.warehouses = player.Content.Gets<global::Data.Warehouse>().Select(warehouses => new global::Data.Database.Warehouse(warehouses)).ToList();

            try
            {
                var equipments = player.ConvertEquipmentsToData();
                player.Database.equipments = equipments;
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("LOGOUT", $"Equipments保存失败 - Player: {player.Id}, Error: {ex.Message}");
            }

            player.Database.activitys = player.Activitys;
            player.Database.signs = player.Content.Gets<global::Data.Quest>().Select(sign => sign.Config.Id).Distinct().ToList();

            try
            {
                global::Data.Database.Agent.Instance.Save(global::Data.Config.MySQL.ConnectionString, player.Database);
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("LOGOUT", $"数据库保存失败 - Player: {player.Id}, Error: {ex.Message}");
            }

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    var parts = player.Content.Gets<global::Data.Part>().ToList();
                    var hp = parts.Sum(p => p.Hp);
                    var maxHp = parts.Sum(p => p.MaxHp);
                    var mapId = player.Map?.Config?.Id ?? 0;
                    
                    global::Data.Database.Agent.Instance.SavePlayerHistory(
                        player.Id,
                        player.Level,
                        player.Exp,
                        hp,
                        maxHp,
                        player.Mp,
                        (int)player.MaxMp,
                        player.Gem,
                        player.OpvpScore,
                        mapId
                    );
                }
                catch (Exception ex)
                {
                    Utils.Debug.Log.Error("HISTORY", $"保存玩家历史记录失败 - Player: {player.Id}, Error: {ex.Message}");
                }
            });

            // Save copy/maze reference before removing player
            var playerCopy = player.Map?.Copy;
            var playerMaze = Logic.Quest.Maze.IsIn(player) ? player.Map.Parent as global::Data.Maze : null;

            if (player.Leader != null)
            {
                Move.Follow.DoUnFollow(player);
            }

            CleanupCompanions(player);

            // 下线清理：只清理事件监听器，不清理子对象
            // 设计说明：
            // - Ability.Release()已改为不递归清理，所以Part不会被移除
            // - 子对象（Part/Skill/Item等）的内存由GC自动回收
            // - 下次登录时会从Database重新加载这些对象
            player.Destroy();
            
            // Cleanup copy/maze AFTER player is removed
            if (playerCopy != null)
            {
                Logic.Quest.Copy.CheckAndDestroy(playerCopy);
            }
            else if (playerMaze != null)
            {
                Logic.Quest.Maze.CheckAndDestroy(playerMaze);
            }
        }

        private static void CleanupCompanions(global::Data.Player player)
        {
            if (player == null || player.Map == null) return;

            var companions = player.Map.Content.Gets<global::Data.Life>(l => l.Leader == player).ToList();
            foreach (var companion in companions)
            {
                companion.Release();
            }
        }
    }
}
