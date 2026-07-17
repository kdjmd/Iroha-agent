using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace IrohaAgentDesktop
{
    internal sealed class VoiceBootstrapProgress
    {
        public int Percent { get; private set; }
        public string Stage { get; private set; }
        public string Detail { get; private set; }
        public bool IsIndeterminate { get; private set; }

        public VoiceBootstrapProgress(int percent, string stage, string detail, bool isIndeterminate)
        {
            Percent = Math.Max(0, Math.Min(100, percent));
            Stage = stage ?? "";
            Detail = detail ?? "";
            IsIndeterminate = isIndeterminate;
        }
    }

    internal sealed class VoiceBootstrapResult
    {
        public bool Success { get; set; }
        public bool RuntimeDeployed { get; set; }
        public bool VoiceImported { get; set; }
        public string RuntimeRoot { get; set; }
        public string ConfigPath { get; set; }
        public string RefAudioPath { get; set; }
        public string PromptText { get; set; }
        public string PromptLang { get; set; }
        public string Message { get; set; }

        public VoiceBootstrapResult()
        {
            RuntimeRoot = "";
            ConfigPath = "";
            RefAudioPath = "";
            PromptText = AppSettings.DefaultVoicePromptText;
            PromptLang = AppSettings.DefaultVoicePromptLang;
            Message = "";
        }
    }

    internal static class VoiceBootstrapper
    {
        public const int CurrentMatchVersion = 3;

        private sealed class RuntimeArchiveInspection
        {
            public long PackedBytes { get; set; }
            public long UnpackedBytes { get; set; }
            public int VolumeCount { get; set; }
        }

        public static string ManagedBaseDirectory
        {
            get { return ResolveManagedBaseDirectory(); }
        }

        public static string ManagedRuntimeDirectory
        {
            get { return Path.Combine(ManagedBaseDirectory, "VoiceRuntime"); }
        }

        public static string ManagedVoiceDirectory
        {
            get { return Path.Combine(ManagedBaseDirectory, "Voice", "iroha"); }
        }

        public static bool IsRuntimeUsable(string root)
        {
            if (string.IsNullOrWhiteSpace(root)) return false;
            try
            {
                string full = Path.GetFullPath(root.Trim().Trim('"'));
                string python = Path.Combine(full, "runtime", "python.exe");
                string api = Path.Combine(full, "api_v2.py");
                if (!File.Exists(python) || !File.Exists(api)) return false;
                if (new FileInfo(python).Length <= 0 || new FileInfo(api).Length <= 0) return false;
                if (IsPathUnder(full, ManagedRuntimeDirectory) &&
                    !File.Exists(Path.Combine(ManagedRuntimeDirectory, ".deployment-ready")))
                {
                    return false;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string ResolveConfigPath(AppSettings settings)
        {
            if (settings != null && !string.IsNullOrWhiteSpace(settings.VoiceRuntimeConfigPath))
            {
                string configured = settings.VoiceRuntimeConfigPath.Trim().Trim('"');
                if (File.Exists(configured)) return configured;
            }

            string root = settings == null ? "" : settings.VoiceRuntimeRoot;
            if (string.IsNullOrWhiteSpace(root)) root = AppSettings.DefaultVoiceRuntimeRoot;
            string candidate = Path.Combine(root ?? "", AppSettings.DefaultVoiceRuntimeConfig.Replace('/', Path.DirectorySeparatorChar));
            return candidate;
        }

        public static VoiceBootstrapResult Prepare(
            AppSettings settings,
            bool forceRedeploy,
            Action<VoiceBootstrapProgress> progress)
        {
            bool acquired = false;
            using (var mutex = new System.Threading.Mutex(false, "Local\\IrohaAgentVoiceDeploymentV3"))
            {
                try
                {
                    try
                    {
                        acquired = mutex.WaitOne(TimeSpan.FromMinutes(30));
                    }
                    catch (System.Threading.AbandonedMutexException)
                    {
                        acquired = true;
                    }

                    if (!acquired)
                    {
                        return new VoiceBootstrapResult
                        {
                            Success = false,
                            Message = "另一个彩叶 Agent 正在部署语音，请等待它完成后重试"
                        };
                    }
                    return PrepareCore(settings, forceRedeploy, progress);
                }
                finally
                {
                    if (acquired)
                    {
                        try { mutex.ReleaseMutex(); }
                        catch { }
                    }
                }
            }
        }

        private static VoiceBootstrapResult PrepareCore(
            AppSettings settings,
            bool forceRedeploy,
            Action<VoiceBootstrapProgress> progress)
        {
            var result = new VoiceBootstrapResult();
            try
            {
                Report(progress, 6, forceRedeploy ? "正在准备重新部署" : "正在检查本地语音", "不会修改你的原始语音包", true);
                RecoverInterruptedDeployment();

                string bundledArchive = FindBundledRuntimeArchive();
                bool managedRuntimeCanBeRestored = !string.IsNullOrWhiteSpace(bundledArchive);
                if (forceRedeploy)
                {
                    StopVoiceProcessesForRoot(settings == null ? "" : settings.VoiceRuntimeRoot);
                    StopVoiceProcessesForRoot(ManagedRuntimeDirectory);
                    Report(progress, 10, "正在准备安全替换", "旧的可用语音会保留到新部署通过检查", true);
                }

                bool preferBundled = managedRuntimeCanBeRestored &&
                    string.Equals(
                        Environment.GetEnvironmentVariable("IROHA_VOICE_PREFER_BUNDLE"),
                        "1",
                        StringComparison.OrdinalIgnoreCase);
                string runtimeRoot = (forceRedeploy && managedRuntimeCanBeRestored) || preferBundled
                    ? ""
                    : FindExistingRuntime(settings, progress);
                if (string.IsNullOrWhiteSpace(runtimeRoot) && !string.IsNullOrWhiteSpace(bundledArchive))
                {
                    try
                    {
                        runtimeRoot = DeployBundledRuntime(bundledArchive, progress);
                        result.RuntimeDeployed = !string.IsNullOrWhiteSpace(runtimeRoot);
                    }
                    catch (Exception deployError)
                    {
                        runtimeRoot = FindRecoveryRuntime(settings);
                        if (string.IsNullOrWhiteSpace(runtimeRoot)) throw;
                        Report(progress, 62, "新部署未完成，已恢复旧语音", LimitMessage(deployError.Message, 120), false);
                    }
                }

                if (string.IsNullOrWhiteSpace(runtimeRoot))
                {
                    runtimeRoot = FindExistingRuntime(settings, progress);
                }

                if (!IsRuntimeUsable(runtimeRoot))
                {
                    result.Message = "没有找到可用的 GPT-SoVITS 运行环境";
                    Report(progress, 100, "暂未找到语音组件", "文字聊天仍可正常使用", false);
                    return result;
                }

                Report(progress, 64, "已找到 GPT-SoVITS", "正在匹配彩叶模型与参考音频", true);

                string configPath = FindExistingConfig(runtimeRoot, settings);
                string refAudioPath = FindExistingReference(runtimeRoot, settings);
                string promptText = settings == null || string.IsNullOrWhiteSpace(settings.VoicePromptText)
                    ? AppSettings.DefaultVoicePromptText
                    : settings.VoicePromptText.Trim();
                string promptLang = settings == null || string.IsNullOrWhiteSpace(settings.VoicePromptLang)
                    ? AppSettings.DefaultVoicePromptLang
                    : settings.VoicePromptLang.Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(configPath) || string.IsNullOrWhiteSpace(refAudioPath))
                {
                    string ckpt;
                    string pth;
                    string wav;
                    if (TryFindExtractedVoiceFiles(runtimeRoot, out ckpt, out pth, out wav))
                    {
                        refAudioPath = wav;
                        configPath = WriteManagedConfig(runtimeRoot, ckpt, pth);
                    }
                    else
                    {
                        string voicePackage = FindVoicePackage();
                        if (!string.IsNullOrWhiteSpace(voicePackage))
                        {
                            string importedConfig;
                            string importedRef;
                            string importedPrompt;
                            string importedLang;
                            if (ImportVoicePackage(
                                runtimeRoot,
                                voicePackage,
                                progress,
                                out importedConfig,
                                out importedRef,
                                out importedPrompt,
                                out importedLang))
                            {
                                configPath = importedConfig;
                                refAudioPath = importedRef;
                                promptText = importedPrompt;
                                promptLang = importedLang;
                                result.VoiceImported = true;
                            }
                        }
                    }
                }

                if (File.Exists(configPath ?? ""))
                {
                    configPath = EnsureManagedWritableConfig(configPath);
                }

                if (!File.Exists(configPath ?? "") || !File.Exists(refAudioPath ?? ""))
                {
                    result.Message = "运行环境已找到，但缺少可用的彩叶模型或参考音频";
                    Report(progress, 100, "语音模型尚未匹配", "请把语音包放在安装目录或桌面后重新部署", false);
                    return result;
                }

                result.Success = true;
                result.RuntimeRoot = Path.GetFullPath(runtimeRoot);
                result.ConfigPath = Path.GetFullPath(configPath);
                result.RefAudioPath = Path.GetFullPath(refAudioPath);
                result.PromptText = promptText;
                result.PromptLang = string.IsNullOrWhiteSpace(promptLang) ? "ja" : promptLang;
                result.Message = result.RuntimeDeployed
                    ? "GPT-SoVITS 已自动部署并匹配"
                    : (result.VoiceImported ? "彩叶语音包已自动导入" : "本地语音已自动匹配");
                Report(progress, 86, "语音组件匹配完成", "正在启动本地服务", false);
                return result;
            }
            catch (Exception ex)
            {
                result.Message = "语音自动部署失败: " + ex.Message;
                Report(progress, 100, "语音准备没有完成", "文字聊天仍可正常使用", false);
                return result;
            }
        }

        public static void StopVoiceProcessesForRoot(string runtimeRoot)
        {
            if (string.IsNullOrWhiteSpace(runtimeRoot)) return;
            string fullRoot;
            try { fullRoot = Path.GetFullPath(runtimeRoot.Trim().Trim('"')).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar; }
            catch { return; }

            var candidates = new List<Process>();
            try { candidates.AddRange(Process.GetProcessesByName("python")); }
            catch { }
            try { candidates.AddRange(Process.GetProcessesByName("pythonw")); }
            catch { }

            foreach (Process process in candidates)
            {
                try
                {
                    string executable = process.MainModule == null ? "" : process.MainModule.FileName;
                    if (!string.IsNullOrWhiteSpace(executable) &&
                        Path.GetFullPath(executable).StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        process.Kill();
                        process.WaitForExit(3000);
                    }
                }
                catch { }
                finally { process.Dispose(); }
            }
        }

        private static string FindExistingRuntime(AppSettings settings, Action<VoiceBootstrapProgress> progress)
        {
            Report(progress, 14, "正在查找 GPT-SoVITS", "检查应用目录与常见安装位置", true);
            var direct = new List<string>();
            AddCandidate(direct, settings == null ? "" : settings.VoiceRuntimeRoot);
            AddCandidate(direct, ManagedRuntimeDirectory);
            AddCandidate(direct, Environment.GetEnvironmentVariable("IROHA_GPT_SOVITS_ROOT"));
            AddCandidate(direct, AppSettings.DefaultVoiceRuntimeRoot);
            AddCandidate(direct, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GPT-SoVITS"));
            AddCandidate(direct, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voice-runtime", "GPT-SoVITS"));
            AddCandidate(direct, @"C:\GPT-SoVITS");

            foreach (string candidate in direct)
            {
                if (IsRuntimeUsable(candidate)) return Path.GetFullPath(candidate);
                string nested = FindRuntimeUnder(candidate, 3);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }

            var searchBases = new List<string>();
            searchBases.Add(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Codex",
                "GPT-SoVITS-runtime"));
            searchBases.Add(ManagedRuntimeDirectory);

            foreach (string searchBase in searchBases)
            {
                string found = FindRuntimeUnder(searchBase, 5);
                if (!string.IsNullOrWhiteSpace(found)) return found;
            }

            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (Directory.Exists(desktop))
            {
                try
                {
                    foreach (string directory in Directory.GetDirectories(desktop))
                    {
                        string name = Path.GetFileName(directory).ToLowerInvariant();
                        if (name.IndexOf("gpt", StringComparison.Ordinal) >= 0 || name.IndexOf("sovits", StringComparison.Ordinal) >= 0)
                        {
                            string found = FindRuntimeUnder(directory, 4);
                            if (!string.IsNullOrWhiteSpace(found)) return found;
                        }
                    }
                }
                catch { }
            }
            return "";
        }

        private static string FindRecoveryRuntime(AppSettings settings)
        {
            var candidates = new List<string>();
            AddCandidate(candidates, settings == null ? "" : settings.VoiceRuntimeRoot);
            AddCandidate(candidates, ManagedRuntimeDirectory);

            foreach (string candidate in candidates)
            {
                if (IsRuntimeUsable(candidate)) return Path.GetFullPath(candidate);
                string nested = FindRuntimeUnder(candidate, 3);
                if (!string.IsNullOrWhiteSpace(nested)) return nested;
            }
            return "";
        }

        private static string FindRuntimeUnder(string baseDirectory, int maxDepth)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory) || !Directory.Exists(baseDirectory)) return "";
            var queue = new Queue<Tuple<string, int>>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            queue.Enqueue(Tuple.Create(Path.GetFullPath(baseDirectory), 0));

            while (queue.Count > 0)
            {
                Tuple<string, int> item = queue.Dequeue();
                string current = item.Item1;
                int depth = item.Item2;
                if (!visited.Add(current)) continue;
                if (IsRuntimeUsable(current)) return current;
                if (depth >= maxDepth) continue;

                try
                {
                    foreach (string child in Directory.GetDirectories(current))
                    {
                        string name = Path.GetFileName(child).ToLowerInvariant();
                        if (name == "runtime" || name == "tools" || name == "gpt_sovits" || name == ".git" || name == "temp") continue;
                        queue.Enqueue(Tuple.Create(child, depth + 1));
                    }
                }
                catch { }
            }
            return "";
        }

        private static string FindBundledRuntimeArchive()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voice-runtime");
            if (!Directory.Exists(folder)) return "";
            string[] preferred =
            {
                "GPT-SoVITS-v2pro-20250604.7z.001",
                "GPT-SoVITS-v2pro-20250604.7z",
                "GPT-SoVITS-runtime.7z.001",
                "GPT-SoVITS-runtime.7z"
            };
            foreach (string name in preferred)
            {
                string path = Path.Combine(folder, name);
                if (File.Exists(path)) return path;
            }
            try
            {
                string[] parts = Directory.GetFiles(folder, "*.7z.001", SearchOption.TopDirectoryOnly);
                if (parts.Length > 0) return parts[0];
                string[] archives = Directory.GetFiles(folder, "*.7z", SearchOption.TopDirectoryOnly);
                if (archives.Length > 0) return archives[0];
            }
            catch { }
            return "";
        }

        private static string DeployBundledRuntime(string archivePath, Action<VoiceBootstrapProgress> progress)
        {
            string sevenZip = Find7Zip();
            if (string.IsNullOrWhiteSpace(sevenZip)) throw new InvalidOperationException("安装包缺少 7-Zip 解压组件");
            RuntimeArchiveInspection inspection = InspectRuntimeArchive(archivePath, sevenZip, progress);
            EnsureDeploymentSpace(inspection);

            string staging = Path.Combine(
                ManagedBaseDirectory,
                "VoiceRuntime.install-" + Process.GetCurrentProcess().Id + "-" + Guid.NewGuid().ToString("N"));
            bool promoted = false;
            Directory.CreateDirectory(staging);
            try
            {
                Report(progress, 18, "正在部署 GPT-SoVITS", "首次部署文件较大，请保持应用开启", true);
                RunRuntimeExtraction(sevenZip, archivePath, staging, progress);

                string root = FindRuntimeUnder(staging, 6);
                if (!IsRuntimeCompleteForDeployment(root))
                {
                    throw new InvalidOperationException("解压完成但 GPT-SoVITS 运行时不完整");
                }

                string relativeRoot = root.Substring(staging.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string marker = Path.Combine(staging, ".deployment-ready");
                File.WriteAllText(
                    marker,
                    "version=" + CurrentMatchVersion + Environment.NewLine +
                    "deployedUtc=" + DateTime.UtcNow.ToString("o") + Environment.NewLine +
                    "archiveBytes=" + inspection.PackedBytes + Environment.NewLine +
                    "runtimeBytes=" + inspection.UnpackedBytes + Environment.NewLine,
                    new UTF8Encoding(false));

                PromoteStagedRuntime(staging);
                promoted = true;
                staging = "";
                string finalRoot = string.IsNullOrWhiteSpace(relativeRoot)
                    ? ManagedRuntimeDirectory
                    : Path.Combine(ManagedRuntimeDirectory, relativeRoot);
                if (!IsRuntimeUsable(finalRoot))
                {
                    throw new InvalidOperationException("语音运行时切换后校验失败");
                }
                CompletePromotedRuntime();
                promoted = false;

                Report(progress, 62, "GPT-SoVITS 部署完成", "正在匹配彩叶模型", false);
                return finalRoot;
            }
            catch
            {
                if (promoted) RollbackPromotedRuntime();
                throw;
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(staging) && Directory.Exists(staging))
                {
                    try { DeleteManagedDirectory(staging); }
                    catch { }
                }
            }
        }

        private static RuntimeArchiveInspection InspectRuntimeArchive(
            string archivePath,
            string sevenZip,
            Action<VoiceBootstrapProgress> progress)
        {
            if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath))
            {
                throw new FileNotFoundException("找不到安装包内的 GPT-SoVITS 运行时", archivePath);
            }

            List<string> volumes = GetArchiveVolumes(archivePath);
            long packedBytes = 0;
            foreach (string volume in volumes) packedBytes += new FileInfo(volume).Length;
            Report(progress, 14, "正在核对语音安装包", volumes.Count + " 个分卷 · " + FormatGiB(packedBytes), true);

            var info = new ProcessStartInfo();
            info.FileName = sevenZip;
            info.Arguments = "l -slt \"" + archivePath + "\"";
            info.WorkingDirectory = Path.GetDirectoryName(sevenZip);
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.WindowStyle = ProcessWindowStyle.Hidden;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;

            string output;
            string error;
            int exitCode;
            using (Process process = Process.Start(info))
            {
                if (process == null) throw new InvalidOperationException("无法启动语音安装包检查");
                output = process.StandardOutput.ReadToEnd();
                error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                exitCode = process.ExitCode;
            }
            if (exitCode != 0)
            {
                throw new InvalidOperationException(
                    "语音安装包不完整或已损坏（检查码 " + exitCode + "）" +
                    (string.IsNullOrWhiteSpace(error) ? "" : "：" + LimitMessage(error, 140)));
            }

            long unpackedBytes = 0;
            using (var reader = new StringReader(output ?? ""))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (!trimmed.StartsWith("Size = ", StringComparison.Ordinal)) continue;
                    long size;
                    if (long.TryParse(trimmed.Substring(7).Trim(), out size) && size > 0)
                    {
                        unpackedBytes += size;
                    }
                }
            }
            if (unpackedBytes <= 0) unpackedBytes = Math.Max(packedBytes, packedBytes * 2);

            return new RuntimeArchiveInspection
            {
                PackedBytes = packedBytes,
                UnpackedBytes = unpackedBytes,
                VolumeCount = volumes.Count
            };
        }

        private static List<string> GetArchiveVolumes(string archivePath)
        {
            var volumes = new List<string>();
            string full = Path.GetFullPath(archivePath);
            if (!full.EndsWith(".001", StringComparison.OrdinalIgnoreCase))
            {
                volumes.Add(full);
                return volumes;
            }

            string prefix = full.Substring(0, full.Length - 3);
            var byNumber = new SortedDictionary<int, string>();
            foreach (string candidate in Directory.GetFiles(Path.GetDirectoryName(full), Path.GetFileName(prefix) + "*", SearchOption.TopDirectoryOnly))
            {
                if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                string suffix = candidate.Substring(prefix.Length);
                int number;
                if (suffix.Length == 3 && int.TryParse(suffix, out number) && number > 0)
                {
                    byNumber[number] = candidate;
                }
            }
            if (!byNumber.ContainsKey(1)) throw new InvalidOperationException("语音运行时缺少第 001 分卷");
            int last = 0;
            foreach (int number in byNumber.Keys) last = Math.Max(last, number);
            for (int number = 1; number <= last; number++)
            {
                string part;
                if (!byNumber.TryGetValue(number, out part))
                {
                    throw new InvalidOperationException("语音运行时缺少第 " + number.ToString("000") + " 分卷");
                }
                volumes.Add(part);
            }
            return volumes;
        }

        private static void EnsureDeploymentSpace(RuntimeArchiveInspection inspection)
        {
            string root = Path.GetPathRoot(Path.GetFullPath(ManagedBaseDirectory));
            if (string.IsNullOrWhiteSpace(root)) return;
            long available = new DriveInfo(root).AvailableFreeSpace;
            long reserve = Math.Max(1024L * 1024L * 1024L, inspection.UnpackedBytes / 10L);
            long required = inspection.UnpackedBytes + reserve;
            if (available < required)
            {
                throw new IOException(
                    "语音部署空间不足：至少需要 " + FormatGiB(required) +
                    "，当前仅剩 " + FormatGiB(available));
            }
        }

        private static void RunRuntimeExtraction(
            string sevenZip,
            string archivePath,
            string destination,
            Action<VoiceBootstrapProgress> progress)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = sevenZip;
            startInfo.Arguments = "x -y -bb0 -bsp1 -o\"" + destination + "\" \"" + archivePath + "\"";
            startInfo.WorkingDirectory = Path.GetDirectoryName(sevenZip);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            int lastPercent = -1;
            object sync = new object();
            var errorText = new StringBuilder();
            using (var process = new Process())
            {
                process.StartInfo = startInfo;
                DataReceivedEventHandler outputHandler = delegate(object sender, DataReceivedEventArgs e)
                {
                    if (string.IsNullOrWhiteSpace(e.Data)) return;
                    Match match = Regex.Match(e.Data, @"(?<!\d)(\d{1,3})%");
                    if (!match.Success) return;
                    int parsed;
                    if (!int.TryParse(match.Groups[1].Value, out parsed)) return;
                    parsed = Math.Max(0, Math.Min(100, parsed));
                    lock (sync)
                    {
                        if (parsed == lastPercent) return;
                        lastPercent = parsed;
                    }
                    int mapped = 18 + (int)Math.Round(parsed * 0.44);
                    Report(progress, mapped, "正在部署 GPT-SoVITS", parsed + "% · 完成后以后启动无需再次解压", false);
                };
                DataReceivedEventHandler errorHandler = delegate(object sender, DataReceivedEventArgs e)
                {
                    if (string.IsNullOrWhiteSpace(e.Data)) return;
                    lock (sync)
                    {
                        if (errorText.Length < 1200) errorText.AppendLine(e.Data.Trim());
                    }
                    outputHandler(sender, e);
                };
                process.OutputDataReceived += outputHandler;
                process.ErrorDataReceived += errorHandler;
                if (!process.Start()) throw new InvalidOperationException("无法启动语音运行时解压组件");
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                process.WaitForExit(1000);
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        "语音运行时解压失败，错误码 " + process.ExitCode +
                        (errorText.Length == 0 ? "" : "：" + LimitMessage(errorText.ToString(), 160)));
                }
            }
        }

        private static bool IsRuntimeCompleteForDeployment(string root)
        {
            if (!IsRuntimeUsable(root)) return false;
            return Directory.Exists(Path.Combine(root, "GPT_SoVITS", "pretrained_models", "chinese-roberta-wwm-ext-large")) &&
                   Directory.Exists(Path.Combine(root, "GPT_SoVITS", "pretrained_models", "chinese-hubert-base"));
        }

        private static void PromoteStagedRuntime(string staging)
        {
            string target = ManagedRuntimeDirectory;
            string backup = target + ".previous";
            if (Directory.Exists(backup)) DeleteManagedDirectory(backup);
            if (Directory.Exists(target)) Directory.Move(target, backup);
            try
            {
                Directory.Move(staging, target);
            }
            catch
            {
                try
                {
                    if (Directory.Exists(target)) DeleteManagedDirectory(target);
                    if (Directory.Exists(backup)) Directory.Move(backup, target);
                }
                catch { }
                throw;
            }
        }

        private static void CompletePromotedRuntime()
        {
            string backup = ManagedRuntimeDirectory + ".previous";
            if (!Directory.Exists(backup)) return;
            try { DeleteManagedDirectory(backup); }
            catch { }
        }

        private static void RollbackPromotedRuntime()
        {
            string target = ManagedRuntimeDirectory;
            string backup = target + ".previous";
            try
            {
                if (Directory.Exists(target)) DeleteManagedDirectory(target);
                if (Directory.Exists(backup)) Directory.Move(backup, target);
            }
            catch { }
        }

        private static string Find7Zip()
        {
            string[] candidates =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voice-runtime", "tools", "7z.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "7z.exe"),
                @"C:\Program Files\7-Zip\7z.exe",
                @"C:\Program Files (x86)\7-Zip\7z.exe"
            };
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate)) return candidate;
            }
            return "";
        }

        private static string FindExistingConfig(string runtimeRoot, AppSettings settings)
        {
            var candidates = new List<string>();
            AddCandidate(candidates, settings == null ? "" : settings.VoiceRuntimeConfigPath);
            AddCandidate(candidates, Path.Combine(runtimeRoot, AppSettings.DefaultVoiceRuntimeConfig.Replace('/', Path.DirectorySeparatorChar)));
            AddCandidate(candidates, Path.Combine(ManagedVoiceDirectory, "tts_infer_iroha.yaml"));
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate) && new FileInfo(candidate).Length > 32) return Path.GetFullPath(candidate);
            }
            return "";
        }

        private static string EnsureManagedWritableConfig(string sourcePath)
        {
            string source = Path.GetFullPath(sourcePath);
            Directory.CreateDirectory(ManagedVoiceDirectory);
            string target = Path.Combine(ManagedVoiceDirectory, "tts_infer_iroha.yaml");
            string targetFull = Path.GetFullPath(target);

            if (!string.Equals(source, targetFull, StringComparison.OrdinalIgnoreCase))
            {
                string temporary = targetFull + ".tmp-" + Guid.NewGuid().ToString("N");
                try
                {
                    File.Copy(source, temporary, true);
                    File.SetAttributes(temporary, FileAttributes.Normal);
                    if (File.Exists(targetFull))
                    {
                        File.SetAttributes(targetFull, FileAttributes.Normal);
                        File.Delete(targetFull);
                    }
                    File.Move(temporary, targetFull);
                }
                finally
                {
                    if (File.Exists(temporary)) File.Delete(temporary);
                }
            }

            File.SetAttributes(targetFull, FileAttributes.Normal);
            using (FileStream probe = new FileStream(targetFull, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                if (probe.Length <= 32) throw new InvalidDataException("GPT-SoVITS 配置文件内容不完整");
            }
            return targetFull;
        }

        private static string FindExistingReference(string runtimeRoot, AppSettings settings)
        {
            var candidates = new List<string>();
            AddCandidate(candidates, settings == null ? "" : settings.VoiceRefAudioPath);
            AddCandidate(candidates, Path.Combine(runtimeRoot, "voices", "iroha", "ref.wav"));
            AddCandidate(candidates, Path.Combine(ManagedVoiceDirectory, "ref.wav"));
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate) && new FileInfo(candidate).Length > 44) return Path.GetFullPath(candidate);
            }
            return "";
        }

        private static bool TryFindExtractedVoiceFiles(string runtimeRoot, out string ckpt, out string pth, out string wav)
        {
            ckpt = "";
            pth = "";
            wav = "";
            string[] roots =
            {
                Path.Combine(runtimeRoot, "voices", "iroha"),
                ManagedVoiceDirectory
            };
            foreach (string root in roots)
            {
                if (!Directory.Exists(root)) continue;
                ckpt = FindLargestFile(root, "*.ckpt");
                pth = FindLargestFile(root, "*.pth");
                string preferred = Path.Combine(root, "ref.wav");
                wav = File.Exists(preferred) ? preferred : FindLargestFile(root, "*.wav");
                if (File.Exists(ckpt) && File.Exists(pth) && File.Exists(wav)) return true;
            }
            ckpt = pth = wav = "";
            return false;
        }

        private static string FindVoicePackage()
        {
            var candidates = new List<string>();
            AddCandidate(candidates, Environment.GetEnvironmentVariable("IROHA_VOICE_PACKAGE"));
            AddCandidate(candidates, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voice-runtime", "iroha-model.zip"));
            AddCandidate(candidates, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "酒寄彩叶gsv模型.zip"));
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            AddCandidate(candidates, Path.Combine(desktop, "酒寄彩叶gsv模型.zip"));
            string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            foreach (string folder in new[] { Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voice-runtime"), desktop, downloads })
            {
                if (!Directory.Exists(folder)) continue;
                try
                {
                    foreach (string file in Directory.GetFiles(folder, "*.zip", SearchOption.TopDirectoryOnly))
                    {
                        string name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                        if (name.IndexOf("gsv", StringComparison.Ordinal) >= 0 ||
                            name.IndexOf("sovits", StringComparison.Ordinal) >= 0 ||
                            name.IndexOf("iroha", StringComparison.Ordinal) >= 0 ||
                            name.IndexOf("彩叶", StringComparison.Ordinal) >= 0)
                        {
                            AddCandidate(candidates, file);
                        }
                    }
                }
                catch { }
            }

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate) && LooksLikeVoicePackage(candidate)) return candidate;
            }
            return "";
        }

        private static bool LooksLikeVoicePackage(string path)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(path))
                {
                    bool ckpt = false;
                    bool pth = false;
                    bool wav = false;
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string extension = Path.GetExtension(entry.FullName).ToLowerInvariant();
                        if (extension == ".ckpt") ckpt = true;
                        else if (extension == ".pth") pth = true;
                        else if (extension == ".wav") wav = true;
                    }
                    return ckpt && pth && wav;
                }
            }
            catch { return false; }
        }

        private static bool ImportVoicePackage(
            string runtimeRoot,
            string packagePath,
            Action<VoiceBootstrapProgress> progress,
            out string configPath,
            out string refAudioPath,
            out string promptText,
            out string promptLang)
        {
            configPath = "";
            refAudioPath = "";
            promptText = AppSettings.DefaultVoicePromptText;
            promptLang = AppSettings.DefaultVoicePromptLang;
            Directory.CreateDirectory(ManagedVoiceDirectory);

            using (ZipArchive archive = ZipFile.OpenRead(packagePath))
            {
                ZipArchiveEntry ckptEntry = FindLargestEntry(archive, ".ckpt");
                ZipArchiveEntry pthEntry = FindLargestEntry(archive, ".pth");
                ZipArchiveEntry listEntry = FindLargestEntry(archive, ".list");
                ZipArchiveEntry refEntry = null;

                if (listEntry != null)
                {
                    string refFileName;
                    ReadReferenceMetadata(listEntry, out refFileName, out promptText, out promptLang);
                    if (!string.IsNullOrWhiteSpace(refFileName)) refEntry = FindEntryByFileName(archive, refFileName);
                }
                if (refEntry == null) refEntry = FindLargestEntry(archive, ".wav");
                if (ckptEntry == null || pthEntry == null || refEntry == null) return false;

                long total = Math.Max(1L, ckptEntry.Length + pthEntry.Length + refEntry.Length);
                long copied = 0;
                string ckptPath = Path.Combine(ManagedVoiceDirectory, "gpt.ckpt");
                string version = InferModelVersion(pthEntry.FullName);
                string pthPath = Path.Combine(ManagedVoiceDirectory, "sovits-" + version + ".pth");
                refAudioPath = Path.Combine(ManagedVoiceDirectory, "ref.wav");

                ExtractEntry(ckptEntry, ckptPath, total, ref copied, progress);
                ExtractEntry(pthEntry, pthPath, total, ref copied, progress);
                ExtractEntry(refEntry, refAudioPath, total, ref copied, progress);
                configPath = WriteManagedConfig(runtimeRoot, ckptPath, pthPath);
                return File.Exists(configPath) && File.Exists(refAudioPath);
            }
        }

        private static void ExtractEntry(
            ZipArchiveEntry entry,
            string destination,
            long total,
            ref long copied,
            Action<VoiceBootstrapProgress> progress)
        {
            if (File.Exists(destination) && new FileInfo(destination).Length == entry.Length)
            {
                copied += entry.Length;
                return;
            }

            string temporary = destination + ".part";
            if (File.Exists(temporary)) File.Delete(temporary);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            byte[] buffer = new byte[1024 * 1024];
            using (Stream input = entry.Open())
            using (FileStream output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, read);
                    copied += read;
                    int percent = 64 + (int)Math.Round(Math.Min(1.0, copied / (double)total) * 18.0);
                    Report(progress, percent, "正在导入彩叶语音模型", Math.Min(100, (int)Math.Round(copied * 100.0 / total)) + "%", false);
                }
            }
            if (File.Exists(destination)) File.Delete(destination);
            File.Move(temporary, destination);
        }

        private static void ReadReferenceMetadata(
            ZipArchiveEntry listEntry,
            out string refFileName,
            out string promptText,
            out string promptLang)
        {
            refFileName = "";
            promptText = AppSettings.DefaultVoicePromptText;
            promptLang = AppSettings.DefaultVoicePromptLang;
            using (Stream stream = listEntry.Open())
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] parts = line.Split('|');
                    if (parts.Length < 4) continue;
                    string text = string.Join("|", parts, 3, parts.Length - 3).Trim();
                    if (text.Length == 0) continue;
                    string source = parts[0].Replace('\\', '/');
                    int slash = source.LastIndexOf('/');
                    refFileName = slash >= 0 ? source.Substring(slash + 1) : source;
                    promptText = text;
                    promptLang = string.IsNullOrWhiteSpace(parts[2]) ? "ja" : parts[2].Trim().ToLowerInvariant();
                    if (promptLang == "jp") promptLang = "ja";
                    return;
                }
            }
        }

        private static string WriteManagedConfig(string runtimeRoot, string ckptPath, string pthPath)
        {
            string bert = Path.Combine(runtimeRoot, "GPT_SoVITS", "pretrained_models", "chinese-roberta-wwm-ext-large");
            string hubert = Path.Combine(runtimeRoot, "GPT_SoVITS", "pretrained_models", "chinese-hubert-base");
            if (!Directory.Exists(bert) || !Directory.Exists(hubert))
            {
                throw new InvalidOperationException("GPT-SoVITS 缺少必要的预训练基础模型");
            }

            Directory.CreateDirectory(ManagedVoiceDirectory);
            string version = InferModelVersion(pthPath);
            var yaml = new StringBuilder();
            yaml.AppendLine("custom:");
            yaml.AppendLine("  bert_base_path: " + YamlQuote(bert));
            yaml.AppendLine("  cnhuhbert_base_path: " + YamlQuote(hubert));
            yaml.AppendLine("  device: cpu");
            yaml.AppendLine("  is_half: false");
            yaml.AppendLine("  t2s_weights_path: " + YamlQuote(ckptPath));
            yaml.AppendLine("  version: " + version);
            yaml.AppendLine("  vits_weights_path: " + YamlQuote(pthPath));
            string path = Path.Combine(ManagedVoiceDirectory, "tts_infer_iroha.yaml");
            File.WriteAllText(path, yaml.ToString(), new UTF8Encoding(false));
            return path;
        }

        private static string InferModelVersion(string path)
        {
            string name = (Path.GetFileName(path) ?? "").ToLowerInvariant();
            if (name.IndexOf("v2pp", StringComparison.Ordinal) >= 0 || name.IndexOf("v2proplus", StringComparison.Ordinal) >= 0 || name.IndexOf("proplus", StringComparison.Ordinal) >= 0) return "v2ProPlus";
            if (name.IndexOf("v2pro", StringComparison.Ordinal) >= 0) return "v2Pro";
            if (name.IndexOf("v4", StringComparison.Ordinal) >= 0) return "v4";
            if (name.IndexOf("v3", StringComparison.Ordinal) >= 0) return "v3";
            if (name.IndexOf("v1", StringComparison.Ordinal) >= 0) return "v1";
            return "v2";
        }

        private static string YamlQuote(string path)
        {
            return "'" + Path.GetFullPath(path).Replace('\\', '/').Replace("'", "''") + "'";
        }

        private static ZipArchiveEntry FindLargestEntry(ZipArchive archive, string extension)
        {
            ZipArchiveEntry best = null;
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (!entry.FullName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) continue;
                if (best == null || entry.Length > best.Length) best = entry;
            }
            return best;
        }

        private static ZipArchiveEntry FindEntryByFileName(ZipArchive archive, string fileName)
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string normalized = entry.FullName.Replace('\\', '/');
                int slash = normalized.LastIndexOf('/');
                string current = slash >= 0 ? normalized.Substring(slash + 1) : normalized;
                if (string.Equals(current, fileName, StringComparison.OrdinalIgnoreCase)) return entry;
            }
            return null;
        }

        private static string FindLargestFile(string root, string pattern)
        {
            try
            {
                string best = "";
                long length = -1;
                foreach (string path in Directory.GetFiles(root, pattern, SearchOption.AllDirectories))
                {
                    long current = new FileInfo(path).Length;
                    if (current > length)
                    {
                        length = current;
                        best = path;
                    }
                }
                return best;
            }
            catch { return ""; }
        }

        private static void AddCandidate(List<string> candidates, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            string clean = value.Trim().Trim('"');
            foreach (string existing in candidates)
            {
                if (string.Equals(existing, clean, StringComparison.OrdinalIgnoreCase)) return;
            }
            candidates.Add(clean);
        }

        private static string ResolveManagedBaseDirectory()
        {
            string configured = Environment.GetEnvironmentVariable("IROHA_VOICE_MANAGED_ROOT");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                string explicitPath = Path.GetFullPath(configured.Trim().Trim('"'));
                if (TryEnsureWritableDirectory(explicitPath)) return explicitPath;
                throw new IOException("配置的语音部署目录不可写：" + explicitPath);
            }

            var candidates = new List<string>();
            AddCandidate(
                candidates,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IrohaLocalAgent"));
            try { AddCandidate(candidates, SettingsStore.DirectoryPath); }
            catch { }
            AddCandidate(candidates, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "user-data"));

            foreach (string candidate in candidates)
            {
                if (TryEnsureWritableDirectory(candidate)) return Path.GetFullPath(candidate);
            }
            throw new IOException("没有可写的语音部署目录");
        }

        private static bool TryEnsureWritableDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            string probe = null;
            try
            {
                Directory.CreateDirectory(path);
                probe = Path.Combine(path, ".voice-write-test-" + Guid.NewGuid().ToString("N"));
                File.WriteAllText(probe, "ok", Encoding.ASCII);
                File.Delete(probe);
                return true;
            }
            catch
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(probe) && File.Exists(probe)) File.Delete(probe);
                }
                catch { }
                return false;
            }
        }

        private static void RecoverInterruptedDeployment()
        {
            string target = ManagedRuntimeDirectory;
            string backup = target + ".previous";
            if (Directory.Exists(backup))
            {
                string currentRoot = Directory.Exists(target) ? FindRuntimeUnder(target, 6) : "";
                if (!string.IsNullOrWhiteSpace(currentRoot))
                {
                    DeleteManagedDirectory(backup);
                }
                else
                {
                    if (Directory.Exists(target)) DeleteManagedDirectory(target);
                    Directory.Move(backup, target);
                }
            }

            try
            {
                foreach (string stale in Directory.GetDirectories(ManagedBaseDirectory, "VoiceRuntime.install-*", SearchOption.TopDirectoryOnly))
                {
                    DeleteManagedDirectory(stale);
                }
            }
            catch { }
        }

        private static bool IsPathUnder(string path, string root)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(root)) return false;
            try
            {
                string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.Equals(fullPath, fullRoot, StringComparison.OrdinalIgnoreCase) ||
                       fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static string LimitMessage(string value, int maxLength)
        {
            string clean = Regex.Replace(value ?? "", @"\s+", " ").Trim();
            if (clean.Length <= maxLength) return clean;
            return clean.Substring(0, Math.Max(0, maxLength - 1)) + "…";
        }

        private static string FormatGiB(long bytes)
        {
            return (Math.Max(0L, bytes) / 1073741824.0).ToString("0.0") + " GB";
        }

        private static void DeleteManagedDirectory(string target)
        {
            if (string.IsNullOrWhiteSpace(target) || !Directory.Exists(target)) return;
            string basePath = Path.GetFullPath(ManagedBaseDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullTarget = Path.GetFullPath(target).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullTarget.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("拒绝清理应用托管目录之外的文件");
            }
            try
            {
                Directory.Delete(target, true);
            }
            catch (UnauthorizedAccessException)
            {
                foreach (string file in Directory.GetFiles(target, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                Directory.Delete(target, true);
            }
        }

        private static void Report(Action<VoiceBootstrapProgress> callback, int percent, string stage, string detail, bool indeterminate)
        {
            if (callback == null) return;
            callback(new VoiceBootstrapProgress(percent, stage, detail, indeterminate));
        }
    }

    internal sealed class VoiceDeploymentForm : Form
    {
        private readonly Timer animationTimer;
        private readonly string windowTitle;
        private int progress;
        private float pulse;
        private string stage;
        private string detail;
        private bool indeterminate;
        private bool completed;
        private bool dismissing;
        private bool completionSucceeded;
        private DateTime dismissAtUtc;

        public VoiceDeploymentForm(bool redeploy)
        {
            windowTitle = redeploy ? "重新部署语音" : "首次语音准备";
            stage = redeploy ? "正在重新准备彩叶的声音" : "正在准备彩叶的声音";
            detail = redeploy ? "原始语音包不会被修改" : "首次部署只需要完成一次";
            progress = 2;
            indeterminate = true;
            Size = new Size(560, 216);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(244, 252, 255);
            DoubleBuffered = true;
            Opacity = 0;
            TopMost = false;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            Region = new Region(CreateRoundedRectangle(new Rectangle(0, 0, Width, Height), 24));

            animationTimer = new Timer();
            animationTimer.Interval = 32;
            animationTimer.Tick += AnimationTimer_Tick;
        }

        public void ShowFor(Form owner)
        {
            Rectangle bounds = owner == null ? Screen.PrimaryScreen.WorkingArea : owner.Bounds;
            Location = new Point(bounds.Left + (bounds.Width - Width) / 2, bounds.Top + (bounds.Height - Height) / 2);
            Show(owner);
            animationTimer.Start();
            BringToFront();
        }

        public void UpdateProgress(VoiceBootstrapProgress value)
        {
            if (value == null || IsDisposed) return;
            progress = value.Percent;
            stage = value.Stage;
            detail = value.Detail;
            indeterminate = value.IsIndeterminate;
            Invalidate();
        }

        public void Dismiss(bool success, string message)
        {
            if (IsDisposed) return;
            completionSucceeded = success;
            completed = true;
            progress = success ? 100 : progress;
            stage = success ? "彩叶的声音已经准备好了" : "语音暂时没有准备完成";
            detail = string.IsNullOrWhiteSpace(message) ? (success ? "以后启动会自动连接" : "文字聊天仍可正常使用") : message;
            indeterminate = false;
            dismissAtUtc = DateTime.UtcNow.AddMilliseconds(success ? 1050 : 1550);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            Rectangle card = new Rectangle(1, 1, Width - 3, Height - 3);
            using (GraphicsPath path = CreateRoundedRectangle(card, 23))
            using (var fill = new LinearGradientBrush(card, Color.FromArgb(252, 255, 255), Color.FromArgb(232, 248, 253), 90F))
            using (var border = new Pen(Color.FromArgb(118, 135, 209, 225), 1F))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }

            using (var titleFont = new Font("Microsoft YaHei UI", 13.6F, FontStyle.Bold))
            using (var stageFont = new Font("Microsoft YaHei UI", 10.4F, FontStyle.Bold))
            using (var detailFont = new Font("Microsoft YaHei UI", 8.6F, FontStyle.Regular))
            using (var percentFont = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(Color.FromArgb(35, 72, 102)))
            using (var stageBrush = new SolidBrush(completed && !completionSucceeded ? Color.FromArgb(170, 92, 92) : Color.FromArgb(41, 126, 150)))
            using (var detailBrush = new SolidBrush(Color.FromArgb(101, 132, 153)))
            {
                g.DrawString(windowTitle, titleFont, titleBrush, new PointF(34, 25));
                g.DrawString(stage ?? "", stageFont, stageBrush, new RectangleF(34, 73, Width - 68, 30));
                g.DrawString(detail ?? "", detailFont, detailBrush, new RectangleF(34, 107, Width - 68, 27));
                g.DrawString(progress + "%", percentFont, stageBrush, new RectangleF(Width - 86, 27, 50, 24), new StringFormat { Alignment = StringAlignment.Far });
            }

            int dotY = 48;
            for (int i = 0; i < 3; i++)
            {
                double wave = (Math.Sin(pulse + i * 1.4) + 1.0) * 0.5;
                int alpha = 70 + (int)Math.Round(wave * 150);
                int size = 5 + (int)Math.Round(wave * 3);
                using (var dot = new SolidBrush(Color.FromArgb(alpha, 54, 194, 207)))
                {
                    g.FillEllipse(dot, 36 + i * 13, dotY - size / 2, size, size);
                }
            }

            Rectangle track = new Rectangle(34, 157, Width - 68, 12);
            using (GraphicsPath trackPath = CreateRoundedRectangle(track, 6))
            using (var trackBrush = new SolidBrush(Color.FromArgb(170, 210, 229, 237)))
            {
                g.FillPath(trackBrush, trackPath);
            }
            int fillWidth = Math.Max(8, (int)Math.Round(track.Width * progress / 100.0));
            Rectangle fillRect = new Rectangle(track.X, track.Y, Math.Min(track.Width, fillWidth), track.Height);
            using (GraphicsPath fillPath = CreateRoundedRectangle(fillRect, 6))
            using (var fillBrush = new LinearGradientBrush(fillRect, Color.FromArgb(78, 207, 214), Color.FromArgb(75, 166, 220), 0F))
            {
                g.FillPath(fillBrush, fillPath);
            }
            if (indeterminate && fillRect.Width > 20)
            {
                int shimmerX = fillRect.X + (int)((Math.Sin(pulse * 0.65) + 1.0) * 0.5 * Math.Max(1, fillRect.Width - 18));
                using (var shimmer = new SolidBrush(Color.FromArgb(80, 255, 255, 255)))
                {
                    g.FillRectangle(shimmer, shimmerX, fillRect.Y + 2, 18, fillRect.Height - 4);
                }
            }

            using (var noteFont = new Font("Microsoft YaHei UI", 7.8F, FontStyle.Regular))
            using (var noteBrush = new SolidBrush(Color.FromArgb(112, 137, 154)))
            {
                g.DrawString("请保持应用开启，部署期间仍可查看当前界面", noteFont, noteBrush, new PointF(34, 181));
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            animationTimer.Stop();
            animationTimer.Dispose();
            base.OnFormClosed(e);
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            pulse += 0.16F;
            if (!dismissing && Opacity < 0.99) Opacity = Math.Min(1.0, Opacity + 0.11);
            if (completed && DateTime.UtcNow >= dismissAtUtc) dismissing = true;
            if (dismissing)
            {
                Opacity = Math.Max(0, Opacity - 0.11);
                if (Opacity <= 0.01)
                {
                    animationTimer.Stop();
                    Close();
                    return;
                }
            }
            Invalidate();
        }

        private static GraphicsPath CreateRoundedRectangle(Rectangle rectangle, int radius)
        {
            var path = new GraphicsPath();
            int diameter = Math.Max(2, radius * 2);
            Rectangle arc = new Rectangle(rectangle.X, rectangle.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = rectangle.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rectangle.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rectangle.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
