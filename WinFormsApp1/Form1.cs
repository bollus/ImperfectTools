
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using ImperfectTools.entity;
using System.Security.Cryptography;
using WinAuth;
using fastJSON;
using ImperfectTools.common;
using Application = System.Windows.Forms.Application;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        
        List<GoogleAuthenticatorCache> googleAuthenticatorCaches = new();
        GoogleAuthenticator googleAuthenticator = new();
        bool checkOpenSSL = false;
        byte[] imageBytes = null;
        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
            progressBar1.Hide();
            button11.Hide();
            label20.Hide();
            label23.Hide();
            label21.Hide();
            label25.Hide();
            label22.Hide();
            label24.Hide();
            richTextBox1.Hide();
            richTextBox2.Hide();
            label26.Hide();
            maskedTextBox10.Hide();
            label29.Hide();
            label31.Hide();
            // 异步检查更新
            new Thread(() => {
                 //网络文件地址
                 string file_url = @"http://opmain.ca0me1.top:8088/api/public/dl/eAO2_Vkr";
                 //实例化唯一文件标识
                 Uri file_uri = new(file_url);
                //返回文件流
                HttpClient httpClient = new();
                HttpResponseMessage response = httpClient.GetAsync(file_uri).Result;
                Stream stream = response.Content.ReadAsStream();
                 //实例化文件内容
                 StreamReader file_content = new StreamReader(stream);
                 //读取文件内容
                 string file_content_str = file_content.ReadToEnd();

                // 获取当前版本号
                string version = Application.ProductVersion;
                if (!version.Equals(file_content_str))
                {
                    Control owner = new();
                    owner = this;
                    while (owner.Parent != null)
                    {
                        owner = owner.Parent;
                    } 
                    DialogResult dialogResult = MessageBox.Show(owner,"当前版本：" + version + "\n最新版本：" + file_content_str, "有新版本可更新", MessageBoxButtons.OKCancel);
                    if (dialogResult == DialogResult.OK)
                    {
                        Process.Start(new ProcessStartInfo("http://opmain.ca0me1.top:8088/api/public/dl/MKRwq5eF") { UseShellExecute = true });
                    }
                    else if (dialogResult == DialogResult.Cancel)
                    {
                        return;
                    }
                }
            }).Start();
            new Thread(() => {
                ReadGoogleAuth();
            }).Start();
        }

        private void ReadGoogleAuth()
        {
            button17.Enabled = false;
            button17.Text = "等待加载";
            string path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\.imperfectTools";
            if(!Directory.Exists(@path))
            {
                Directory.CreateDirectory(@path);
            }
            if (!File.Exists(@path + "/google-auth.json"))
            {
                // File.Create(@path + "/google-auth.json").Close();
                FileTools.WriteAllText(@path + "/google-auth.json", "", Encoding.UTF8);  
            }
            
            string googleAutherJson = FileTools.ReadAllText(@path + "/google-auth.json", Encoding.UTF8);
            GoogleAuthers googleAuthers = new();
            if (!string.IsNullOrEmpty(googleAutherJson))
            {
                //googleAutherJsonArray 转换成对象
                googleAuthers = JSON.ToObject<GoogleAuthers>(googleAutherJson);
                foreach (GoogleAuther googleAuther in googleAuthers.Auths)
                {
                    if (!string.IsNullOrEmpty(googleAuther.Secret))
                    {
                        string itemName = googleAuther.Name + " - (" + googleAuther.Account + ")";
                        if (!comboBox1.Items.Contains(itemName))
                        {
                            GoogleAuthenticator googleAuthenticator = new();
                            googleAuthenticator.Enroll(googleAuther.Secret);
                            googleAuthenticatorCaches.Add(new GoogleAuthenticatorCache(googleAuther.Name + " - (" + googleAuther.Account + ")", googleAuthenticator));
                            comboBox1.Items.Add(itemName);
                        }
                    }
                }
            }
            button17.Text = "导入令牌";
            button17.Enabled = true;
        }

        private static void p_Process_Exited(object sender, EventArgs e)
        {
            Debug.WriteLine("命令执行完毕");
        }

        void p_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Process p = (Process)sender;
            if (p == null)
                return;
            Console.WriteLine(e.Data);
            MessageBox.Show("失败：" + e.Data);
        }

        void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Debug.WriteLine(456);
            if (!String.IsNullOrEmpty(e.Data))
            {
                this.BeginInvoke(new Action(() =>
                {
                    // 修改label7的文本
                    Debug.WriteLine(e);
                    label7.Text = e.Data;
                    Debug.WriteLine(e.Data);
                }));
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            string buildToolsFolder = "";
            string keyStoreFile = "";
            string keyStorePass = "";
            if (maskedTextBox1.Text == null || maskedTextBox1.Text == "")
            {
                MessageBox.Show("请选择BuildTools目录");
                return;
            }
            else if (maskedTextBox2.Text == null || maskedTextBox2.Text == "")
            {
                MessageBox.Show("请选择Keystore文件");
                return;
            }
            else if (maskedTextBox3.Text == null || maskedTextBox3.Text == "")
            {
                MessageBox.Show("请填写Keystore Pass");
                return;
            }
            else
            {
                buildToolsFolder = maskedTextBox1.Text;
                keyStoreFile = maskedTextBox2.Text;
                keyStorePass = maskedTextBox3.Text;
            }
            OpenFileDialog dialog = new()
            {
                RestoreDirectory = true,
                Multiselect = true,//该值确定是否可以选择多个文件
                Title = "请选择文件",
                Filter = "安卓应用包(*.apk)|*.apk"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                //执行控制台命令 C:\Users\luigi\AppData\Local\Android\Sdk\build-tools\33.0.0>zipalign.exe -v path path.dq.apk
                string path = dialog.FileName;
                string filename = dialog.SafeFileName;
                string pathNoFileName = path.Replace(filename, "");
                string filenameNoFix = filename.Replace(".apk", "");
                string dqFileName = "";
                string firmFileName = "";
                Debug.WriteLine("=================================\n\n[获取到待处理文件]\n 文件名：" + filename + "\n 文件路径：" + path + "\n\n=================================");
                button2.Hide();
                progressBar1.Value = 0;
                progressBar1.Show();
                progressBar1.Refresh();
                progressBar1.PerformStep();
                progressBar1.Refresh();
                using (Process p = new())
                {
                    p.StartInfo = new ProcessStartInfo("cmd.exe")
                    {
                        RedirectStandardInput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        WorkingDirectory = @buildToolsFolder,
                        // event handlers for output & error
                        CreateNoWindow = true
                    };
                    p.ErrorDataReceived += p_ErrorDataReceived;
                    p.EnableRaisingEvents = true;
                    p.Exited += new EventHandler(p_Process_Exited);
                    progressBar1.PerformStep();
                    progressBar1.Refresh();
                    p.Start();
                    // 执行对齐命令
                    dqFileName = filenameNoFix + "_dq_" + timeStamp + ".apk";
                    p.StandardInput.WriteLineAsync("zipalign.exe -f -v 4 " + path + " " + pathNoFileName + dqFileName + " & exit");
                    p.WaitForExit();
                    p.Close();
                    progressBar1.PerformStep();
                    progressBar1.Refresh();

                    // 执行签名命令
                    p.Start();
                    firmFileName = filenameNoFix + "_firm_" + timeStamp + ".apk";
                    p.StandardInput.WriteLineAsync("apksigner sign --force-stamp-overwrite --ks " + keyStoreFile + " --ks-pass pass:" + keyStorePass + " --in " + pathNoFileName + dqFileName + " --out " + pathNoFileName + firmFileName + " & exit");
                    p.WaitForExit();
                    // string rst = p.StandardOutput.ReadToEnd();
                    // Debug.WriteLine(rst);
                    p.Close();
                    progressBar1.PerformStep();
                    progressBar1.Refresh();
                }
                if (File.Exists(@pathNoFileName + firmFileName))
                {
                    DialogResult mesSelection = MessageBox.Show(path + ".firm.apk", "加固签名完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    File.Delete(@pathNoFileName + dqFileName); // 删除对齐包
                    progressBar1.Refresh();
                    MessageBox.Show("请检查BuildTools、Keystore文件及Keystore Pass。", "加固签名失败！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                progressBar1.Hide();
                button2.Show();
                return;

                //结束程序
                //System.Environment.Exit(0);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string Selected_Path = dialog.SelectedPath;
                if (!File.Exists(@Selected_Path + "\\zipalign.exe"))
                {
                    MessageBox.Show("该目录不包含 ZipAlign(zipalign.exe) 可执行程序，请重新选择");
                }
                else if (!File.Exists(@Selected_Path + "\\apksigner.bat"))
                {
                    MessageBox.Show("该目录不包含 ApkSigner(apksigner.bat) 可执行批处理文件，请重新选择");
                }
                else
                {
                    maskedTextBox1.Text = Selected_Path;
                }

            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                RestoreDirectory = true,
                Multiselect = false,//该值确定是否可以选择多个文件
                Title = "请选择keystore文件",
                Filter = "KeyStore文件(*.keystore)|*.keystore"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                maskedTextBox2.Text = dialog.FileName;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button5_Click_2(object sender, EventArgs e)
        {
            try
            {
                using Process p = new();
                p.StartInfo = new ProcessStartInfo("cmd.exe")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = "C:\\",
                    // event handlers for output & error
                    CreateNoWindow = true
                };
                //p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
                p.ErrorDataReceived += p_ErrorDataReceived;
                p.EnableRaisingEvents = true;
                p.Exited += new EventHandler(p_Process_Exited);
                p.Start();
                p.StandardInput.WriteLineAsync("openssl version & exit");
                p.WaitForExit();
                string rst = p.StandardOutput.ReadToEnd();
                p.Close();
                // 判断OpenSSL是否安装
                if (rst != null && rst.Contains("Library"))
                {
                    string[] rstArray = rst.Split("\n");
                    foreach (string s in rstArray)
                    {
                        if (s.Contains("Library"))
                        {
                            checkOpenSSL = true;
                            label7.Text = string.Concat("OpenSSL已安装，当前版本为：", s.Split("Library: ")[1].Split(")")[0]);
                            label7.ForeColor = Color.FromArgb(((int)(((byte)(103)))), ((int)(((byte)(194)))), ((int)(((byte)(52))))); ;
                            label7.Font = new Font(label7.Font.Name, 9, FontStyle.Bold);
                            return;
                        }
                    }
                }
                else 
                {
                    label7.Text = string.Concat("OpenSSL未安装或未正确配置环境变量");
                    label7.ForeColor = Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(108)))), ((int)(((byte)(108))))); ;
                    label7.Font = new Font(label7.Font.Name, 9, FontStyle.Bold);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void button6_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                RestoreDirectory = true,
                Multiselect = false,//该值确定是否可以选择多个文件
                Title = "请选择证书文件",
                Filter = "(公钥证书文件)|*.cer;*.pem"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                maskedTextBox4.Text = dialog.FileName;
            }
        }
        private void button7_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                RestoreDirectory = true,
                Multiselect = false,//该值确定是否可以选择多个文件
                Title = "请选择证书文件",
                Filter = "(私钥证书文件)|*.key;*.pem"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                maskedTextBox5.Text = dialog.FileName;
            }
        }
        private void button9_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                maskedTextBox7.Text = dialog.SelectedPath;
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (!checkOpenSSL)
            {
                MessageBox.Show("请先检测OpenSSL环境！");
                return;
            }
            string publicCert = "";
            string privateCertKey = "";
            string pfxPassword = "";
            string exportPath = "";
            if (maskedTextBox4.Text == null || maskedTextBox4.Text == "")
            {
                MessageBox.Show("请选择公钥证书文件");
                return;
            }
            else if (maskedTextBox5.Text == null || maskedTextBox5.Text == "")
            {
                MessageBox.Show("请选择私钥证书文件");
                return;
            }
            else if (maskedTextBox6.Text == null || maskedTextBox6.Text == "")
            {
                MessageBox.Show("请填写Pfx导出密码");
                return;
            }
            else if (maskedTextBox7.Text == null || maskedTextBox7.Text == "")
            {
                MessageBox.Show("请选择导出路径");
                return;
            }
            else
            {
                publicCert = maskedTextBox4.Text;
                privateCertKey = maskedTextBox5.Text;
                pfxPassword = maskedTextBox6.Text;
                exportPath = maskedTextBox7.Text;
            }

            try
            {
                string timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
                using (Process p = new())
                {
                    p.StartInfo = new ProcessStartInfo("cmd.exe")
                    {
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        WorkingDirectory = @exportPath,
                        // event handlers for output & error
                        CreateNoWindow = true
                    };
                    p.ErrorDataReceived += p_ErrorDataReceived;
                    p.EnableRaisingEvents = true;
                    p.Exited += new EventHandler(p_Process_Exited);
                    p.Start();
                    string[] strArry = publicCert.Split("\\");
                    if (strArry != null)
                    {
                        string certName = strArry[strArry.Length - 1].Split(".")[0];
                        Debug.WriteLine(certName);
                        Debug.WriteLine(String.Concat("执行命令：", "openssl pkcs12 -export -out " + exportPath + "\\" + certName + ".pfx -inkey " + privateCertKey + " -in " + publicCert + " -password pass:" + pfxPassword + " & exit"));
                        p.StandardInput.WriteLineAsync("openssl pkcs12 -export -out " + exportPath + "\\" + certName + ".pfx -inkey " + privateCertKey + " -in " + publicCert + " -password pass:" + pfxPassword + " & exit");
                        p.WaitForExit();
                        p.Close();
                        string exportFullPath = exportPath + "\\" + certName + ".pfx";
                        Debug.WriteLine(exportFullPath);
                        if (File.Exists(@exportFullPath))
                        {
                            MessageBox.Show(exportPath + "\\" + certName + ".pfx", "证书转换成功！", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("请检查原始证书文件！", "证书转换失败！", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        return;
                    }
                    else
                    {
                        MessageBox.Show("当前公钥证书文件不支持！");
                        return;
                    }


                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            panel4.Show();
            panel3.Hide();
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void tabPage3_Click_1(object sender, EventArgs e)
        {

        }

        private void button10_Click(object sender, EventArgs e)
        {
            string methodType;
            // 判断类型
            if (radioButton1.Checked)
            {
                // 头像
                methodType = "sjtx";
            }
            else if (radioButton2.Checked)
            {
                // 壁纸
                methodType = "sjbz";
            }
            else
            {
                MessageBox.Show("请选择图片类型！");
                return;
            }
            // 判断输出端
            string r_method;
            if (radioButton3.Checked)
            {
                // PC端
                r_method = "pc";
            }
            else if (radioButton4.Checked)
            {
                // 移动端
                r_method = "mobile";
            }
            else
            {
                MessageBox.Show("请选择输出端类型！");
                return;
            }
            // 判断输出风格
            string r_lx;
            switch (methodType)
            {
                case "sjtx":
                    r_lx = radioButton6.Checked ? "a1" :
                        radioButton5.Checked ? "b1" :
                        radioButton7.Checked ? "c1" :
                        radioButton8.Checked ? "c2" :
                        radioButton9.Checked ? "c3" :
                        "c1";
                    break;
                case "sjbz":
                    r_lx = radioButton14.Checked ? "meizi" :
                        radioButton13.Checked ? "dongman" :
                        radioButton12.Checked ? "fengjing" :
                        radioButton11.Checked ? "suiji" :
                        "";
                    break;
                default:
                    MessageBox.Show("图片类型错误！");
                    return;
            }

            // 调用HTTP GET请求
            HttpClient httpClient = new();
            Uri url = new("http://api.btstu.cn/" + methodType + "/api.php?format=images" +
                "&method=" + r_method +
                "&lx=" + r_lx);
            HttpResponseMessage response = httpClient.GetAsync(url).Result;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream data = response.Content.ReadAsStreamAsync().Result;
                byte[] bytes = GetImageByPath(data);
                MemoryStream ms = new(bytes, 0, bytes.Length);
                //设置图片
                Image returnImage = Image.FromStream(ms);
                pictureBox2.Image = returnImage;
                imageBytes = bytes;
                button11.Show();
                button10.Text = "立即保存";
                button11.Click -= new EventHandler(button10_Click);
                button11.Click += new EventHandler(button10_Click);
                button10.Click -= new EventHandler(button10_Click);
                button10.Click -= new EventHandler(saveImagesAsync);
                button10.Click += new EventHandler(saveImagesAsync);
                return;
            }
            else
            {
                MessageBox.Show("接口异常，请求失败！");
            }
        }

        private async void saveImagesAsync(object sender, EventArgs e)
        {
            if (maskedTextBox8.Text == "")
            {
                MessageBox.Show("给图片取个名字吧！");
                return;
            }
            else
            {
                string fileName = maskedTextBox8.Text;
                FolderBrowserDialog dialog = new();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string savePath = dialog.SelectedPath;
                    bool existsZip = File.Exists(@savePath + "\\" + fileName + ".zip");
                    bool existsRar = File.Exists(@savePath + "\\" + fileName + ".rar");
                    using (FileStream fs = new(@savePath + "\\" + fileName + ".jpg", FileMode.Create))
                    {
                        await fs.WriteAsync(imageBytes);
                    }
                    if (existsZip || existsRar)
                    {
                        // 加密操作
                        using (Process p = new())
                        {
                            p.StartInfo = new ProcessStartInfo("cmd.exe")
                            {
                                RedirectStandardInput = true,
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                WorkingDirectory = @savePath,
                                // event handlers for output & error
                                CreateNoWindow = true
                            };
                            //p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
                            p.ErrorDataReceived += p_ErrorDataReceived;
                            p.EnableRaisingEvents = true;
                            p.Exited += new EventHandler(p_Process_Exited);
                            p.Start();
                            string extensionName = existsZip ? ".zip" : ".rar";
                            p.StandardInput.WriteLine("copy /b " + fileName + ".jpg+" + fileName + extensionName + " " + fileName + ".jpg > nul & exit");
                            p.WaitForExit();
                            p.Close();
                            if (File.Exists(@savePath + "\\" + fileName + ".jpg"))
                            {
                                MessageBox.Show("路径为：" + savePath + "\\" + fileName + ".jpg", "图片保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                MessageBox.Show("出现系统异常 (-3)", "图片保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }

                        }
                    }
                    else
                    {
                        if (File.Exists(@savePath + "\\" + fileName + ".jpg"))
                        {
                            MessageBox.Show("路径为：" + savePath + "\\" + fileName + ".jpg", "图片保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("出现系统异常 (-3)", "图片保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    return;
                }
            }

        }

        // 根据HTTP图片路径获取字节
        private static byte[] GetImageByPath(Stream responseStream)
        {
            byte[] bytes;
            using (Stream stream = responseStream)
            {
                using (MemoryStream mstream = new MemoryStream())
                {
                    int count = 0;
                    byte[] buffer = new byte[1024];
                    int readNum = 0;
                    while ((readNum = stream.Read(buffer, 0, 1024)) > 0)
                    {
                        count = count + readNum;
                        mstream.Write(buffer, 0, readNum);
                    }
                    mstream.Position = 0;
                    using (BinaryReader br = new BinaryReader(mstream))
                    {
                        bytes = br.ReadBytes(count);
                    }
                }
            }
            return bytes;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            panel4.Hide();
            panel3.Show();
        }


        private static TencentResponse? CheckDomainByTencent(string domain)
        {
            try
            {
                HttpClient httpClient = new();
                Uri uri = new("http://api.btstu.cn/qqsafe/api.php?domain=" + domain);
                HttpResponseMessage response = httpClient.GetAsync(uri).Result;
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string resultStream = response.Content.ReadAsStringAsync().Result;
                    TencentResponse tencentResonpse = JsonConvert.DeserializeObject<TencentResponse>(resultStream)!;
                    if (tencentResonpse != null)
                    {
                        return tencentResonpse;
                    }
                    else
                    {
                        MessageBox.Show("接口返回数据错误！");
                        return null;
                    }
                }
                else
                {
                    MessageBox.Show("接口异常，请求失败！");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        private WechatResponse? CheckDomainByWechat(string domain)
        {
            try
            {
                HttpClient httpClient = new();
                string token = maskedTextBox10.Text;
                Uri uri = new("https://api.new.urlzt.com/api/vx?token=" + token + "&format=json&url=" + domain);
                HttpResponseMessage response = httpClient.GetAsync(uri).Result;
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string resultStream = response.Content.ReadAsStringAsync().Result;
                    WechatResponse wechatResponse = JsonConvert.DeserializeObject<WechatResponse>(resultStream)!;
                    if (wechatResponse != null)
                    {
                        return wechatResponse;
                    }
                    else
                    {
                        MessageBox.Show("接口返回数据错误！");
                        return null;
                    }
                }
                else
                {
                    MessageBox.Show("接口异常，请求失败！");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            if (maskedTextBox9.Text == "")
            {
                MessageBox.Show("请输入域名！");
                return;
            }
            else
            { 
                if(!Regex.IsMatch(maskedTextBox9.Text, "^((?!-)[A-Za-z0-9-]{1,63}(?<!-)\\.)+[A-Za-z]{2,6}$"))
                {
                    MessageBox.Show("域名格式不正确");
                    return;
                }
            }
            bool tencentChecker = checkBox1.Checked;
            bool wechatChecker = checkBox2.Checked;
            if (!tencentChecker && !wechatChecker)
            {
                MessageBox.Show("请至少选择一种检测类型");
                return;
            }
            if (wechatChecker)
            {
                if (maskedTextBox10.Text == "")
                {
                    MessageBox.Show("微信类型为收费接口，请填写极强域名(urlzt.com)的Token密钥");
                    return;
                }
            }
            label20.Hide();
            label23.Hide();
            label21.Hide();
            label25.Hide();
            label22.Hide();
            label24.Hide();
            richTextBox1.Hide();
            richTextBox2.Hide();
            button12.Enabled = false;
            button12.Text = "检测中...";
            TencentResponse tencentResonpse = null;
            WechatResponse wechatResponse = null;
            if (tencentChecker)
            {
                tencentResonpse = CheckDomainByTencent(maskedTextBox9.Text);
            }
            if (wechatChecker)
            {
                wechatResponse = CheckDomainByWechat(maskedTextBox9.Text);
            }
            CheckDomainResponse checkDomainResponse = new()
            {
                Tencent = tencentResonpse,
                Wechat = wechatResponse
            };
            if (tencentChecker)
            {
                if (checkDomainResponse.Tencent != null)
                {
                    label20.Show();
                    label21.Show();
                    label20.Text = checkDomainResponse.Tencent.Msg;
                    if (checkDomainResponse.Tencent.Code != 200)
                    {
                        /**if (checkDomainResponse.Tencent.Msg.Contains("非官方页")) 
                        {
                            label20.Text = "域名正常";
                            label20.ForeColor = Color.Green;
                        }
                        else
                        {
                            // label20.Text = "域名拦截";
                            label20.ForeColor = Color.Red;
                        }
                        */
                        label20.ForeColor = Color.Red;
                        label22.Show();
                        richTextBox1.Show();
                        richTextBox1.Text = checkDomainResponse.Tencent.Wording;
                    }
                    else
                    {
                        // label20.Text = "域名正常";
                        label22.Hide();
                        richTextBox1.Hide();
                        label20.ForeColor = Color.Green;
                    }
                }
                else
                {
                    MessageBox.Show("接口异常，请求失败！");
                    return;
                }
            }
            if (wechatChecker)
            {
                if (checkDomainResponse.Wechat != null && checkDomainResponse.Wechat.Code != null)
                {
                    label23.Show();
                    label25.Show();
                    if (checkDomainResponse.Wechat.Code == 200)
                    {
                        label23.Text = "域名正常";
                        label24.Hide();
                        richTextBox2.Hide();
                        label23.ForeColor = Color.Green;
                    }
                    else if (checkDomainResponse.Wechat.Code == -2)
                    {
                        label23.Text = "请求无效";
                        label23.ForeColor = Color.Blue;
                        label24.Show();
                        richTextBox2.Show();
                        richTextBox2.Text = checkDomainResponse.Wechat.Msg;
                    }
                    else
                    {
                        label23.Text = "域名拦截";
                        label23.ForeColor = Color.Red;
                        label24.Show();
                        richTextBox2.Show();
                        richTextBox2.Text = checkDomainResponse.Wechat.Msg;
                    }
                }
                else
                {
                    MessageBox.Show("接口异常，请求失败！");
                }
            }
            button12.Enabled = true;
            button12.Text = "立即检测";
            return;
        }

        // Unicode转String
        public static string Unicode2String(string source)
        {
            return new Regex(@"\\u([0-9A-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled).Replace(
                source, x => string.Empty + Convert.ToChar(Convert.ToUInt16(x.Result("$1"), 16)));
        }

        private void label21_Click(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                label26.Show();
                maskedTextBox10.Show();
            }
            else
            {
                label26.Hide();
                maskedTextBox10.Hide();
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            try
            {
                button13.Enabled = false;
                using Process p = new();
                p.StartInfo = new ProcessStartInfo("cmd.exe")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = "C:\\",
                    // event handlers for output & error
                    CreateNoWindow = true
                };
                //p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
                p.ErrorDataReceived += p_ErrorDataReceived;
                p.EnableRaisingEvents = true;
                p.Exited += new EventHandler(p_Process_Exited);
                p.Start();
                string command = "ipconfig/flushdns & exit";
                p.StandardInput.WriteLineAsync(command);
                p.WaitForExit();
                string rst = p.StandardOutput.ReadToEnd();
                p.Close();
                // 判断OpenSSL是否安装
                if (rst != null)
                {
                    rst = rst.Split("ipconfig/flushdns & exit")[1].Replace("\r\n", " ");
                    MessageBox.Show(rst);
                }
                else
                {
                    MessageBox.Show("操作失败！");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            button13.Enabled = true;
            return;
        }

        private void TableIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPage5)
            {
                new Thread(() => SearchCNIP()).Start();
                new Thread(() => SearchCN2OSIP()).Start();
                new Thread(() => GetEthernetInfo()).Start();
            }
        }

        // 获取以太网信息
        private void GetEthernetInfo()
        {
            // 获取网卡信息
            NetworkInterface[] nic = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface networkInterface in nic)
            {
                // 获取以太网信息
                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    label39.Text = networkInterface.Name;
                    label37.Text = (networkInterface.Speed / 1000000).ToString() + "(Mbps)";
                    return;
                }
            }
        }


        // 查询国内IP
        private void SearchCNIP()
        {
            try
            {
                if (label32.Text != "")
                {
                    label35.Text = "刷新中...";
                    Thread.Sleep(800);
                }
                else
                {
                    label35.Text = "查询中...";
                }

                CnIPSearchResponse? cnIPSearchResponse = new();
                HttpClient httpClient = new();
                Uri CN_IP_URL = new("https://whois.pconline.com.cn/ipJson.jsp?json=true");
                HttpResponseMessage response = httpClient.GetAsync(CN_IP_URL).Result;
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string isoContent = "";
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    using (StreamReader sr = new(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("gbk")))
                    {
                        isoContent = sr.ReadToEnd();
                    }
                    cnIPSearchResponse = JsonConvert.DeserializeObject<CnIPSearchResponse>(isoContent);
                    label32.Text = cnIPSearchResponse.Ip;
                    label35.Text = cnIPSearchResponse.Addr;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }
        private void SearchCN2OSIP()
        {
            try
            {
                if (label33.Text != "")
                {
                    label36.Text = "刷新中...";
                    Thread.Sleep(800);
                }
                else
                {
                    label36.Text = "查询中...";
                }
                CN2OSSearchResponse? n2OSSearchResponse = new();
                HttpClient httpClient = new();
                Uri CN_IP_URL = new("http://ip-api.com/json");
                HttpResponseMessage response = httpClient.GetAsync(CN_IP_URL).Result;
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    n2OSSearchResponse = JsonConvert.DeserializeObject<CN2OSSearchResponse>(result);
                    label33.Text = n2OSSearchResponse.Query;
                    label36.Text = n2OSSearchResponse.Country;
                }
            }
            catch (Exception ex)
            {
                label33.Text = "";
                label36.Text = "查询失败";
                //MessageBox.Show(ex.Message);
                return;
            }
        }

        private void CopyDown(object sender, EventArgs e)
        {
            if (((Label)sender).Text.Equals("请选择令牌"))
            {
                return;
            }
            // 复制当前点击组件的文本
            Clipboard.SetText(((Label)sender).Text);
            // 桌面通知
            notifyIcon1.ShowBalloonTip(1000, ((Label)sender).Text, "已复制到剪贴板", ToolTipIcon.Info);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            if (maskedTextBox11.Text == "")
            {
                MessageBox.Show("请输入需要查询的IP地址");
                return;
            }
            try
            {
                CnIPSearchResponse? cnIPSearchResponse = new();
                HttpClient httpClient = new();
                string ip = maskedTextBox11.Text;
                Uri IP_Check = new("https://whois.pconline.com.cn/ipJson.jsp?json=true&ip=" + ip);
                HttpResponseMessage response = httpClient.GetAsync(IP_Check).Result;
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string isoContent = "";
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                    using (StreamReader sr = new(response.Content.ReadAsStreamAsync().Result, Encoding.GetEncoding("gbk")))
                    {
                        isoContent = sr.ReadToEnd();
                    }
                    cnIPSearchResponse = JsonConvert.DeserializeObject<CnIPSearchResponse>(isoContent);
                    label29.Show();
                    label31.Show();
                    label29.Text = cnIPSearchResponse.Ip;
                    label31.Text = cnIPSearchResponse.Addr != "" ? cnIPSearchResponse.Addr : cnIPSearchResponse.Err;
                }
            }
            catch (Exception ex)
            {
                label29.Text = "";
                label31.Text = "查询失败";
                //MessageBox.Show(ex.Message);
                return;
            }
        }

        private void button15_Click(object sender, EventArgs e)
        {
            try
            {
                button15.Text = "重启中...";
                button15.Enabled = false;
                using Process p = new();
                p.StartInfo = new ProcessStartInfo("cmd.exe")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = "C:\\",
                    // event handlers for output & error
                    CreateNoWindow = true
                };
                //p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
                p.ErrorDataReceived += p_ErrorDataReceived;
                p.EnableRaisingEvents = true;
                p.Exited += new EventHandler(p_Process_Exited);
                p.Start();

                string command = "netsh interface set interface \"" + label39.Text + "\" disable & exit";
                p.StandardInput.WriteLineAsync(command);
                p.WaitForExit();
                p.Close();
                p.Start();
                command = "netsh interface set interface \"" + label39.Text + "\" enable & exit";
                p.StandardInput.WriteLineAsync(command);
                p.WaitForExit();
                p.Close();
                button15.Enabled = true;
                button15.Text = "重启网络";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return;
        }

        private void RefreshNetInfo(object sender, EventArgs e)
        {
            GetEthernetInfo();       
        }

        private void button16_Click(object sender, EventArgs e)
        {
            GetGoogleAuthCode();
        }
        private void GetGoogleAuthCode()
        {
            if (googleAuthenticatorCaches.Count > 0)
            {

                #pragma warning disable CS8600 
                GoogleAuthenticatorCache authenticatorCache = googleAuthenticatorCaches.Find(delegate(GoogleAuthenticatorCache cache)
                {
                    return comboBox1.SelectedItem switch
                    {
                        null => false,
                        _ => cache.Issuer == comboBox1.SelectedItem.ToString()
                    };
                });
                #pragma warning restore CS8600 
                if (authenticatorCache != null && authenticatorCache.GoogleAuthenticator != null)
                {
                    googleAuthenticator = authenticatorCache.GoogleAuthenticator;
                    label40.Text = googleAuthenticator.CurrentCode;
                    timer1.Start();
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            GetGoogleAuthCode();
        }

        private readonly static DateTime _epoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private long GetCurrentCounter()
        {
            return GetCurrentCounter(DateTime.UtcNow, _epoch, 30);
        }

        private long GetCurrentCounter(DateTime now, DateTime epoch, int timeStep)
        {
            return (long)(now - epoch).TotalSeconds / timeStep;
        }

        public string GeneratePINAtInterval(string accountSecretKey, long counter, int digits = 6)
        {
            return GenerateHashedCode(accountSecretKey, counter, digits);
        }

        internal string GenerateHashedCode(string secret, long iterationNumber, int digits = 6)
        {
            byte[] key = Encoding.UTF8.GetBytes(secret);
            return GenerateHashedCode(key, iterationNumber, digits);
        }

        internal string GenerateHashedCode(byte[] key, long iterationNumber, int digits = 6)
        {
            byte[] counter = BitConverter.GetBytes(iterationNumber);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counter);
            }

            HMACSHA1 hmac = new(key);

            byte[] hash = hmac.ComputeHash(counter);

            int offset = hash[hash.Length - 1] & 0xf;

            // Convert the 4 bytes into an integer, ignoring the sign.
            int binary =
              ((hash[offset] & 0x7f) << 24)
              | (hash[offset + 1] << 16)
              | (hash[offset + 2] << 8)
              | (hash[offset + 3]);

            int password = binary % (int)Math.Pow(10, digits);
            return password.ToString(new string('0', digits));
        }

        public string[] GetCurrentPINs(string accountSecretKey, TimeSpan timeTolerance)
        {
            List<string> codes = new();
            long iterationCounter = GetCurrentCounter();
            int iterationOffset = 0;

            if (timeTolerance.TotalSeconds > 30)
            {
                iterationOffset = Convert.ToInt32(timeTolerance.TotalSeconds / 30.00);
            }

            long iterationStart = iterationCounter - iterationOffset;
            long iterationEnd = iterationCounter + iterationOffset;

            for (long counter = iterationStart; counter <= iterationEnd; counter++)
            {
                codes.Add(GeneratePINAtInterval(accountSecretKey, counter));
            }
            foreach(string code in codes){
                Debug.WriteLine(code);
            }
            return codes.ToArray();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (googleAuthenticator != null && progressBar2.Visible == true)
            {
                int time = (int)(googleAuthenticator.ServerTime / 1000L) % 30;
                progressBar2.Value = time + 1;
                if (time == 0)
                {
                    GetGoogleAuthCode();
                }
            }
        }

        private async void button17_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                RestoreDirectory = true,
                Multiselect = false,//该值确定是否可以选择多个文件
                Filter = "(令牌json文件)|*.json"
            };
            label40.Text = "导入令牌中...";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                new Thread(() => {
                    string path = dialog.FileName;
                    string authContent = File.ReadAllText(path, Encoding.UTF8);
                    List<string> authContentList = JSON.ToObject<List<string>>(authContent);
                    GoogleAuthers googleAuthers = new();
                    List<GoogleAuther> auths = new();
                    foreach (string content in authContentList)
                    {
                        // content = "otpauth://totp/Aliyun%3A%E9%A3%98%E6%9F%94003?algorithm=SHA1&digits=6&issuer=Aliyun&secret=ERXEEPXT6XYK2VN2TTIFHQAKO3XZQN5EWQ252X7OU53TUAWIT3KGQWLTXW47VSLX"
                        string decodedUrl = Uri.UnescapeDataString(content);
                        string authInfo = decodedUrl.Replace("otpauth://totp/", "");
                        string[] authInfoArray = authInfo.Split("?");
                        string account = authInfoArray[0];
                        string[] issuerSplit = authInfoArray[1].Split("issuer=");
                        string name = (issuerSplit.Length < 2) ? name = "" : authInfoArray[1].Split("issuer=")[1].Split("&")[0];
                        string secret = authInfoArray[1].Split("secret=")[1].Split("&")[0];
                        GoogleAuther googleAuther = new()
                        {
                            Name = name,
                            Account = account,
                            Secret = secret
                        };
                        auths.Add(googleAuther);
                    }
                    googleAuthers.Auths = auths;
                    string googleAuthersJson = JSON.ToJSON(googleAuthers);
                    string configPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\.imperfectTools";
                    FileTools.WriteAllText(configPath + "\\google-auth.json", googleAuthersJson, Encoding.UTF8);             
                    ReadGoogleAuth();
                    if (comboBox1.SelectedItem != null){
                        GetGoogleAuthCode();
                    }
                    else 
                    {
                        label40.Text = "请选择令牌";
                    }
                    MessageBox.Show("导入令牌成功", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }).Start();
            }
            else
            {
                label40.Text = "请选择令牌";
            }
        }
    }
}