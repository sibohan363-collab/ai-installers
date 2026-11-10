// DeepSeek Harness 一键安装：从 DeepSeek 官网下载 → 静默安装 → 桌面图标 → 打开
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

static class Program
{
    [DllImport("user32.dll")] static extern bool SetProcessDPIAware();

    [STAThread]
    static void Main(string[] args)
    {
        try { SetProcessDPIAware(); } catch { }
        ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // TLS 1.2
        Application.EnableVisualStyles();
        Application.Run(new MainForm(Array.IndexOf(args, "--test") >= 0));
    }
}

class MainForm : Form
{
    const string DownloadUrl = "https://download.deepseek.com/desktop/dsh-latest-windows-x64.exe";
    const string OfficialPage = "https://www.deepseek.com/harness/";

    static readonly Color Bg = Color.FromArgb(14, 16, 21);
    static readonly Color Card = Color.FromArgb(23, 26, 33);
    static readonly Color Muted = Color.FromArgb(139, 147, 161);
    static readonly Color Green = Color.FromArgb(74, 222, 128);
    static readonly Color Blue = Color.FromArgb(96, 165, 250);
    static readonly Color Amber = Color.FromArgb(245, 181, 68);
    static readonly Color Red = Color.FromArgb(248, 113, 113);

    readonly bool test;
    readonly Label[] dots = new Label[4];
    readonly Label[] names = new Label[4];
    readonly Label status;
    readonly Button btn;
    float s;

