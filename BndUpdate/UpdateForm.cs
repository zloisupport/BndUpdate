﻿
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SevenZipExtractor;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BndUpdate
{
    public partial class UpdateForm : Form
    {
        public UpdateForm()
        {
            InitializeComponent();
            IsAvailableNetworkActive();

            SystemLanguage systemLanguage = new SystemLanguage();

            if (systemLanguage.getSystemLanguage() == "ru-RU")
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("ru-RU");
            }
            else
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");
            }
        }
        private static readonly string tempPath = Path.GetTempPath() + "BND_MOD";
        private static readonly string guanjiaUrl = "https://pan.baidu.com/disk/cmsdata?platform=guanjia";
        private static readonly string gitUrl = @"https://api.github.com/repos/zloisupport/BaiduNetDiskTranslation/releases";
        private static bool statusNetwork = false;

        private string[] userAgents = {
            "Mozilla/5.0 (iPad; CPU OS 8_4 like Mac OS X) AppleWebKit/600.1.4 (KHTML, like Gecko) Version/8.0 Mobile/12H143 Safari/600.1.4",
            "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/44.0.2403.157 Safari/537.36",
            "Mozilla/5.0 (Linux; U; Android 4.0.4; en-us; KFJWA Build/IMM76D) AppleWebKit/537.36 (KHTML, like Gecko) Silk/3.68 like Chrome/39.0.2171.93 Safari/537.36",
            "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) coc_coc_browser/50.0.125 Chrome/44.0.2403.125 Safari/537.36"
        };

        enum BND
        {
            BaiduNetDisk,
            BaiduNetDiskRU,
            BaiduNetDiskEn
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {

            _ = CloseForm();
            Application.Exit();
        }

        private bool checkingFile(string fileName)
        {
            if (File.Exists(fileName))
                return true;
            return false;
        }

        private string getFileVerison()
        {
            if (!checkingFile(@"../BaiduNetdisk.exe"))
            {
                listBox1.Items.Insert(0, $"BaiduNetdisk.exe not found");
                return null;
                
            }
            var versionInfo = FileVersionInfo.GetVersionInfo(@"../BaiduNetdisk.exe");
            string version = versionInfo.ProductVersion;
            return version;
        }


        private List<string> getGitVersion()
        {
            //==========================================
            List<string> gitData = new List<string>();
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.DefaultConnectionLimit = 9999;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls;
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
            var line = "";
            Random rnd = new Random();
            if (ConnectionTimeout()) {
                 WebClient webClient = new WebClient();
                 webClient.Headers.Add("user-agent",userAgents[rnd.Next(0,3)]);
                 line = webClient.DownloadString(new Uri(gitUrl));
                 JsonWriteFile(line, tempPath+"//Config//gitRelease.json");
            }
            else
            {

            }

            if (!checkingFile(tempPath + "//Config//gitRelease.json"))
                Application.Exit();
                line = File.ReadAllText(tempPath + "//Config//gitRelease.json");


            // line = new StreamReader("release.json").ReadToEnd();
            dynamic parsedJson =JsonConvert.DeserializeObject(line);
            var output = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            JArray jsonArray = JArray.Parse(output);
            dynamic categories = JObject.Parse(jsonArray[0].ToString());


            string tagName = categories["tag_name"];
            gitData.Add(tagName);
            gitData.Add((string)categories["assets"][0]["browser_download_url"]);
            gitData.Add((string)categories["assets"][1]["browser_download_url"]);
            gitData.Add((string)categories["assets"][0]["size"]);
            return gitData;
        }


        public void JsonWriteFile(string line,string name)
        {
            dynamic parsedJson =  JsonConvert.DeserializeObject(line);
            var data = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            File.Delete($@"{name}");
            File.WriteAllText($@"{name}", data);
        }


        private List<string> getBaiduVersion(string Version)
        {
            var guanjia1 = string.Empty;
            if (ConnectionTimeout() == true) { 
                WebClient webClient = new WebClient();
                Random rnd = new Random();
                webClient.Headers.Add("user-agent", userAgents[rnd.Next(0, 3)]);
                guanjia1 = webClient.DownloadString(guanjiaUrl);
                JsonWriteFile(guanjia1, tempPath + "//Config//BaiduRelease.json");
            }
            guanjia1 = new StreamReader(tempPath + "//Config//BaiduRelease.json").ReadToEnd();
            List<string> baiduData = new List<string>();

            JObject version = JObject.Parse(guanjia1);
         
            var jsonVerions = from p in version["list"] select (string)p["version"];

            string baiduServerVersion = string.Empty;

            string baiduServerVersion1 = string.Empty;
            int x = 0;
            string urlFile = null;
            string size;
            foreach (var item in jsonVerions)
            {
                baiduServerVersion = Regex.Match(item, @"\d.*").Value;
                if (baiduServerVersion == Version)
                {
                    urlFile = (string)version["list"][x]["url"];
                    size = (string)version["list"][x]["size"];
                    baiduServerVersion1 = baiduServerVersion;
                    baiduData.Add(baiduServerVersion1);
                    baiduData.Add(urlFile);
                    baiduData.Add(size);
                }
                x++;
            }

            return baiduData;
        }


        #region ConvertKBtoMB
        static double ConvertBytesToMegabytes(long bytes)
        {
            return (bytes / 1024f) / 1024f;
        }

        static double ConvertKilobytesToMegabytes(long kilobytes)
        {
            return kilobytes / 1024f;
        }
        #endregion


       // private async Task DownloadingAsyncFile(Dictionary<string,string>link)
        private async Task DownloadingAsyncFile(string link ,string name)
        {
          

            using (WebClient client = new WebClient())
            {
               
                client.DownloadFileCompleted += (s, e) =>
                {
                    progressBar1.Value = 0;
                };

                client.DownloadProgressChanged += (s, e) =>
                {
                    double TotalReceive = (short)ConvertBytesToMegabytes(e.TotalBytesToReceive);
                    double MbReceive = (short)ConvertBytesToMegabytes(e.BytesReceived);
                    double percentage = e.ProgressPercentage;
                    progressBar1.Value = e.ProgressPercentage;
                    toolStripStatusLabel1.Text = $"{percentage} % {MbReceive} mb / { TotalReceive} mb:";
                };
                 await client.DownloadFileTaskAsync(new Uri(link), $@"{tempPath}//{name}");
               
            }
        }

        private bool GetResponseCode(string link)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
              
                if (response.StatusCode == HttpStatusCode.OK)
                {
                  
                    return true;
                }
              
            }
            catch
            {
                return false;
            }
            return true;
        
        }

        private async Task GetAllUrl()
        {
            btnClearCache.Enabled = false;
            string getInstFile = getFileVerison();
            var getGitTag = getGitVersion()[0];
            var getGitAssetEn = getGitVersion()[1];
            var getGitAssetRu = getGitVersion()[2];
            var getLocale = getGitVersion()[2];
            var getBaiduFile = getBaiduVersion(getGitTag);
            long getBaiduFileSize = Convert.ToInt64(getBaiduFile[2].Replace("M", string.Empty)) - 2;
            long getGitFileSize = (long)ConvertBytesToMegabytes(Convert.ToInt64(getGitVersion()[3]));

            string tempFileVerision = GetTempFileVersion();

            if (tempFileVerision == getGitTag)
            {
                GetFilesHash();
            }
            var link = new Dictionary<string, string>(){
               {"BaiduNetDisk",getBaiduFile[1]},
               {"BaiduNetDiskRU",getGitAssetRu},
               {"BaiduNetDiskEn",getGitAssetEn}
            };
            long fileSizeibMbs;
            foreach (var lin in link)
            {

           
                if (!File.Exists($"{tempPath}//{lin.Key}"))
                {

                  
                    if (GetResponseCode(lin.Value))
                        await DownloadingAsyncFile(lin.Value, lin.Key);
                    else
                    {
                        toolStripStatusLabel1.Text = "Server Error Code:Baidu - 403 Forbidden.";
                        toolStripStatusLabel1.ForeColor = System.Drawing.Color.Red;
                        return;
                    }

                }
                else
                {
                    FileInfo fileinf = new FileInfo($"{tempPath}//{lin.Key}");

                    fileSizeibMbs = (long)ConvertBytesToMegabytes(Convert.ToInt64(fileinf.Length));

                    if (lin.Key == "BaiduNetDisk" && fileSizeibMbs < getBaiduFileSize || fileSizeibMbs < getGitFileSize)
                    {
                        if (GetResponseCode(lin.Value))
                        {
                            Console.WriteLine(GetResponseCode(lin.Value).ToString());
                            await DownloadingAsyncFile(lin.Value, lin.Key);
                        }
                        else
                        {
                            toolStripStatusLabel1.ForeColor = System.Drawing.Color.Red;
                            toolStripStatusLabel1.Text = "Server Error Code :Git - 403 Forbidden. ";
                            return;
                        }
                    }

                }
            }
                await ExtractFileAsync();
                DeleteTelemetryFiles();
                GenerateHashFile(getGitTag);
                btnGo.Enabled = true;
                listBox1.Items.Clear();
                listBox1.Items.Insert(0, getGitTag);
                btnCancel.Text = $"{Language.strings.EXIT}";
                btnClearCache.Enabled = true;

        }
        private static string GetMD5HashFromFile(string fileName)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(fileName))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                }
            }
        }

        private async Task ExtractFileAsync()
        {
            var format = new Dictionary<string, SevenZipFormat>(){
               {"BaiduNetDisk",SevenZipFormat.Nsis},
               {"BaiduNetDiskRU",SevenZipFormat.Zip},
               {"BaiduNetDiskEn",SevenZipFormat.Zip}
            };

            if (rbtnRus.Checked == true)
            {
                format.Remove("BaiduNetDiskEn");
            }
            else
            {
                format.Remove("BaiduNetDiskRU");
            }
            progressBar1.Style = ProgressBarStyle.Marquee;

            toolStripStatusLabel1.Text = $"{Language.strings.UNPACKING}";



            foreach (var i in format)
            {
                await Unzip(i.Key, i.Value);
            }
            progressBar1.Style = ProgressBarStyle.Blocks;


            toolStripStatusLabel1.Text = $"{Language.strings.WAIT_USER_INPUT}";          
            

            toolStripStatusLabel1.Text = $"{Language.strings.UPDATE_INSTALLED}";

        }
        private void Compare(string gitVersion)
        {

            string getInstFile = getFileVerison();
            var getSerFile = getGitVersion()[0];
            var getSerFile1 = getGitVersion()[1];
            var getBaiduFile = getBaiduVersion(getSerFile);

            var fileInfo = new FileInfo("../BaiduNetdisk.exe");
          
            if (fileInfo.Length <= 0 ){
                listBox1.Items.Insert(0, $"Bad file");
                listBox2.Items.Insert(0, $"Bad file");
            }
            else
            {
                var serverVersion = new Version(getSerFile);
                var installedVersion = new Version(getInstFile);
                var result = serverVersion.CompareTo(installedVersion);

                //listBox2.Items.Insert(0, $"{getBaiduFile}");
                //listBox1.Items.Insert(0, $"{getInstFile}");

                if (result > 0)
                {
                    listBox2.Items.Insert(0, $"{serverVersion}");
                    listBox1.Items.Insert(0, $"{getInstFile}");
                    btnGo.Text = $"{Language.strings.DOWNLOAD}";
                    btnCancel.Text = $"{Language.strings.EXIT}";
                }
                else if (result < 0)
                {
                    listBox2.Items.Insert(0, $"{serverVersion}");
                    listBox1.Items.Insert(0, $"{getInstFile}");

                    btnCancel.Text = $"{Language.strings.EXIT}";
                }
                else
                {
                    listBox1.Items.Insert(0, $"{getInstFile}");
                    //btnGo.Visible = false;


                    listBox2.Items.Insert(0, $"{Language.strings.ACTUAL_VERSION}");
                }
            }


        }

        private void Form1_Load(object sender, EventArgs e)
        {


            setLocale();
            if (!statusNetwork)
            {
               

                toolStripStatusLabel1.ForeColor = System.Drawing.Color.Red;
                toolStripStatusLabel1.Text = $"{Language.strings.NOT_CONNECTION}";
                // logList.Text = $"{DateTime.Now.ToString("HH:mm")} Not connected to the internet";
                btnGo.Enabled = false;
                btnClearCache.Enabled = false;
                rbtnEng.Enabled = false;
                rbtnRus.Enabled = false;
                listBox1.Enabled = false;
                listBox2.Enabled = false;
                //MessageBox.Show("Not connection", "Warning! ", MessageBoxButtons.OK, MessageBoxIcon.Error);
               // System.Environment.Exit(0);

            }
            else
            {
           
                //MessageBox.Show(Directory.GetCurrentDirectory().ToString());
                //var path = Directory.GetCurrentDirectory();

                //Environment.CurrentDirectory = path+ "ModUpdate";
                if (!File.Exists("../BaiduNetdisk.exe"))
                {
                    toolStripStatusLabel1.ForeColor = System.Drawing.Color.Red;
                    toolStripStatusLabel1.Text =$"{Language.strings.FILE_MISSING} : BaiduNetdisk.exe";
                    btnGo.Enabled = false;
                    btnClearCache.Enabled = false;
                    btnCancel.Text = $"{Language.strings.EXIT}";
                    btnCancel.ForeColor = System.Drawing.Color.Green;
                    rbtnEng.Enabled = false;
                    rbtnRus.Enabled = false;
                    listBox1.Enabled = false;
                    listBox2.Enabled = false;
                  //  logList.Items.Insert(0,"File : BaiduNetdisk.exe not found ");
                    return;
                    //MessageBox.Show("File : BaiduNetdisk.exe not found ", "Warning! ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //System.Environment.Exit(0);
                }
                InitDirectory();
                Compare(getGitVersion()[0]);
                if (IsDirectoryEmpty())
                {
                    btnClearCache.Enabled = false;
                }
            }

            // GenerateHashFile("test version");

        }
        private static string GetTempFileVersion()
        {
            if (File.Exists($"{tempPath}//System.json")) { 
                string alldata = File.ReadAllText($"{tempPath}//System.json");
                JObject data = JObject.Parse(alldata);
                string version = (string)data["Version"];
                return version;
            }
            return null;
        }
        private static bool GetFilesHash()
        {

            List<string> hashList = new List<string>();

            foreach (int file in Enum.GetValues(typeof(BND)))
            {
                string _file = Enum.GetName(typeof(BND), file);
                if (File.Exists($"{tempPath}//{_file}"))
                {
                    string fileHash = GetMD5HashFromFile($"{tempPath}//" + _file);
                    hashList.Add(fileHash);
                }
                else
                {
                    hashList.Add(null);
                }
            }

            string alldata = File.ReadAllText($"{tempPath}//System.json");
            JObject version = JObject.Parse(alldata);

            List<string> bndhash = new List<string>() { (string)version["Hashsum"]["hash"][0],
                                                        (string)version["Hashsum"]["hash"][1],
                                                        (string)version["Hashsum"]["hash"][2] };
          
            foreach (int file in Enum.GetValues(typeof(BND)))
            {
                string _file = Enum.GetName(typeof(BND), file);
                if (File.Exists($"{tempPath}//{_file}"))
                {
                    if (bndhash[file] != hashList[file])
                    {
                        File.Delete($"{tempPath}//{_file}");
                    }
                };
            }

            return false;
        }

        private Task GenerateTimeOutFile()
        {
            if (statusNetwork)
            {
                Timeout timeout = new Timeout();
                timeout.LatestConnection = DateTime.Now.ToString("MM/dd/yyyy HH:mm");
                var data = JsonConvert.SerializeObject(timeout, Formatting.Indented);
                File.WriteAllText($"{tempPath}//Config//TimeOut.json", data);
            }

            return Task.FromResult<object>(null);
        }

        private static bool ConnectionTimeout()
        {
            if(!File.Exists($"{tempPath}//Config//TimeOut.json")){
                return true;
            }
            string alldata = File.ReadAllText($"{tempPath}//Config//TimeOut.json");
            JObject version = JObject.Parse(alldata);

            DateTime createdDate = Convert.ToDateTime(version["LatestConnection"]);
            DateTime dt = Convert.ToDateTime(DateTime.Now.ToString("MM/dd/yyyy HH:mm"));
            int hourSave = dt.Hour;
            int hourCurrent = createdDate.Hour;

            int hourRes = hourCurrent - hourSave;
            if(hourRes <= 1 && hourRes < 0 || hourRes >=1 )
            {
                return true;
            }
            return false;
        }


        static void GenerateHashFile(string version)
        {
            List<string> hashList = new List<string>();
            List<string> fileList = new List<string>();
            foreach (int file in Enum.GetValues(typeof(BND)))
            {
                string _file = Enum.GetName(typeof(BND), file);
                if (File.Exists($"{tempPath}//" + _file))
                {
                    string fileHash = GetMD5HashFromFile($"{tempPath}//" + _file);
                    hashList.Add(fileHash);
                    fileList.Add(_file);
                };
            }

            HashSum hashSum = new HashSum();
            hashSum.name = fileList;
            hashSum.hash = hashList;

            Setting setting = new Setting
            {
                Title = "BaiduNetDisk",
                Version = version,
                CreatedDate = DateTime.Now.ToString("dd MMM yyyy HH:mm"),
                Hashsum = hashSum
            };
            var data = JsonConvert.SerializeObject(setting, Formatting.Indented);
            File.WriteAllText($"{tempPath}//System.json",data);
        }

        private void DeleteTelemetryFiles()
        {
            List<string> delFiles = new List<string>()
            {
                $"..//uninst.exe",
                $"..//autoDiagnoseUpdate.exe",
                $"..//DuiEngine license.txt",
                $"..//HelpUtility.exe",
                $"..//kernelUpdate.exe",
                $"..//libtorrent_license.txt",
                //$"..//pepflashplayer.dll",
                $"..//AppProperty.xml",
                $"..//module//BrowserEngine",
                $"..//module//KernelCom",
                $"..//browserres",
                $"..//AutoUpdate",
                $"..//YunUtilityService.exe",
                $"..//$TEMP",
                $"..//$PLUGINSDIR",

            };


            foreach (var file in delFiles)
            {

                if (File.Exists(file))
                {
                    File.Delete(file);
                }
                else
                {
                    if (Directory.Exists(file))
                    {
                        Directory.Delete(file, true);
                    }
                }
            }
        }
        async Task Unzip(string name, SevenZipFormat sevenZipFormat)
        {
            string filePath = $"{tempPath}//" + name;
            FileInfo file = new FileInfo(filePath);
            await Task.Run(() =>
            {
                if (file.Length != 0 && file.Length > 9004460)
                {
                    FileStream fileStream = File.OpenRead(filePath);
                    using (ArchiveFile archiveFile = new ArchiveFile(fileStream, sevenZipFormat))
                    {
                        archiveFile.Extract("../", true);
                        fileStream.Close();
                    }
                }
            });
        }

        private void TerminateProcess(string Name)
        {
            Process[] workers = Process.GetProcessesByName(Name);
            foreach (Process worker in workers)
            {
                worker.Kill();
                worker.WaitForExit();
                worker.Dispose();
            }
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            btnGo.Enabled = false;
            TerminateProcess("YunDetectService");
            TerminateProcess("BaiduNetdisk");
            _ = GetAllUrl();

        }
        public  void setLocale() {
            btnCancel.Text = $"{Language.strings.EXIT}";
            btnGo.Text = $"{Language.strings.GO}";
            groupBox3.Text = $"{Language.strings.LANGUAGE}";
            btnClearCache.Text = $"{Language.strings.CLEAR_CACHE}";
            groupBox1.Text = $"{Language.strings.YOU_APP_VERSION}";
            groupBox2.Text = $"{Language.strings.SERVER_APP_VERSION}";
            rbtnEng.Text = $"{Language.strings.LANG_EN}";
            rbtnRus.Text = $"{Language.strings.LANG_RU}";
            statusStrip1.Text = $"{Language.strings.WAIT_USER_INPUT}";
            toolStripStatusLabel1.Text = $"{Language.strings.WAIT_USER_INPUT}";
        }

        public static bool IsAvailableNetworkActive()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://ya.ru");
                request.Timeout = 5000;
                request.Credentials = CredentialCache.DefaultNetworkCredentials;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK) {
                    statusNetwork = true;
                    return true;
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }
        private void InitDirectory()
        {
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory($"{tempPath}//Config");
            }
        }
        private void btnClearCache_Click(object sender, EventArgs e)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(tempPath);
             if(Directory.Exists(tempPath)){
                foreach (FileInfo file in directoryInfo.GetFiles())
                {
               
                    file.Delete();
                }
            }
            toolStripStatusLabel1.Text = $"{Language.strings.CLEAR_SUCCESS}";
            btnClearCache.Enabled = false;
        }

        public bool IsDirectoryEmpty()
        {
          return !Directory.EnumerateFileSystemEntries(tempPath).Any();
        }

        async Task CloseForm()
        {
           await GenerateTimeOutFile();
        }

        private void UpdateForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _ = CloseForm();
        }
    }
}
