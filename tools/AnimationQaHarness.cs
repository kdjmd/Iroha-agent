using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace IrohaAgentDesktop
{
    internal static class AnimationQaProgram
    {
        private const int DwmExtendedFrameBounds = 9;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(
            IntPtr windowHandle,
            int attribute,
            out NativeRect value,
            int valueSize);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr windowHandle, IntPtr targetDc, uint flags);

        [STAThread]
        private static void Main(string[] args)
        {
            string output = GetArgument(args, "--output");
            string requestedState = GetArgument(args, "--state") ?? "idle";
            string captureMode = GetArgument(args, "--capture") ?? "printwindow";
            int width = GetIntegerArgument(args, "--width", 1280);
            int height = GetIntegerArgument(args, "--height", 720);
            if (string.IsNullOrWhiteSpace(output))
            {
                throw new ArgumentException("--output is required");
            }

            string missingAsset;
            if (!RequiredVisualAssets.TryValidate(out missingAsset))
            {
                Console.Error.WriteLine("PACKAGED_UI_QA_BLOCKED_MISSING_ASSET: " + missingAsset);
                Environment.Exit(3);
                return;
            }

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
                AvatarControl avatar = GetPrivateField<AvatarControl>(form, "avatar");
                System.Windows.Forms.Timer timer = GetPrivateField<System.Windows.Forms.Timer>(avatar, "timer");
                timer.Stop();
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(24, 24);
                form.Size = new Size(width, height);
                form.TopMost = true;
                form.Show();
                form.Activate();
                form.BringToFront();
                Application.DoEvents();

                ConfigureAnimationState(avatar, requestedState);
                if (string.Equals(requestedState, "settings", StringComparison.OrdinalIgnoreCase))
                {
                    InvokePrivateMethod(form, "ToggleSettingsDrawer");
                }
                if (string.Equals(requestedState, "nameplate-click", StringComparison.OrdinalIgnoreCase))
                {
                    DialogueNameplateControl nameplate = GetPrivateField<DialogueNameplateControl>(form, "dialogueNameLabel");
                    InvokeControlClick(nameplate);
                }

                form.PerformLayout();
                avatar.Invalidate();
                form.Refresh();
                Application.DoEvents();
                Thread.Sleep(600);
                Application.DoEvents();

                string directory = System.IO.Path.GetDirectoryName(output);
                if (!string.IsNullOrEmpty(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }
                if (string.Equals(captureMode, "screen", StringComparison.OrdinalIgnoreCase))
                {
                    CaptureCompositedWindow(form, output);
                }
                else
                {
                    CapturePrintWindow(form, output);
                }
                form.Close();
            }
        }

        private static void CapturePrintWindow(Form form, string output)
        {
            using (var bitmap = new Bitmap(form.Width, form.Height, PixelFormat.Format32bppArgb))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    if (!PrintWindow(form.Handle, hdc, 2))
                    {
                        throw new InvalidOperationException("PrintWindow failed.");
                    }
                }
                finally
                {
                    graphics.ReleaseHdc(hdc);
                }
                bitmap.Save(output, ImageFormat.Png);
            }
        }

        private static void CaptureCompositedWindow(Form form, string output)
        {
            NativeRect nativeBounds;
            int dwmResult = DwmGetWindowAttribute(
                form.Handle,
                DwmExtendedFrameBounds,
                out nativeBounds,
                Marshal.SizeOf(typeof(NativeRect)));
            if (dwmResult != 0 || nativeBounds.Right <= nativeBounds.Left || nativeBounds.Bottom <= nativeBounds.Top)
            {
                throw new InvalidOperationException("Unable to resolve the physical DWM window bounds for QA capture.");
            }
            Point screenOrigin = new Point(nativeBounds.Left, nativeBounds.Top);
            Size captureSize = new Size(nativeBounds.Right - nativeBounds.Left, nativeBounds.Bottom - nativeBounds.Top);
            using (var bitmap = new Bitmap(captureSize.Width, captureSize.Height, PixelFormat.Format32bppArgb))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(screenOrigin, Point.Empty, captureSize, CopyPixelOperation.SourceCopy);
                bitmap.Save(output, ImageFormat.Png);
            }
        }

        private static void ConfigureAnimationState(AvatarControl avatar, string requestedState)
        {
            int frame = 0;
            int blinkStep = -1;
            AvatarState state = AvatarState.Idle;
            string stateKey = (requestedState ?? string.Empty).Trim().ToLowerInvariant();
            int requestedFrame;
            if (stateKey.StartsWith("speak-phase-", StringComparison.Ordinal) &&
                int.TryParse(stateKey.Substring("speak-phase-".Length), out requestedFrame))
            {
                state = AvatarState.Speaking;
                frame = Math.Max(0, requestedFrame);
            }
            else switch (stateKey)
            {
                case "blink-half":
                    blinkStep = 1;
                    break;
                case "blink-closed":
                    blinkStep = 3;
                    break;
                case "blink-start":
                    blinkStep = 0;
                    break;
                case "blink-lower":
                    blinkStep = 2;
                    break;
                case "blink-reopen":
                    blinkStep = 4;
                    break;
                case "blink-end":
                    blinkStep = 5;
                    break;
                case "speak-small":
                    state = AvatarState.Speaking;
                    frame = 3;
                    break;
                case "speak-open":
                    state = AvatarState.Speaking;
                    frame = 5;
                    break;
                case "happy":
                    state = AvatarState.Happy;
                    frame = 3;
                    break;
                case "thinking":
                    state = AvatarState.Thinking;
                    frame = 12;
                    break;
                case "shy":
                    state = AvatarState.Shy;
                    frame = 12;
                    break;
                case "surprised":
                    state = AvatarState.Surprised;
                    frame = 8;
                    break;
            }
            avatar.SetState(state);
            SetPrivateField(avatar, "frame", frame);
            SetPrivateField(avatar, "blinkStep", blinkStep);
        }

        private static T GetPrivateField<T>(object owner, string name) where T : class
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

        private static void InvokePrivateMethod(object owner, string name)
        {
            MethodInfo method = owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(owner.GetType().FullName, name);
            method.Invoke(owner, null);
        }

        private static void InvokeControlClick(Control control)
        {
            MethodInfo method = typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(typeof(Control).FullName, "OnClick");
            method.Invoke(control, new object[] { EventArgs.Empty });
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

        private static int GetIntegerArgument(string[] args, string name, int fallback)
        {
            int value;
            return int.TryParse(GetArgument(args, name), out value) ? value : fallback;
        }
    }
}
