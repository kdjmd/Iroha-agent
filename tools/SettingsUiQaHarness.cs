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
    internal static class SettingsUiQaProgram
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                Run(args);
            }
            catch (Exception ex)
            {
                string message = "FAIL: " + ex.GetType().Name + ": " + ex.Message;
                string report = GetArgument(args, "--report");
                try
                {
                    string directory = Path.GetDirectoryName(report);
                    if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
                    if (!string.IsNullOrWhiteSpace(report)) File.WriteAllText(report, message, new UTF8Encoding(false));
                }
                catch { }
                Console.Error.WriteLine(message);
                Environment.ExitCode = 1;
            }
        }

        private static void Run(string[] args)
        {
            try { SetProcessDPIAware(); }
            catch { }
            string modelShot = GetArgument(args, "--model-screenshot");
            string voiceShot = GetArgument(args, "--voice-screenshot");
            string compactShot = GetArgument(args, "--compact-screenshot");
            string toolShot = GetArgument(args, "--tool-screenshot");
            string toolPrivacyShot = GetArgument(args, "--tool-privacy-screenshot");
            string toolSkillsShot = GetArgument(args, "--tool-skills-screenshot");
            string report = GetArgument(args, "--report");
            int viewportWidth = GetIntArgument(args, "--width", 1280);
            int viewportHeight = GetIntArgument(args, "--height", 720);
            if (string.IsNullOrWhiteSpace(modelShot) || string.IsNullOrWhiteSpace(voiceShot) || string.IsNullOrWhiteSpace(compactShot) || string.IsNullOrWhiteSpace(toolShot) || string.IsNullOrWhiteSpace(toolPrivacyShot) || string.IsNullOrWhiteSpace(toolSkillsShot) || string.IsNullOrWhiteSpace(report))
            {
                throw new ArgumentException("--model-screenshot, --voice-screenshot, --compact-screenshot, --tool-screenshot and --report are required");
            }

            var results = new List<string>();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var form = new MainForm())
            {
                AppSettings settings = GetPrivateField<AppSettings>(form, "settings");
                settings.VoiceEnabled = false;
                settings.ApiKey = string.Empty;
                if (settings.ProviderApiKeys != null && !string.IsNullOrWhiteSpace(settings.ProviderId))
                {
                    settings.ProviderApiKeys[settings.ProviderId] = string.Empty;
                }
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(30, 30);
                form.Size = new Size(viewportWidth, viewportHeight);
                form.Show();
                Application.DoEvents();

                List<Button> quickActions = GetPrivateField<List<Button>>(form, "quickActionButtons");
                bool quickActionsHaveCleanCaptions = true;
                foreach (Button quickAction in quickActions)
                {
                    var glassQuickAction = quickAction as GlassButton;
                    if (glassQuickAction != null && !string.IsNullOrWhiteSpace(glassQuickAction.SecondaryText))
                    {
                        quickActionsHaveCleanCaptions = false;
                        break;
                    }
                }
                Assert(quickActionsHaveCleanCaptions, "quick action buttons have no clipped secondary captions", results);

                InvokePrivateMethod(form, "ToggleSettingsDrawer");
                WaitForPaint();
                GlassPanel drawer = GetPrivateField<GlassPanel>(form, "settingsDrawer");
                ComboBox providers = GetPrivateField<ComboBox>(form, "drawerProviderBox");
                ComboBox models = GetPrivateField<ComboBox>(form, "drawerModelBox");
                Button redeploy = GetPrivateField<Button>(form, "drawerRedeployVoiceButton");
                Assert(drawer.Visible, "settings drawer is visible", results);
                Assert(providers.Visible && models.Visible, "model page is visible", results);
                Assert(!redeploy.Visible, "voice controls do not stack under model page", results);
                if (viewportWidth >= 1450 && viewportHeight >= 800)
                {
                    Assert(GetPrivateField<GlassPanel>(form, "memoryCard").Visible, "full viewport shows memory cards", results);
                }
                Capture(form, modelShot);

                Button voiceTab = GetPrivateField<Button>(form, "drawerVoiceTabButton");
                InvokeControlClick(voiceTab);
                WaitForPaint();
                Assert(redeploy.Visible, "voice redeploy is visible on voice page", results);
                Assert(!providers.Visible && !models.Visible, "model controls do not stack under voice page", results);
                Assert(drawer.ClientRectangle.Contains(redeploy.Bounds), "voice redeploy stays inside drawer", results);
                Capture(form, voiceShot);

                InvokePrivateMethod(form, "HideSettingsDrawer");
                form.Size = new Size(1280, 720);
                WaitForPaint();
                GlassPanel voiceDock = GetPrivateField<GlassPanel>(form, "voiceDock");
                GlassPanel inputComposer = GetPrivateField<GlassPanel>(form, "inputComposer");
                FooterBarControl footerBar = GetPrivateField<FooterBarControl>(form, "footerBar");
                Button testVoice = GetPrivateField<Button>(form, "testVoiceButton");
                Label voiceState = GetPrivateField<Label>(form, "voiceStateLabel");
                Control voiceDivider = GetPrivateField<Control>(form, "voiceDockDivider");
                WaveformControl waveform = GetPrivateField<WaveformControl>(form, "waveform");
                Label voiceEngine = GetPrivateField<Label>(form, "voiceEngineLabel");
                Label status = GetPrivateField<Label>(form, "statusLabel");
                Label quickHint = GetPrivateField<Label>(form, "quickHintLabel");
                Assert(voiceDock.Visible && voiceDock.Width >= 320, "compact viewport keeps a usable voice dock", results);
                Assert(testVoice.Visible && voiceDock.ClientRectangle.Contains(testVoice.Bounds), "voice play button stays inside compact dock", results);
                Assert(voiceState.Visible && voiceDock.ClientRectangle.Contains(voiceState.Bounds), "voice caption stays inside compact dock", results);
                Assert(voiceDivider.Visible && voiceDock.ClientRectangle.Contains(voiceDivider.Bounds), "voice divider is aligned in compact dock", results);
                Assert(waveform.Visible && waveform.Width >= 92 && voiceDock.ClientRectangle.Contains(waveform.Bounds), "voice waveform remains visible in compact dock", results);
                Assert(voiceEngine.Visible && voiceDock.ClientRectangle.Contains(voiceEngine.Bounds), "voice engine status remains visible in compact dock", results);
                Assert(!voiceDock.Bounds.IntersectsWith(inputComposer.Bounds), "voice dock does not overlap the input composer", results);
                Assert(!voiceDock.Bounds.IntersectsWith(footerBar.Bounds), "voice dock clears the footer", results);
                Assert(status.Top >= footerBar.Top && status.Bottom <= footerBar.Bottom, "left footer text stays vertically contained", results);
                Assert(quickHint.Top >= footerBar.Top && quickHint.Bottom <= footerBar.Bottom, "right footer text stays vertically contained", results);
                Capture(form, compactShot);
                form.Close();
            }

            using (var toolForm = new ToolCenterForm(new AppSettings()))
            {
                toolForm.StartPosition = FormStartPosition.Manual;
                toolForm.Location = new Point(40, 40);
                toolForm.Size = new Size(940, 650);
                toolForm.Show();
                WaitForPaint();
                CheckBox toolsEnabled = GetPrivateField<CheckBox>(toolForm, "toolsEnabledBox");
                CheckBox bundleC = GetPrivateField<CheckBox>(toolForm, "bundleCBox");
                Assert(toolsEnabled.Visible && bundleC.Visible, "tool center bundle page is visible", results);
                Assert(toolForm.ClientRectangle.Contains(toolsEnabled.Bounds), "tool controls stay inside window", results);
                CapturePlain(toolForm, toolShot);
                Button[] tabs = GetPrivateField<Button[]>(toolForm, "tabs");
                InvokeControlClick(tabs[1]);
                WaitForPaint();
                ListBox directoryList = GetPrivateField<ListBox>(toolForm, "directoryList");
                ListBox applicationList = GetPrivateField<ListBox>(toolForm, "applicationList");
                Assert(directoryList.Visible && applicationList.Visible, "tool privacy page is visible", results);
                Assert(!directoryList.Bounds.IntersectsWith(applicationList.Bounds), "privacy lists do not overlap", results);
                CapturePlain(toolForm, toolPrivacyShot);
                InvokeControlClick(tabs[2]);
                WaitForPaint();
                CheckedListBox skillList = GetPrivateField<CheckedListBox>(toolForm, "skillList");
                Assert(skillList.Visible && skillList.Items.Count == AgentToolSettings.DefaultSkills.Length, "tool skills page is complete", results);
                CapturePlain(toolForm, toolSkillsShot);
                toolForm.Close();
            }

            string directory = Path.GetDirectoryName(report);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.WriteAllLines(report, results.ToArray(), new UTF8Encoding(false));
        }

        private static void WaitForPaint()
        {
            DateTime deadline = DateTime.UtcNow.AddMilliseconds(350);
            while (DateTime.UtcNow < deadline)
            {
                Application.DoEvents();
                Thread.Sleep(16);
            }
        }

        private static void Capture(Form form, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            using (var bitmap = new Bitmap(form.Width, form.Height))
            {
                Application.DoEvents();
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                DrawControlLayer(bitmap, form, GetPrivateField<TopBarControl>(form, "topBarControl"));
                DrawControlLayer(bitmap, form, GetPrivateField<Button>(form, "topSettingsButton"));
                DrawControlLayer(bitmap, form, GetPrivateField<Button>(form, "minimizeButton"));
                DrawControlLayer(bitmap, form, GetPrivateField<Button>(form, "maximizeButton"));
                DrawControlLayer(bitmap, form, GetPrivateField<Button>(form, "closeButton"));
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private static void CapturePlain(Form form, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            using (var bitmap = new Bitmap(form.Width, form.Height))
            {
                Application.DoEvents();
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private static void DrawControlLayer(Bitmap target, Control ancestor, Control control)
        {
            if (target == null || ancestor == null || control == null || !control.Visible || control.Width <= 0 || control.Height <= 0) return;
            Point offset = Point.Empty;
            Control current = control;
            while (current != null && current != ancestor)
            {
                offset.Offset(current.Left, current.Top);
                current = current.Parent;
            }
            if (current != ancestor) return;

            using (var layer = new Bitmap(control.Width, control.Height))
            using (Graphics graphics = Graphics.FromImage(target))
            {
                control.DrawToBitmap(layer, new Rectangle(Point.Empty, control.Size));
                graphics.DrawImageUnscaled(layer, offset);
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

        private static void InvokeControlClick(Control control)
        {
            MethodInfo method = typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(typeof(Control).FullName, "OnClick");
            method.Invoke(control, new object[] { EventArgs.Empty });
        }

        private static void Assert(bool condition, string description, IList<string> results)
        {
            results.Add((condition ? "PASS: " : "FAIL: ") + description);
            if (!condition) throw new InvalidOperationException(description);
        }

        private static string GetArgument(string[] args, string name)
        {
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }
            return null;
        }

        private static int GetIntArgument(string[] args, string name, int fallback)
        {
            int value;
            return int.TryParse(GetArgument(args, name), out value) && value > 0 ? value : fallback;
        }
    }
}
