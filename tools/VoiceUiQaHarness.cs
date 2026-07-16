using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace IrohaAgentDesktop
{
    internal static class VoiceUiQaProgram
    {
        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr windowHandle, IntPtr targetDc, uint flags);

        [STAThread]
        private static void Main(string[] args)
        {
            string screenshot = GetArgument(args, "--screenshot");
            string report = GetArgument(args, "--report");
            if (string.IsNullOrWhiteSpace(screenshot) || string.IsNullOrWhiteSpace(report))
            {
                throw new ArgumentException("--screenshot and --report are required");
            }

            var results = new List<string>();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var form = new MainForm())
            {
                AppSettings settings = GetPrivateField<AppSettings>(form, "settings");
                settings.VoiceEnabled = true;
                settings.VoiceServerUrl = "http://127.0.0.1:9880";
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(24, 24);
                form.Size = new Size(1280, 720);
                form.Show();

                DateTime readyDeadline = DateTime.UtcNow.AddSeconds(15);
                while (!GetPrivateField<bool>(form, "voiceServiceReady") && DateTime.UtcNow < readyDeadline)
                {
                    Application.DoEvents();
                    Thread.Sleep(20);
                }
                Assert(GetPrivateField<bool>(form, "voiceServiceReady"), "voice service becomes ready in the UI", results);

                Button voiceButton = GetPrivateField<Button>(form, "testVoiceButton");
                AvatarControl avatar = GetPrivateField<AvatarControl>(form, "avatar");
                VoiceStatusLabel voiceState = GetPrivateField<VoiceStatusLabel>(form, "voiceStateLabel");
                WaveformControl waveform = GetPrivateField<WaveformControl>(form, "waveform");
                InvokePrivateMethod(form, "TestVoiceButton_Click", voiceButton, EventArgs.Empty);

                bool speakingSeen = false;
                DateTime speakingDeadline = DateTime.UtcNow.AddSeconds(90);
                while (DateTime.UtcNow < speakingDeadline)
                {
                    Application.DoEvents();
                    if (avatar.State == AvatarState.Speaking)
                    {
                        speakingSeen = true;
                        Capture(form, screenshot);
                        Assert(voiceState.Text.Contains("说话中"), "voice dock reports speaking", results);
                        Assert(waveform.Active, "waveform activates during speech", results);
                        break;
                    }
                    Thread.Sleep(20);
                }
                Assert(speakingSeen, "speaking state is reached", results);

                DateTime finishDeadline = DateTime.UtcNow.AddSeconds(30);
                while (!voiceButton.Enabled && DateTime.UtcNow < finishDeadline)
                {
                    Application.DoEvents();
                    Thread.Sleep(20);
                }
                Assert(voiceButton.Enabled, "voice test returns to an interactive state", results);
                Assert(!voiceState.Text.Contains("说话中"), "voice dock leaves speaking state", results);
                form.Close();
            }

            string reportDirectory = Path.GetDirectoryName(report);
            if (!string.IsNullOrEmpty(reportDirectory)) Directory.CreateDirectory(reportDirectory);
            File.WriteAllLines(report, results.ToArray(), new UTF8Encoding(false));
        }

        private static void Capture(Form form, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            using (var bitmap = new Bitmap(form.Width, form.Height))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    if (!PrintWindow(form.Handle, hdc, 2)) throw new InvalidOperationException("PrintWindow failed");
                }
                finally
                {
                    graphics.ReleaseHdc(hdc);
                }
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
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
            if (!condition) throw new InvalidOperationException(line);
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
