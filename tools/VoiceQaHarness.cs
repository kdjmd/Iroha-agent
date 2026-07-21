using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IrohaAgentDesktop
{
    internal static class VoiceQaProgram
    {
        private static string reportPath = "";
        private static readonly List<string> liveResults = new List<string>();

        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                Run(args);
            }
            catch (Exception ex)
            {
                liveResults.Add("ERROR " + NormalizeDiagnostic(ex.Message));
                FlushResults();
                Environment.ExitCode = 1;
            }
        }

        private static void Run(string[] args)
        {
            string output = GetArgument(args, "--output");
            if (string.IsNullOrWhiteSpace(output))
            {
                throw new ArgumentException("--output is required");
            }
            reportPath = Path.GetFullPath(output);

            var results = liveResults;
            WindowsFormsSynchronizationContext.AutoInstall = false;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var form = new MainForm())
            {
                AppSettings settings = GetPrivateField<AppSettings>(form, "settings");
                settings.VoiceEnabled = true;
                settings.VoiceServerUrl = "http://127.0.0.1:9880";
                settings.VoiceRefAudioPath = AppSettings.DefaultVoiceRefAudioPath;
                settings.VoicePromptText = AppSettings.DefaultVoicePromptText;
                settings.VoicePromptLang = AppSettings.DefaultVoicePromptLang;

                Task<bool> readyTask = (Task<bool>)InvokePrivateMethod(form, "EnsureVoiceServiceReadyAsync");
                var startupTimer = Stopwatch.StartNew();
                int nextHeartbeat = 30;
                while (!readyTask.Wait(5000))
                {
                    int elapsedSeconds = (int)startupTimer.Elapsed.TotalSeconds;
                    if (elapsedSeconds < nextHeartbeat) continue;
                    string currentDiagnostics = Convert.ToString(InvokePrivateMethod(form, "GetVoiceServiceDiagnostic"));
                    results.Add(
                        "INFO  voice_startup_elapsed_seconds=" + elapsedSeconds +
                        " diagnostics=" + NormalizeDiagnostic(currentDiagnostics));
                    FlushResults();
                    nextHeartbeat += 30;
                }
                bool serviceReady = readyTask.GetAwaiter().GetResult();
                if (!serviceReady)
                {
                    string setupMessage = GetPrivateField<string>(form, "voiceSetupMessage");
                    string diagnostics = Convert.ToString(InvokePrivateMethod(form, "GetVoiceServiceDiagnostic"));
                    results.Add("INFO  voice_setup_message=" + NormalizeDiagnostic(setupMessage));
                    results.Add("INFO  voice_process_diagnostics=" + NormalizeDiagnostic(diagnostics));
                    FlushResults();
                }
                Assert(serviceReady, "voice service health check", results);

                var timer = Stopwatch.StartNew();
                const string FullVoiceSample = "おはようございます。また会えて本当にうれしいです。今日はどんなことを話したいですか。あなたの考えや気持ちを一つも省略せずに受け止めながら、計画を立てたり、アイデアを探したり、これまでの歩みを一緒に振り返ったりできます。数字や箇条書き、細かな条件も含めて、画面に表示された内容を最後まで忠実にお伝えします。急がなくて大丈夫です。あなたのペースで話してください。";
                Task<string> prepareTask = (Task<string>)InvokePrivateMethod(
                    form,
                    "PrepareVoiceAudioFileAsync",
                    FullVoiceSample);
                string wavPath = prepareTask.GetAwaiter().GetResult();
                timer.Stop();
                if (string.IsNullOrWhiteSpace(wavPath) || !File.Exists(wavPath))
                {
                    RichTextBox feedback = GetPrivateField<RichTextBox>(form, "chatLog");
                    results.Add("INFO  voice_feedback=" + (feedback.Text ?? "").Replace("\r", " ").Replace("\n", " | "));
                    FlushResults();
                }
                Assert(!string.IsNullOrWhiteSpace(wavPath) && File.Exists(wavPath), "voice WAV generated", results);
                Assert(timer.Elapsed < TimeSpan.FromSeconds(90), "voice generation completes within timeout", results);
                results.Add("INFO  generation_seconds=" + timer.Elapsed.TotalSeconds.ToString("0.00"));

                double peak;
                double rms;
                double duration;
                InspectPcm16Wav(wavPath, out peak, out rms, out duration);
                Assert(duration > 12.0 && duration < 60.0, "full multi-sentence voice WAV duration is plausible", results);
                Assert(peak >= 0.82 && peak <= 0.91, "voice peak is automatically normalized", results);
                Assert(rms > 0.004, "voice WAV is not silent", results);
                results.Add(
                    "INFO  duration_seconds=" + duration.ToString("0.00") +
                    " peak_dbfs=" + (20.0 * Math.Log10(Math.Max(0.000001, peak))).ToString("0.0") +
                    " rms_dbfs=" + (20.0 * Math.Log10(Math.Max(0.000001, rms))).ToString("0.0"));

                Task<bool> playTask = (Task<bool>)InvokePrivateMethod(form, "PlayPreparedVoiceAsync", wavPath);
                Assert(playTask.GetAwaiter().GetResult(), "SoundPlayer playback succeeds", results);
                Assert(!File.Exists(wavPath), "temporary voice WAV is cleaned", results);

                string missingPath = Path.Combine(Path.GetTempPath(), "iroha-missing-voice-" + Guid.NewGuid().ToString("N") + ".wav");
                Task<bool> failedPlayTask = (Task<bool>)InvokePrivateMethod(form, "PlayPreparedVoiceAsync", missingPath);
                Assert(!failedPlayTask.GetAwaiter().GetResult(), "playback failure degrades without throwing", results);

                string invalidPath = Path.Combine(Path.GetTempPath(), "iroha-invalid-voice-" + Guid.NewGuid().ToString("N") + ".wav");
                File.WriteAllBytes(invalidPath, Encoding.UTF8.GetBytes(new string('x', 8192)));
                try
                {
                    object[] parameters = { invalidPath, 0.0, 0.0 };
                    bool accepted = (bool)InvokePrivateMethod(form, "TryPrepareVoiceWavForPlayback", parameters);
                    Assert(!accepted, "non-WAV payload is rejected", results);
                }
                finally
                {
                    if (File.Exists(invalidPath)) File.Delete(invalidPath);
                }
            }

            string directory = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllLines(output, results.ToArray(), new UTF8Encoding(false));
        }

        private static void InspectPcm16Wav(string path, out double peak, out double rms, out double duration)
        {
            byte[] bytes = File.ReadAllBytes(path);
            int dataOffset = -1;
            int dataSize = 0;
            int byteRate = 0;
            int index = 12;
            while (index + 8 <= bytes.Length)
            {
                string id = Encoding.ASCII.GetString(bytes, index, 4);
                int size = BitConverter.ToInt32(bytes, index + 4);
                if (size < 0 || index + 8 + size > bytes.Length) break;
                if (id == "fmt " && size >= 16)
                {
                    byteRate = BitConverter.ToInt32(bytes, index + 16);
                }
                else if (id == "data")
                {
                    dataOffset = index + 8;
                    dataSize = size;
                }
                index += 8 + size;
                if ((size & 1) == 1) index++;
            }
            if (dataOffset < 0 || dataSize <= 0 || byteRate <= 0)
            {
                throw new InvalidDataException("WAV data chunk is missing");
            }

            int sampleCount = dataSize / 2;
            int absolutePeak = 0;
            double squareSum = 0.0;
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = BitConverter.ToInt16(bytes, dataOffset + i * 2);
                int absolute = sample == short.MinValue ? 32768 : Math.Abs((int)sample);
                if (absolute > absolutePeak) absolutePeak = absolute;
                double normalized = sample / 32768.0;
                squareSum += normalized * normalized;
            }
            peak = absolutePeak / 32768.0;
            rms = Math.Sqrt(squareSum / Math.Max(1, sampleCount));
            duration = dataSize / (double)byteRate;
        }

        private static T GetPrivateField<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, name);
            return (T)field.GetValue(target);
        }

        private static object InvokePrivateMethod(object target, string name, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(target.GetType().FullName, name);
            return method.Invoke(target, args);
        }

        private static void Assert(bool condition, string description, List<string> results)
        {
            string line = (condition ? "PASS  " : "FAIL  ") + description;
            results.Add(line);
            FlushResults();
            if (!condition) throw new InvalidOperationException(line);
        }

        private static void FlushResults()
        {
            if (string.IsNullOrWhiteSpace(reportPath)) return;
            string directory = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllLines(reportPath, liveResults.ToArray(), new UTF8Encoding(false));
        }

        private static string NormalizeDiagnostic(string value)
        {
            string clean = (value ?? "").Replace("\r", " ").Replace("\n", " ").Trim();
            return clean.Length <= 600 ? clean : clean.Substring(0, 599) + "…";
        }

        private static string GetArgument(string[] args, string name)
        {
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }
            return null;
        }
    }
}
