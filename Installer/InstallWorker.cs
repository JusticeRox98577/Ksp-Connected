using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text.RegularExpressions;

namespace KspConnectedInstaller
{
    /// <summary>
    /// All installation logic runs here on a background thread.
    /// Callbacks are used to report progress back to the UI.
    /// If repoRoot is null the source is downloaded from GitHub automatically.
    /// </summary>
    public class InstallWorker
    {
        private readonly string _kspPath;
        private          string _repoRoot;   // may start null; filled by Step0
        private readonly bool   _installServer;
        private readonly bool   _createShortcut;

        private readonly Action<string, bool> _log;
        private readonly Action<int>          _progress;
        private readonly Action               _onDone;
        private readonly Action<string>       _onError;

        private const string SourceZipUrl =
            "https://github.com/JusticeRox98577/Ksp-Connected/archive/refs/heads/main.zip";

        public InstallWorker(
            string kspPath, string repoRoot,
            bool installServer, bool createShortcut,
            Action<string, bool> log, Action<int> progress,
            Action onDone, Action<string> onError)
        {
            _kspPath        = kspPath;
            _repoRoot       = repoRoot;
            _installServer  = installServer;
            _createShortcut = createShortcut;
            _log            = log;
            _progress       = progress;
            _onDone         = onDone;
            _onError        = onError;
        }

        public void Run()
        {
            try
            {
                Step0_EnsureSource();
                Step1_ValidateKsp();
                Step2_CheckDotnetSdk();
                Step3_BuildShared();
                Step4_BuildPlugin();
                Step5_CopyToGameData();
                if (_installServer) Step6_BuildServer();
                if (_installServer && _createShortcut) Step7_CreateShortcut();

                _progress(100);
                _log("", false);
                _log("✔  Installation complete!", false);
                _onDone();
            }
            catch (Exception ex)
            {
                _log("", false);
                _log("✘  " + ex.Message, true);
                _onError(ex.Message);
            }
        }

        // ── steps ────────────────────────────────────────────────────────────

        private void Step0_EnsureSource()
        {
            if (_repoRoot != null)
            {
                _log("Source found at: " + _repoRoot, false);
                return;
            }

            _log("Source not found locally — downloading from GitHub...", false);

            string tempDir = Path.Combine(Path.GetTempPath(), "KspConnected-install");
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
            Directory.CreateDirectory(tempDir);

            string zipPath = Path.Combine(tempDir, "source.zip");

            using (var client = new WebClient())
            {
                client.DownloadFile(SourceZipUrl, zipPath);
            }
            _log("  Download complete. Extracting...", false);

            ZipFile.ExtractToDirectory(zipPath, tempDir);

            // GitHub ZIP extracts to a sub-folder named "Ksp-Connected-main"
            string[] dirs = Directory.GetDirectories(tempDir);
            if (dirs.Length == 0)
                throw new Exception("Failed to extract source archive.");

            _repoRoot = dirs[0];
            _log("  Source ready.", false);
            _progress(5);
        }

        private void Step1_ValidateKsp()
        {
            _log("Validating KSP installation...", false);
            if (!Directory.Exists(Path.Combine(_kspPath, "GameData")))
                throw new Exception($"'GameData' folder not found in: {_kspPath}");
            _log("  KSP path OK: " + _kspPath, false);
            _progress(10);
        }

        private void Step2_CheckDotnetSdk()
        {
            _log("Checking .NET 6 SDK...", false);
            string dotnet = FindDotnet();
            if (dotnet == null)
                throw new Exception(
                    ".NET 6 SDK not found.\n\n" +
                    "Download and install it from:\n" +
                    "  https://dotnet.microsoft.com/download/dotnet/6.0\n\n" +
                    "Then re-run this installer.");
            _log("  SDK found: " + dotnet, false);
            _progress(15);
        }

        private void Step3_BuildShared()
        {
            _log("Building shared library...", false);
            Dotnet($"build \"{Proj("Shared", "KspConnected.Shared.csproj")}\" -c Release -nologo -v q");
            _progress(35);
        }

