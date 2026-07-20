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
            string mainShot = GetArgument(args, "--main-screenshot");
            string compactShot = GetArgument(args, "--compact-screenshot");
            string narrowShot = GetArgument(args, "--narrow-screenshot");
            string toolShot = GetArgument(args, "--tool-screenshot");
            string toolPrivacyShot = GetArgument(args, "--tool-privacy-screenshot");
            string toolSkillsShot = GetArgument(args, "--tool-skills-screenshot");
            string report = GetArgument(args, "--report");
            int viewportWidth = GetIntArgument(args, "--width", 1280);
            int viewportHeight = GetIntArgument(args, "--height", 720);
            if (string.IsNullOrWhiteSpace(modelShot) || string.IsNullOrWhiteSpace(voiceShot) || string.IsNullOrWhiteSpace(mainShot) || string.IsNullOrWhiteSpace(compactShot) || string.IsNullOrWhiteSpace(narrowShot) || string.IsNullOrWhiteSpace(toolShot) || string.IsNullOrWhiteSpace(toolPrivacyShot) || string.IsNullOrWhiteSpace(toolSkillsShot) || string.IsNullOrWhiteSpace(report))
            {
                throw new ArgumentException("main, settings, compact, narrow, tool screenshots and --report are required");
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
                AssertBottomLayout(form, results, "full viewport", true);
                Capture(form, mainShot);

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
                AssertBottomLayout(form, results, "compact viewport", true);
                Capture(form, compactShot);

                form.Size = new Size(980, 552);
                WaitForPaint();
                AssertBottomLayout(form, results, "minimum viewport", false);
                Capture(form, narrowShot);

                Size[] layoutMatrix =
                {
                    new Size(1024, 576),
                    new Size(1088, 612),
                    new Size(1100, 620),
                    new Size(1280, 720),
                    new Size(1366, 768),
                    new Size(1450, 816),
                    new Size(1672, 941),
                    new Size(1920, 1080)
                };
                foreach (Size layoutSize in layoutMatrix)
                {
                    form.Size = layoutSize;
                    WaitForPaint();
                    AssertBottomLayout(form, results, layoutSize.Width + "x" + layoutSize.Height, null);
                }
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
                Assert(toolsEnabled.BackColor.A == 255 && bundleC.BackColor.A == 255, "tool cards use an opaque compositing surface", results);
                toolsEnabled.Focus();
                WaitForPaint();
                Assert(!HasNearBlackPerimeter(toolsEnabled) && !HasNearBlackPerimeter(bundleC), "tool cards have no black frame or focus rectangle", results);
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

        private static void AssertBottomLayout(MainForm form, IList<string> results, string label, bool? expectTelemetry)
        {
            GlassPanel quickBar = GetPrivateField<GlassPanel>(form, "quickActionBar");
            List<Button> quickActions = GetPrivateField<List<Button>>(form, "quickActionButtons");
            Assert(quickBar.Visible && quickActions.Count == 4, label + " keeps all quick actions visible", results);
            for (int i = 0; i < quickActions.Count; i++)
            {
                Button action = quickActions[i];
                GlassButton glass = action as GlassButton;
                Assert(quickBar.ClientRectangle.Contains(action.Bounds), label + " contains quick action " + (i + 1), results);
                Assert(glass != null && action.BackColor.A == 0 && !glass.OpaqueBackfill, label + " quick action " + (i + 1) + " has no rectangular backfill", results);
                Assert(HasClippedCorners(action), label + " quick action " + (i + 1) + " clips its transparent square corners", results);
                if (i > 0)
                {
                    Assert(!quickActions[i - 1].Bounds.IntersectsWith(action.Bounds) && action.Left - quickActions[i - 1].Right >= 6, label + " separates quick actions " + i + " and " + (i + 1), results);
                }
            }

            GlassPanel composer = GetPrivateField<GlassPanel>(form, "inputComposer");
            TextBox input = GetPrivateField<TextBox>(form, "inputBox");
            Label placeholder = GetPrivateField<Label>(form, "inputPlaceholderLabel");
            Button attach = GetPrivateField<Button>(form, "attachImageButton");
            Button send = GetPrivateField<Button>(form, "sendButton");
            GlassButton attachGlass = attach as GlassButton;
            GlassButton sendGlass = send as GlassButton;
            Assert(composer.ClientRectangle.Contains(input.Bounds) && composer.ClientRectangle.Contains(attach.Bounds) && composer.ClientRectangle.Contains(send.Bounds), label + " contains every composer control", results);
            Assert(!attach.Bounds.IntersectsWith(send.Bounds) && send.Left - attach.Right >= 10, label + " separates attachment and send controls", results);
            Assert(send.Right + 12 <= composer.ClientRectangle.Right, label + " keeps the send circle clear of the composer edge", results);
            Assert(input.Right + 8 <= attach.Left, label + " keeps input text clear of composer actions", results);
            Assert(attach.BackColor.A == 0 && send.BackColor.A == 0, label + " composer actions have no opaque square backfill", results);
            Assert(attach.Region == null && HasClippedCorners(send), label + " keeps attachment borderless while clipping send to a circle", results);
            Assert(attachGlass != null && attachGlass.MinimalChrome && !attachGlass.CircularChrome && !attachGlass.OpaqueBackfill, label + " renders attachment as a borderless icon control", results);
            Assert(sendGlass != null && sendGlass.CircularChrome, label + " renders the send action as circular chrome", results);
            Assert(string.Equals(attach.AccessibleDescription, "composer-attach", StringComparison.Ordinal) && attach.Text == "\uE723", label + " attachment action owns only the paperclip glyph", results);
            Assert(string.Equals(send.AccessibleDescription, "composer-send", StringComparison.Ordinal) && string.IsNullOrEmpty(send.Text), label + " send action uses only its custom paper-plane glyph", results);
            Assert(composer.AllowDrop && input.AllowDrop && placeholder.AllowDrop && attach.AllowDrop && send.AllowDrop, label + " accepts file drops across the full composer", results);

            GlassPanel dock = GetPrivateField<GlassPanel>(form, "voiceDock");
            Button play = GetPrivateField<Button>(form, "testVoiceButton");
            Label state = GetPrivateField<Label>(form, "voiceStateLabel");
            Control divider = GetPrivateField<Control>(form, "voiceDockDivider");
            WaveformControl wave = GetPrivateField<WaveformControl>(form, "waveform");
            Label engine = GetPrivateField<Label>(form, "voiceEngineLabel");
            Assert(dock.ClientRectangle.Contains(play.Bounds) && dock.ClientRectangle.Contains(state.Bounds), label + " contains voice play and caption controls", results);
            Assert(!play.Bounds.IntersectsWith(state.Bounds), label + " separates voice play and caption controls", results);
            bool telemetryVisible = wave.Visible && divider.Visible && engine.Visible;
            Assert(wave.Visible == divider.Visible && wave.Visible == engine.Visible, label + " keeps voice telemetry controls in one mode", results);
            if (expectTelemetry.HasValue)
            {
                Assert(telemetryVisible == expectTelemetry.Value, label + " uses the expected telemetry mode", results);
            }
            if (telemetryVisible)
            {
                Assert(dock.ClientRectangle.Contains(divider.Bounds) && dock.ClientRectangle.Contains(wave.Bounds) && dock.ClientRectangle.Contains(engine.Bounds), label + " contains voice telemetry", results);
                Assert(state.Right + 8 <= divider.Left && divider.Right + 8 <= wave.Left, label + " separates caption, divider and waveform", results);
                Assert(!state.Bounds.IntersectsWith(wave.Bounds) && !wave.Bounds.IntersectsWith(engine.Bounds) && wave.Bottom + 2 <= engine.Top, label + " prevents voice text and waveform overlap", results);
            }
        }

        private static bool HasNearBlackPerimeter(Control control)
        {
            if (control == null || control.Width <= 0 || control.Height <= 0) return true;
            using (var bitmap = new Bitmap(control.Width, control.Height))
            {
                control.DrawToBitmap(bitmap, new Rectangle(Point.Empty, control.Size));
                int darkPixels = 0;
                int edge = Math.Min(4, Math.Min(bitmap.Width, bitmap.Height));
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        if (x >= edge && x < bitmap.Width - edge && y >= edge && y < bitmap.Height - edge) continue;
                        Color pixel = bitmap.GetPixel(x, y);
                        if (pixel.R < 24 && pixel.G < 24 && pixel.B < 24) darkPixels++;
                    }
                }
                return darkPixels > 6;
            }
        }

        private static bool HasClippedCorners(Control control)
        {
            if (control == null || control.Region == null || control.Width < 4 || control.Height < 4) return false;
            return !control.Region.IsVisible(0, 0) &&
                !control.Region.IsVisible(control.Width - 1, 0) &&
                !control.Region.IsVisible(0, control.Height - 1) &&
                !control.Region.IsVisible(control.Width - 1, control.Height - 1) &&
                control.Region.IsVisible(control.Width / 2, control.Height / 2);
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
