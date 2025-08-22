using FailReport.Upload;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace FailReport.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class FailReportController : ControllerBase
    {
        private readonly IExcelUploadService _uploadService;
        const string VerifyNumer = @"^(\-|\+)?\d+(\.\d+)?$";

        private SerialPort? serialPort;

        //Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LOG");
        public static string LogPath;
        public static string FCTPath;
        public static string DefaultPathName;
        // 构造函数注入文件上传服务
        public FailReportController(IExcelUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        [HttpGet]
        public string GetPassReport(string PathName = "")
        {
            List<PassReport> result = new List<PassReport>();
            string FileNameCont = "Pass";
            // 获取全部Pass 的文件
            // 获取指定目录所有的log  csv 的或 txt
            // 如果未提供路径，则使用默认路径
            string LogPathDir = PathName ?? DefaultPathName;

            // 判断是否存在文件夹
            if (!Directory.Exists(LogPathDir))
            {
                return "文件夹不存在";
            }

            string[] LogAll = Directory.GetFileSystemEntries(LogPathDir);
            string[] FailS = LogAll.Where(m => m.Contains(FileNameCont)).ToArray();
            string FileType = Path.GetExtension(FailS[0].ToString()); //.txt
            try
            {
                foreach (string FailPath in FailS)
                {
                    string[] fileLines = System.IO.File.ReadAllLines(FailPath, Encoding.UTF8); // 读取文件内容

                    string searchString = FileNameCont; // 要查找的字符串

                    foreach (string line in fileLines)
                    {


                        if (line.Contains(searchString) && (!line.Contains("TestResult")) && line.Split("\t").Count() > 4)
                        {
                            string TestName = $"{line.Split("\t")[0]}_{line.Split("\t")[1]}";
                            string TestValues = (line.Split("\t")[3]).Replace(",", "").Trim();
                            bool IsV = System.Text.RegularExpressions.Regex.IsMatch(TestValues, VerifyNumer);
                            if (IsV)
                            {

                                PassReport? report = result.FirstOrDefault<PassReport>(M => M.TestName == TestName);

                                PassReport passReport = report is null ? new PassReport()
                                {
                                    TestName = TestName,
                                    PassData = new List<double>()
                                } : report;
                                passReport.PassData.Add(double.Parse(TestValues));
                                int resCount = result.Where(M => M.TestName == TestName).Count();
                                if (resCount <= 0)
                                {
                                    result.Add(passReport);
                                }
                            }

                            // 读取文件 若为数字的部分则添加到list中
                            // 组合数据返回  
                        }
                    }
                }
            }
            catch (Exception ex) { }

            return JsonSerializer.Serialize(result);
        }


        /// <summary>
        /// 修改LogPath
        /// </summary>
        [HttpGet]
        public string SetLogsPath(string? Path)
        {
            string Mes = string.Empty;
            // 判断path是否为合法路径
            if (!string.IsNullOrEmpty(Path) && Directory.Exists(Path))
            {
                LogPath = Path;
                Mes = "修改成功！";
            }
            return System.Text.Json.JsonSerializer.Serialize(new { LogPath, Mes });
        }


        /// <summary>
        /// 获取全部Fail 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public string GetAllFail(string Path)
        {
            return GetData(Path);
        }

        /// <summary>
        /// 获取Log 列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public string GetLogDirectorys()
        {

            string[] folderNames = Directory.GetDirectories(LogPath);
            return System.Text.Json.JsonSerializer.Serialize(folderNames);
        }


        [HttpGet]
        public string OpenFileDirectory(string Path)
        {
            // 打开指定文件夹
            Process.Start("explorer.exe", Path);
            return Path + " 打开成功";
        }

        [HttpGet]
        public string GetFailData()
        {
            string[] AllFail = Directory.GetFileSystemEntries(FCTPath)
                .Where(m => System.Text.RegularExpressions.Regex.IsMatch(m, ("^[^\u4e00-\u9fa5]+$")))
                .ToArray();
            return System.Text.Json.JsonSerializer.Serialize(AllFail);
        }




        [HttpGet]
        public double GetTemp(string? Com, string? SendDBStr)
        {
            Com = Com != null ? Com : "COM61";
            SendDBStr = SendDBStr != null ? SendDBStr : "01 04 04 00 00 01 30 FA";
            string result = string.Empty;

            serialPort = new SerialPort(Com, 9600);
            lock (serialPort)
            {

                serialPort.Open();
                serialPort.DiscardInBuffer();
                Byte[] crcbuf;
                string[] SendDB = SendDBStr.Split(' ');
                List<Byte> bytedata = new List<Byte>();
                foreach (var item in SendDB)
                {
                    bytedata.Add(Byte.Parse(item, System.Globalization.NumberStyles.AllowHexSpecifier));
                }
                crcbuf = new Byte[bytedata.Count];
                crcbuf = bytedata.ToArray();
                serialPort.Write(crcbuf, 0, crcbuf.Count());


                int len;
                byte[] datas;
                Stopwatch sw = new Stopwatch();
                sw.Restart();
                len = serialPort.BytesToRead;
                Thread.Sleep(50);
                while (len < serialPort.BytesToRead)
                {
                    len = serialPort.BytesToRead;
                    Thread.Sleep(10);
                }
                if (len > 0)
                {
                    datas = new byte[len];
                    serialPort.Read(datas, 0, len);
                    for (int i = 0; i < datas.Length; i++)
                    {
                        string str = Convert.ToString(datas[i], 16);
                        if (str.Length == 1)
                        {
                            str = string.Format("0{0}", str);
                        }
                        result += " " + str;
                    }
                }
                serialPort.DiscardInBuffer();
                serialPort.Close();
            }
            return Convert.ToInt32(result.Trim().Substring(9, 5).Replace(" ", ""), 16);
        }

        private string GetData(string PathName)
        {
            string searchString = "Fail";
            string _Pass = "Pass";
            string _Fail = "Fail";
            FailList _failList = new FailList();
            _failList.Data = new List<FailData>();
            _failList.PassReport = new List<PassReport>();
            // 获取指定目录所有的log  csv 的或 txt
            string[] LogAll = Directory.GetFileSystemEntries(PathName);


            string[] FailS = LogAll.Where(m => m.Contains(_Fail)).ToArray();
            string[] PassS = LogAll.Where(m => m.Contains(_Pass)).ToArray();

            if (LogAll.Count() > 0)
            {
                string FileType = Path.GetExtension(LogAll[0].ToString()); //.txt
                try
                {
                    foreach (string FailPath in LogAll)
                    {
                        string TpStr = "";
                        string[] fileLines = System.IO.File.ReadAllLines(FailPath, Encoding.UTF8); // 读取文件内容
                        if (!FailPath.Contains(_Fail) && FailPath.Contains(_Pass))
                        {
                            TpStr = "Pass";
                        }
                        else
                        {
                            TpStr = "Fail";
                        }


                        foreach (string line in fileLines)
                        {
                            //    要查找的字符串
                            if (line.Contains(TpStr) && (!line.Contains("TestResult")))
                            {
                                //Console.WriteLine("Fail:\t" + line + "\t Path: \t" + FailPath);

                                bool? TempHigh = null;
                                if (line.Contains("L<=x<=H"))
                                {
                                    string[] T_IsHigh = line.Replace("\t", "").Split(",");
                                    //48;48;18
                                    if (!T_IsHigh[3].Contains(';'))
                                    {
                                        //decimal Low = decimal.Parse(T_IsHigh[1]);

                                        // 可能是非数组的情况
                                        if (System.Text.RegularExpressions.Regex.IsMatch(T_IsHigh[3], VerifyNumer) &&
                                            System.Text.RegularExpressions.Regex.IsMatch(T_IsHigh[2], VerifyNumer))
                                        {
                                            decimal High = decimal.Parse(T_IsHigh[2]);
                                            decimal Target = decimal.Parse(T_IsHigh[3]);
                                            // 2  > 8
                                            TempHigh = High < Target;
                                        }
                                        else
                                        {
                                            TempHigh = false;
                                        }
                                    }
                                    else
                                    {

                                        //string[] LowStr = T_IsHigh[1].Split(";");
                                        string[] HighStr = T_IsHigh[2].Split(";");
                                        string[] TargetStr = T_IsHigh[3].Split(";");
                                        bool[]? TempHighs = new bool[TargetStr.Count()];

                                        for (int i = 0; i < TargetStr.Count(); i++)
                                        {
                                            string High = HighStr.Length >= TargetStr.Length ? HighStr[i] : HighStr[0];
                                            // 判断是否为数字
                                            if (!System.Text.RegularExpressions.Regex.IsMatch(TargetStr[i], VerifyNumer))
                                            {
                                                TempHighs[i] = false;
                                                continue;
                                            }
                                            ;
                                            decimal TargetValue = decimal.Parse(TargetStr[i]);
                                            TempHighs[i] = decimal.Parse(High) < TargetValue;

                                        }

                                        TempHigh = TempHighs.Where(m => m == true).Count() >= 1;
                                    }
                                }

                                if (TpStr == "Fail")
                                {
                                    _failList.Data.Add(new FailData()
                                    {
                                        FailDate = System.IO.File.GetLastWriteTime(FailPath),
                                        // 这里根据后缀名判断是否要将table 替换为逗号
                                        FailItme = System.IO.Path.GetExtension(FailPath).ToLower() == ".csv" ? line : line.Replace("\t", ","),
                                        FailPath = FailPath,
                                        // AC采集卡IN3, 216, 235, 211.83, V, Fail, 210, L<=x<=H
                                        IsHigh = TempHigh,
                                    });
                                }
                                else
                                {
                                    string TestName = $"{line.Split("\t")[0]}_{line.Split("\t")[1]}";
                                    string TestValues = (line.Split("\t")[3]).Replace(",", "").Trim();
                                    bool IsV = System.Text.RegularExpressions.Regex.IsMatch(TestValues, VerifyNumer);
                                    if (IsV)
                                    {

                                        PassReport? report = _failList.PassReport.FirstOrDefault<PassReport>(M => M.TestName == TestName);

                                        if (report is null)
                                        {

                                            PassReport passReport = new PassReport()
                                            {
                                                TestName = TestName,
                                                PassData = new List<double> { double.Parse(TestValues) }
                                            };

                                            _failList.PassReport.Add(passReport);
                                        }
                                        else
                                        {
                                            report.PassData.Add(double.Parse(TestValues));
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                    throw ex;
                }
            }

            // 读取所有包含Fail 的文件
            // 读取Fail 具体项目
            //添加到List


            //failList.Data = Data;

            // _failList.StrMes = "Passed:\t" + LogAll.Count() + "\nFailing:\t"
            //     + FailS.Count() + "\nSuccessRate:\t" + Math.Round((1 - (1.0 * FailS.Count() / LogAll.Count())) * 100, 2) + "%";
            // _failList.FailCount = _failList.Data.Count;

            _failList.StrMes = "Passed:\t" + PassS.Count()
                + "\nFailing:\t" + FailS.Count()
                + "\nSuccessRate:\t" + Math.Round((1 - (1.0 * FailS.Count() / LogAll.Count())) * 100, 2) + "%";
            _failList.FailCount = _failList.Data.Count;
            _failList.GroupDatas = new List<GroupData>();


            //_failList.GroupDatas = (from P in _failList.Data
            //                        group P by P.FailItme.Split(",")[0] into ped
            //                        select new GroupData
            //                        {
            //                            itmeName = ped.Key,
            //                            itmeCount = ped.Count()
            //                        })
            //                        .OrderBy(m => m.itmeCount)
            //                        .ToList();


            _failList.GroupDatas = _failList.Data
                .GroupBy(m => $"{m.FailItme.Split(',')[0]},{m.FailItme.Split(',')[1]}")
                .Select(p => new GroupData { itmeCount = p.Count(), itmeName = p.Key })
                .OrderByDescending(m => m.itmeCount)
                .ToList();

            // 查询图表需要的数据 


            return System.Text.Json.JsonSerializer.Serialize(_failList);

        }


        /// <summary>
        /// 上传Excel文件到服务器
        /// </summary>
        /// <param name="file">要上传的Excel文件</param>
        /// <returns>上传结果</returns>
        [HttpPost("upload")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            // 检查文件是否为空
            if (file == null || file.Length == 0)
            {
                return BadRequest("请选择要上传的Excel文件");
            }

            // 检查文件类型
            var allowedExtensions = new[] { ".xlsx", ".zip" };
            var fileExtension = System.IO.Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new
                {
                    message = "只允许上传Excel文件(.xlsx)或压缩包(.zip)"
                });
            }

            // 检查文件大小（限制为20MB）
            if (file.Length > 20 * 1024 * 1024)
            {

                return Ok(new
                {
                    message = "文件大小超过限制（20MB）",
                    fileName = file.FileName,
                    fileSize = file.Length / 1024 + " KB"
                });
            }

            // 调用服务上传文件
            var result = await _uploadService.SaveExcelFileAsync(file);

            if (result.Success)
            {
                Stopwatch Ps = Stopwatch.StartNew();
                // 若是Excel文件，则读取数据，zip文件则解压缩 
                if (result.FilePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    await _uploadService.ReadExcelData2Txt(result.FilePath);
                    Debug.WriteLine($"读取Excel数据耗时: {Ps.ElapsedMilliseconds} ms");
                }
                else
                {
                    // 如果是zip文件，则解压缩
                    await _uploadService.UnZipFile(result.FilePath);
                    Debug.WriteLine($"解压缩文件耗时: {Ps.ElapsedMilliseconds} ms");
                }

                return Ok(new
                {
                    message = "文件上传成功",
                    filefullPath = result.FilePath,
                    fileName = System.IO.Path.GetFileName(result.FilePath),
                    filePath = result.FilePath.Substring(0, result.FilePath.LastIndexOf('\\')),
                    // 不要文件后缀名
                    Select = System.IO.Path.GetFileNameWithoutExtension(result.FilePath)
                });
            }
            else
            {
                return StatusCode(500, new { message = "文件上传失败", error = result.ErrorMessage });
            }
        }
    }


    public class FailData
    {
        public string FailItme { get; set; } = string.Empty;
        public string FailPath { get; set; } = string.Empty;
        public DateTime FailDate { get; set; }
        public bool? IsHigh { get; set; }
    }


    public class FailList
    {
        //   public int FailCount { get; set { if (value < 0) { throw new ArgumentOutOfRangeException("FailCount", "不能设置负数"); } else { FailCount = value; } } }
        public int FailCount
        {
            get; set;
            //get { return FailCount; }
            //set
            //{
            //    if (Data.Count > 0)
            //    {
            //        FailCount = Data.Count;
            //    }
            //}

        }
        public List<FailData> Data { get; set; }

        public string StrMes { get; set; }
        public List<GroupData> GroupDatas { get; set; }
        public List<PassReport> PassReport { get; set; }


    }



    public class GroupData
    {
        public string itmeName { get; set; } = string.Empty;
        public int itmeCount { get; set; }
    }


    public class PassReport
    {/// <summary>
     /// 测试项目名称
     /// </summary>
        public string TestName { get; set; }

        public List<double> PassData { get; set; }
    }
}
