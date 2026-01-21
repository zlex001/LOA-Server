using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Utils
{
    public static class Json
    {
        public static T Deserialize<T>(string json)
        {
            try 
            { 
                return JsonConvert.DeserializeObject<T>(json); 
            }
            catch 
            { 
                return default; 
            }
        }

        public static Dictionary<T, U> Deserialize<T, U>(string json)
        {
            try 
            { 
                return JsonConvert.DeserializeObject<Dictionary<T, U>>(json); 
            }
            catch 
            { 
                return new Dictionary<T, U>(); 
            }
        }

        public static Dictionary<string, object> Parse(string json)
        {
            try 
            { 
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json) 
                    ?? new Dictionary<string, object>(); 
            }
            catch 
            { 
                return new Dictionary<string, object>(); 
            }
        }

        public static T Get<T>(this Dictionary<string, object> dict, string key, T defaultValue = default)
        {
            if (dict != null && dict.TryGetValue(key, out object value) && !string.IsNullOrEmpty(value?.ToString()))
            {
                try 
                { 
                    if (value is T directValue)
                        return directValue;

                    if (typeof(T) == typeof(int) && value is long longValue && longValue <= int.MaxValue && longValue >= int.MinValue)
                        return (T)(object)(int)longValue;

                    if (typeof(T) == typeof(double) && value is long longDoubleValue)
                        return (T)(object)(double)longDoubleValue;

                    return (T)Convert.ChangeType(value, typeof(T)); 
                }
                catch 
                { 
                    return defaultValue; 
                }
            }
            return defaultValue;
        }
    }

    public static class WorldConverter
    {
        public class RegionMap
        {
            public string RegionName { get; set; }
            public BoundingBox Bounds { get; set; }
            public int TotalPoints { get; set; }
            public List<Coordinate> Coordinates { get; set; }
        }

        public class BoundingBox
        {
            public int MinRow { get; set; }
            public int MaxRow { get; set; }
            public int MinColumn { get; set; }
            public int MaxColumn { get; set; }
        }

        public class Map
        {
            public string Name { get; set; }
            public int TotalRows { get; set; }
            public int TotalColumns { get; set; }
            public List<Coordinate> Coordinates { get; set; }
            public List<List<bool>> CellMap { get; set; }
        }

        public class Coordinate
        {
            public int Row { get; set; }
            public int Column { get; set; }
            public string Region { get; set; }
        }

        public static void FromExcelToJson(string excelFileName, string outputJsonPath = null)
        {
            try
            {
                var excelPath = System.IO.Path.Combine(Paths.DesignData, excelFileName);
                if (!File.Exists(excelPath))
                    throw new FileNotFoundException($"Excel file not found: {excelPath}");

                var cells = Excel.LoadAsCells(excelPath);
                var mapData = ProcessCells(cells, Path.GetFileNameWithoutExtension(excelFileName));

                outputJsonPath ??= Path.Combine(Paths.Documents, $"{Path.GetFileNameWithoutExtension(excelFileName)}_map.json");
                var jsonString = JsonConvert.SerializeObject(mapData, Formatting.Indented);
                FileManager.Instance.SaveWithDirectory(jsonString, outputJsonPath);

                ExportRegionMaps(mapData.Coordinates, Path.Combine(Path.GetDirectoryName(outputJsonPath), "regions"));
            }
            catch (Exception ex)
            {
                Utils.Debug.Log.Error("JSON", $"Conversion failed: {ex.Message}");
                throw;
            }
        }

        private static void ExportRegionMaps(List<Coordinate> allCoords, string outputDir)
        {
            var regionGroups = allCoords
                .GroupBy(c => c.Region.Split('·')[0])
                .ToDictionary(g => g.Key, g => g.ToList());

            var summaryList = new List<RegionMap>();

            foreach (var kvp in regionGroups)
            {
                string regionName = kvp.Key;
                var coords = kvp.Value;

                var bounds = new BoundingBox
                {
                    MinRow = coords.Min(c => c.Row),
                    MaxRow = coords.Max(c => c.Row),
                    MinColumn = coords.Min(c => c.Column),
                    MaxColumn = coords.Max(c => c.Column)
                };

                var regionMap = new RegionMap
                {
                    RegionName = regionName,
                    Bounds = bounds,
                    TotalPoints = coords.Count,
                    Coordinates = coords
                };

                summaryList.Add(regionMap);

                string regionJson = JsonConvert.SerializeObject(regionMap, Formatting.Indented);
                string regionPath = Path.Combine(outputDir, $"{regionName}.json");
                FileManager.Instance.SaveWithDirectory(regionJson, regionPath);
            }

            var summaryJson = JsonConvert.SerializeObject(summaryList.Select(r => new
            {
                r.RegionName,
                r.Bounds,
                r.TotalPoints
            }), Formatting.Indented);

            string summaryPath = Path.Combine(outputDir, "regions_summary.json");
            FileManager.Instance.SaveWithDirectory(summaryJson, summaryPath);
        }

        private static Map ProcessCells(List<List<object>> cells, string mapName)
        {
            if (cells?.Count == 0)
            {
                return new Map { Name = mapName, TotalRows = 0, TotalColumns = 0, Coordinates = new List<Coordinate>(), CellMap = new List<List<bool>>() };
            }

            var coordinates = new List<Coordinate>();
            var cellMap = new List<List<bool>>();

            for (int row = 0; row < cells.Count; row++)
            {
                var rowData = cells[row];
                var cellRowMap = new List<bool>();

                for (int col = 0; col < (rowData?.Count ?? 0); col++)
                {
                    bool hasContent = rowData[col] != null && !string.IsNullOrWhiteSpace(rowData[col].ToString());
                    
                    cellRowMap.Add(hasContent);

                    if (hasContent)
                    {
                        coordinates.Add(new Coordinate
                        {
                            Row = row,
                            Column = col,
                            Region = rowData[col].ToString().Trim()
                        });
                    }
                }

                cellMap.Add(cellRowMap);
            }

            return new Map
            {
                Name = mapName,
                TotalRows = cells.Count,
                TotalColumns = cells.Max(row => row?.Count ?? 0),
                Coordinates = coordinates,
                CellMap = cellMap
            };
        }
    }
}