using System;
using System.IO;
using System.Linq;

namespace Utils
{
    public static class ConvertXlsxToCsv
    {
        public static void ConvertAllDesignData()
        {
            ConvertDesignData();
            ConvertConfigData();
            ConvertWorldFile();
        }

        private static void ConvertDesignData()
        {
            string designDataPath = Paths.DesignData;
            
            var files = new[]
            {
                "剧情.xlsx", "生物.xlsx", "道具.xlsx", "技能.xlsx", "招式.xlsx",
                "效应.xlsx", "场景.xlsx", "地图.xlsx", "行为树.xlsx", "商城.xlsx",
                "故事.xlsx", "迷宫.xlsx", "语言.xlsx"
            };

            foreach (var file in files)
            {
                string xlsxPath = Path.Combine(designDataPath, file);
                string csvPath = Path.Combine(designDataPath, file.Replace(".xlsx", ".csv"));
                
                if (File.Exists(xlsxPath))
                {
                    try
                    {
                        var rows = Excel.LoadAsRows(xlsxPath);
                        if (rows.Count > 0)
                        {
                            Csv.SaveByRows(rows, csvPath);
                            Console.WriteLine($"Converted: {file} -> {file.Replace(".xlsx", ".csv")}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to convert {file}: {ex.Message}");
                    }
                }
            }
        }

        private static void ConvertConfigData()
        {
            string configPath = Paths.Config;
            
            var files = new[]
            {
                "BehaviorTree.xlsx", "Item.xlsx", "Movement.xlsx", "Skill.xlsx", "Life.xlsx",
                "Scene.xlsx", "Map.xlsx", "Maze.xlsx", "Quest.xlsx", "Multilingual.xlsx",
                "Buff.xlsx", "Shop.xlsx", "SeveredLimb.xlsx", "Step.xlsx"
            };

            foreach (var file in files)
            {
                string xlsxPath = Path.Combine(configPath, file);
                string csvPath = Path.Combine(configPath, file.Replace(".xlsx", ".csv"));
                
                if (File.Exists(xlsxPath))
                {
                    try
                    {
                        var rows = Excel.LoadAsRows(xlsxPath);
                        if (rows.Count > 0)
                        {
                            Csv.SaveByRows(rows, csvPath);
                            Console.WriteLine($"Converted: {file} -> {file.Replace(".xlsx", ".csv")}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to convert {file}: {ex.Message}");
                    }
                }
            }
        }

        private static void ConvertWorldFile()
        {
            string designDataPath = Paths.DesignData;
            string worldXlsxPath = Path.Combine(designDataPath, "世界.xlsx");
            string worldCsvPath = Path.Combine(designDataPath, "地图坐标.csv");
            string sceneMapCsvPath = Path.Combine(designDataPath, "场景坐标.csv");
            string sceneMapSourcePath = Path.Combine(Directory.GetCurrentDirectory(), "SceneMap.csv");

            if (File.Exists(worldXlsxPath))
            {
                try
                {
                    var cells = Excel.LoadAsCells(worldXlsxPath);
                    Csv.SaveByCells(cells, worldCsvPath);
                    Console.WriteLine($"Converted: 世界.xlsx -> 地图坐标.csv");

                    try
                    {
                        var sceneMapCells = Excel.LoadAsCells(worldXlsxPath, "SceneMap");
                        if (sceneMapCells != null && sceneMapCells.Count > 0)
                        {
                            Csv.SaveByCells(sceneMapCells, sceneMapCsvPath);
                            Console.WriteLine($"Converted: 世界.xlsx (SceneMap sheet) -> 场景坐标.csv");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (File.Exists(sceneMapSourcePath))
                        {
                            File.Copy(sceneMapSourcePath, sceneMapCsvPath, true);
                            Console.WriteLine($"Copied: SceneMap.csv -> 场景坐标.csv");
                        }
                        else
                        {
                            Console.WriteLine($"SceneMap sheet not found and SceneMap.csv not found: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to convert 世界.xlsx: {ex.Message}");
                }
            }
            else if (File.Exists(sceneMapSourcePath))
            {
                File.Copy(sceneMapSourcePath, sceneMapCsvPath, true);
                Console.WriteLine($"Copied: SceneMap.csv -> 场景坐标.csv");
            }
        }
    }
}

