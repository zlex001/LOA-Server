using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Utils
{
    public static class Csv
    {
        static Csv()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static List<Dictionary<string, object>> LoadAsRows(string path)
        {
            if (!File.Exists(path))
            {
                return new List<Dictionary<string, object>>();
            }

            var lines = File.ReadAllLines(path, DetectEncoding(path));
            if (lines.Length == 0)
            {
                return new List<Dictionary<string, object>>();
            }

            var headers = ParseCsvLine(lines[0]);
            var result = new List<Dictionary<string, object>>();

            for (int i = 1; i < lines.Length; i++)
            {
                var values = ParseCsvLine(lines[i]);
                
                bool isEmptyRow = true;
                for (int k = 0; k < values.Count; k++)
                {
                    if (!string.IsNullOrWhiteSpace(values[k]))
                    {
                        isEmptyRow = false;
                        break;
                    }
                }
                
                if (isEmptyRow)
                {
                    continue;
                }
                
                var rowDict = new Dictionary<string, object>();
                
                for (int j = 0; j < headers.Count; j++)
                {
                    string key = headers[j];
                    string value = j < values.Count ? values[j] : "";
                    rowDict[key] = value;
                }
                
                result.Add(rowDict);
            }

            return result;
        }

        public static List<List<object>> LoadAsCells(string path)
        {
            if (!File.Exists(path))
            {
                return new List<List<object>>();
            }

            var lines = File.ReadAllLines(path, DetectEncoding(path));
            var result = new List<List<object>>();

            if (lines.Length == 0)
            {
                return result;
            }

            int maxCols = 0;
            foreach (var line in lines)
            {
                var values = ParseCsvLine(line);
                if (values.Count > maxCols)
                {
                    maxCols = values.Count;
                }
            }

            foreach (var line in lines)
            {
                var values = ParseCsvLine(line);
                var rowData = new List<object>();
                
                for (int c = 0; c < maxCols; c++)
                {
                    if (c < values.Count)
                    {
                        string value = values[c];
                        rowData.Add(string.IsNullOrWhiteSpace(value) ? null : value);
                    }
                    else
                    {
                        rowData.Add(null);
                    }
                }
                
                result.Add(rowData);
            }

            return result;
        }

        public static void SaveByRows(List<Dictionary<string, object>> datas, string path)
        {
            if (datas == null || datas.Count == 0)
            {
                File.WriteAllText(path, "", Encoding.UTF8);
                return;
            }

            var sb = new StringBuilder();
            var headers = datas[0].Keys.ToList();
            
            sb.AppendLine(string.Join(",", headers.Select(h => EscapeCsvField(h))));

            foreach (var row in datas)
            {
                var values = new List<string>();
                foreach (var header in headers)
                {
                    var value = row.ContainsKey(header) ? row[header]?.ToString() ?? "" : "";
                    values.Add(EscapeCsvField(value));
                }
                sb.AppendLine(string.Join(",", values));
            }

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        public static void SaveByCells(List<List<object>> datas, string path)
        {
            if (datas == null || datas.Count == 0)
            {
                File.WriteAllText(path, "", Encoding.UTF8);
                return;
            }

            int maxCols = datas.Max(row => row?.Count ?? 0);
            var sb = new StringBuilder();

            foreach (var row in datas)
            {
                var values = new List<string>();
                for (int c = 0; c < maxCols; c++)
                {
                    if (c < row.Count && row[c] != null)
                    {
                        values.Add(EscapeCsvField(row[c].ToString()));
                    }
                    else
                    {
                        values.Add("");
                    }
                }
                sb.AppendLine(string.Join(",", values));
            }

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(line))
            {
                return result;
            }

            bool inQuotes = false;
            var currentField = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            result.Add(currentField.ToString());
            return result;
        }

        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return "";
            }

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }

            return field;
        }

        private static Encoding DetectEncoding(string path)
        {
            var bytes = File.ReadAllBytes(path);
            
            if (bytes.Length < 3)
            {
                return Encoding.UTF8;
            }

            if (bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            {
                return new UTF8Encoding(true);
            }

            if (bytes[0] == 0xFF && bytes[1] == 0xFE)
            {
                return Encoding.Unicode;
            }

            if (bytes[0] == 0xFE && bytes[1] == 0xFF)
            {
                return Encoding.BigEndianUnicode;
            }

            bool hasChineseBytes = false;
            for (int i = 0; i < Math.Min(bytes.Length - 1, 1000); i++)
            {
                if (bytes[i] >= 0x80)
                {
                    hasChineseBytes = true;
                    break;
                }
            }

            if (hasChineseBytes)
            {
                try
                {
                    var utf8Text = Encoding.UTF8.GetString(bytes);
                    var utf8Bytes = Encoding.UTF8.GetBytes(utf8Text);
                    
                    bool utf8Valid = true;
                    for (int i = 0; i < Math.Min(bytes.Length, utf8Bytes.Length); i++)
                    {
                        if (bytes[i] != utf8Bytes[i])
                        {
                            utf8Valid = false;
                            break;
                        }
                    }
                    
                    if (!utf8Valid)
                    {
                        return Encoding.GetEncoding("GBK");
                    }
                }
                catch
                {
                    return Encoding.GetEncoding("GBK");
                }
            }

            return Encoding.UTF8;
        }
    }
}

