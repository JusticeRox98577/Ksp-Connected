using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace KspConnectedInstaller
{
    public class MainForm : Form
    {
        // ── wizard pages ─────────────────────────────────────────────────────
        private const int PAGE_WELCOME  = 0;
        private const int PAGE_PATH     = 1;
        private const int PAGE_OPTIONS  = 2;
        private const int PAGE_PROGRESS = 3;
        private const int PAGE_DONE     = 4;

        private int _page = PAGE_WELCOME;

        // ── controls ─────────────────────────────────────────────────────────
        private Panel  _header;
        private Label  _headerTitle;
        private Label  _headerSubtitle;
        private Panel  _content;
        private Panel  _footer;
        private Button _backBtn;
        private Button _nextBtn;
        private Button _cancelBtn;

        // Page panels
        private Panel _pageWelcome;
        private Panel _pagePath;
        private Panel _pageOptions;
        private Panel _pageProgress;
        private Panel _pageDone;

        // Path page
        private TextBox _kspPathBox;

        // Options page
        private CheckBox _serverCheck;
        private CheckBox _shortcutCheck;

        // Progress page
        private RichTextBox _logBox;
        private ProgressBar _progressBar;
        private Label       _progressLabel;

        // Done page
        private Label _doneTitle;
        private Label _doneMsg;

        private bool _busy;
        private bool _success;

        // ── colours ──────────────────────────────────────────────────────────
        private static readonly Color BgDark    = Color.FromArgb(24,  24,  36);
        private static readonly Color BgHeader  = Color.FromArgb(36,  36,  52);
        private static readonly Color BgContent = Color.White;
        private static readonly Color BgFooter  = Color.FromArgb(240, 240, 245);
        private static readonly Color BgInput   = Color.FromArgb(248, 248, 252);
        private static readonly Color Accent    = Color.FromArgb(255, 160,  30);
        private static readonly Color BtnNormal = Color.FromArgb(70,  130, 180);
        private static readonly Color BtnGreen  = Color.FromArgb(40,  160,  40);
        private static readonly Color BtnGray   = Color.FromArgb(160, 160, 170);
        private static readonly Color TextDark  = Color.FromArgb( 30,  30,  40);
        private static readonly Color TextMuted = Color.FromArgb(110, 110, 130);
        private static readonly Color LogGreen  = Color.FromArgb( 30, 130,  30);
        private static readonly Color LogRed    = Color.FromArgb(180,  30,  30);

        public MainForm()
        {
            BuildUi();
            TryAutoDetect();
        }

        // ── UI construction ───────────────────────────────────────────────────

        private void BuildUi()
        {
            Text            = "KSP-Connected Setup";
            ClientSize      = new Size(500, 400);
            MinimumSize     = new Size(500, 400);
            MaximumSize     = new Size(500, 400);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            Font            = new Font("Segoe UI", 9f);

            // ── header ───────────────────────────────────────────────────────
            _header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 70,
                BackColor = BgHeader,
            };
            _headerTitle = new Label
            {
                Location  = new Point(20, 12),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Accent,
            };
            _headerSubtitle = new Label
            {
                Location  = new Point(22, 44),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 180, 200),
            };
            _header.Controls.Add(_headerTitle);
            _header.Controls.Add(_headerSubtitle);

            // ── content area ─────────────────────────────────────────────────
            _content = new Panel
            {
                Location  = new Point(0, 70),
                Size      = new Size(500, 270),
                BackColor = BgContent,
            };

            // ── footer ───────────────────────────────────────────────────────
            _footer = new Panel
            {
                Location  = new Point(0, 340),
                Size      = new Size(500, 60),
                BackColor = BgFooter,
            };

            var sep = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(500, 1),
                BackColor = Color.FromArgb(200, 200, 210),
            };
            _footer.Controls.Add(sep);

            _cancelBtn = MakeFooterBtn("Cancel", 390, BtnGray);
            _nextBtn   = MakeFooterBtn("Next >", 290, BtnNormal);
            _backBtn   = MakeFooterBtn("< Back", 190, BtnGray);

            _cancelBtn.Click += OnCancel;
            _backBtn.Click   += OnBack;
            _nextBtn.Click   += OnNext;

            _footer.Controls.Add(_cancelBtn);
            _footer.Controls.Add(_nextBtn);
            _footer.Controls.Add(_backBtn);

            Controls.Add(_header);
            Controls.Add(_content);
            Controls.Add(_footer);

            BuildPageWelcome();
            BuildPagePath();
            BuildPageOptions();
            BuildPageProgress();
            BuildPageDone();

            ShowPage(PAGE_WELCOME);
        }

        private Button MakeFooterBtn(string text, int x, Color color)
        {
            var b = new Button
            {
                Text      = text,
                Location  = new Point(x, 14),
                Size      = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                Font      = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor    = Cursors.Hand,
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        // ── page builders ─────────────────────────────────────────────────────

        private void BuildPageWelcome()
        {
            _pageWelcome = new Panel { Dock = DockStyle.Fill, BackColor = BgContent };

            // Left accent bar
            var bar = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(6, 270),
                BackColor = Accent,
            };

            var welcome = new Label
            {
                Text      = "Welcome to KSP-Connected Setup",
                Location  = new Point(30, 30),
                Size      = new Size(440, 36),
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = TextDark,
            };
            var desc = new Label
            {
                Text = "This wizard will install KSP-Connected — the cross-PC multiplayer\n" +
                       "mod for Kerbal Space Program 1.12.\n\n" +
                       "The installer will:\n" +
                       "  •  Auto-detect or let you choose your KSP folder\n" +
                       "  •  Download and build the mod from source\n" +
                       "  •  Copy the mod files into your KSP GameData folder\n\n" +
                       "Click Next to continue.",
                Location  = new Point(30, 80),
                Size      = new Size(440, 160),
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = TextDark,
            };
            var ver = new Label
            {
                Text      = "v1.0.1-alpha",
                Location  = new Point(30, 245),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8f),
                ForeColor = TextMuted,
            };

            _pageWelcome.Controls.AddRange(new Control[] { bar, welcome, desc, ver });
            _content.Controls.Add(_pageWelcome);
        }

        private void BuildPagePath()
        {
            _pagePath = new Panel { Dock = DockStyle.Fill, BackColor = BgContent };

            var title = ContentTitle("Choose KSP Installation Folder", 20);
            var note  = ContentNote("The folder that contains KSP_x64.exe or KSP.exe", 55);

            _kspPathBox = new TextBox
            {
                Location    = new Point(20, 80),
                Width       = 340,
                BackColor   = BgInput,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Segoe UI", 9.5f),
            };

            var browseBtn  = SmallBtn("Browse…",     366, 79);
            var detectBtn  = SmallBtn("Auto-detect",  20, 115);
            browseBtn.Click += OnBrowse;
            detectBtn.Click += (_, __) => TryAutoDetect(userInitiated: true);

            var infoLine = ContentNote("Requires .NET 6 SDK to build the mod from source.", 150);
            var sdkLink  = new LinkLabel
            {
                Text      = "Download .NET 6 SDK if not installed",
                Location  = new Point(20, 167),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8.5f),
                LinkColor = BtnNormal,
            };
            sdkLink.LinkClicked += (_, __) =>
                System.Diagnostics.Process.Start("https://dotnet.microsoft.com/download/dotnet/6.0");

            _pagePath.Controls.AddRange(new Control[]
                { title, note, _kspPathBox, browseBtn, detectBtn, infoLine, sdkLink });
            _content.Controls.Add(_pagePath);
        }

        private void BuildPageOptions()
        {
            _pageOptions = new Panel { Dock = DockStyle.Fill, BackColor = BgContent };

            var title = ContentTitle("Installation Options", 20);
            var note  = ContentNote("Choose what to install alongside the mod.", 55);

            _serverCheck = new CheckBox
            {
                Text      = "Install multiplayer server  (needed to host a game)",
                Location  = new Point(20, 90),
                AutoSize  = true,
                Checked   = true,
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = TextDark,
            };
            _shortcutCheck = new CheckBox
            {
                Text      = "Create Desktop shortcut for the server",
                Location  = new Point(40, 120),
                AutoSize  = true,
                Checked   = true,
                Font      = new Font("Segoe UI", 9f),
                ForeColor = TextMuted,
            };

            _serverCheck.CheckedChanged += (_, __) =>
                _shortcutCheck.Enabled = _serverCheck.Checked;

            var relayNote = ContentNote(
                "No port forwarding? Use the Relay tab in-game to play via room codes.", 170);

            _pageOptions.Controls.AddRange(new Control[]
                { title, note, _serverCheck, _shortcutCheck, relayNote });
            _content.Controls.Add(_pageOptions);
        }

        private void BuildPageProgress()
        {
            _pageProgress = new Panel { Dock = DockStyle.Fill, BackColor = BgContent };

            _progressLabel = new Label
            {
                Text      = "Installing…",
                Location  = new Point(20, 12),
                AutoSize  = true,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = TextDark,
            };

            _logBox = new RichTextBox
            {
                Location    = new Point(20, 35),
                Size        = new Size(460, 170),
                ReadOnly    = true,
                BackColor   = Color.FromArgb(250, 252, 250),
                ForeColor   = LogGreen,
                Font        = new Font("Consolas", 8f),
                BorderStyle = BorderStyle.FixedSingle,
                WordWrap    = false,
                ScrollBars  = RichTextBoxScrollBars.Both,
            };

            _progressBar = new ProgressBar
            {
                Location  = new Point(20, 215),
                Size      = new Size(460, 18),
                Style     = ProgressBarStyle.Continuous,
            };

            _pageProgress.Controls.AddRange(new Control[]
                { _progressLabel, _logBox, _progressBar });
            _content.Controls.Add(_pageProgress);
        }

        private void BuildPageDone()
        {
            _pageDone = new Panel { Dock = DockStyle.Fill, BackColor = BgContent };

            var bar = new Panel
            {
                Location  = new Point(0, 0),
                Size      = new Size(6, 270),
                BackColor = BtnGreen,
            };

            _doneTitle = new Label
            {
                Location  = new Point(30, 40),
                Size      = new Size(440, 36),
                Font      = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = TextDark,
            };
            _doneMsg = new Label
            {
                Location  = new Point(30, 90),
                Size      = new Size(440, 160),
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = TextDark,
            };

            _pageDone.Controls.AddRange(new Control[] { bar, _doneTitle, _doneMsg });
            _content.Controls.Add(_pageDone);
        }

        // ── page factory helpers ──────────────────────────────────────────────

        private Label ContentTitle(string text, int y) => new Label
        {
            Text      = text,
            Location  = new Point(20, y),
            Size      = new Size(460, 28),
            Font      = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = TextDark,
        };

        private Label ContentNote(string text, int y) => new Label
        {
            Text      = text,
            Location  = new Point(20, y),
            Size      = new Size(460, 20),
            Font      = new Font("Segoe UI", 8.5f),
            ForeColor = TextMuted,
        };

        private Button SmallBtn(string text, int x, int y)
        {
            var b = new Button
            {
                Text      = text,
                Location  = new Point(x, y),
                Size      = new Size(96, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(230, 230, 240),
                ForeColor = TextDark,
                Font      = new Font("Segoe UI", 8.5f),
                Cursor    = Cursors.Hand,
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(190, 190, 210);
            b.FlatAppearance.BorderSize  = 1;
            return b;
        }

        // ── page navigation ───────────────────────────────────────────────────

        private static readonly (string title, string subtitle)[] PageHeaders =
        {
            ("KSP-Connected Setup",   "v1.0.1-alpha — Cross-PC Multiplayer Mod"),
            ("KSP Installation",      "Step 1 of 3 — Choose your KSP folder"),
            ("Options",               "Step 2 of 3 — Choose what to install"),
            ("Installing",            "Step 3 of 3 — Please wait…"),
            ("Setup Complete",        "KSP-Connected is ready to use"),
        };

        private void ShowPage(int page)
        {
            _page = page;

            _pageWelcome.Visible  = page == PAGE_WELCOME;
            _pagePath.Visible     = page == PAGE_PATH;
            _pageOptions.Visible  = page == PAGE_OPTIONS;
            _pageProgress.Visible = page == PAGE_PROGRESS;
            _pageDone.Visible     = page == PAGE_DONE;

            var (title, sub) = PageHeaders[page];
            _headerTitle.Text    = title;
            _headerSubtitle.Text = sub;

            // Back button
            _backBtn.Visible = page != PAGE_WELCOME && page != PAGE_PROGRESS && page != PAGE_DONE;

            // Next / Finish button
            switch (page)
            {
                case PAGE_WELCOME:
                    _nextBtn.Text      = "Next >";
                    _nextBtn.BackColor = BtnNormal;
                    _nextBtn.Enabled   = true;
                    break;
                case PAGE_PATH:
                    _nextBtn.Text      = "Next >";
                    _nextBtn.BackColor = BtnNormal;
                    _nextBtn.Enabled   = true;
                    break;
                case PAGE_OPTIONS:
                    _nextBtn.Text      = "Install";
                    _nextBtn.BackColor = BtnGreen;
                    _nextBtn.Enabled   = true;
                    break;
                case PAGE_PROGRESS:
                    _nextBtn.Visible  = false;
                    _cancelBtn.Enabled = false;
                    break;
                case PAGE_DONE:
                    _nextBtn.Text      = _success ? "Finish" : "Retry";
                    _nextBtn.BackColor = _success ? BtnGreen : Color.FromArgb(180, 50, 50);
                    _nextBtn.Visible   = true;
                    _cancelBtn.Visible = false;
                    break;
            }
        }

        private void OnBack(object sender, EventArgs e)
        {
            if (_page > PAGE_WELCOME) ShowPage(_page - 1);
        }

        private void OnNext(object sender, EventArgs e)
        {
            switch (_page)
            {
                case PAGE_WELCOME:
                    ShowPage(PAGE_PATH);
                    break;

                case PAGE_PATH:
                    if (!IsKsp(_kspPathBox.Text.Trim()))
                    {
                        MessageBox.Show(
                            "KSP not found at the selected path.\n\nPlease Browse to the folder containing KSP_x64.exe.",
                            "KSP Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    ShowPage(PAGE_OPTIONS);
                    break;

                case PAGE_OPTIONS:
                    StartInstall();
                    break;

                case PAGE_DONE:
                    if (_success)
                        Close();
                    else
                        ResetForRetry();
                    break;
            }
        }

        private void OnCancel(object sender, EventArgs e)
        {
            if (_busy) return;
            Close();
        }

        // ── KSP auto-detection ────────────────────────────────────────────────

        private void TryAutoDetect(bool userInitiated = false)
        {
            string found = AutoDetectKsp();
            if (found != null)
            {
                _kspPathBox.Text = found;
                if (userInitiated)
                    MessageBox.Show($"Found KSP at:\n{found}", "KSP Detected",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (userInitiated)
            {
                MessageBox.Show(
                    "KSP not found automatically.\nPlease click Browse and select your KSP folder.",
                    "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private string AutoDetectKsp()
        {
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string pf   = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            string[] fixedCandidates =
            {
                Path.Combine(pf86, @"Steam\steamapps\common\Kerbal Space Program"),
                Path.Combine(pf,   @"Steam\steamapps\common\Kerbal Space Program"),
                Path.Combine(pf86, @"GOG Galaxy\Games\Kerbal Space Program"),
                Path.Combine(pf,   @"GOG Galaxy\Games\Kerbal Space Program"),
                Path.Combine(pf,   @"Epic Games\KerbalSpaceProgram"),
            };
            foreach (string c in fixedCandidates)
                if (IsKsp(c)) return c;

            // Steam library folders from VDF
            string vdfPath = Path.Combine(pf86, @"Steam\config\libraryfolders.vdf");
            if (File.Exists(vdfPath))
            {
                string vdf = File.ReadAllText(vdfPath);
                foreach (Match m in Regex.Matches(vdf, @"""path""\s+""([^""]+)"""))
                {
                    string p = Path.Combine(
                        m.Groups[1].Value.Replace(@"\\", @"\"),
                        @"steamapps\common\Kerbal Space Program");
                    if (IsKsp(p)) return p;
                }
            }

            // Scan all drive letters
            string[] subPaths =
            {
                @"Kerbal Space Program",
                @"KSP\Kerbal Space Program",
                @"Games\Kerbal Space Program",
                @"Games\KSP",
                @"SteamLibrary\steamapps\common\Kerbal Space Program",
                @"Steam\steamapps\common\Kerbal Space Program",
                @"GOG Games\Kerbal Space Program",
            };
            foreach (char drive in "CDEFGHIJKLMNOPQRSTUVWXYZ")
            {
                string root = drive + @":\";
                try { if (!Directory.Exists(root)) continue; }
                catch { continue; }
                foreach (string sub in subPaths)
                {
                    string p = Path.Combine(root, sub);
                    if (IsKsp(p)) return p;
                }
            }
            return null;
        }

        private static bool IsKsp(string path) =>
            Directory.Exists(path) &&
            (File.Exists(Path.Combine(path, "KSP_x64.exe")) ||
             File.Exists(Path.Combine(path, "KSP.exe")));

        // ── browse ────────────────────────────────────────────────────────────

        private void OnBrowse(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select your KSP installation folder (must contain KSP_x64.exe)";
                dlg.ShowNewFolderButton = false;
                if (!string.IsNullOrWhiteSpace(_kspPathBox.Text) &&
                    Directory.Exists(_kspPathBox.Text))
                    dlg.SelectedPath = _kspPathBox.Text;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                    _kspPathBox.Text = dlg.SelectedPath;
            }
        }

        // ── installation ──────────────────────────────────────────────────────

        private void StartInstall()
        {
            _busy = true;
            _logBox.Clear();
            _progressBar.Value = 0;
            ShowPage(PAGE_PROGRESS);

            string repoRoot = FindRepoRoot();

            var worker = new InstallWorker(
                _kspPathBox.Text.Trim(), repoRoot,
                _serverCheck.Checked, _shortcutCheck.Checked,
                Log, SetProgress, OnDone, OnError);

            new Thread(worker.Run) { IsBackground = true }.Start();
        }

        private void Log(string msg, bool isError)
        {
            if (InvokeRequired) { Invoke(new Action(() => Log(msg, isError))); return; }

            _logBox.SelectionStart  = _logBox.TextLength;
            _logBox.SelectionLength = 0;
            _logBox.SelectionColor  = isError ? LogRed : LogGreen;
            _logBox.AppendText(msg + "\n");
            _logBox.ScrollToCaret();

            if (!string.IsNullOrWhiteSpace(msg))
                _progressLabel.Text = msg.Length > 70 ? msg.Substring(0, 70) + "…" : msg;
        }

        private void SetProgress(int value)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetProgress(value))); return; }
            _progressBar.Value = Math.Max(0, Math.Min(100, value));
        }

        private void OnDone()
        {
            if (InvokeRequired) { Invoke(new Action(OnDone)); return; }
            _busy    = false;
            _success = true;

            _doneTitle.Text = "Installation Complete!";
            _doneMsg.Text =
                "KSP-Connected has been installed successfully.\n\n" +
                "What's next:\n" +
                "  1. Launch KSP\n" +
                "  2. Go to the Space Center\n" +
                "  3. Use the KSP-Connected window to connect to a server\n\n" +
                (File.Exists(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "KSP-Connected Server.lnk"))
                    ? "A 'KSP-Connected Server' shortcut was created on your Desktop."
                    : "");

            ShowPage(PAGE_DONE);
        }

        private void OnError(string msg)
        {
            if (InvokeRequired) { Invoke(new Action(() => OnError(msg))); return; }
            _busy    = false;
            _success = false;

            _doneTitle.Text    = "Installation Failed";
            _doneTitle.ForeColor = Color.FromArgb(180, 30, 30);
            _doneMsg.Text      = "An error occurred:\n\n" + msg +
                                 "\n\nClick Retry to go back and try again.";

            // Change done bar colour to red
            if (_pageDone.Controls.Count > 0)
                _pageDone.Controls[0].BackColor = Color.FromArgb(180, 30, 30);

            ShowPage(PAGE_DONE);
        }

        private void ResetForRetry()
        {
            _doneTitle.ForeColor = TextDark;
            if (_pageDone.Controls.Count > 0)
                _pageDone.Controls[0].BackColor = BtnGreen;
            _cancelBtn.Visible = true;
            ShowPage(PAGE_OPTIONS);
        }

        // ── repo root detection ───────────────────────────────────────────────

        private static string FindRepoRoot()
        {
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int up = 0; up < 5; up++)
            {
                if (IsRepoRoot(dir)) return dir;
                string parent = Directory.GetParent(dir)?.FullName;
                if (parent == null || parent == dir) break;
                dir = parent;
            }
            return null; // InstallWorker will download from GitHub
        }

        private static bool IsRepoRoot(string dir) =>
            Directory.Exists(Path.Combine(dir, "Shared")) &&
            Directory.Exists(Path.Combine(dir, "Client")) &&
            Directory.Exists(Path.Combine(dir, "Server")) &&
            Directory.Exists(Path.Combine(dir, "GameData"));
    }
}
