using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace KspConnectedInstaller
{
    /// <summary>
    /// All installation logic runs here on a background thread.
    /// Callbacks are used to report progress back to the UI.
    /// </summary>
    public class InstallWorker
    {
        private readonly string _kspPath;
        private readonly string _repoRoot;
        private readonly bool   _installServer;
        private readonly bool   _createShortcut;

        private readonly Action<string, bool> _log;       // (message, isError)
        private readonly Action<int>          _progress;  // 0–100
        private readonly Action               _onDone;
        private readonly Action<string>       _onError;

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

        private void Step1_ValidateKsp()
        {
            _log("Validating KSP installation...", false);
            if (!Directory.Exists(Path.Combine(_kspPath, "GameData")))
                throw new Exception($"'GameData' folder not found in: {_kspPath}");
            _log("  KSP path OK: " + _kspPath, false);
            _progress(5);
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
            _progress(10);
        }

        private void Step3_BuildShared()
        {
            _log("Building shared library...", false);
            Dotnet($"build \"{Proj("Shared", "KspConnected.Shared.csproj")}\" -c Release -nologo -v q");
            _progress(30);
        }

        private void Step4_BuildPlugin()
        {
            _log("Building KSP plugin (this may take a minute)...", false);
            Dotnet($"build \"{Proj("Client", "KspConnected.Client.csproj")}\" " +
                   $"-c Release -nologo -v q " +
                   $"-p:KspPath=\"{_kspPath}\"");
            _progress(60);
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
            _progress(75);
        }

        private void Step6_BuildServer()
        {
            _log("Building multiplayer server...", false);
            Dotnet($"build \"{Proj("Server", "KspConnected.Server.csproj")}\" -c Release -nologo -v q");
            _progress(92);
        }

        private void Step7_CreateShortcut()
        {
            _log("Creating Desktop shortcut for server...", false);
            try
            {
                string dll     = Path.Combine(_repoRoot, "Server", "bin", "Release", "net6.0", "KspConnected.Server.dll");
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string lnk     = Path.Combine(desktop, "KSP-Connected Server.lnk");

                // Use Windows Script Host COM object — available on every Windows PC
                Type   shellT = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellT);
                dynamic link  = shell.CreateShortcut(lnk);
                link.TargetPath       = FindDotnet() ?? "dotnet";
                link.Arguments        = $"\"{dll}\"";
                link.WorkingDirectory = Path.GetDirectoryName(dll);
                link.Description      = "KSP-Connected multiplayer server";
                link.Save();

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
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe"),
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

                    // Accept SDK 6, 7, 8, 9 …
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

            // Stream output to log in real time
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
