// Codex 一键安装：检查网络 → 从微软商店安装 ChatGPT 电脑版（含 Codex）→ 桌面图标 → 打开
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
    const string StoreId = "9PLM9XGG6VKS";
    const string StoreInstaller = "https://get.microsoft.com/installer/download/9PLM9XGG6VKS?cid=website_cta_psi";

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
    readonly Label tips;
    readonly Button btn;
    float s;

    public MainForm(bool test)
    {
        this.test = test;
        using (var g = CreateGraphics()) s = g.DpiX / 96f;
        Text = "Codex 一键安装";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Bg;
        ForeColor = Color.White;
        ClientSize = new Size(P(520), P(600));
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

        Add(new Label { Text = "Codex 一键安装", Font = F(20, FontStyle.Bold), AutoSize = true, Location = new Point(P(28), P(24)) });
        Add(new Label { Text = "全部自动完成，你只需要最后登录 ChatGPT 账号。", Font = F(10), ForeColor = Muted, AutoSize = true, Location = new Point(P(30), P(68)) });

        string[] stepNames = { "检查网络（梯子）", "安装 ChatGPT 电脑版（含 Codex）", "放到桌面", "打开并登录" };
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

        tips = new Label
        {
            Font = F(10), ForeColor = Muted, Location = new Point(P(30), P(390)), Size = new Size(P(460), P(140)),
            Text = "做 PPT：打开后切到「Work」或「Codex」模式\n" +
                   "1. 选一个文件夹，把论文、资料放进去\n" +
                   "2. 告诉它：「根据这个文件夹里的资料，做一份 15 页的 PPT，\n    风格简洁专业，保存成 .pptx」\n" +
                   "3. 不满意就直接说哪里要改，它会接着修改"
        };
        Add(tips);

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
            // 1. 网络
            Step(0, 1);
            Say("正在连接 OpenAI…", Blue);
            int tries = 0;
            while (!CanReach("https://chatgpt.com/") && !CanReach("https://openai.com/"))
            {
                tries++;
                Say("连不上 OpenAI。请先打开梯子，开好后这里会自动继续。\n（已等待 " + (tries * 5) + " 秒）", Amber);
                Thread.Sleep(5000);
            }
            Step(0, 2);

            // 2. 安装
            Step(1, 1);
            string app = FindApp();
            if (app == null && test)
            {
                Say("测试模式：网络正常，winget " + (Winget() != null ? "可用" : "不可用") + "，未执行安装。", Green);
                Step(1, 0);
                Finish("测试完成。", true);
                return;
            }
            if (app == null) app = InstallWithWinget();
            if (app == null) app = InstallWithStoreInstaller();
            if (app == null) app = InstallWithStorePage();
            if (app == null)
            {
                Step(1, 3);
                Finish("安装没有完成。请打开「Microsoft Store」，搜索 ChatGPT，点「获取」安装。", false);
                return;
            }
            Step(1, 2);

            // 3. 桌面图标
            Step(2, 1);
            Say("正在添加桌面图标…", Blue);
            Step(2, MakeShortcut(app) ? 2 : 3);

            // 4. 打开
            Step(3, 1);
            Process.Start("explorer.exe", "shell:AppsFolder\\" + app.Split('|')[1]);
            Step(3, 2);
            Finish("装好了！最后一步：在打开的窗口里登录你的 ChatGPT 账号。\n以后双击桌面上的 ChatGPT 图标就能用。", true);
        }
        catch (Exception ex)
        {
            Finish("出错了：" + ex.Message, false);
        }
    }

    // —— 网络 ——
    static bool CanReach(string url)
    {
        try
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Timeout = 8000;
            req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
            using (req.GetResponse()) { }
            return true;
        }
        catch (WebException ex)
        {
            // 收到任何 HTTP 回应（哪怕是 403）都说明网络能通
            if (ex.Response != null) { ex.Response.Close(); return true; }
            return false;
        }
        catch { return false; }
    }

    // —— 查找已安装的应用，返回 "名称|AppID" ——
    static string FindApp()
    {
        string o = RunHidden("powershell.exe",
            "-NoProfile -Command \"[Console]::OutputEncoding=[Text.Encoding]::UTF8; " +
            "$a = Get-StartApps | ? { $_.AppID -match 'OpenAI' -or $_.Name -match '^(ChatGPT|Codex)' } | Sort-Object { if ($_.Name -match 'ChatGPT') {0} else {1} } | Select-Object -First 1; " +
            "if ($a) { $a.Name + '|' + $a.AppID }\"", 60);
        o = (o ?? "").Trim();
        return o.Contains("|") ? o.Split('\n')[0].Trim() : null;
    }

    static string Winget()
    {
        string p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WindowsApps\winget.exe");
        return File.Exists(p) ? p : null;
    }

    string InstallWithWinget()
    {
        string w = Winget();
        if (w == null) return null;
        Say("正在从微软商店下载安装，大约需要 2～10 分钟，请不要关闭…", Blue);
        RunHidden(w, "install --id " + StoreId + " -s msstore --accept-package-agreements --accept-source-agreements --silent", 30 * 60);
        return WaitApp(60);
    }

    string InstallWithStoreInstaller()
    {
        try
        {
            Say("正在下载微软商店安装器…", Blue);
            string f = Path.Combine(Path.GetTempPath(), "ChatGPT-Installer.exe");
            using (var wc = new WebClient()) wc.DownloadFile(StoreInstaller, f);
            Say("正在安装。如果弹出安装窗口，请点「安装」。", Blue);
            Process.Start(f);
            return WaitApp(20 * 60);
        }
        catch { return null; }
    }

    string InstallWithStorePage()
    {
        try
        {
            Process.Start("ms-windows-store://pdp/?ProductId=" + StoreId);
            Say("已打开微软商店，请点「获取」或「安装」。装好后这里会自动继续。", Amber);
            return WaitApp(20 * 60);
        }
        catch { return null; }
    }

    static string WaitApp(int seconds)
    {
        var end = DateTime.Now.AddSeconds(seconds);
        do
        {
            string a = FindApp();
            if (a != null) return a;
            Thread.Sleep(5000);
        } while (DateTime.Now < end);
        return null;
    }

    // —— 桌面图标 ——
    static bool MakeShortcut(string app)
    {
        try
        {
            string[] parts = app.Split('|');
            string name = parts[0].Trim();
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, ' ');
            string lnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), name + ".lnk");
            string family = parts[1].Split('!')[0];
            string icon = RunHidden("powershell.exe",
                "-NoProfile -Command \"$p = Get-AppxPackage | ? { $_.PackageFamilyName -eq '" + family + "' } | Select-Object -First 1; " +
                "if ($p) { Get-ChildItem $p.InstallLocation -Filter *.exe -Recurse -EA SilentlyContinue | Sort-Object Length -Descending | Select-Object -First 1 -ExpandProperty FullName }\"", 60);
            icon = (icon ?? "").Trim();

            Type t = Type.GetTypeFromProgID("WScript.Shell");
            object sh = Activator.CreateInstance(t);
            object sc = t.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, sh, new object[] { lnk });
            Type st = sc.GetType();
            st.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe") });
            st.InvokeMember("Arguments", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { "shell:AppsFolder\\" + parts[1] });
            if (icon.Length > 0 && File.Exists(icon))
                st.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { icon + ",0" });
            st.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, sc, null);
            return File.Exists(lnk);
        }
        catch { return false; }
    }

    static string RunHidden(string exe, string args, int timeoutSec)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            using (var p = Process.Start(psi))
            {
                string output = "";
                var t = new Thread(delegate () { output = p.StandardOutput.ReadToEnd(); }) { IsBackground = true };
                t.Start();
                p.StandardError.ReadToEndAsync();
                if (!p.WaitForExit(timeoutSec * 1000)) { try { p.Kill(); } catch { } }
                t.Join(3000);
                return output;
            }
        }
        catch { return null; }
    }
}
