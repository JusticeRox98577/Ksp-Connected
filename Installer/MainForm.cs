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
        // ── controls ─────────────────────────────────────────────────────────
        private TextBox     _kspPathBox;
        private Button      _browseBtn;
        private Button      _detectBtn;
        private CheckBox    _serverCheck;
        private CheckBox    _shortcutCheck;
        private RichTextBox _logBox;
        private ProgressBar _progressBar;
        private Label       _statusLabel;
        private Button      _installBtn;

        private bool _busy;

        // ── colours ──────────────────────────────────────────────────────────
        private static readonly Color BgDark    = Color.FromArgb(24,  24,  36);
        private static readonly Color BgPanel   = Color.FromArgb(36,  36,  52);
        private static readonly Color BgInput   = Color.FromArgb(46,  46,  64);
        private static readonly Color Accent    = Color.FromArgb(255, 160,  30);
        private static readonly Color BtnNormal = Color.FromArgb(58,  58,  82);
        private static readonly Color BtnGreen  = Color.FromArgb(48, 120,  48);
        private static readonly Color BtnRed    = Color.FromArgb(140,  40,  40);
        private static readonly Color TextMain  = Color.FromArgb(230, 230, 240);
        private static readonly Color TextMuted = Color.FromArgb(160, 160, 180);
        private static readonly Color LogGreen  = Color.FromArgb(100, 220, 130);
        private static readonly Color LogRed    = Color.FromArgb(255,  90,  80);

        // ── constructor ───────────────────────────────────────────────────────
        public MainForm()
        {
            BuildUi();
            TryAutoDetect();
        }

        // ── UI construction ───────────────────────────────────────────────────
        private void BuildUi()
        {
            // Form
            Text            = "KSP-Connected Installer";
            ClientSize      = new Size(600, 560);
            MinimumSize     = new Size(600, 560);
            MaximumSize     = new Size(600, 560);
            StartPosition   = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            BackColor       = BgDark;
            ForeColor       = TextMain;
            Font            = new Font("Segoe UI", 9f);
            Padding         = new Padding(0);

            // ── header panel ─────────────────────────────────────────────────
            var header = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 70,
                BackColor = BgPanel,
                Padding   = new Padding(20, 0, 0, 0),
            };
            var titleLabel = new Label
            {
                Text      = "KSP-Connected",
                Font      = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = Accent,
                AutoSize  = true,
                Location  = new Point(20, 10),
            };
            var subtitleLabel = new Label
            {
                Text      = "Cross-PC Multiplayer Mod — Installer",
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = TextMuted,
                AutoSize  = true,
                Location  = new Point(22, 44),
            };
            header.Controls.Add(titleLabel);
            header.Controls.Add(subtitleLabel);

            // ── body panel ───────────────────────────────────────────────────
            var body = new Panel
            {
                Location  = new Point(0, 70),
                Size      = new Size(600, 490),
                BackColor = BgDark,
            };

            // KSP path section
            var pathSectionLabel = SectionLabel("KSP Installation Folder", 16, 14);
            var pathNote = Note("The folder that contains KSP_x64.exe", 16, 35);

            _kspPathBox = new TextBox
            {
                Location    = new Point(16, 58),
                Width       = 384,
                BackColor   = BgInput,
                ForeColor   = TextMain,
                BorderStyle = BorderStyle.FixedSingle,
                Font        = new Font("Segoe UI", 9.5f),
            };

            _browseBtn = MakeButton("Browse…", 406, 57, 80);
            _detectBtn = MakeButton("Auto-detect", 490, 57, 94);
            _browseBtn.Click += OnBrowse;
            _detectBtn.Click += (_, __) => TryAutoDetect();

            // Separator
            var sep1 = Separator(16, 100, 568);

            // Options section
            var optLabel = SectionLabel("Options", 16, 112);
            _serverCheck = MakeCheckbox(
                "Install multiplayer server  (needed to host a game)", 16, 134);
            _shortcutCheck = MakeCheckbox(
                "Create Desktop shortcut for the server", 36, 156);

            // Keep shortcut check in sync with server check
            _serverCheck.CheckedChanged += (_, __) =>
                _shortcutCheck.Enabled = _serverCheck.Checked;

            // Separator
            var sep2 = Separator(16, 183, 568);

            // Log section
            var logLabel = SectionLabel("Progress", 16, 195);
            _logBox = new RichTextBox
            {
                Location    = new Point(16, 215),
                Size        = new Size(568, 218),
                ReadOnly    = true,
                BackColor   = Color.FromArgb(14, 14, 22),
                ForeColor   = LogGreen,
                Font        = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.FixedSingle,
                WordWrap    = false,
                ScrollBars  = RichTextBoxScrollBars.Both,
            };

            // Progress bar
            _progressBar = new ProgressBar
            {
                Location  = new Point(16, 442),
                Size      = new Size(568, 14),
                Style     = ProgressBarStyle.Continuous,
                ForeColor = Accent,
            };

            // Status label
            _statusLabel = new Label
            {
                Text      = "Ready.",
                Location  = new Point(16, 460),
                AutoSize  = true,
                ForeColor = TextMuted,
                Font      = new Font("Segoe UI", 8.5f),
            };

            // Install button
            _installBtn = new Button
            {
                Text      = "Install Now",
                Location  = new Point(448, 492),
                Size      = new Size(136, 40),
                Font      = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = BtnGreen,
                ForeColor = Color.White,
                Cursor    = Cursors.Hand,
            };
            _installBtn.FlatAppearance.BorderSize = 0;
            _installBtn.Click += OnInstall;

            // Version label
            var versionLabel = new Label
            {
                Text      = "v1.0.0-alpha",
                Location  = new Point(16, 505),
                AutoSize  = true,
                ForeColor = Color.FromArgb(80, 80, 100),
                Font      = new Font("Segoe UI", 8f),
            };

            // Add everything to body
            body.Controls.AddRange(new Control[]
            {
                pathSectionLabel, pathNote,
                _kspPathBox, _browseBtn, _detectBtn,
                sep1,
                optLabel, _serverCheck, _shortcutCheck,
                sep2,
                logLabel, _logBox,
                _progressBar,
                _statusLabel,
                _installBtn,
                versionLabel,
            });

            Controls.Add(header);
            Controls.Add(body);
        }

        // ── control factory helpers ───────────────────────────────────────────

        private Button MakeButton(string text, int x, int y, int w)
        {
            var b = new Button
            {
                Text      = text,
                Location  = new Point(x, y),
                Size      = new Size(w, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = BtnNormal,
                ForeColor = TextMain,
                Cursor    = Cursors.Hand,
                Font      = new Font("Segoe UI", 8.5f),
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(80, 80, 110);
            b.FlatAppearance.BorderSize  = 1;
            return b;
        }

        private CheckBox MakeCheckbox(string text, int x, int y)
        {
            return new CheckBox
            {
                Text      = text,
                Location  = new Point(x, y),
                AutoSize  = true,
                Checked   = true,
                ForeColor = TextMuted,
                Font      = new Font("Segoe UI", 9f),
            };
        }

        private Label SectionLabel(string text, int x, int y)
        {
            return new Label
            {
                Text      = text,
                Location  = new Point(x, y),
                AutoSize  = true,
                ForeColor = TextMain,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            };
        }

        private Label Note(string text, int x, int y)
        {
            return new Label
            {
                Text      = text,
                Location  = new Point(x, y),
                AutoSize  = true,
                ForeColor = TextMuted,
                Font      = new Font("Segoe UI", 8.5f),
            };
        }

        private Panel Separator(int x, int y, int w)
        {
            return new Panel
            {
                Location  = new Point(x, y),
                Size      = new Size(w, 1),
                BackColor = Color.FromArgb(55, 55, 75),
            };
        }

        // ── KSP auto-detection ────────────────────────────────────────────────

        private void TryAutoDetect()
        {
            string found = AutoDetectKsp();
            if (found != null)
            {
                _kspPathBox.Text = found;
                Log("Auto-detected KSP at: " + found, false);
            }
            else
            {
                Log("KSP not found automatically — please Browse to your KSP folder.", false);
            }
        }

        private string AutoDetectKsp()
        {
            // Common Steam paths
            string pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string pf   = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string[] candidates =
            {
                Path.Combine(pf86, @"Steam\steamapps\common\Kerbal Space Program"),
                Path.Combine(pf,   @"Steam\steamapps\common\Kerbal Space Program"),
            };

            foreach (string c in candidates)
                if (IsKsp(c)) return c;

            // Parse Steam library folders VDF
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

            return null;
        }

        private static bool IsKsp(string path) =>
            Directory.Exists(path) &&
            (File.Exists(Path.Combine(path, "KSP_x64.exe")) ||
             File.Exists(Path.Combine(path, "KSP.exe")));

        // ── event handlers ────────────────────────────────────────────────────

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

        private void OnInstall(object sender, EventArgs e)
        {
            if (_busy) return;

            string kspPath = _kspPathBox.Text.Trim();
            if (!IsKsp(kspPath))
            {
                MessageBox.Show(
                    "KSP installation not found at the selected path.\n\n" +
                    "Please click Browse and select the folder that contains KSP_x64.exe.",
                    "KSP Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string repoRoot = FindRepoRoot();
            if (repoRoot == null)
            {
                MessageBox.Show(
                    "Could not find the KSP-Connected source files next to this installer.\n\n" +
                    "Make sure KspConnected-Installer.exe is inside the KSP-Connected repository folder.",
                    "Source Files Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Lock UI
            _busy = true;
            _installBtn.Enabled = false;
            _installBtn.Text    = "Installing…";
            _logBox.Clear();
            _progressBar.Value  = 0;
            SetStatus("Installing…");

            var worker = new InstallWorker(
                kspPath, repoRoot,
                _serverCheck.Checked, _shortcutCheck.Checked,
                Log, SetProgress, OnDone, OnError);

            new Thread(worker.Run) { IsBackground = true }.Start();
        }

        // ── install callbacks (marshalled back to UI thread) ──────────────────

        private void Log(string msg, bool isError)
        {
            if (InvokeRequired) { Invoke(new Action(() => Log(msg, isError))); return; }

            _logBox.SelectionStart  = _logBox.TextLength;
            _logBox.SelectionLength = 0;
            _logBox.SelectionColor  = isError ? LogRed : LogGreen;
            _logBox.AppendText(msg + "\n");
            _logBox.ScrollToCaret();

            if (!string.IsNullOrWhiteSpace(msg))
                SetStatus(msg);
        }

        private void SetProgress(int value)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetProgress(value))); return; }
            _progressBar.Value = Math.Max(0, Math.Min(100, value));
        }

        private void SetStatus(string msg)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetStatus(msg))); return; }
            _statusLabel.Text = msg.Length > 80 ? msg.Substring(0, 80) + "…" : msg;
        }

        private void OnDone()
        {
            if (InvokeRequired) { Invoke(new Action(OnDone)); return; }

            _busy = false;
            _installBtn.Text      = "Done ✓";
            _installBtn.BackColor = Color.FromArgb(30, 140, 60);
            SetStatus("Installation complete!");

            MessageBox.Show(
                "KSP-Connected installed successfully!\n\n" +
                "What's next:\n" +
                "  • Launch KSP\n" +
                "  • Go to the Space Center\n" +
                "  • Use the KSP-Connected window to enter the host IP and connect\n\n" +
                (File.Exists(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "KSP-Connected Server.lnk"))
                    ? "A 'KSP-Connected Server' shortcut was created on your Desktop.\n" +
                      "Double-click it to start hosting."
                    : ""),
                "Installation Complete!",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OnError(string msg)
        {
            if (InvokeRequired) { Invoke(new Action(() => OnError(msg))); return; }

            _busy = false;
            _installBtn.Enabled   = true;
            _installBtn.Text      = "Retry";
            _installBtn.BackColor = BtnRed;
            SetStatus("Installation failed.");
        }

        // ── repo root detection ───────────────────────────────────────────────

        private static string FindRepoRoot()
        {
            // Walk up from the installer's own location looking for the repo structure
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int up = 0; up < 5; up++)
            {
                if (IsRepoRoot(dir)) return dir;
                string parent = Directory.GetParent(dir)?.FullName;
                if (parent == null || parent == dir) break;
                dir = parent;
            }
            return null;
        }

        private static bool IsRepoRoot(string dir) =>
            Directory.Exists(Path.Combine(dir, "Shared")) &&
            Directory.Exists(Path.Combine(dir, "Client")) &&
            Directory.Exists(Path.Combine(dir, "Server")) &&
            Directory.Exists(Path.Combine(dir, "GameData"));
    }
}
