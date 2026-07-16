using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace IrohaAgentDesktop
{
    internal static class VoiceDeploymentUiQaProgram
    {
        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr windowHandle, IntPtr targetDc, uint flags);

        [STAThread]
        private static void Main(string[] args)
        {
            string screenshot = GetArgument(args, "--screenshot");
            if (string.IsNullOrWhiteSpace(screenshot)) throw new ArgumentException("--screenshot is required");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var form = new VoiceDeploymentForm(false))
            {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(60, 60);
                form.Show();
                form.UpdateProgress(new VoiceBootstrapProgress(
                    47,
                    "正在部署 GPT-SoVITS",
                    "47% · 完成后以后启动无需再次解压",
                    false));
                DateTime deadline = DateTime.UtcNow.AddMilliseconds(420);
                while (DateTime.UtcNow < deadline)
                {
                    Application.DoEvents();
                    Thread.Sleep(16);
                }
                Capture(form, screenshot);
                form.Close();
            }
        }

        private static void Capture(Form form, string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            using (var bitmap = new Bitmap(form.Width, form.Height))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                IntPtr hdc = graphics.GetHdc();
                try
                {
                    if (!PrintWindow(form.Handle, hdc, 2)) throw new InvalidOperationException("PrintWindow failed");
                }
                finally { graphics.ReleaseHdc(hdc); }
                bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
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
