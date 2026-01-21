using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Utils
{
    public static class Excel
    {
        public static List<Dictionary<string, object>> LoadAsRows(string path)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = WorkbookFactory.Create(fileStream);
                ISheet sheet = workbook.GetSheetAt(0);
                var result = new List<Dictionary<string, object>>();
                var headerRow = sheet.GetRow(0);
                var columnCount = headerRow.LastCellNum;
                for (int r = 1; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    var rowDict = new Dictionary<string, object>();
                    if (row != null)
                    {
                        for (int c = 0; c < columnCount; c++)
                        {
                            var cellValue = row.GetCell(c)?.ToString() ?? "";
                            rowDict.Add(headerRow.GetCell(c).ToString(), cellValue);
                        }
                    }
                    result.Add(rowDict);
                }
                return result;
            }
        }
        public static List<Dictionary<string, object>> LoadAsRows(string path, string name)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = WorkbookFactory.Create(fileStream);
                ISheet sheet = workbook.GetSheet(name);
                var result = new List<Dictionary<string, object>>();
                var headerRow = sheet.GetRow(0);
                var columnCount = headerRow.LastCellNum;
                for (int r = 1; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    var rowDict = new Dictionary<string, object>();
                    if (row != null)
                    {
                        for (int c = 0; c < columnCount; c++)
                        {
                            var cellValue = row.GetCell(c)?.ToString() ?? "";
                            rowDict.Add(headerRow.GetCell(c).ToString(), cellValue);
                        }
                    }
                    result.Add(rowDict);
                }
                return result;
            }
        }
        public static List<List<object>> LoadAsCells(string path)
        {
            return LoadAsCells(path, 0);
        }

        public static List<List<object>> LoadAsCells(string path, string sheetName)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = WorkbookFactory.Create(fileStream);
                ISheet sheet = workbook.GetSheet(sheetName);
                var result = new List<List<object>>();

                if (sheet == null || sheet.LastRowNum < 0)
                    return result;

                int maxCols = 0;
                for (int r = 0; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row != null && row.LastCellNum > maxCols)
                    {
                        maxCols = row.LastCellNum;
                    }
                }

                for (int r = 0; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    var rowData = new List<object>();

                    for (int c = 0; c < maxCols; c++)
                    {
                        var cell = row?.GetCell(c);
                        if (cell == null || string.IsNullOrWhiteSpace(cell.ToString()))
                        {
                            rowData.Add(null);
                        }
                        else
                        {
                            rowData.Add(GetCellValueAvoidingAmbiguity(cell));
                        }
                    }

                    result.Add(rowData);
                }

                return result;
            }
        }

        public static List<List<object>> LoadAsCells(string path, int sheetIndex)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = WorkbookFactory.Create(fileStream);
                ISheet sheet = workbook.GetSheetAt(sheetIndex);
                var result = new List<List<object>>();

                if (sheet == null || sheet.LastRowNum < 0)
                    return result;

                int maxCols = 0;
                for (int r = 0; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    if (row != null && row.LastCellNum > maxCols)
                    {
                        maxCols = row.LastCellNum;
                    }
                }

                for (int r = 0; r <= sheet.LastRowNum; r++)
                {
                    var row = sheet.GetRow(r);
                    var rowData = new List<object>();

                    for (int c = 0; c < maxCols; c++)
                    {
                        var cell = row?.GetCell(c);
                        if (cell == null || string.IsNullOrWhiteSpace(cell.ToString()))
                        {
                            rowData.Add(null);
                        }
                        else
                        {
                            rowData.Add(GetCellValueAvoidingAmbiguity(cell));
                        }
                    }

                    result.Add(rowData);
                }

                return result;
            }
        }
        private static object GetCellValueAvoidingAmbiguity(ICell cell)
        {
            return cell.CellType switch
            {
                CellType.Boolean => cell.BooleanCellValue,
                CellType.Numeric => cell.NumericCellValue,
                CellType.String => cell.StringCellValue,
                CellType.Formula => cell.CellFormula,
                _ => null,
            };
        }
        public static void SaveByRows(List<Dictionary<string, object>> datas, string path)
        {
            SaveByRows(datas, path, "Sheet1");
        }
        public static void Add(List<Dictionary<string, object>> datas, string path)
        {
            IWorkbook book;
            if (System.IO.File.Exists(path))
            {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    book = WorkbookFactory.Create(stream);
                }
            }
            else
            {
                book = new XSSFWorkbook();
            }
            if (book.NumberOfSheets == 0)
            {
                book.CreateSheet();
            }
            ISheet sheet = book.GetSheetAt(0) ?? book.CreateSheet();
            // 写入新数据
            int rowCount = sheet.LastRowNum + 1;
            IRow headerRow = sheet.GetRow(0);
            if (headerRow == null)
            {
                headerRow = sheet.CreateRow(0);
                foreach (var key in datas[0].Keys)
                {
                    int columnIndex = headerRow.Cells.Count;
                    ICell headerCell = headerRow.CreateCell(columnIndex, CellType.String);
                    headerCell.SetCellValue(key);
                }
            }
            for (int r = 0; r < datas.Count; r++)
            {
                IRow row = sheet.CreateRow(rowCount + r);
                foreach (var key in datas[r].Keys)
                {
                    int columnIndex = headerRow.Cells.FindIndex(c => c.StringCellValue == key);
                    if (columnIndex >= 0)
                    {
                        ICell cell = row.CreateCell(columnIndex);
                        object cellValue = datas[r][key];
                        if (cellValue is int || cellValue is double || cellValue is float)
                        {
                            cell.SetCellValue(Convert.ToDouble(cellValue));
                        }
                        else
                        {
                            cell.SetCellValue(cellValue?.ToString() ?? "");
                        }
                    }
                }
            }
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            {
                book.Write(stream);
            }
        }
        public static void SaveByRows(List<Dictionary<string, object>> datas, string path, string sheetName)
        {
            IWorkbook book;
            if (System.IO.File.Exists(path))
            {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    book = WorkbookFactory.Create(stream);
                }
            }
            else
            {
                book = new XSSFWorkbook();
            }
            ISheet sheet = book.GetSheet(sheetName);

            if (book.GetSheet(sheetName) == null)
            {
                sheet = book.CreateSheet(sheetName);
            }
            // 清空表格数据
            for (int r = sheet.LastRowNum; r >= 0; r--)
            {
                IRow row = sheet.GetRow(r);
                if (row != null)
                {
                    sheet.RemoveRow(row);
                }
            }
            // 写入表头
            IRow headerRow = sheet.CreateRow(0);
            foreach (var key in datas[0].Keys)
            {
                int columnIndex = headerRow.Cells.Count;
                ICell headerCell = headerRow.CreateCell(columnIndex, CellType.String);
                headerCell.SetCellValue(key);
            }
            // 写入新数据
            for (int r = 0; r < datas.Count; r++)
            {
                IRow row = sheet.CreateRow(r + 1);
                foreach (var key in datas[r].Keys)
                {
                    int columnIndex = headerRow.Cells.FindIndex(c => c.StringCellValue == key);
                    if (columnIndex >= 0)
                    {
                        ICell cell = row.CreateCell(columnIndex);
                        object cellValue = datas[r][key];
                        if (cellValue is int || cellValue is double || cellValue is float)
                        {
                            cell.SetCellValue(Convert.ToDouble(cellValue));
                        }
                        else
                        {
                            cell.SetCellValue(cellValue?.ToString() ?? "");
                        }
                    }
                }
            }
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite))
            {
                book.Write(stream);
            }
        }
        public static void SaveByCells(List<List<object>> datas, string path)
        {
            IWorkbook book;
            if (File.Exists(path))
            {
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    book = WorkbookFactory.Create(stream);
                }
            }
            else
            {
                book = new XSSFWorkbook();
            }

            ISheet sheet = book.GetSheetAt(0) ?? book.CreateSheet();
            for (int r = sheet.LastRowNum; r >= 0; r--)
            {
                IRow row = sheet.GetRow(r);
                if (row != null)
                {
                    sheet.RemoveRow(row);
                }
            }

            int count = 0;
            double total = datas.Sum(subList => subList.Count(cell => cell != null && !string.IsNullOrEmpty(cell.ToString())));

            for (int r = 0; r < datas.Count; r++)
            {
                IRow row = sheet.CreateRow(r);
                for (int c = 0; c < datas[r].Count; c++)
                {
                    object cellValue = datas[r][c];
                    if (cellValue == null || string.IsNullOrEmpty(cellValue.ToString()))
                    {
                        continue;
                    }

                    count++;
                    ICell cell = row.CreateCell(c);

                    switch (cellValue)
                    {
                        case int intValue:
                            cell.SetCellValue(intValue);
                            break;
                        case double doubleValue:
                            cell.SetCellValue(doubleValue);
                            break;
                        case float floatValue:
                            cell.SetCellValue((double)floatValue);
                            break;
                        default:
                            cell.SetCellValue(cellValue.ToString());
                            break;
                    }

                }
            }

            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                book.Write(stream);
            }

        }
        public static void Copy(string sourcePath, string destinationPath)
        {
            List<List<object>> sourceData = LoadAsCells(sourcePath);
            SaveByCells(sourceData, destinationPath);
        }
        public static void ReplaceValuesInExcel(string inputFilePath, string outputFilePath, Dictionary<object, object> replaceDict, string saveMode = "Cells")
        {
            try
            {
                if (saveMode == "Cells")
                {
                    List<List<object>> sourceData = Excel.LoadAsCells(inputFilePath);
                    for (int row = 0; row < sourceData.Count; row++)
                    {
                        for (int col = 0; col < sourceData[row].Count; col++)
                        {
                            object cellValue = sourceData[row][col];
                            if (cellValue != null && replaceDict.ContainsKey(cellValue.ToString()))
                            {
                                sourceData[row][col] = replaceDict[cellValue.ToString()];
                            }
                        }
                    }

                    SaveByCells(sourceData, outputFilePath);
                }
                else if (saveMode == "Rows")
                {
                    List<Dictionary<string, object>> sourceData = Excel.LoadAsRows(inputFilePath);
                    foreach (var rowDict in sourceData)
                    {
                        foreach (var key in rowDict.Keys.ToList())
                        {
                            object cellValue = rowDict[key];

                            // 如果单元格值在替换字典中，则进行替换
                            if (cellValue != null && replaceDict.ContainsKey(cellValue.ToString()))
                            {
                                rowDict[key] = replaceDict[cellValue.ToString()];
                            }
                        }
                    }

                    // 保存替换后的数据到指定文件
                    SaveByRows(sourceData, outputFilePath);
                }
                else
                {
                    throw new ArgumentException($"未知的保存模式：{saveMode}");
                }
            }
            catch (Exception ex)
            {
            }
        }

    }

}