        private void Step4_BuildPlugin()
        {
            _log("Building KSP plugin (this may take a minute)...", false);
            Dotnet($"build \"{Proj("Client", "KspConnected.Client.csproj")}\" " +
                   $"-c Release -nologo -v q " +
                   $"-p:KspPath=\"{_kspPath}\"");
            _progress(65);
        }

        private void Step5_CopyToGameData()
        {
            _log("Copying mod files to KSP GameData...", false);
            string src  = Path.Combine(_repoRoot, "GameData", "KspConnected");
            string dest = Path.Combine(_kspPath,  "GameData", "KspConnected");

            if (Directory.Exists(dest))
            {
                _log("  Removing previous installation...", false);
                Directory.Delete(dest, recursive: true);
            }

            CopyDir(src, dest);
            _log("  Installed to: " + dest, false);
            _progress(80);
        }

        private void Step6_BuildServer()
        {
            _log("Building multiplayer server...", false);
            Dotnet($"build \"{Proj("Server", "KspConnected.Server.csproj")}\" -c Release -nologo -v q");
            _progress(93);
        }

        private void Step7_CreateShortcut()
        {
            _log("Creating Desktop shortcut for server...", false);
            try
            {
                string dll     = Path.Combine(_repoRoot, "Server", "bin", "Release", "net6.0", "KspConnected.Server.dll");
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string lnk     = Path.Combine(desktop, "KSP-Connected Server.lnk");

                var flags   = System.Reflection.BindingFlags.InvokeMethod;
                var setProp = System.Reflection.BindingFlags.SetProperty;
                Type   shellT = Type.GetTypeFromProgID("WScript.Shell");
                object shell  = Activator.CreateInstance(shellT);
                object link   = shellT.InvokeMember("CreateShortcut", flags, null, shell, new object[] { lnk });
                Type   linkT  = link.GetType();
                linkT.InvokeMember("TargetPath",       setProp, null, link, new object[] { FindDotnet() ?? "dotnet" });
                linkT.InvokeMember("Arguments",        setProp, null, link, new object[] { $"\"{dll}\"" });
                linkT.InvokeMember("WorkingDirectory", setProp, null, link, new object[] { Path.GetDirectoryName(dll) });
                linkT.InvokeMember("Description",      setProp, null, link, new object[] { "KSP-Connected multiplayer server" });
                linkT.InvokeMember("Save",             flags,   null, link, null);

                _log("  Shortcut created on Desktop.", false);
            }
            catch (Exception ex)
            {
                _log("  (Shortcut skipped: " + ex.Message + ")", false);
            }
            _progress(97);
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private string Proj(string folder, string file) =>
            Path.Combine(_repoRoot, folder, file);

        private string _dotnetExe;
        private string FindDotnet()
        {
            if (_dotnetExe != null) return _dotnetExe;

            string[] candidates =
            {
                "dotnet",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                             "dotnet", "dotnet.exe"),
            };

            foreach (string c in candidates)
            {
                try
                {
                    var psi = new ProcessStartInfo(c, "--version")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute        = false,
                        CreateNoWindow         = true,
                    };
                    var p = Process.Start(psi);
                    string ver = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit();

                    if (p.ExitCode == 0 && Regex.IsMatch(ver, @"^[6-9]\.|^[1-9]\d+\."))
                    {
                        _dotnetExe = c;
                        return c;
                    }
                }
                catch { /* try next */ }
            }
            return null;
        }

        private void Dotnet(string args)
        {
            string exe = FindDotnet()
                ?? throw new Exception(".NET SDK disappeared — cannot continue.");

            var psi = new ProcessStartInfo(exe, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true,
            };

            var p = Process.Start(psi);
            p.OutputDataReceived += (_, e) => { if (e.Data != null) _log("  " + e.Data, false); };
            p.ErrorDataReceived  += (_, e) => { if (e.Data != null) _log("  " + e.Data, true);  };
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();

            if (p.ExitCode != 0)
                throw new Exception($"Build command failed (exit code {p.ExitCode}). See the log above for details.");
        }

        private static void CopyDir(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (string file in Directory.GetFiles(src))
                File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), overwrite: true);
            foreach (string dir in Directory.GetDirectories(src))
                CopyDir(dir, Path.Combine(dst, Path.GetFileName(dir)));
        }
    }
}
