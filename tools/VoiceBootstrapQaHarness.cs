using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace IrohaAgentDesktop
{
    internal static class VoiceBootstrapQaProgram
    {
        private static readonly List<string> Results = new List<string>();
        private static string ReportPath = "";

        private static void Main(string[] args)
        {
            string output = GetArgument(args, "--output");
            string workRoot = GetArgument(args, "--work-root");
            if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(workRoot))
            {
                throw new ArgumentException("--output and --work-root are required");
            }
            ReportPath = Path.GetFullPath(output);

            workRoot = Path.GetFullPath(workRoot);
            Directory.CreateDirectory(workRoot);
            string managedBase = Path.Combine(workRoot, "managed-base");
            string externalRuntime = Path.Combine(workRoot, "external-runtime");
            string voicePackage = Path.Combine(workRoot, "iroha-model.zip");
            Environment.SetEnvironmentVariable("IROHA_VOICE_MANAGED_ROOT", managedBase);
            Environment.SetEnvironmentVariable("IROHA_VOICE_PACKAGE", voicePackage);

            CreateFakeRuntime(externalRuntime);
            CreateFakeVoicePackage(voicePackage);
            string sourceHash = HashFile(voicePackage);
            string externalSentinel = Path.Combine(externalRuntime, "external-owner-sentinel.txt");

            var settings = new AppSettings
            {
                VoiceRuntimeRoot = externalRuntime,
                VoiceRuntimeConfigPath = Path.Combine(externalRuntime, "missing.yaml"),
                VoiceRefAudioPath = Path.Combine(externalRuntime, "missing.wav"),
                VoicePromptText = AppSettings.DefaultVoicePromptText,
                VoicePromptLang = AppSettings.DefaultVoicePromptLang
            };

            var firstProgress = new List<VoiceBootstrapProgress>();
            VoiceBootstrapResult first = VoiceBootstrapper.Prepare(settings, false, firstProgress.Add);
            Assert(first.Success, "first-run matching succeeds");
            Assert(!first.RuntimeDeployed, "existing external runtime is reused");
            Assert(first.VoiceImported, "voice package is imported automatically");
            Assert(File.Exists(first.ConfigPath), "managed GPT-SoVITS config is generated");
            string generatedConfig = File.ReadAllText(first.ConfigPath, Encoding.UTF8);
            Assert(generatedConfig.Contains("device: cpu"), "generated config defaults to CPU compatibility mode");
            Assert(generatedConfig.Contains("is_half: false"), "generated config defaults to full precision");
            Assert(File.Exists(first.RefAudioPath), "reference audio is imported");
            Assert(first.PromptLang == "ja", "reference language is normalized to ja");
            Assert(first.PromptText == "さすがにここは危ないかもいや、警察に届けるか", "reference transcript is imported");
            Assert(IsUnder(first.ConfigPath, managedBase), "generated config stays in managed app data");
            Assert(File.Exists(externalSentinel), "external runtime remains untouched after matching");
            Assert(HashFile(voicePackage) == sourceHash, "source voice package remains byte-identical");
            Assert(HasStage(firstProgress, "正在导入彩叶语音模型"), "matching emits model import progress");

            settings.VoiceRuntimeRoot = first.RuntimeRoot;
            settings.VoiceRuntimeConfigPath = first.ConfigPath;
            settings.VoiceRefAudioPath = first.RefAudioPath;
            settings.VoicePromptText = first.PromptText;
            settings.VoicePromptLang = first.PromptLang;

            string bundleDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "voice-runtime");
            CreateBundledRuntimeArchive(externalRuntime, bundleDirectory);
            string staleRuntimeFile = Path.Combine(VoiceBootstrapper.ManagedRuntimeDirectory, "stale.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(staleRuntimeFile));
            File.WriteAllText(staleRuntimeFile, "stale", Encoding.UTF8);

            var redeployProgress = new List<VoiceBootstrapProgress>();
            VoiceBootstrapResult redeployed = VoiceBootstrapper.Prepare(settings, true, redeployProgress.Add);
            Assert(redeployed.Success, "forced redeploy succeeds");
            Assert(redeployed.RuntimeDeployed, "bundled runtime archive is deployed");
            Assert(IsUnder(redeployed.RuntimeRoot, VoiceBootstrapper.ManagedRuntimeDirectory), "redeployed runtime stays in managed app data");
            Assert(!File.Exists(staleRuntimeFile), "stale managed runtime content is replaced");
            Assert(File.Exists(externalSentinel), "forced redeploy never removes the external runtime");
            Assert(HashFile(voicePackage) == sourceHash, "forced redeploy never modifies the source voice package");
            Assert(HasStage(redeployProgress, "正在部署 GPT-SoVITS"), "redeploy emits runtime deployment progress");
            Assert(HasStage(redeployProgress, "语音组件匹配完成"), "redeploy emits matching completion progress");

            string reportDirectory = Path.GetDirectoryName(ReportPath);
            if (!string.IsNullOrWhiteSpace(reportDirectory)) Directory.CreateDirectory(reportDirectory);
            File.WriteAllLines(ReportPath, Results.ToArray(), new UTF8Encoding(false));
        }

        private static void CreateFakeRuntime(string root)
        {
            Directory.CreateDirectory(Path.Combine(root, "runtime"));
            Directory.CreateDirectory(Path.Combine(root, "GPT_SoVITS", "pretrained_models", "chinese-roberta-wwm-ext-large"));
            Directory.CreateDirectory(Path.Combine(root, "GPT_SoVITS", "pretrained_models", "chinese-hubert-base"));
            File.WriteAllBytes(Path.Combine(root, "runtime", "python.exe"), new byte[] { 77, 90, 0, 0 });
            File.WriteAllText(Path.Combine(root, "api_v2.py"), "# QA fixture", new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(root, "external-owner-sentinel.txt"), "do not delete", new UTF8Encoding(false));
        }

        private static void CreateFakeVoicePackage(string path)
        {
            if (File.Exists(path)) File.Delete(path);
            using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "model/iroha_v2pp-e15.ckpt", CreateBytes(4096, 23));
                WriteEntry(archive, "model/iroha_v2pp_e8_s128.pth", CreateBytes(3072, 41));
                WriteEntry(archive, "dataset/ref.wav", CreateWav());
                WriteEntry(
                    archive,
                    "dataset/train.list",
                    Encoding.UTF8.GetBytes("dataset/ref.wav|iroha|JA|さすがにここは危ないかもいや、警察に届けるか\n"));
            }
        }

        private static void CreateBundledRuntimeArchive(string sourceRuntime, string bundleDirectory)
        {
            Directory.CreateDirectory(bundleDirectory);
            string archive = Path.Combine(bundleDirectory, "GPT-SoVITS-runtime.7z");
            if (File.Exists(archive)) File.Delete(archive);
            string sevenZip = @"C:\Program Files\7-Zip\7z.exe";
            if (!File.Exists(sevenZip)) throw new FileNotFoundException("7-Zip is required for deployment QA", sevenZip);

            string tools = Path.Combine(bundleDirectory, "tools");
            Directory.CreateDirectory(tools);
            File.Copy(sevenZip, Path.Combine(tools, "7z.exe"), true);
            string sevenZipDll = Path.Combine(Path.GetDirectoryName(sevenZip), "7z.dll");
            if (File.Exists(sevenZipDll)) File.Copy(sevenZipDll, Path.Combine(tools, "7z.dll"), true);

            var info = new ProcessStartInfo();
            info.FileName = sevenZip;
            info.Arguments = "a -t7z -mx=1 \"" + archive + "\" \"" + Path.Combine(sourceRuntime, "*") + "\"";
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            using (Process process = Process.Start(info))
            {
                if (process == null) throw new InvalidOperationException("Unable to start 7-Zip");
                process.WaitForExit();
                if (process.ExitCode != 0) throw new InvalidOperationException("7-Zip fixture creation failed: " + process.ExitCode);
            }
        }

        private static byte[] CreateBytes(int length, int seed)
        {
            byte[] bytes = new byte[length];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = (byte)((i * seed + 17) % 251);
            return bytes;
        }

        private static byte[] CreateWav()
        {
            const int sampleRate = 16000;
            const int sampleCount = 1600;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.ASCII))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + sampleCount * 2);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(sampleRate);
                writer.Write(sampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(sampleCount * 2);
                for (int i = 0; i < sampleCount; i++) writer.Write((short)(Math.Sin(i * 0.08) * 5000));
                writer.Flush();
                return stream.ToArray();
            }
        }

        private static void WriteEntry(ZipArchive archive, string name, byte[] value)
        {
            ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.Fastest);
            using (Stream output = entry.Open()) output.Write(value, 0, value.Length);
        }

        private static string HashFile(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream input = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(input)).Replace("-", "");
            }
        }

        private static bool IsUnder(string path, string root)
        {
            string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
            string fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            return string.Equals(fullPath, fullRoot, StringComparison.OrdinalIgnoreCase) ||
                   fullPath.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasStage(List<VoiceBootstrapProgress> progress, string stage)
        {
            foreach (VoiceBootstrapProgress item in progress)
            {
                if (string.Equals(item.Stage, stage, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static void Assert(bool condition, string description)
        {
            string line = (condition ? "PASS  " : "FAIL  ") + description;
            Results.Add(line);
            if (!string.IsNullOrWhiteSpace(ReportPath))
            {
                string directory = Path.GetDirectoryName(ReportPath);
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                File.WriteAllLines(ReportPath, Results.ToArray(), new UTF8Encoding(false));
            }
            if (!condition) throw new InvalidOperationException(line);
        }

        private static string GetArgument(string[] args, string name)
        {
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }
            return "";
        }
    }
}
