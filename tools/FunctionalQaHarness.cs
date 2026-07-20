using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace IrohaAgentDesktop
{
    internal static class FunctionalQaProgram
    {
        private static readonly List<string> Results = new List<string>();
        private static string ReportPath = "";

        [STAThread]
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
                Results.Add("ERROR: " + (ex.Message ?? "Functional QA failed"));
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

            ReportPath = Path.GetFullPath(output);
            var results = Results;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var form = new MainForm())
            {
                AppSettings settings = GetPrivateField<AppSettings>(form, "settings");
                bool originalVoiceEnabled = settings.VoiceEnabled;
                form.Disposed += delegate
                {
                    settings.VoiceEnabled = originalVoiceEnabled;
                    SettingsStore.Save(settings);
                };
                settings.VoiceEnabled = false;
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(24, 24);
                form.Size = new Size(1280, 720);
                form.Show();
                Application.DoEvents();

                string originalVoiceUrl = settings.VoiceServerUrl;
                var occupiedPort = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
                occupiedPort.Start();
                int occupiedPortNumber = ((System.Net.IPEndPoint)occupiedPort.LocalEndpoint).Port;
                Assert(!(bool)InvokePrivateMethod(form, "IsTcpPortAvailable", occupiedPortNumber), "occupied voice port is detected", results);
                occupiedPort.Stop();
                Assert((bool)InvokePrivateMethod(form, "IsTcpPortAvailable", occupiedPortNumber), "released voice port becomes available", results);
                settings.VoiceServerUrl = "http://127.0.0.1:" + occupiedPortNumber;
                Assert((int)InvokePrivateMethod(form, "GetConfiguredVoicePort") == occupiedPortNumber, "configured local voice port is preserved", results);
                settings.VoiceServerUrl = originalVoiceUrl;

                AvatarControl avatar = GetPrivateField<AvatarControl>(form, "avatar");
                Timer timer = GetPrivateField<Timer>(avatar, "timer");
                timer.Stop();

                Assert(form.FormBorderStyle == FormBorderStyle.None, "borderless window", results);
                Assert(form.MinimumSize == new Size(980, 552), "declared minimum viewport", results);

                TextBox input = GetPrivateField<TextBox>(form, "inputBox");
                Label inputPlaceholder = GetPrivateField<Label>(form, "inputPlaceholderLabel");
                GlassPanel inputComposer = GetPrivateField<GlassPanel>(form, "inputComposer");
                Button attach = GetPrivateField<Button>(form, "attachImageButton");
                Button send = GetPrivateField<Button>(form, "sendButton");
                Button voice = GetPrivateField<Button>(form, "testVoiceButton");
                GlassPanel dialogue = GetPrivateField<GlassPanel>(form, "dialoguePanel");
                VnDialogueTextControl dialogueText = GetPrivateField<VnDialogueTextControl>(form, "dialogueTextBox");
                Assert(input.Visible && send.Visible && voice.Visible, "primary composer and voice playback visible", results);
                Assert(dialogue.Visible && !string.IsNullOrWhiteSpace(dialogueText.Text), "VN dialogue populated", results);
                Assert(IsInside(form.ClientRectangle, input) && IsInside(form.ClientRectangle, send), "primary controls inside standard viewport", results);
                Assert(inputComposer.AllowDrop && input.AllowDrop && inputPlaceholder.AllowDrop && attach.AllowDrop && send.AllowDrop, "composer supports file drop on every visible surface", results);
                Assert(string.Equals(attach.AccessibleDescription, "composer-attach", StringComparison.Ordinal) && attach.Text == "\uE723", "attachment button is paperclip-only", results);

                string attachmentPath = Path.Combine(Path.GetTempPath(), "iroha-agent-attachment-qa-" + Guid.NewGuid().ToString("N") + ".txt");
                string unsupportedPath = Path.ChangeExtension(attachmentPath, ".exe");
                try
                {
                    File.WriteAllText(attachmentPath, "attachment drag and drop qa", new UTF8Encoding(false));
                    File.WriteAllText(unsupportedPath, "unsupported attachment qa", new UTF8Encoding(false));
                    var dropData = new DataObject();
                    dropData.SetData(DataFormats.FileDrop, new[] { attachmentPath });
                    var dragArgs = new DragEventArgs(dropData, 0, 0, 0, DragDropEffects.Copy, DragDropEffects.None);
                    InvokePrivateMethod(form, "Attachment_DragEnter", inputComposer, dragArgs);
                    Assert(dragArgs.Effect == DragDropEffects.Copy, "supported document drag is accepted", results);
                    Assert(inputComposer.BorderColor != Theme.BorderStrong, "valid drag highlights the composer", results);
                    InvokePrivateMethod(form, "Attachment_DragDrop", inputComposer, dragArgs);
                    Assert(string.Equals(GetPrivateField<string>(form, "pendingDocumentPath"), Path.GetFullPath(attachmentPath), StringComparison.OrdinalIgnoreCase), "dropped document becomes the pending attachment", results);
                    Assert(inputPlaceholder.Text.Contains(Path.GetFileName(attachmentPath)), "dropped document name appears in the composer hint", results);
                    Assert(((GlassButton)attach).Accent, "attachment control visibly confirms the pending file", results);

                    object[] unsupportedArguments = { unsupportedPath, "" };
                    Assert(!(bool)InvokePrivateMethod(form, "TryAttachFile", unsupportedArguments) && !string.IsNullOrWhiteSpace((string)unsupportedArguments[1]), "unsupported attachment is rejected with feedback", results);
                }
                finally
                {
                    if (File.Exists(attachmentPath)) File.Delete(attachmentPath);
                    if (File.Exists(unsupportedPath)) File.Delete(unsupportedPath);
                    SetPrivateField(form, "pendingDocumentPath", null);
                    SetPrivateField(form, "pendingImagePath", null);
                    InvokePrivateMethod(form, "SetAttachmentButtonActive", false);
                    InvokePrivateMethod(form, "SetAttachmentDropHighlight", false);
                    InvokePrivateMethod(form, "UpdateInputPlaceholder");
                }

                List<Button> quickActions = GetPrivateField<List<Button>>(form, "quickActionButtons");
                Assert(quickActions.Count == 4, "four quick actions", results);
                input.Clear();
                InvokeControlClick(quickActions[1]);
                Assert(!string.IsNullOrWhiteSpace(input.Text), "quick action populates composer", results);

                List<ConversationItemControl> conversations = GetPrivateField<List<ConversationItemControl>>(form, "sidebarConversationItems");
                Assert(conversations.Count >= 5, "conversation history populated", results);
                ConversationItemControl pinTarget = conversations[conversations.Count - 1];
                bool originalPinned = pinTarget.Pinned;
                InvokePrivateMethod(form, "ToggleConversationPin", pinTarget);
                Assert(pinTarget.Pinned != originalPinned, "pin state toggles", results);
                if (pinTarget.Pinned)
                {
                    Assert(object.ReferenceEquals(conversations[0], pinTarget), "pinned conversation moves first", results);
                }
                InvokePrivateMethod(form, "ToggleConversationPin", pinTarget);
                Assert(pinTarget.Pinned == originalPinned, "pin state restores", results);

                ConversationItemControl menuTarget = conversations[0];
                bool menuTargetPinned = menuTarget.Pinned;
                InvokePrivateMethod(form, "ShowConversationMenu", menuTarget, new Point(8, 8));
                Application.DoEvents();
                ContextMenuStrip conversationMenu = GetPrivateField<ContextMenuStrip>(form, "activeConversationMenu");
                Assert(conversationMenu != null && !conversationMenu.IsDisposed, "conversation menu opens", results);
                Assert(conversationMenu.Items.Count == 4, "conversation menu exposes rename, pin, and delete", results);
                conversationMenu.Items[1].PerformClick();
                conversationMenu.Close();
                Application.DoEvents();
                Assert(!conversationMenu.IsDisposed, "conversation menu survives the closing input message", results);
                DateTime menuCleanupDeadline = DateTime.UtcNow.AddMilliseconds(900);
                while (!conversationMenu.IsDisposed && DateTime.UtcNow < menuCleanupDeadline)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(20);
                }
                Assert(conversationMenu.IsDisposed, "conversation menu disposes after deferred cleanup", results);
                if (menuTarget.Pinned != menuTargetPinned)
                {
                    InvokePrivateMethod(form, "ToggleConversationPin", menuTarget);
                }

                GlassPanel settingsDrawer = GetPrivateField<GlassPanel>(form, "settingsDrawer");
                ToolRailPanel toolRail = GetPrivateField<ToolRailPanel>(form, "rightToolRail");
                Rectangle[] toolBounds = toolRail.GetItemBounds();
                InvokeControlMouse(toolRail, "OnMouseDown", toolBounds[1]);
                InvokeControlMouse(toolRail, "OnMouseUp", toolBounds[1]);
                Application.DoEvents();
                Assert(settingsDrawer.Visible && toolRail.SelectedIndex == 1, "settings tool opens and selects the drawer", results);
                ComboBox providerBox = GetPrivateField<ComboBox>(form, "drawerProviderBox");
                ComboBox modelBox = GetPrivateField<ComboBox>(form, "drawerModelBox");
                TextBox providerKeyBox = GetPrivateField<TextBox>(form, "drawerApiKeyBox");
                Button modelTab = GetPrivateField<Button>(form, "drawerModelTabButton");
                Button voiceTab = GetPrivateField<Button>(form, "drawerVoiceTabButton");
                Assert(providerBox.Visible && modelBox.Visible, "settings exposes provider-first model selection", results);
                Assert(providerBox.Items.Count >= 15, "mainstream provider catalog is available", results);
                Assert(!string.IsNullOrWhiteSpace(modelBox.Text), "active provider has a model selection", results);

                string originalProviderId = settings.ProviderId;
                providerKeyBox.Text = "qa-key-for-" + originalProviderId;
                SelectProvider(providerBox, "openai");
                Application.DoEvents();
                Assert(settings.ProviderId == "openai", "provider selection updates active profile", results);
                providerKeyBox.Text = "qa-key-for-openai";
                SelectProvider(providerBox, originalProviderId);
                Application.DoEvents();
                Assert(providerKeyBox.Text == "qa-key-for-" + originalProviderId, "provider keys remain isolated", results);

                InvokeControlClick(voiceTab);
                Application.DoEvents();
                Button redeployVoice = GetPrivateField<Button>(form, "drawerRedeployVoiceButton");
                GlassCheckBox optimizePrompt = GetPrivateField<GlassCheckBox>(form, "drawerOptimizeBox");
                Assert(redeployVoice.Visible && redeployVoice.Text.Contains("重新部署"), "settings exposes voice redeploy", results);
                Assert(settingsDrawer.ClientRectangle.Contains(redeployVoice.Bounds), "voice redeploy stays inside settings drawer", results);
                Assert(!redeployVoice.Bounds.IntersectsWith(optimizePrompt.Bounds), "voice redeploy does not overlap prompt optimization", results);
                InvokeControlClick(modelTab);
                Application.DoEvents();
                Assert(providerBox.Visible && !redeployVoice.Visible, "settings pages switch without stacked controls", results);
                Assert(!settingsDrawer.Bounds.IntersectsWith(toolRail.Bounds), "settings drawer clears the right tool rail", results);
                GlassPanel voiceDock = GetPrivateField<GlassPanel>(form, "voiceDock");
                Assert(!settingsDrawer.Bounds.IntersectsWith(voiceDock.Bounds), "settings drawer clears the voice dock", results);
                InvokeControlMouse(toolRail, "OnMouseDown", toolBounds[1]);
                InvokeControlMouse(toolRail, "OnMouseUp", toolBounds[1]);
                Assert(!settingsDrawer.Visible, "settings drawer closes", results);
                InvokeControlMouse(toolRail, "OnMouseDown", toolBounds[2]);
                InvokeControlMouse(toolRail, "OnMouseUp", toolBounds[2]);
                Assert(toolRail.SelectedIndex == 2, "appearance tool provides selected feedback", results);
                InvokePrivateMethod(form, "SetToolRailSelection", GetPrivateField<Button>(form, "memoryButton"));

                TopBarControl topBar = GetPrivateField<TopBarControl>(form, "topBarControl");
                topBar.Refresh();
                Assert(!topBar.LastTitleBounds.IntersectsWith(topBar.LastModelBadgeBounds), "model badge clears the product title", results);
                Assert(topBar.LastModelBadgeBounds.Left - topBar.LastTitleBounds.Right >= 8, "model badge keeps title spacing", results);
                string originalModel = topBar.ModelName;
                topBar.ModelName = "deepseek-v4-pro";
                Assert(string.Equals(topBar.ModelName, "deepseek-v4-pro", StringComparison.OrdinalIgnoreCase), "model badge state accepts Pro", results);
                topBar.ModelName = originalModel;

                avatar.SetState(AvatarState.Speaking);
                Assert(avatar.State == AvatarState.Speaking, "speaking state activates", results);
                avatar.SetState(AvatarState.Idle);

                form.Size = new Size(980, 552);
                form.PerformLayout();
                Application.DoEvents();
                GlassPanel memoryCard = GetPrivateField<GlassPanel>(form, "memoryCard");
                GlassPanel compressionCard = GetPrivateField<GlassPanel>(form, "compressionCard");
                GlassPanel serviceCard = GetPrivateField<GlassPanel>(form, "serviceCard");
                Assert(!memoryCard.Visible && !compressionCard.Visible && !serviceCard.Visible, "compact viewport hides secondary cards", results);
                Assert(input.Visible && send.Visible && voice.Visible, "compact viewport preserves primary workflow", results);
                Assert(IsInside(form.ClientRectangle, input) && IsInside(form.ClientRectangle, send), "primary controls inside compact viewport", results);

                form.Close();
            }

            FlushResults();
        }

        private static bool IsInside(Rectangle viewport, Control control)
        {
            Rectangle screenBounds = control.RectangleToScreen(control.ClientRectangle);
            Rectangle formBounds = control.FindForm().RectangleToScreen(viewport);
            return formBounds.Contains(screenBounds);
        }

        private static void Assert(bool condition, string label, List<string> results)
        {
            results.Add((condition ? "PASS: " : "FAIL: ") + label);
            FlushResults();
            if (!condition) throw new InvalidOperationException("FAIL: " + label);
        }

        private static void FlushResults()
        {
            if (string.IsNullOrWhiteSpace(ReportPath)) return;
            string directory = Path.GetDirectoryName(ReportPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllLines(ReportPath, Results.ToArray(), new UTF8Encoding(false));
        }

        private static T GetPrivateField<T>(object owner, string name)
        {
            FieldInfo field = owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(owner.GetType().FullName, name);
            return (T)field.GetValue(owner);
        }

        private static void SetPrivateField(object owner, string name, object value)
        {
            FieldInfo field = owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(owner.GetType().FullName, name);
            field.SetValue(owner, value);
        }

        private static object InvokePrivateMethod(object owner, string name, params object[] values)
        {
            MethodInfo method = owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(owner.GetType().FullName, name);
            return method.Invoke(owner, values);
        }

        private static void InvokeControlClick(Control control)
        {
            MethodInfo method = typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(typeof(Control).FullName, "OnClick");
            method.Invoke(control, new object[] { EventArgs.Empty });
        }

        private static void SelectProvider(ComboBox box, string providerId)
        {
            foreach (object item in box.Items)
            {
                ModelProviderDefinition provider = item as ModelProviderDefinition;
                if (provider != null && string.Equals(provider.Id, providerId, StringComparison.OrdinalIgnoreCase))
                {
                    box.SelectedItem = provider;
                    return;
                }
            }
            throw new InvalidOperationException("Provider not found: " + providerId);
        }

        private static void InvokeControlMouse(Control control, string methodName, Rectangle bounds)
        {
            MethodInfo method = control.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(control.GetType().FullName, methodName);
            int x = bounds.X + bounds.Width / 2;
            int y = bounds.Y + bounds.Height / 2;
            method.Invoke(control, new object[] { new MouseEventArgs(MouseButtons.Left, 1, x, y, 0) });
        }

        private static string GetArgument(string[] args, string name)
        {
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}