    static string AppExe
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\DeepSeek Harness\DeepSeek Harness.exe"); }
    }

    public MainForm(bool test)
    {
        this.test = test;
        using (var g = CreateGraphics()) s = g.DpiX / 96f;
        Text = "DeepSeek Harness 一键安装";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Bg;
        ForeColor = Color.White;
        ClientSize = new Size(P(520), P(600));
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        Add(new Label { Text = "DeepSeek Harness 一键安装", Font = F(18, FontStyle.Bold), AutoSize = true, Location = new Point(P(28), P(24)) });
        Add(new Label { Text = "不需要梯子。全部自动完成，最后登录 DeepSeek 账号即可。", Font = F(10), ForeColor = Muted, AutoSize = true, Location = new Point(P(30), P(68)) });

        string[] stepNames = { "检查网络", "从 DeepSeek 官网下载", "安装并放到桌面", "打开并登录" };
        var card = new Panel { BackColor = Card, Location = new Point(P(28), P(104)), Size = new Size(P(464), P(196)) };
        Controls.Add(card);
        for (int i = 0; i < 4; i++)
        {
            dots[i] = new Label { Text = "○", Font = F(14), ForeColor = Muted, AutoSize = true, Location = new Point(P(18), P(16 + i * 44)) };
            names[i] = new Label { Text = stepNames[i], Font = F(11.5f), ForeColor = Muted, AutoSize = true, Location = new Point(P(52), P(20 + i * 44)) };
            card.Controls.Add(dots[i]); card.Controls.Add(names[i]);
        }

        status = new Label { Font = F(11), ForeColor = Blue, Location = new Point(P(30), P(316)), Size = new Size(P(460), P(70)) };
        Add(status);

        Add(new Label
        {
            Font = F(10), ForeColor = Muted, Location = new Point(P(30), P(390)), Size = new Size(P(460), P(140)),
            Text = "DeepSeek 官方 AI 助手，能直接处理电脑里的文件。\n" +
                   "做 PPT：选一个文件夹，把资料放进去，然后告诉它：\n" +
                   "「根据这个文件夹里的资料，做一份 15 页的 PPT，\n  风格简洁专业，保存成 .pptx」\n" +
                   "不满意就直接说哪里要改，它会接着修改。"
        });

        btn = new Button
        {
            Text = "请稍等…", Enabled = false, Font = F(11, FontStyle.Bold), FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(38, 43, 53), ForeColor = Color.White,
            Location = new Point(P(28), P(540)), Size = new Size(P(464), P(42))
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.Click += delegate { Close(); };
        Add(btn);

        Shown += delegate { new Thread(Run) { IsBackground = true }.Start(); };
    }

    int P(int v) { return (int)(v * s); }
    Font F(float size, FontStyle st = FontStyle.Regular) { return new Font("Microsoft YaHei UI", size, st); }
    void Add(Control c) { Controls.Add(c); }

    void Ui(Action a) { try { if (IsHandleCreated) BeginInvoke(a); } catch { } }
    void Say(string text, Color c) { Ui(delegate { status.Text = text; status.ForeColor = c; }); }
    void Step(int i, int state) // 0 等待 1 进行中 2 完成 3 失败
    {
        Ui(delegate
        {
            dots[i].Text = state == 2 ? "✓" : state == 3 ? "✕" : state == 1 ? "●" : "○";
            dots[i].ForeColor = state == 2 ? Green : state == 3 ? Red : state == 1 ? Blue : Muted;
            names[i].ForeColor = state == 0 ? Muted : Color.White;
        });
    }
    void Finish(string text, bool ok)
    {
        Ui(delegate
        {
            btn.Text = ok ? "完成，关闭" : "关闭";
            btn.Enabled = true;
            btn.BackColor = ok ? Color.FromArgb(22, 163, 74) : Color.FromArgb(38, 43, 53);
        });
        Say(text, ok ? Green : Red);
    }

    void Run()
    {
        try
        {
            bool installed = File.Exists(AppExe);

            // 1. 网络
            Step(0, 1);
            Say("正在连接 DeepSeek…", Blue);
            int tries = 0;
            while (!CanReach("https://www.deepseek.com/"))
            {
                tries++;
                Say("连不上 DeepSeek，请检查网络。连上后这里会自动继续。\n（已等待 " + (tries * 5) + " 秒）", Amber);
                Thread.Sleep(5000);
            }
            Step(0, 2);

            if (test)
            {
                Say("测试模式：网络正常，" + (installed ? "已安装" : "未安装") + "。没有下载和安装。", Green);
                Finish("测试完成。", true);
                return;
            }

            string setup = null;
            if (!installed)
            {
                // 2. 下载
                Step(1, 1);
                setup = Download();
                if (setup == null)
                {
                    Step(1, 3);
                    try { Process.Start(OfficialPage); } catch { }
                    Finish("下载失败。已打开 DeepSeek 官网，请点「Windows 下载」手动安装。", false);
                    return;
                }
                Step(1, 2);

                // 3. 安装
                Step(2, 1);
                Say("正在安装，大约 1 分钟…", Blue);
                RunAndWait(setup, "/S", 10 * 60);
                if (!File.Exists(AppExe))
                {
                    // 静默安装没成功，就打开正常的安装界面
                    Say("请在弹出的安装窗口里点「安装」。装好后这里会自动继续。", Amber);
                    RunAndWait(setup, "", 20 * 60);
                }
                if (!File.Exists(AppExe))
                {
                    Step(2, 3);
                    Finish("安装没有完成。请重新运行本程序，或到 deepseek.com/harness 手动下载。", false);
                    return;
                }
            }
            else
            {
                Step(1, 2);
                Step(2, 1);
            }
            MakeShortcut();
            Step(2, 2);
            try { if (setup != null) File.Delete(setup); } catch { }

            // 4. 打开
            Step(3, 1);
            Process.Start(AppExe);
            Step(3, 2);
            Finish("装好了！最后一步：在打开的窗口里按提示登录 DeepSeek 账号。\n以后双击桌面上的 DeepSeek Harness 图标就能用。", true);
        }
        catch (Exception ex)
        {
            Finish("出错了：" + ex.Message, false);
        }
    }

    static bool CanReach(string url)
    {
        try
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Timeout = 10000;
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
            using (req.GetResponse()) { }
            return true;
        }
        catch (WebException ex)
        {
            if (ex.Response != null) { ex.Response.Close(); return true; }
            return false;
        }
        catch { return false; }
    }

    string Download()
    {
        string file = Path.Combine(Path.GetTempPath(), "DeepSeek-Harness-Setup.exe");
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(DownloadUrl);
                req.Timeout = 60000;
                req.ReadWriteTimeout = 120000;
                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var input = resp.GetResponseStream())
                using (var output = File.Create(file))
                {
                    long total = resp.ContentLength, got = 0;
                    var buf = new byte[1 << 16];
                    int n; var last = DateTime.MinValue;
                    while ((n = input.Read(buf, 0, buf.Length)) > 0)
                    {
                        output.Write(buf, 0, n); got += n;
                        if ((DateTime.Now - last).TotalMilliseconds > 300)
                        {
                            last = DateTime.Now;
                            Say(total > 0
                                ? string.Format("正在下载… {0}%（{1:N0} / {2:N0} MB）", got * 100 / total, got / 1048576.0, total / 1048576.0)
                                : string.Format("正在下载… {0:N0} MB", got / 1048576.0), Blue);
                        }
                    }
                    if (total > 0 && got != total) throw new IOException("下载不完整");
                }
                if (new FileInfo(file).Length < 10 * 1048576) throw new IOException("文件太小");
                return file;
            }
            catch
            {
                Say("下载中断，正在重试（第 " + attempt + " 次）…", Amber);
                Thread.Sleep(3000);
            }
        }
        return null;
    }

    static void RunAndWait(string exe, string args, int timeoutSec)
    {
        try
        {
            using (var p = Process.Start(new ProcessStartInfo(exe, args) { UseShellExecute = true }))
                p.WaitForExit(timeoutSec * 1000);
        }
        catch { }
    }

    static void MakeShortcut()
    {
        try
        {
            string lnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "DeepSeek Harness.lnk");
            if (File.Exists(lnk)) return;
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            object sh = Activator.CreateInstance(t);
            object sc = t.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, sh, new object[] { lnk });
            Type st = sc.GetType();
            st.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { AppExe });
            st.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { Path.GetDirectoryName(AppExe) });
            st.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { AppExe + ",0" });
            st.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, sc, null);
        }
        catch { }
    }
}
