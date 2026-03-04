using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CronAPI.Infrastructure.Method.Static
{
    public static class EasyExcel
    {
        #region 读取相关
        /// <summary>
        /// 读取 Excel (多个工作表，可指定)
        /// </summary>
        /// <param name="excelFilePath">Excel 文件路径</param>
        /// <param name="sheetNames">需要读取的工作表名称列表</param>
        /// <returns>工作表：工作表数据</returns>
        public static Dictionary<string, DataTable> ReadAllExcel(string excelFilePath, List<string> sheetNames = null)
        {
            Dictionary<string, DataTable> result = new Dictionary<string, DataTable>();

            try
            {
                //输出到临时文件
                excelFilePath = GetTempFile(excelFilePath);
                if (sheetNames == null)
                {
                    sheetNames = GetSheetNameList(excelFilePath);
                }
                using (var workbook = new XLWorkbook(excelFilePath))
                {
                    // 遍历所有需要读取的工作表
                    foreach (var sheetName in sheetNames)
                    {
                        // 判断工作表是否存在
                        if (workbook.Worksheets.Contains(sheetName))
                        {
                            var worksheet = workbook.Worksheet(sheetName);
                            DataTable dataTable = ConvertWorksheetToDataTable(worksheet);
                            result.Add(sheetName, dataTable);
                        }
                        else
                        {
                            Console.WriteLine($"工作表 {sheetName} 不存在！");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取 Excel 文件时发生错误: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// 将目标文件输出到临时文件目录，返回临时文件路径
        /// </summary>
        private static string GetTempFile(string sourceFilePath)
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()+Path.GetExtension(sourceFilePath));
            File.Copy(sourceFilePath, tempFilePath, true);
            return tempFilePath;
        }

        /// <summary>
        /// 读取 Excel （第一个工作表）
        /// </summary>
        /// <param name="excelFilePath">Excel 文件路径</param>
        /// <param name="sheetNames">需要读取的工作表名称列表</param>
        /// <returns>工作表：工作表数据</returns>
        public static (string sheetName, DataTable datas) ReadFirstExcel(string excelFilePath)
        {
            var first = GetSheetNameList(excelFilePath).FirstOrDefault();
            var result = ReadAllExcel(excelFilePath, new List<string> { first });
            return (sheetName: result.Keys.FirstOrDefault(), datas: result.Values.FirstOrDefault());
        }

        /// <summary>
        /// 获取所有的sheet名称
        /// </summary>
        public static List<string> GetSheetNameList(string filePath)
        {
            // 打开 Excel 文件
            filePath = GetTempFile(filePath);
            using (var workbook = new XLWorkbook(filePath))
            {
                // 获取所有工作表(sheet)名称
                List<string> sheetNames = new List<string>();
                foreach (var worksheet in workbook.Worksheets)
                {
                    sheetNames.Add(worksheet.Name);
                }
                return sheetNames;
            }
        }

        /// <summary>
        /// 将工作表转换为 DataTable
        /// </summary>
        /// <param name="worksheet">工作表</param>
        /// <returns>转换后的 DataTable</returns>
        private static DataTable ConvertWorksheetToDataTable(IXLWorksheet worksheet)
        {
            DataTable dataTable = new DataTable();
            bool isHeaderRow = true;

            // 遍历工作表的每一行
            foreach (var row in worksheet.Rows())
            {
                if (isHeaderRow)
                {
                    // 第一个非空单元格是列头
                    foreach (var cell in row.Cells())
                    {
                        dataTable.Columns.Add(cell.Value.ToString());
                    }
                    isHeaderRow = false;
                }
                else
                {
                    // 每一行对应 DataTable 的一行
                    DataRow dataRow = dataTable.NewRow();
                    int columnIndex = 0;

                    foreach (var cell in row.Cells())
                    {
                        dataRow[columnIndex] = cell.Value.ToString();
                        columnIndex++;
                    }

                    dataTable.Rows.Add(dataRow);
                }
            }

            return dataTable;
        }
        #endregion

        #region 写相关
        /// <summary>
        /// 写入Excel（多个工作表）
        /// </summary>
        public static bool WriteAllExcel<T>(ExcelInPutDetails<T> excel) where T : class
        {
            try
            {
                //填充进Excel(ClosedXML.Excel)
                using (var workbook = new XLWorkbook())
                {
                    int sheetCount = 1;
                    foreach (var jtem in excel.DataDetails)
                    {
                        #region
                        //1.增加工作表
                        var worksheet = workbook.Worksheets.Add(string.IsNullOrEmpty(jtem.WorkSheetName) ? $"sheet{sheetCount}" : jtem.WorkSheetName);

                        //2.获取列表的范围，并且设置相关样式，例如A1:C1
                        List<string> columns = GenerateExcelColumns();
                        var count = jtem.Data.Count;
                        var propertyInfos = typeof(T).GetProperties();
                        var filedcount = propertyInfos.Length;//字段的长度
                                                              // 设置标题行字体为加粗
                        worksheet.Range($"A1:{columns[filedcount - 1]}1").Style.Font.Bold = true;
                        // 启用筛选器
                        worksheet.Range($"A1:{columns[filedcount - 1]}1").SetAutoFilter();
                        // 设置所有内容表格框
                        SetBorder(worksheet.Range($"A1:{columns[filedcount - 1]}{count + 1}"));

                        // 3. 设置所有单元格的居中对齐
                        worksheet.Cells().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        worksheet.Cells().Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                        // 4. 设置所有单元格的字体为宋体
                        worksheet.Cells().Style.Font.FontName = "SimSun";



                        //5.填充列表（列名：学生姓名，学生班级，学生成绩）
                        if (filedcount != jtem.Titlelist.Count)
                        {
                            jtem.Titlelist = propertyInfos.Select(x => x.Name).ToList();
                        }
                        var index = 0;
                        foreach (var item in propertyInfos)
                        {
                            worksheet.Cell($"{columns[index]}1").Value = jtem.Titlelist[index];
                            index++;
                        }

                        //6.填充数据
                        int rowIndex = 2;
                        foreach (var item in jtem.Data)
                        {
                            int columnIndex = 0;
                            foreach (var property in propertyInfos)
                            {
                                worksheet.Cell(rowIndex, columnIndex + 1).Value = property.GetValue(item)?.ToString();
                                columnIndex++;
                            }
                            worksheet.Column(columnIndex).Style.Alignment.WrapText = true;// 设置第x列自动换行
                            rowIndex++;
                        }

                        //7.自动调整列宽
                        worksheet.Columns().AdjustToContents();
                        #endregion

                        sheetCount++;
                    }
                    //保存excel文件
                    workbook.SaveAs(excel.Filepath);//存储路径

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }

        /// <summary>
        /// 写入Excel（一个工作表）
        /// </summary>
        public static bool WritFitstExcel<T>(string Filepath, MulData<T> data) where T : class, new()
        {

            return WriteAllExcel(new ExcelInPutDetails<T>
            {
                Filepath = Filepath,
                DataDetails = new List<MulData<T>> { data }
            });
        }

        /// <summary>
        /// 给表格画边框线
        /// </summary>
        /// <param name="range"></param>
        private static void SetBorder(IXLRange range)
        {
            // 设置表格框线
            range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            range.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            // 设置表格框线颜色和粗细
            var borderColor = XLColor.Black;
            range.Style.Border.OutsideBorderColor = borderColor;
            range.Style.Border.LeftBorderColor = borderColor;
            range.Style.Border.RightBorderColor = borderColor;
            range.Style.Border.TopBorderColor = borderColor;
            range.Style.Border.BottomBorderColor = borderColor;

            range.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
            range.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            range.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            //range.Style.Border.SetBorderWidth(borderWidth);

        }

        /// <summary>
        /// 生成A-ZZ列
        /// </summary>
        private static List<string> GenerateExcelColumns()
        {
            List<string> columns = new List<string>();

            // 从 A 到 Z
            for (char c = 'A'; c <= 'Z'; c++)
            {
                columns.Add(c.ToString());
            }
            // 从 AA 到 AZ
            for (char c1 = 'A'; c1 <= 'Z'; c1++)
            {
                for (char c2 = 'A'; c2 <= 'Z'; c2++)
                {
                    columns.Add(c1.ToString() + c2);
                }
            }
            return columns;
        }

        /// <summary>
        /// Excel目标路径+数据信息
        /// </summary>
        public class ExcelInPutDetails<T>
        {
            /// <summary>
            /// 目标Excel文件路径
            /// </summary>
            public string Filepath { get; set; }

            /// <summary>
            /// sheet名称、标题名称（为空则默认显示字段名称）、数据
            /// </summary>
            public List<MulData<T>> DataDetails { get; set; } = new List<MulData<T>>();
        }

        /// <summary>
        /// 数据相关实体
        /// </summary>
        public class MulData<T>
        {
            /// <summary>
            /// 数据
            /// </summary>
            public List<T> Data { get; set; } = new List<T>();
            /// <summary>
            /// 标题（为空则默认显示字段名称）
            /// </summary>

            public List<string> Titlelist { get; set; } = new List<string>();
            /// <summary>
            /// sheet名称(为空则显示sheetx)
            /// </summary>
            public string WorkSheetName { get; set; }
        }
        #endregion
    }
}
