using NPOI.SS.Formula;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Collections.Generic;
using System.Numerics;

namespace FailReport.Upload
{
    public class ExcelUploadService : IExcelUploadService
    {


        // 配置文件保存路径 
        private string _uploadPath = Path.Combine("C:\\", "Logs_Uploads");


        public ExcelUploadService()
        {
            // 确保上传目录存在
            if (!Directory.Exists(_uploadPath))
            {
                Directory.CreateDirectory(_uploadPath);
            }
        }

        public async Task<FileUploadResult> SaveExcelFileAsync(IFormFile file)
        {
            try
            {
                // 生成唯一的文件名，避免覆盖
                var fileName = $"{Guid.NewGuid().ToString().Substring(0, 8)}_{file.FileName}";// {Path.GetExtension(file.FileName)}
                var filePath = Path.Combine(_uploadPath, fileName);

                // 保存文件到服务器
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return new FileUploadResult
                {
                    Success = true,
                    FilePath = filePath
                };
            }
            catch (Exception ex)
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }
        public async Task<string> ReadExcelData2Txt(string filePath)
        {
            HashSet<string> seen = new HashSet<string>();
            // 替换为Excel文件路径
            // string filePath = @"D:\LOG\1869B1 FCT基础数据_20250804.xlsx";
            string LogsPath = Path.ChangeExtension(filePath, null);
            if (!Directory.Exists(LogsPath))//如果不存在就创建file文件夹
            {
                Directory.CreateDirectory(LogsPath);
            }
            if (!File.Exists(filePath))
            {
                Console.WriteLine("文件不存在！");
                return ("文件不存在！");
            }
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                IWorkbook workbook = new XSSFWorkbook(fileStream);
                ISheet sheet = workbook.GetSheetAt(0); // 获取工作表

                if (sheet.LastRowNum < 0)
                {
                    Console.WriteLine("Excel文件中没有数据！");
                    return ("Excel文件中没有数据！");
                }

                // 获取表头行
                IRow headerRow = sheet.GetRow(0);
                Dictionary<int, string> headerDict = new Dictionary<int, string>();

                // 构建列名到索引的映射
                for (int col = 0; col < headerRow.LastCellNum; col++)
                {
                    ICell cell = headerRow.GetCell(col);
                    if (cell != null)
                    {
                        headerDict[col] = cell is null ?"":cell.ToString();
                    }
                }

          
                string serialNumber = string.Empty;
                string result = string.Empty;


                // 遍历所有数据行（从第二行开始）
                //for (int rowNum = 1; rowNum <= sheet.LastRowNum; rowNum++)
                // 从最后一行开始
                for (int rowNum = sheet.LastRowNum; rowNum >= 1; rowNum--)
                {
                    IRow row = sheet.GetRow(rowNum);
                    if (row == null) continue;

                    serialNumber = GetCellValue(row, headerDict, "SerialNumber");
                    if (!seen.Add(serialNumber)) // Add失败说明已存在（重复）
                    {
                        continue;
                    }
                    result = GetCellValue(row, headerDict, "Result");
                    string testTime = GetCellValue(row, headerDict, "TestTime");
                    string fixture = GetCellValue(row, headerDict, "Fixture");
                    string cavity = GetCellValue(row, headerDict, "Cavity");
                    string operatorName = GetCellValue(row, headerDict, "Operator");
                    string orderNo = GetCellValue(row, headerDict, "OrderNO");
                    string itemCode = GetCellValue(row, headerDict, "ItemCode");
                    string itemName = GetCellValue(row, headerDict, "ItemName");
                    string P = @"ProdName
ProdCode:
SerialNumber:" + serialNumber + @"
TestUser: " + (operatorName.Replace("Operator/", "")) + @"
TestFixture: " + fixture + @"
Cavity: " + cavity + @"
TestTime: " + testTime + @"
TestResult: " + result + @"
Step	Item	Range	TestValue	Result
";
                    int Index = 1;
                    //Console.WriteLine($"行 {rowNum + 1}: SerialNumber = {serialNumber}");
                    //Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~``");
                    string TpTxt = string.Empty;
                    // 获取测试 Step 
                    foreach (var header in headerDict)
                    {
                        if (header.Value.Contains("Step"))
                        {
                            string stepValue = GetCellValue(row, headerDict, header.Value);
                            //char[] Ch = stepValue.ToArray();
                            //Ch.Dump();
                            //if (stepValue.Split('\u200B').Count() != 6)
                            //{
                            //	stepValue.Split('\u200B').Dump();// 必须包含\u200B count大于=6
                            //}
                            TpTxt += $"{Index}\t{(stepValue.Replace('\u200B', '\t'))}\r\n";
                            // Console.WriteLine($"{header.Value}: {stepValue}");
                        }
                        Index++;
                    }
                    string TxtName = LogsPath + "\\" + serialNumber + "___" + DateTime.Now.ToString("yyyyMMddhhmmssffff") + "___" + result + ".txt";
                    //TxtName.Dump();
                    using (StreamWriter writer = new StreamWriter(TxtName))
                    {
                        writer.Write((P + TpTxt));
                        //Thread.Sleep(100);
                    }
                    //(P + TpTxt).Dump();
                    Console.WriteLine("---------------" + TxtName + "生成完成！----------------------");
                }

                return ("---------------" + "生成完成 " + sheet.LastRowNum + "条数据！----------------------");
                //Console.WriteLine("---------------" + "生成完成 " + sheet.LastRowNum + "条数据！----------------------");

            }
        }

        private string GetCellValue(IRow row, Dictionary<int, string> headerDict, string columnName)
        {
            var column = headerDict.FirstOrDefault(h => h.Value == columnName);
            if (column.Key >= 0 && column.Key < row.LastCellNum)
            {
                ICell cell = row.GetCell(column.Key);
                return cell?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }
    }
    public interface IExcelUploadService
    {
        /// <summary>
        /// 保存Excel文件到服务器
        /// </summary>
        /// <param name="file">上传的文件</param>
        /// <returns>包含上传结果的对象</returns>
        Task<FileUploadResult> SaveExcelFileAsync(IFormFile file);
        Task<string> ReadExcelData2Txt(string filePath);
    }

    /// <summary>
    /// 文件上传结果模型
    /// </summary>
    public class FileUploadResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; }
        public string ErrorMessage { get; set; }
    }



}
