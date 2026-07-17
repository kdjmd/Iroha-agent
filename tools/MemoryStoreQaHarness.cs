using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IrohaAgentDesktop
{
    internal static class MemoryStoreQaProgram
    {
        private static readonly List<string> Results = new List<string>();
        private static string ReportPath = "";

        private static void Main(string[] args)
        {
            string output = GetArgument(args, "--output");
            if (!string.IsNullOrWhiteSpace(output)) ReportPath = Path.GetFullPath(output);
            try
            {
                Run(args);
            }
            catch (Exception ex)
            {
                Results.Add("ERROR " + (ex.Message ?? "Memory QA failed"));
                FlushResults();
                Environment.ExitCode = 1;
            }
        }

        private static void Run(string[] args)
        {
            string output = GetArgument(args, "--output");
            string workRoot = GetArgument(args, "--work-root");
            if (string.IsNullOrWhiteSpace(output) || string.IsNullOrWhiteSpace(workRoot))
            {
                throw new ArgumentException("--output and --work-root are required");
            }

            workRoot = Path.GetFullPath(workRoot);
            Directory.CreateDirectory(workRoot);
            Environment.SetEnvironmentVariable("IROHA_APP_DATA_ROOT", workRoot);

            var first = new AgentMemory();
            first.Notes.Add("2026-07-17 用户偏好：请记住，我喜欢简洁的中文回答。");
            first.Notes.Add("2026-07-17 手动记忆：日语语音保持自然。こんにちは。");
            MemoryStore.Save(first);

            AgentMemory loaded = MemoryStore.Load();
            Assert(loaded.Notes.Count == 2, "UTF-8 memory round-trip keeps every note");
            Assert(loaded.Notes[1].IndexOf("こんにちは", StringComparison.Ordinal) >= 0, "Japanese memory text survives round-trip");
            Assert(!File.Exists(MemoryStore.FilePath + ".tmp"), "atomic save leaves no temporary file");

            var second = new AgentMemory();
            second.Notes.Add("2026-07-17 用户偏好：以后请叫我测试用户。");
            MemoryStore.Save(second);
            Assert(File.Exists(MemoryStore.FilePath + ".bak"), "second save creates a recovery backup");

            File.WriteAllText(MemoryStore.FilePath, "{broken-json", new UTF8Encoding(false));
            AgentMemory recovered = MemoryStore.Load();
            Assert(recovered.Notes.Count == 2, "corrupt primary memory falls back to the last valid backup");
            Assert(File.Exists(MemoryStore.FilePath + ".corrupt"), "corrupt memory is preserved for manual recovery");
            Assert(MemoryStore.Load().Notes.Count == 2, "recovered backup is restored as the new primary file");

            var noisy = new AgentMemory();
            noisy.Notes.Add(null);
            noisy.Notes.Add("  ");
            for (int i = 0; i < 95; i++)
            {
                noisy.Notes.Add("2026-07-17 用户偏好：测试偏好 " + i);
            }
            noisy.Notes.Add("2026-07-17 用户说：测试偏好 94");
            MemoryStore.Save(noisy);
            AgentMemory normalized = MemoryStore.Load();
            Assert(normalized.Notes.Count == MemoryCapture.MaxNotes, "memory is bounded to the configured maximum");
            Assert(normalized.Notes[normalized.Notes.Count - 1].IndexOf("测试偏好 94", StringComparison.Ordinal) >= 0, "normalization keeps the newest durable memory");

            string note;
            Assert(MemoryCapture.TryCreateNote("请记住，我喜欢简洁的回答。", out note), "explicit preference is remembered");
            Assert(MemoryCapture.TryCreateNote("以后请叫我小王。", out note), "future naming preference is remembered");
            Assert(MemoryCapture.TryCreateNote("我的职业是设计师。", out note), "durable identity is remembered");
            Assert(!MemoryCapture.TryCreateNote("我想要你帮我修复这个程序。", out note), "one-off task is not recorded as long-term memory");
            Assert(!MemoryCapture.TryCreateNote("这是我的代码，帮我检查。", out note), "generic possessive chat is not recorded as memory");
            Assert(!MemoryCapture.TryCreateNote("请记住我的 API Key 是 sk-test-secret-value。", out note), "secrets are never recorded as memory");

            Parallel.For(0, 24, delegate(int index)
            {
                var concurrent = new AgentMemory();
                concurrent.Notes.Add("2026-07-17 用户偏好：并发写入 " + index);
                MemoryStore.Save(concurrent);
            });
            AgentMemory afterConcurrentWrites = MemoryStore.Load();
            Assert(afterConcurrentWrites.Notes.Count == 1, "concurrent saves leave one complete JSON document");
            Assert(!File.Exists(MemoryStore.FilePath + ".tmp"), "concurrent saves leave no partial temporary file");

            var settingsOne = new AppSettings { Model = "deepseek-v4-flash", ApiKey = "qa-first" };
            var settingsTwo = new AppSettings { Model = "deepseek-v4-pro", ApiKey = "qa-second" };
            SettingsStore.Save(settingsOne);
            SettingsStore.Save(settingsTwo);
            File.WriteAllText(SettingsStore.FilePath, "{broken-settings", new UTF8Encoding(false));
            AppSettings recoveredSettings = SettingsStore.Load();
            Assert(recoveredSettings.ApiKey == "qa-first", "corrupt settings fall back to the last valid backup");

            ReportPath = Path.GetFullPath(output);
            FlushResults();
        }

        private static void Assert(bool condition, string description)
        {
            string line = (condition ? "PASS  " : "FAIL  ") + description;
            Results.Add(line);
            FlushResults();
            if (!condition) throw new InvalidOperationException(line);
        }

        private static void FlushResults()
        {
            if (string.IsNullOrWhiteSpace(ReportPath)) return;
            string directory = Path.GetDirectoryName(ReportPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.WriteAllLines(ReportPath, Results.ToArray(), new UTF8Encoding(false));
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
