
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SevenZipExtractor;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BndUpdate
{
    public partial class UpdateForm : Form
    {
        public UpdateForm()
        {
            InitializeComponent();
        }

        private static string guanjiaUrl = "https://pan.baidu.com/disk/cmsdata?platform=guanjia";
        private static readonly string gitUrl = @"https://api.github.com/repos/zloisupport/BaiduNetDiskTranslation/releases";

        private void btnCancel_Click(object sender, EventArgs e)
        {
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
                listBox1.Items.Insert(0, $"BaiduNetdisk.exe Not Found");
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

            WebClient webClient = new WebClient();
            webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; " +
                                  "Windows NT 5.2; .NET CLR 1.0.3705;)");
            var line = "";
            try
            {
                line = webClient.DownloadString(gitUrl);
            }
            catch (System.Net.WebException)
            {
                MessageBox.Show($"403 Forbidden\n Please launch later (1 hour)", "Server time out", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //  throw new ArgumentOutOfRangeException("Server time out", e);
                Environment.Exit(0);
            }
            //var line = new StreamReader("release.json").ReadToEnd();


            dynamic parsedJson = JsonConvert.DeserializeObject(line);
            var output = JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            JArray jsonArray = JArray.Parse(output);
            dynamic categories = JObject.Parse(jsonArray[0].ToString());
            string data1 = categories["assets"][0]["browser_download_url"];
            string locale = "eng";
            if (rbtnRus.Checked)
            {
                data1 = categories["assets"][1]["browser_download_url"];
                locale = "rus";
            }

            string tagName = categories["tag_name"];
            string createdAt = categories["created_at"];
            gitData.Add(tagName);
            gitData.Add(data1);
            gitData.Add(locale);
            return gitData;
        }
        private List<string> getBaiduVersion(string Version)
        {
            WebClient webClient = new WebClient();
            var guanjia1 = webClient.DownloadString(guanjiaUrl);
            List<string> baiduData = new List<string>();
            //var guanjia1 = new StreamReader("guanjia1.json").ReadToEnd();

            JObject version = JObject.Parse(guanjia1);
            // Query Select list all version search Version
            var jsonVerions = from p in version["list"] select (string)p["version"];

            string baiduServerVersion = "";
            string baiduServerVersion1 = "";
            int x = 0;
            string urlFile = null;
            foreach (var item in jsonVerions)
            {
                baiduServerVersion = Regex.Match(item, @"\d.*").Value;
                if (baiduServerVersion == Version)
                {
                    urlFile = (string)version["list"][x]["url"];
                    baiduServerVersion1 = baiduServerVersion;
                    baiduData.Add(baiduServerVersion1);
                    baiduData.Add(urlFile);
                }
                x++;
            }

            return baiduData;
        }


        private void ClearTemp()
        {

        }

        private void UpdateApp(string Url, string name, SevenZipFormat sevenZipFormat)
        {

            WebClient webClient = new WebClient();
            if (!Directory.Exists("Temp"))
            {
                Directory.CreateDirectory("Temp");
            }
            webClient.DownloadFileAsync(new Uri(Url), $@"Temp\\{name}");
            webClient.DownloadProgressChanged += (s, e) =>
            {
                double percentage = e.ProgressPercentage;
                progressBar1.Value = e.ProgressPercentage;
                toolStripStatusLabel1.Text = $"{percentage} % {e.BytesReceived} bytes / { e.TotalBytesToReceive} bytes:";
                btnGo.Enabled = false;
            };
            webClient.DownloadFileCompleted += (s, e) =>
            {
                //   toolStripProgressBar1.Visible = false;
                progressBar1.Value = 0;
                toolStripStatusLabel1.Text = "Success";
                btnGo.Enabled = true;
                _ = Unzip($@"Temp\\{name}", sevenZipFormat);
            };
        }

        private async void GetAllUrl()
        {
            string getInstFile = getFileVerison();
            var getSerFile = getGitVersion()[0];
            var getSerFile1 = getGitVersion()[1];
            var getLocale = getGitVersion()[2];
            var getBaiduFile = getBaiduVersion(getSerFile);

            if (checkingFile($"Temp\\BaiduNetDisk{getSerFile}.exe"))
            {
                btnGo.Enabled = false;
                progressBar1.Style = ProgressBarStyle.Marquee;
                toolStripStatusLabel1.Text = "Unpacking ..";
                await Unzip($@"Temp\\BaiduNetDisk{getSerFile}.exe", SevenZipFormat.Nsis);
                progressBar1.Style = ProgressBarStyle.Continuous;
                toolStripStatusLabel1.Text = "Success!";
                btnCancel.Text = "Exit";
            }
            else
            {
                UpdateApp(getBaiduFile[1], $"BaiduNetDisk{getSerFile}.exe", SevenZipFormat.Nsis);
            }



            if (checkingFile($"Temp\\BaiduNetDisk{getSerFile}{getLocale}.zip"))
            {
                btnGo.Enabled = false;
                toolStripStatusLabel1.Text = "Unpacking ..";
                progressBar1.Style = ProgressBarStyle.Marquee;
                await Unzip($@"Temp\\BaiduNetDisk{getSerFile}{getLocale}.zip", SevenZipFormat.Zip);
                progressBar1.Style = ProgressBarStyle.Continuous;
                toolStripStatusLabel1.Text = "Success!";
                btnCancel.Text = "Exit";
            }
            else
            {
                UpdateApp(getSerFile1, $"BaiduNetDisk{getSerFile}{getLocale}.zip", SevenZipFormat.Zip);
            }
            DeleteTelemetryFiles();


        }
        //
        private void Compare(string gitVersion)
        {
            string getInstFile = getFileVerison();
            var getSerFile = getGitVersion()[0];
            var getSerFile1 = getGitVersion()[1];
            var getBaiduFile = getBaiduVersion(getSerFile);
            if (!File.Exists("../BaiduNetdisk.exe"))
            {

                MessageBox.Show("File not found BaiduNetdisk.exe", "Error 01", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var serverVersion = new Version(getSerFile);

            var installedVersion = new Version(getInstFile);


            var result = serverVersion.CompareTo(installedVersion);



            //listBox2.Items.Insert(0, $"{getBaiduFile}");
            //listBox1.Items.Insert(0, $"{getInstFile}");

            if (result > 0)
            {
                listBox2.Items.Insert(0, $"{serverVersion}");
                listBox1.Items.Insert(0, $"{getInstFile}");
                btnGo.Text = "Update";
            }
            else if (result < 0)
            {
                listBox2.Items.Insert(0, $"{serverVersion}");
                listBox1.Items.Insert(0, $"{getInstFile}");
                //listBox1.Items.Insert(0, $"{getSerFile1}");
                //listBox2.Items.Insert(1, $"{getBaiduFile[1]}");
                btnGo.Text = "Update";

            }
            else
            {
                listBox1.Items.Insert(0, $"{getInstFile}");
                btnGo.Visible = false;
                listBox2.Items.Insert(0, $"You have the current version");
            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {

            Compare(getGitVersion()[0]);
            var connection = IsAvailableNetworkActive();
            if (!connection)
            {
                toolStripStatusLabel1.Text = "Not connection!";
                btnGo.Enabled = false;
            }
            if (IsDirectoryEmpty())
            {
                btnClearCache.Enabled = false;
            }

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
        async Task<bool> Unzip(string name, SevenZipFormat sevenZipFormat)
        {
            await Task.Run(() =>
            {
                FileStream fileStream = File.OpenRead(name);
                var archiveFile = new ArchiveFile(fileStream, sevenZipFormat);
                archiveFile.Extract("../", true);
                fileStream.Close();

                return true;
            });

            return false;
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
            TerminateProcess("YunDetectService");
            GetAllUrl();
        }

        public static bool IsAvailableNetworkActive()
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://ya.ru");
                request.Timeout = 5000;
                request.Credentials = CredentialCache.DefaultNetworkCredentials;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        private void btnClearCache_Click(object sender, EventArgs e)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo("Temp");

            foreach(FileInfo file in directoryInfo.GetFiles())
            {
                file.Delete();
            }
            toolStripStatusLabel1.Text = "Success!";
            btnClearCache.Enabled = false;
        }

        //public static bool DirIsEmpty(string path="Temp")
        //{
        //    int num = Directory.GetFiles(path).Length + Directory.GetDirectories(path).Length;
        //    return num == 0;
        //}

        public bool IsDirectoryEmpty(string path = "Temp")
        {
            return !Directory.EnumerateFileSystemEntries(path).Any();
        }
    }
}
