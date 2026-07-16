using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace IrohaAgentDesktop
{
    internal static class RequiredVisualAssets
    {
        private static readonly string[] RequiredPaths =
        {
            Path.Combine("assets", "ui", "vn-room-bg.png"),
            Path.Combine("assets", "character", "iroha-portrait.png")
        };

        public static string Find(string relativePath)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, relativePath),
                Path.GetFullPath(Path.Combine(baseDir, "..", relativePath)),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", relativePath)),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", relativePath))
            };
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        public static bool TryValidate(out string missingAsset)
        {
            foreach (string relativePath in RequiredPaths)
            {
                string resolvedPath = Find(relativePath);
                if (string.IsNullOrEmpty(resolvedPath))
                {
                    missingAsset = relativePath;
                    return false;
                }
                try
                {
                    using (Image image = Image.FromFile(resolvedPath))
                    {
                        if (image.Width <= 0 || image.Height <= 0)
                        {
                            missingAsset = relativePath + " (invalid image dimensions)";
                            return false;
                        }
                    }
                }
                catch
                {
                    missingAsset = relativePath + " (unreadable image)";
                    return false;
                }
            }
            missingAsset = null;
            return true;
        }

        public static void EnsurePresent()
        {
            string missingAsset;
            if (!TryValidate(out missingAsset))
            {
                throw new InvalidOperationException(
                    "Required visual asset is missing: " + missingAsset +
                    ". Keep IrohaAgent.exe beside the packaged assets folder.");
            }
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
            {
                ShowStartupError(e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                ShowStartupError(e.ExceptionObject as Exception);
            };
            string missingAsset;
            if (!RequiredVisualAssets.TryValidate(out missingAsset))
            {
                MessageBox.Show(
                    "界面资源不完整，彩叶 Agent 无法安全启动。\r\n\r\n" +
                    "请重新解压完整安装包，不要只复制 IrohaAgent.exe。",
                    "彩叶 Iroha Agent",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            Application.Run(new MainForm());
        }

        private static void ShowStartupError(Exception exception)
        {
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "IrohaLocalAgent");
                Directory.CreateDirectory(dir);
                string log = Path.Combine(dir, "crash.log");
                File.AppendAllText(log,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine +
                    Convert.ToString(exception) + Environment.NewLine + Environment.NewLine,
                    Encoding.UTF8);
                MessageBox.Show(
                    "彩叶 Agent 暂时没能启动。请重新打开应用；如果仍然失败，我可以继续帮你检查。",
                    "彩叶 Agent",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                MessageBox.Show("彩叶 Agent 暂时没能启动。", "彩叶 Agent");
            }
        }
    }

    public sealed class AppSettings
    {
        public static readonly string DefaultVoiceRuntimeRoot = ResolveVoiceRuntimeRoot();
        public static readonly string DefaultVoiceRefAudioPath = ResolveVoiceRefAudioPath();
        public const string DefaultVoicePromptText = "さすがにここは危ないかもいや、警察に届けるか";
        public const string DefaultVoicePromptLang = "ja";
        public const string DefaultVoiceRuntimeConfig = "GPT_SoVITS/configs/tts_infer_iroha.yaml";

        private static string ResolveVoiceRuntimeRoot()
        {
            string configured = Environment.GetEnvironmentVariable("IROHA_GPT_SOVITS_ROOT");
            if (!string.IsNullOrWhiteSpace(configured)) return configured.Trim().Trim('"');

            string bundled = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GPT-SoVITS");
            if (Directory.Exists(bundled)) return bundled;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Codex",
                "GPT-SoVITS-runtime",
                "extracted-v2pro",
                "GPT-SoVITS-v2pro-20250604");
        }

        private static string ResolveVoiceRefAudioPath()
        {
            string configured = Environment.GetEnvironmentVariable("IROHA_VOICE_REF_AUDIO");
            if (!string.IsNullOrWhiteSpace(configured)) return configured.Trim().Trim('"');
            return Path.Combine(DefaultVoiceRuntimeRoot, "voices", "iroha", "ref.wav");
        }

        private static string ResolveVoiceRuntimeConfigPath()
        {
            string configured = Environment.GetEnvironmentVariable("IROHA_VOICE_CONFIG");
            if (!string.IsNullOrWhiteSpace(configured)) return configured.Trim().Trim('"');
            return Path.Combine(
                DefaultVoiceRuntimeRoot,
                DefaultVoiceRuntimeConfig.Replace('/', Path.DirectorySeparatorChar));
        }

        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
        public string Model { get; set; }
        public string VoiceServerUrl { get; set; }
        public string VoiceRuntimeRoot { get; set; }
        public string VoiceRuntimeConfigPath { get; set; }
        public string VoiceRefAudioPath { get; set; }
        public string VoicePromptText { get; set; }
        public string VoicePromptLang { get; set; }
        public bool VoiceAutoMatched { get; set; }
        public int VoiceMatchVersion { get; set; }
        public bool VoiceEnabled { get; set; }
        public bool MemoryEnabled { get; set; }
        public bool AutoOptimizePrompt { get; set; }

        public AppSettings()
        {
            BaseUrl = "https://api.deepseek.com";
            ApiKey = "";
            Model = "deepseek-v4-flash";
            VoiceServerUrl = "http://127.0.0.1:9880";
            VoiceRuntimeRoot = DefaultVoiceRuntimeRoot;
            VoiceRuntimeConfigPath = ResolveVoiceRuntimeConfigPath();
            VoiceRefAudioPath = DefaultVoiceRefAudioPath;
            VoicePromptText = DefaultVoicePromptText;
            VoicePromptLang = DefaultVoicePromptLang;
            VoiceAutoMatched = false;
            VoiceMatchVersion = 0;
            VoiceEnabled = true;
            MemoryEnabled = true;
            AutoOptimizePrompt = false;
        }
    }

    public sealed class AgentMemory
    {
        public List<string> Notes { get; set; }

        public AgentMemory()
        {
            Notes = new List<string>();
        }
    }

    internal static class SettingsStore
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public static string DirectoryPath
        {
            get
            {
                string configured = Environment.GetEnvironmentVariable("IROHA_APP_DATA_ROOT");
                if (!string.IsNullOrWhiteSpace(configured))
                {
                    return Path.GetFullPath(configured.Trim().Trim('"'));
                }
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "IrohaLocalAgent");
            }
        }

        public static string FilePath
        {
            get { return Path.Combine(DirectoryPath, "settings.json"); }
        }

        public static AppSettings Load()
        {
            Directory.CreateDirectory(DirectoryPath);
            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            try
            {
                string json = File.ReadAllText(FilePath, Encoding.UTF8);
                AppSettings settings = Serializer.Deserialize<AppSettings>(json);
                if (settings == null) return new AppSettings();
                if (string.IsNullOrWhiteSpace(settings.Model)) settings.Model = "deepseek-v4-flash";
                if (string.IsNullOrWhiteSpace(settings.VoiceServerUrl)) settings.VoiceServerUrl = "http://127.0.0.1:9880";
                if (string.IsNullOrWhiteSpace(settings.VoiceRuntimeRoot)) settings.VoiceRuntimeRoot = AppSettings.DefaultVoiceRuntimeRoot;
                if (string.IsNullOrWhiteSpace(settings.VoiceRuntimeConfigPath))
                {
                    settings.VoiceRuntimeConfigPath = Path.Combine(
                        settings.VoiceRuntimeRoot,
                        AppSettings.DefaultVoiceRuntimeConfig.Replace('/', Path.DirectorySeparatorChar));
                }
                if (string.IsNullOrWhiteSpace(settings.VoiceRefAudioPath)) settings.VoiceRefAudioPath = AppSettings.DefaultVoiceRefAudioPath;
                if (string.IsNullOrWhiteSpace(settings.VoicePromptText)) settings.VoicePromptText = AppSettings.DefaultVoicePromptText;
                if (string.IsNullOrWhiteSpace(settings.VoicePromptLang)) settings.VoicePromptLang = AppSettings.DefaultVoicePromptLang;
                return settings;
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static void Save(AppSettings settings)
        {
            Directory.CreateDirectory(DirectoryPath);
            string json = Serializer.Serialize(settings);
            File.WriteAllText(FilePath, json, Encoding.UTF8);
        }
    }

    internal static class MemoryStore
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

        public static string FilePath
        {
            get { return Path.Combine(SettingsStore.DirectoryPath, "memory.json"); }
        }

        public static AgentMemory Load()
        {
            Directory.CreateDirectory(SettingsStore.DirectoryPath);
            if (!File.Exists(FilePath))
            {
                return new AgentMemory();
            }

            try
            {
                string json = File.ReadAllText(FilePath, Encoding.UTF8);
                AgentMemory memory = Serializer.Deserialize<AgentMemory>(json);
                return memory ?? new AgentMemory();
            }
            catch
            {
                return new AgentMemory();
            }
        }

        public static void Save(AgentMemory memory)
        {
            Directory.CreateDirectory(SettingsStore.DirectoryPath);
            File.WriteAllText(FilePath, Serializer.Serialize(memory ?? new AgentMemory()), Encoding.UTF8);
        }
    }

    internal enum AvatarState
    {
        Idle,
        Thinking,
        Speaking,
        Happy,
        Error,
        Shy,
        Surprised,
        Cheer,
        Focus
    }

    internal sealed class AgentReply
    {
        public string ChineseText;
        public string JapaneseSpeech;
        public AvatarState Mood;
    }

    internal sealed class MainForm : Form
    {
        private const string SidebarSearchPlaceholder = "搜索聊天记录";

        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();
        private readonly List<Dictionary<string, string>> history = new List<Dictionary<string, string>>();
        private readonly object voiceStartupLock = new object();

        private AppSettings settings;
        private AgentMemory memory;
        private Task<bool> voiceStartupTask;
        private Process voiceServiceProcess;
        private VoiceDeploymentForm voiceDeploymentForm;
        private bool isVoiceSetupRunning;
        private string voiceSetupMessage;
        private AvatarControl avatar;
        private RichTextBox chatLog;
        private TextBox inputBox;
        private TextBox quickApiKeyBox;
        private CheckBox autoOptimizeBox;
        private Label statusLabel;
        private Label quickHintLabel;
        private Button sendButton;
        private Button settingsButton;
        private Button testVoiceButton;
        private Button saveChatButton;
        private Button clearChatButton;
        private Button memoryButton;
        private Button quickSaveKeyButton;
        private Button quickTestButton;
        private Button appearanceButton;
        private Button topSettingsButton;
        private Button minimizeButton;
        private Button maximizeButton;
        private Button closeButton;
        private Button optimizeToggleButton;
        private Button memoryManageButton;
        private AvatarControl vnRoot;
        private VNBackgroundControl vnBackground;
        private TopBarControl topBarControl;
        private CharacterTopOverlayControl characterTopOverlay;
        private GlassPanel leftSidebar;
        private GlassPanel memoryCard;
        private GlassPanel compressionCard;
        private GlassPanel serviceCard;
        private GlassPanel dialoguePanel;
        private GlassPanel quickActionBar;
        private GlassPanel inputComposer;
        private GlassPanel voiceDock;
        private ToolRailPanel rightToolRail;
        private GlassPanel sidebarSearchShell;
        private GlassPanel settingsDrawer;
        private FooterBarControl footerBar;
        private VnDialogueTextControl dialogueTextBox;
        private DialogueNameplateControl dialogueNameLabel;
        private Label inputPlaceholderLabel;
        private Label voiceStateLabel;
        private Label voiceEngineLabel;
        private Label memoryCardBodyLabel;
        private Label compressionCardBodyLabel;
        private Label serviceCardBodyLabel;
        private Label settingsDrawerHintLabel;
        private Label settingsDrawerStatusLabel;
        private TextBox sidebarSearchBox;
        private TextBox drawerApiKeyBox;
        private Label todayLabel;
        private Label earlierLabel;
        private ChibiCardControl chibiCard;
        private Button newChatButton;
        private Button drawerSaveButton;
        private Button drawerTestButton;
        private Button drawerCloseButton;
        private Button drawerMemoryButton;
        private Button drawerAdvancedButton;
        private Button drawerRedeployVoiceButton;
        private CheckBox drawerVoiceEnabledBox;
        private CheckBox drawerMemoryEnabledBox;
        private CheckBox drawerOptimizeBox;
        private WaveformControl waveform;
        private CompressionStatusControl compressionStatusControl;
        private ServiceStatusControl serviceStatusControl;
        private readonly List<Button> quickActionButtons = new List<Button>();
        private readonly List<ConversationItemControl> sidebarConversationItems = new List<ConversationItemControl>();
        private ContextMenuStrip activeConversationMenu;
        private bool isDraggingWindow;
        private bool sidebarSearchPlaceholderActive;
        private bool voiceServiceReady;
        private DateTime lastAvatarInteractionUtc;
        private Point dragOrigin;

        public MainForm()
        {
            RequiredVisualAssets.EnsurePresent();
            settings = SettingsStore.Load();
            memory = MemoryStore.Load();
            Text = "彩叶 Iroha Agent";
            MinimumSize = new Size(980, 552);
            Rectangle workArea = Screen.PrimaryScreen != null ? Screen.PrimaryScreen.WorkingArea : new Rectangle(0, 0, 1280, 720);
            int targetWidth = Math.Min(1280, Math.Max(MinimumSize.Width, workArea.Width - 32));
            int targetHeight = Math.Max(MinimumSize.Height, (int)Math.Round(targetWidth * 941.0 / 1672.0));
            if (targetHeight > workArea.Height - 32)
            {
                targetHeight = Math.Max(MinimumSize.Height, workArea.Height - 32);
                targetWidth = Math.Max(MinimumSize.Width, (int)Math.Round(targetHeight * 1672.0 / 941.0));
            }
            Size = new Size(targetWidth, targetHeight);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.FromArgb(236, 248, 250);
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            DoubleBuffered = true;
            TryUseEmbeddedAppIcon();

            BuildLayout();
            string greeting = string.IsNullOrWhiteSpace(settings.ApiKey) ?
                "早安！很高兴又见到你～\n新的一天也要加油哦，今天想聊点什么呢？\n先在设定中填好 DeepSeek API Key，我就能认真听你说啦。" :
                "早安！很高兴又见到你～\n新的一天也要加油哦，今天想聊点什么呢？\n我可以陪你聊天、帮你做计划、寻找灵感，或者一起复盘进步。\n你尽管说，我会认真听，和你一起想办法的！";
            AddAssistantLine(greeting);
            avatar.SetState(AvatarState.Idle);
            Shown += delegate { StartVoiceWarmup(); };
        }

        private void TryUseEmbeddedAppIcon()
        {
            try
            {
                Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (icon != null)
                {
                    Icon = icon;
                }
            }
            catch
            {
            }
        }

        private void BuildLayout()
        {
            Controls.Clear();
            quickActionButtons.Clear();

            avatar = new AvatarControl();
            avatar.StageMode = true;
            avatar.BackColor = Theme.AppBg;
            avatar.Cursor = Cursors.Default;
            avatar.MouseClick += AvatarStage_MouseClick;
            avatar.MouseMove += AvatarStage_MouseMove;

            vnRoot = avatar;
            vnRoot.Dock = DockStyle.Fill;
            vnRoot.BackColor = Theme.AppBg;
            Controls.Add(vnRoot);

            vnBackground = null;

            topBarControl = new TopBarControl();
            topBarControl.ModelName = settings.Model;
            topBarControl.PaintBarChrome = true;
            topBarControl.BackColor = Color.White;
            topBarControl.MouseDown += TopBar_MouseDown;
            topBarControl.MouseMove += TopBar_MouseMove;
            topBarControl.MouseUp += TopBar_MouseUp;
            Controls.Add(topBarControl);

            characterTopOverlay = new CharacterTopOverlayControl(avatar);
            characterTopOverlay.Dock = DockStyle.Fill;
            characterTopOverlay.Enabled = false;
            topBarControl.Controls.Add(characterTopOverlay);
            characterTopOverlay.BringToFront();

            leftSidebar = new GlassPanel();
            leftSidebar.PaintChrome = true;
            leftSidebar.FillColor = Color.FromArgb(238, 250, 254, 255);
            leftSidebar.BorderColor = Color.FromArgb(108, 176, 222, 236);
            leftSidebar.Radius = 20;
            leftSidebar.Shadow = true;
            vnRoot.Controls.Add(leftSidebar);

            sidebarSearchShell = new GlassPanel();
            sidebarSearchShell.PaintChrome = true;
            sidebarSearchShell.FillColor = Color.FromArgb(244, 255, 255, 255);
            sidebarSearchShell.BorderColor = Color.FromArgb(120, 206, 232, 240);
            sidebarSearchShell.Radius = 18;
            sidebarSearchShell.Shadow = false;
            sidebarSearchShell.IconKind = "search";
            leftSidebar.Controls.Add(sidebarSearchShell);

            sidebarSearchBox = new TextBox();
            sidebarSearchPlaceholderActive = true;
            sidebarSearchBox.Text = SidebarSearchPlaceholder;
            sidebarSearchBox.BorderStyle = BorderStyle.None;
            sidebarSearchBox.BackColor = Color.FromArgb(250, 254, 255);
            sidebarSearchBox.ForeColor = Color.FromArgb(116, 143, 165);
            sidebarSearchBox.Font = new Font("Microsoft YaHei UI", 9.2F, FontStyle.Regular);
            sidebarSearchBox.GotFocus += SidebarSearchBox_GotFocus;
            sidebarSearchBox.LostFocus += SidebarSearchBox_LostFocus;
            sidebarSearchBox.TextChanged += SidebarSearchBox_TextChanged;
            sidebarSearchShell.Controls.Add(sidebarSearchBox);

            newChatButton = CreateGlassButton("+", false);
            SetButtonChrome(newChatButton, true);
            newChatButton.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Regular);
            ((GlassButton)newChatButton).CircularChrome = true;
            newChatButton.Click += delegate
            {
                history.Clear();
                chatLog.Clear();
                var conversation = AddSidebarConversationItem("新的对话", "刚刚", true, true);
                SetActiveSidebarConversation(conversation);
                AddAssistantLine("新的对话已经准备好了。今天想从什么开始呢？");
                SetStatus("已创建新对话", AvatarState.Happy);
                ReorderSidebarConversationItems();
                ApplySidebarSearchFilter();
            };
            leftSidebar.Controls.Add(newChatButton);

            todayLabel = CreateTransparentLabel("今天", 9F, FontStyle.Bold, Theme.TextSub);
            todayLabel.TextAlign = ContentAlignment.MiddleLeft;
            leftSidebar.Controls.Add(todayLabel);

            AddSidebarConversationItem("和彩叶的日常陪伴", "刚刚", true);
            AddSidebarConversationItem("周计划讨论", "09:15", false);
            AddSidebarConversationItem("灵感头脑风暴", "昨天", false);
            AddSidebarConversationItem("读书笔记复盘", "前天", false);
            AddSidebarConversationItem("旅行计划制定", "3 天前", false);

            earlierLabel = CreateTransparentLabel("更早", 9F, FontStyle.Bold, Theme.TextSub);
            earlierLabel.TextAlign = ContentAlignment.MiddleLeft;
            leftSidebar.Controls.Add(earlierLabel);

            saveChatButton = CreateGlassButton("保存对话", false);
            SetButtonChrome(saveChatButton, true);
            ((GlassButton)saveChatButton).AccessibleDescription = "sidebar-save";
            ((GlassButton)saveChatButton).OpaqueBackfill = true;
            saveChatButton.Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Regular);
            saveChatButton.Click += SaveChatButton_Click;
            leftSidebar.Controls.Add(saveChatButton);

            clearChatButton = CreateGlassButton("清空对话", false);
            SetButtonChrome(clearChatButton, true);
            ((GlassButton)clearChatButton).AccessibleDescription = "sidebar-delete";
            ((GlassButton)clearChatButton).OpaqueBackfill = true;
            clearChatButton.Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Regular);
            clearChatButton.Click += ClearChatButton_Click;
            leftSidebar.Controls.Add(clearChatButton);

            chibiCard = new ChibiCardControl();
            chibiCard.Cursor = Cursors.Hand;
            chibiCard.Click += TestVoiceButton_Click;
            leftSidebar.Controls.Add(chibiCard);

            quickApiKeyBox = new TextBox();
            quickApiKeyBox.Text = settings.ApiKey ?? "";
            quickApiKeyBox.Visible = false;
            vnRoot.Controls.Add(quickApiKeyBox);

            quickSaveKeyButton = CreateGlassButton("保存 Key", true);
            quickSaveKeyButton.Visible = false;
            quickSaveKeyButton.Click += QuickSaveKeyButton_Click;
            vnRoot.Controls.Add(quickSaveKeyButton);

            quickTestButton = CreateGlassButton("测试", false);
            quickTestButton.Visible = false;
            quickTestButton.Click += QuickTestButton_Click;
            vnRoot.Controls.Add(quickTestButton);

            autoOptimizeBox = new CheckBox();
            autoOptimizeBox.Visible = false;
            autoOptimizeBox.Checked = settings.AutoOptimizePrompt;
            autoOptimizeBox.CheckedChanged += AutoOptimizeBox_CheckedChanged;
            vnRoot.Controls.Add(autoOptimizeBox);

            memoryCard = CreateInfoCard("长期记忆", out memoryCardBodyLabel);
            memoryCard.PaintChrome = true;
            memoryCard.IconKind = "memory";
            memoryCard.FooterText = "彩叶会记得这些，持续陪伴你成长";
            memoryManageButton = CreateGlassButton("管理", false);
            memoryManageButton.Font = new Font("Microsoft YaHei UI", 7.8F, FontStyle.Bold);
            ((GlassButton)memoryManageButton).OpaqueBackfill = true;
            memoryManageButton.Click += MemoryButton_Click;
            memoryCard.Controls.Add(memoryManageButton);
            vnRoot.Controls.Add(memoryCard);

            compressionCard = CreateInfoCard("上下文压缩", out compressionCardBodyLabel);
            compressionCard.PaintChrome = true;
            compressionCard.IconKind = "compress";
            optimizeToggleButton = CreateGlassButton("", false);
            SetButtonChrome(optimizeToggleButton, true);
            optimizeToggleButton.Font = new Font("Microsoft YaHei UI", 7.8F, FontStyle.Bold);
            ((GlassButton)optimizeToggleButton).OpaqueBackfill = true;
            optimizeToggleButton.Click += ToggleOptimizePrompt_Click;
            compressionCard.Controls.Add(optimizeToggleButton);
            compressionCardBodyLabel.Visible = false;
            compressionStatusControl = new CompressionStatusControl();
            compressionCard.Controls.Add(compressionStatusControl);
            vnRoot.Controls.Add(compressionCard);

            serviceCard = CreateInfoCard("服务状态", out serviceCardBodyLabel);
            serviceCard.PaintChrome = true;
            serviceCard.IconKind = "service";
            serviceCardBodyLabel.Visible = false;
            serviceStatusControl = new ServiceStatusControl();
            serviceCard.Controls.Add(serviceStatusControl);
            vnRoot.Controls.Add(serviceCard);

            dialoguePanel = new GlassPanel();
            dialoguePanel.PaintChrome = true;
            dialoguePanel.FillColor = Color.FromArgb(198, 194, 236, 249);
            dialoguePanel.BorderColor = Color.FromArgb(172, 118, 207, 222);
            dialoguePanel.Radius = 24;
            dialoguePanel.Shadow = true;
            dialoguePanel.DecorationKind = "dialogue";
            vnRoot.Controls.Add(dialoguePanel);

            dialogueNameLabel = new DialogueNameplateControl();
            dialogueNameLabel.Text = "酒寄彩叶 · Iroha";
            dialogueNameLabel.Cursor = Cursors.Hand;
            dialogueNameLabel.AccessibleName = "试听彩叶语音";
            dialogueNameLabel.AccessibleDescription = "点击播放彩叶的日语语音";
            dialogueNameLabel.Click += TestVoiceButton_Click;
            dialoguePanel.Controls.Add(dialogueNameLabel);

            dialogueTextBox = new VnDialogueTextControl();
            dialogueTextBox.ReadOnly = true;
            dialogueTextBox.BorderStyle = BorderStyle.None;
            dialogueTextBox.BackColor = Color.Transparent;
            dialogueTextBox.ForeColor = Theme.TextMain;
            dialogueTextBox.Font = new Font("Microsoft YaHei UI", 8.7F, FontStyle.Regular);
            dialogueTextBox.DetectUrls = false;
            dialogueTextBox.ScrollBars = RichTextBoxScrollBars.None;
            dialoguePanel.Controls.Add(dialogueTextBox);

            quickActionBar = new GlassPanel();
            quickActionBar.PaintChrome = true;
            quickActionBar.OpaqueBackfill = false;
            quickActionBar.FillColor = Color.FromArgb(214, 250, 254, 255);
            quickActionBar.BorderColor = Color.FromArgb(96, 180, 224, 238);
            quickActionBar.Radius = 18;
            quickActionBar.Shadow = false;
            quickActionBar.BareSurface = true;
            vnRoot.Controls.Add(quickActionBar);

            AddQuickActionButton(quickActionBar, "陪我聊", "chat", "陪我轻松聊一会儿，先用温柔一点的方式问问我今天怎么样。");
            AddQuickActionButton(quickActionBar, "做计划", "plan", "帮我把接下来要做的事情整理成一个轻量计划，先问我目标和时间。");
            AddQuickActionButton(quickActionBar, "找灵感", "idea", "陪我发散几个有趣点子，先给我三个方向，再问我喜欢哪个。");
            AddQuickActionButton(quickActionBar, "复盘", "review", "带我做一个今日复盘：做得好的、卡住的、明天可以轻一点推进的。");

            inputComposer = new GlassPanel();
            inputComposer.PaintChrome = true;
            inputComposer.FillColor = Color.FromArgb(235, 255, 255, 255);
            inputComposer.BorderColor = Theme.BorderStrong;
            inputComposer.Radius = 22;
            inputComposer.Shadow = true;
            vnRoot.Controls.Add(inputComposer);

            inputBox = new TextBox();
            inputBox.Multiline = true;
            inputBox.BorderStyle = BorderStyle.None;
            inputBox.BackColor = Color.FromArgb(250, 254, 255);
            inputBox.ForeColor = Color.FromArgb(42, 70, 94);
            inputBox.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular);
            inputBox.Text = "";
            inputBox.TextChanged += InputBox_TextChanged;
            inputBox.GotFocus += InputBox_FocusChanged;
            inputBox.LostFocus += InputBox_FocusChanged;
            inputBox.KeyDown += InputBox_KeyDown;
            inputComposer.Controls.Add(inputBox);

            inputPlaceholderLabel = CreateTransparentLabel("输入你的消息...（Shift + Enter 换行）", 9.6F, FontStyle.Regular, Color.FromArgb(116, 150, 174));
            inputPlaceholderLabel.TextAlign = ContentAlignment.MiddleLeft;
            inputPlaceholderLabel.Cursor = Cursors.IBeam;
            inputPlaceholderLabel.Click += delegate
            {
                inputBox.Focus();
                UpdateInputPlaceholder();
            };
            inputComposer.Controls.Add(inputPlaceholderLabel);

            sendButton = CreateGlassButton("\uE724", true);
            SetButtonChrome(sendButton, true);
            sendButton.Text = "";
            sendButton.AccessibleDescription = "composer-send";
            sendButton.Font = new Font("Segoe Fluent Icons", 17F, FontStyle.Regular);
            ((GlassButton)sendButton).OpaqueBackfill = true;
            sendButton.Click += SendButton_Click;
            inputComposer.Controls.Add(sendButton);

            voiceDock = new GlassPanel();
            voiceDock.PaintChrome = true;
            voiceDock.FillColor = Color.FromArgb(240, 250, 254, 255);
            voiceDock.BorderColor = Color.FromArgb(132, 175, 223, 236);
            voiceDock.Radius = 22;
            voiceDock.Shadow = true;
            vnRoot.Controls.Add(voiceDock);

            testVoiceButton = CreateGlassButton("试听彩叶", true);
            testVoiceButton.Text = "";
            testVoiceButton.AccessibleDescription = "voice-play";
            var voicePlayGlass = testVoiceButton as GlassButton;
            if (voicePlayGlass != null)
            {
                voicePlayGlass.CircularChrome = true;
                voicePlayGlass.Accent = false;
            }
            SetButtonChrome(testVoiceButton, true);
            testVoiceButton.Click += TestVoiceButton_Click;
            voiceDock.Controls.Add(testVoiceButton);

            voiceStateLabel = new VoiceStatusLabel();
            voiceStateLabel.Text = "试听彩叶\n日语 · 女声";
            voiceStateLabel.AutoSize = false;
            voiceStateLabel.BackColor = Color.Transparent;
            voiceStateLabel.ForeColor = Theme.TextMain;
            voiceStateLabel.TextAlign = ContentAlignment.MiddleLeft;
            voiceDock.Controls.Add(voiceStateLabel);

            waveform = new WaveformControl();
            waveform.BackColor = Color.Transparent;
            voiceDock.Controls.Add(waveform);

            voiceEngineLabel = CreateTransparentLabel("GPT-SoVITS · 本地", 7.2F, FontStyle.Regular, Color.FromArgb(92, 128, 154));
            voiceEngineLabel.TextAlign = ContentAlignment.MiddleLeft;
            voiceDock.Controls.Add(voiceEngineLabel);

            rightToolRail = new ToolRailPanel();
            rightToolRail.PaintChrome = true;
            rightToolRail.FillColor = Color.FromArgb(224, 248, 253, 255);
            rightToolRail.BorderColor = Color.FromArgb(166, 174, 214, 226);
            rightToolRail.Radius = 22;
            rightToolRail.Shadow = true;
            vnRoot.Controls.Add(rightToolRail);

            memoryButton = CreateGlassButton("记忆", false);
            memoryButton.Visible = false;
            memoryButton.Click += MemoryButton_Click;
            rightToolRail.Controls.Add(memoryButton);

            settingsButton = CreateGlassButton("设定", false);
            settingsButton.Visible = false;
            settingsButton.Click += SettingsButton_Click;
            rightToolRail.Controls.Add(settingsButton);

            appearanceButton = CreateGlassButton("外观", false);
            appearanceButton.Visible = false;
            appearanceButton.Click += AppearanceButton_Click;
            rightToolRail.Controls.Add(appearanceButton);
            rightToolRail.SetActions(memoryButton, settingsButton, appearanceButton);
            rightToolRail.MemoryClicked += MemoryButton_Click;
            rightToolRail.SettingsClicked += SettingsButton_Click;
            rightToolRail.AppearanceClicked += AppearanceButton_Click;

            settingsDrawer = new GlassPanel();
            settingsDrawer.FillColor = Color.FromArgb(244, 255, 255, 255);
            settingsDrawer.BorderColor = Color.FromArgb(178, 128, 210, 226);
            settingsDrawer.Radius = 24;
            settingsDrawer.Shadow = true;
            settingsDrawer.Visible = false;
            vnRoot.Controls.Add(settingsDrawer);

            var drawerTitleLabel = CreateTransparentLabel("设定中心", 14F, FontStyle.Bold, Theme.TextMain);
            drawerTitleLabel.TextAlign = ContentAlignment.MiddleLeft;
            settingsDrawer.Controls.Add(drawerTitleLabel);

            drawerCloseButton = CreateGlassButton("×", false);
            drawerCloseButton.Font = new Font("Segoe UI Symbol", 12F, FontStyle.Bold);
            ((GlassButton)drawerCloseButton).OpaqueBackfill = true;
            drawerCloseButton.Click += delegate { HideSettingsDrawer(); };
            settingsDrawer.Controls.Add(drawerCloseButton);

            settingsDrawerHintLabel = CreateTransparentLabel("连接 DeepSeek、语音和记忆，不打扰主聊天画面。", 8.7F, FontStyle.Regular, Theme.TextSub);
            settingsDrawerHintLabel.TextAlign = ContentAlignment.MiddleLeft;
            settingsDrawer.Controls.Add(settingsDrawerHintLabel);

            drawerApiKeyBox = new TextBox();
            drawerApiKeyBox.Text = settings.ApiKey ?? "";
            drawerApiKeyBox.UseSystemPasswordChar = true;
            drawerApiKeyBox.BorderStyle = BorderStyle.None;
            drawerApiKeyBox.BackColor = Color.FromArgb(248, 253, 255);
            drawerApiKeyBox.ForeColor = Theme.TextMain;
            drawerApiKeyBox.Font = new Font("Microsoft YaHei UI", 9.4F, FontStyle.Regular);
            settingsDrawer.Controls.Add(drawerApiKeyBox);

            drawerSaveButton = CreateGlassButton("保存 Key", true);
            ((GlassButton)drawerSaveButton).OpaqueBackfill = true;
            drawerSaveButton.Click += DrawerSaveButton_Click;
            settingsDrawer.Controls.Add(drawerSaveButton);

            drawerTestButton = CreateGlassButton("测试连接", false);
            ((GlassButton)drawerTestButton).OpaqueBackfill = true;
            drawerTestButton.Click += QuickTestButton_Click;
            settingsDrawer.Controls.Add(drawerTestButton);

            drawerVoiceEnabledBox = CreateDrawerCheckBox("日语语音");
            drawerVoiceEnabledBox.Checked = settings.VoiceEnabled;
            drawerVoiceEnabledBox.CheckedChanged += DrawerOption_CheckedChanged;
            settingsDrawer.Controls.Add(drawerVoiceEnabledBox);

            drawerMemoryEnabledBox = CreateDrawerCheckBox("长期记忆");
            drawerMemoryEnabledBox.Checked = settings.MemoryEnabled;
            drawerMemoryEnabledBox.CheckedChanged += DrawerOption_CheckedChanged;
            settingsDrawer.Controls.Add(drawerMemoryEnabledBox);

            drawerOptimizeBox = CreateDrawerCheckBox("省 token");
            drawerOptimizeBox.Checked = settings.AutoOptimizePrompt;
            drawerOptimizeBox.CheckedChanged += DrawerOption_CheckedChanged;
            settingsDrawer.Controls.Add(drawerOptimizeBox);

            drawerRedeployVoiceButton = CreateGlassButton("重新部署语音", false);
            ((GlassButton)drawerRedeployVoiceButton).OpaqueBackfill = true;
            drawerRedeployVoiceButton.Click += RedeployVoiceButton_Click;
            settingsDrawer.Controls.Add(drawerRedeployVoiceButton);

            drawerMemoryButton = CreateGlassButton("管理记忆", false);
            ((GlassButton)drawerMemoryButton).OpaqueBackfill = true;
            drawerMemoryButton.Click += MemoryButton_Click;
            settingsDrawer.Controls.Add(drawerMemoryButton);

            drawerAdvancedButton = CreateGlassButton("高级设置", false);
            ((GlassButton)drawerAdvancedButton).OpaqueBackfill = true;
            drawerAdvancedButton.Click += OpenAdvancedSettingsForm;
            settingsDrawer.Controls.Add(drawerAdvancedButton);

            settingsDrawerStatusLabel = CreateTransparentLabel("", 8.6F, FontStyle.Regular, Color.FromArgb(66, 145, 166));
            settingsDrawerStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            settingsDrawer.Controls.Add(settingsDrawerStatusLabel);

            topSettingsButton = CreateGlassButton("\uE713", false);
            SetButtonChrome(topSettingsButton, true);
            topSettingsButton.Font = new Font("Segoe Fluent Icons", 12F, FontStyle.Regular);
            ((GlassButton)topSettingsButton).MinimalChrome = true;
            topSettingsButton.BackColor = Color.Transparent;
            topSettingsButton.Click += SettingsButton_Click;
            topBarControl.Controls.Add(topSettingsButton);
            topSettingsButton.Visible = false;

            minimizeButton = CreateGlassButton("\uE921", false);
            SetButtonChrome(minimizeButton, true);
            minimizeButton.Font = new Font("Segoe Fluent Icons", 11F, FontStyle.Regular);
            ((GlassButton)minimizeButton).MinimalChrome = true;
            minimizeButton.BackColor = Color.Transparent;
            minimizeButton.Click += delegate { WindowState = FormWindowState.Minimized; };
            topBarControl.Controls.Add(minimizeButton);
            minimizeButton.Visible = false;

            maximizeButton = CreateGlassButton("\uE922", false);
            SetButtonChrome(maximizeButton, true);
            maximizeButton.Font = new Font("Segoe Fluent Icons", 11F, FontStyle.Regular);
            ((GlassButton)maximizeButton).MinimalChrome = true;
            maximizeButton.BackColor = Color.Transparent;
            maximizeButton.Click += delegate { ToggleWindowMaximize(); };
            topBarControl.Controls.Add(maximizeButton);
            maximizeButton.Visible = false;

            closeButton = CreateGlassButton("\uE8BB", false);
            SetButtonChrome(closeButton, true);
            closeButton.Font = new Font("Segoe Fluent Icons", 11F, FontStyle.Regular);
            ((GlassButton)closeButton).MinimalChrome = true;
            closeButton.BackColor = Color.Transparent;
            closeButton.Click += delegate { Close(); };
            topBarControl.Controls.Add(closeButton);
            closeButton.Visible = false;

            footerBar = new FooterBarControl();
            vnRoot.Controls.Add(footerBar);

            statusLabel = CreateTransparentLabel("", 8.8F, FontStyle.Regular, Theme.TextSub);
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            vnRoot.Controls.Add(statusLabel);

            quickHintLabel = CreateTransparentLabel("", 8.8F, FontStyle.Regular, Color.FromArgb(78, 142, 166));
            quickHintLabel.TextAlign = ContentAlignment.MiddleLeft;
            vnRoot.Controls.Add(quickHintLabel);

            chatLog = new RichTextBox();
            chatLog.ReadOnly = true;
            chatLog.Visible = false;
            chatLog.BorderStyle = BorderStyle.None;
            chatLog.DetectUrls = false;
            vnRoot.Controls.Add(chatLog);

            RefreshInfoCards();
            SyncQuickSettingsView();
            LayoutVNControls();
            ApplyVNZOrder();
            Resize += delegate { LayoutVNControls(); };
        }

        private GlassPanel CreateInfoCard(string title, out Label bodyLabel)
        {
            var card = new GlassPanel();
            card.FillColor = Color.FromArgb(238, 252, 255, 255);
            card.BorderColor = Theme.Border;
            card.Radius = 18;
            card.Shadow = true;

            var titleLabel = CreateTransparentLabel(title, 8.9F, FontStyle.Bold, Theme.TextMain);
            titleLabel.SetBounds(40, 17, 200, 26);
            card.Controls.Add(titleLabel);

            bodyLabel = CreateTransparentLabel("", 7.6F, FontStyle.Regular, Theme.TextSub);
            bodyLabel.SetBounds(22, 49, 238, 98);
            bodyLabel.TextAlign = ContentAlignment.TopLeft;
            card.Controls.Add(bodyLabel);
            return card;
        }

        private Label CreateTransparentLabel(string text, float size, FontStyle style, Color color)
        {
            var label = new Label();
            label.Text = text;
            label.AutoSize = false;
            label.BackColor = Color.Transparent;
            label.ForeColor = color;
            label.Font = new Font("Microsoft YaHei UI", size, style);
            return label;
        }

        private Button CreateGlassButton(string text, bool primary)
        {
            var button = new GlassButton();
            button.Text = text;
            StyleButton(button, primary);
            return button;
        }

        private void SetButtonChrome(Button button, bool paintChrome)
        {
            var glass = button as GlassButton;
            if (glass != null)
            {
                glass.PaintChrome = paintChrome;
            }
        }

        private void SetToolRailSelection(Button selected)
        {
            if (rightToolRail == null) return;
            if (object.ReferenceEquals(selected, settingsButton)) rightToolRail.SelectedIndex = 1;
            else if (object.ReferenceEquals(selected, appearanceButton)) rightToolRail.SelectedIndex = 2;
            else rightToolRail.SelectedIndex = 0;
        }

        private CheckBox CreateDrawerCheckBox(string text)
        {
            var box = new GlassCheckBox();
            box.Text = text;
            box.AutoSize = false;
            box.FlatStyle = FlatStyle.Flat;
            box.BackColor = Color.FromArgb(244, 252, 255);
            box.ForeColor = Theme.TextMain;
            box.Font = new Font("Microsoft YaHei UI", 9.1F, FontStyle.Bold);
            box.Cursor = Cursors.Hand;
            box.TabStop = false;
            return box;
        }

        private void LayoutVNControls()
        {
            if (vnRoot == null) return;
            int w = Math.Max(1, ClientSize.Width);
            int h = Math.Max(1, ClientSize.Height);
            int sx = Math.Max(1, w) ;
            int sy = Math.Max(1, h) ;

            topBarControl.SetBounds(0, 0, w, ScaleY(70, sy));
            int topButtonSize = 32;
            int topButtonY = ScaleY(18, sy);
            int closeX = w - 18 - topButtonSize;
            int maxX = closeX - 14 - topButtonSize;
            int minX = maxX - 14 - topButtonSize;
            int settingsX = minX - 30 - topButtonSize;
            topSettingsButton.SetBounds(settingsX, topButtonY, topButtonSize, topButtonSize);
            minimizeButton.SetBounds(minX, topButtonY, topButtonSize, topButtonSize);
            maximizeButton.SetBounds(maxX, topButtonY, topButtonSize, topButtonSize);
            closeButton.SetBounds(closeX, topButtonY, topButtonSize, topButtonSize);
            topSettingsButton.Region = null;
            minimizeButton.Region = null;
            maximizeButton.Region = null;
            closeButton.Region = null;

            if (w < 1120 || h < 630)
            {
                LayoutCompactVNControls(w, h);
                return;
            }

            memoryCard.Visible = true;
            compressionCard.Visible = true;
            serviceCard.Visible = true;

            leftSidebar.SetBounds(ScaleX(12, sx), ScaleY(86, sy), ScaleX(294, sx), Math.Max(ScaleY(520, sy), h - ScaleY(108, sy)));
            LayoutLeftSidebarControls();

            memoryCard.SetBounds(ScaleX(324, sx), ScaleY(88, sy), ScaleX(291, sx), ScaleY(225, sy));
            compressionCard.SetBounds(ScaleX(324, sx), ScaleY(312, sy), ScaleX(291, sx), ScaleY(128, sy));
            serviceCard.SetBounds(ScaleX(324, sx), ScaleY(441, sy), ScaleX(291, sx), ScaleY(108, sy));
            LayoutInfoCardChildren(memoryCard, memoryCardBodyLabel, memoryManageButton);
            LayoutInfoCardChildren(compressionCard, compressionCardBodyLabel, optimizeToggleButton);
            LayoutInfoCardChildren(serviceCard, serviceCardBodyLabel, null);
            LayoutInfoCardStatusControls();

            int avatarX = ScaleX(682, sx);
            int avatarY = ScaleY(34, sy);
            int avatarW = Math.Min(ScaleX(832, sx), w - avatarX - ScaleX(34, sx));
            int avatarH = h - ScaleY(48, sy);
            avatar.CharacterStageBounds = new Rectangle(avatarX, avatarY, Math.Max(360, avatarW), Math.Max(440, avatarH));

            dialoguePanel.SetBounds(ScaleX(326, sx), ScaleY(568, sy), ScaleX(740, sx), ScaleY(201, sy));
            int nameplateWidth = Math.Min(184, Math.Max(156, (int)Math.Round(dialoguePanel.Width * 0.325)));
            dialogueNameLabel.SetBounds(1, 2, Math.Min(nameplateWidth, dialoguePanel.Width - 19), 32);
            dialogueTextBox.SetBounds(42, 44, Math.Max(100, dialoguePanel.Width - 84), Math.Max(58, dialoguePanel.Height - 53));

            quickActionBar.SetBounds(ScaleX(326, sx), ScaleY(769, sy), ScaleX(662, sx), ScaleY(54, sy));
            LayoutQuickActionButtons();

            inputComposer.SetBounds(ScaleX(326, sx), ScaleY(828, sy), ScaleX(730, sx), ScaleY(90, sy));
            LayoutInputComposer();

            voiceDock.SetBounds(ScaleX(1080, sx), ScaleY(828, sy), ScaleX(458, sx), ScaleY(90, sy));
            LayoutVoiceDock();

            rightToolRail.SetBounds(ScaleX(1544, sx), ScaleY(620, sy), Math.Max(66, ScaleX(92, sx)), ScaleY(258, sy));
            LayoutRightToolRail();

            int drawerWidth = Math.Min(390, Math.Max(330, ScaleX(390, sx)));
            int drawerGap = Math.Max(12, ScaleX(14, sx));
            int drawerY = ScaleY(86, sy);
            int drawerRight = rightToolRail.Left - drawerGap;
            int drawerBottom = voiceDock.Top - drawerGap;
            settingsDrawer.SetBounds(
                drawerRight - drawerWidth,
                drawerY,
                drawerWidth,
                Math.Max(420, drawerBottom - drawerY));
            LayoutSettingsDrawer();

            int footerHeight = Math.Max(20, ScaleY(27, sy));
            footerBar.SetBounds(0, h - footerHeight, w, footerHeight);
            statusLabel.SetBounds(ScaleX(22, sx), h - footerHeight, ScaleX(500, sx), footerHeight);
            quickHintLabel.SetBounds(ScaleX(520, sx), h - footerHeight, w - ScaleX(580, sx), footerHeight);
            chatLog.SetBounds(0, 0, 1, 1);
            ApplyVNZOrder();
        }

        private void LayoutCompactVNControls(int w, int h)
        {
            int topHeight = topBarControl.Height;
            int sidebarY = topHeight + 8;
            int sidebarWidth = Math.Min(224, Math.Max(208, w / 5));
            leftSidebar.SetBounds(8, sidebarY, sidebarWidth, Math.Max(430, h - sidebarY - 30));
            LayoutLeftSidebarControls();

            memoryCard.Visible = false;
            compressionCard.Visible = false;
            serviceCard.Visible = false;

            int contentLeft = leftSidebar.Right + 12;
            int dialogueWidth = Math.Min(490, Math.Max(430, w - contentLeft - 250));
            int inputTop = h - 88;
            int quickTop = inputTop - 42;
            int dialogueHeight = 142;
            int dialogueTop = quickTop - dialogueHeight - 6;

            int avatarX = Math.Max(contentLeft + 150, (int)Math.Round(w * 0.43));
            int avatarY = Math.Max(18, topHeight - 10);
            avatar.CharacterStageBounds = new Rectangle(avatarX, avatarY, Math.Max(390, w - avatarX - 58), Math.Max(470, h - avatarY - 4));

            dialoguePanel.SetBounds(contentLeft, dialogueTop, dialogueWidth, dialogueHeight);
            int compactNameplateWidth = Math.Min(184, Math.Max(154, (int)Math.Round(dialoguePanel.Width * 0.39)));
            dialogueNameLabel.SetBounds(1, 2, Math.Min(compactNameplateWidth, dialoguePanel.Width - 17), 30);
            dialogueTextBox.SetBounds(30, 42, Math.Max(100, dialoguePanel.Width - 60), Math.Max(48, dialoguePanel.Height - 50));

            quickActionBar.SetBounds(contentLeft, quickTop, Math.Min(dialogueWidth, 470), 38);
            LayoutQuickActionButtons();

            inputComposer.SetBounds(contentLeft, inputTop, dialogueWidth, 58);
            LayoutInputComposer();

            int railWidth = 62;
            int railX = w - railWidth - 6;
            int voiceX = contentLeft + dialogueWidth + 8;
            int voiceWidth = Math.Max(166, railX - voiceX - 8);
            voiceDock.SetBounds(voiceX, inputTop, voiceWidth, 58);
            LayoutVoiceDock();

            rightToolRail.SetBounds(railX, Math.Max(topHeight + 78, dialogueTop - 4), railWidth, 194);
            LayoutRightToolRail();

            int drawerWidth = Math.Min(340, Math.Max(310, w / 3));
            int drawerY = topHeight + 10;
            int drawerRight = rightToolRail.Left - 10;
            int drawerBottom = voiceDock.Top - 10;
            settingsDrawer.SetBounds(
                drawerRight - drawerWidth,
                drawerY,
                drawerWidth,
                Math.Max(380, drawerBottom - drawerY));
            LayoutSettingsDrawer();

            footerBar.SetBounds(0, h - 26, w, 26);
            statusLabel.SetBounds(14, h - 25, Math.Min(340, w / 2), 22);
            quickHintLabel.SetBounds(Math.Min(350, w / 2 - 20), h - 25, Math.Max(240, w - 370), 22);
            chatLog.SetBounds(0, 0, 1, 1);
            ApplyVNZOrder();
        }

        private void ApplyVNZOrder()
        {
            if (vnBackground != null) vnBackground.SendToBack();
            if (avatar != null) avatar.BringToFront();
            if (topBarControl != null) topBarControl.BringToFront();
            if (leftSidebar != null) leftSidebar.BringToFront();
            if (memoryCard != null) memoryCard.BringToFront();
            if (compressionCard != null) compressionCard.BringToFront();
            if (serviceCard != null) serviceCard.BringToFront();
            if (dialoguePanel != null) dialoguePanel.BringToFront();
            if (quickActionBar != null) quickActionBar.BringToFront();
            if (inputComposer != null) inputComposer.BringToFront();
            if (voiceDock != null) voiceDock.BringToFront();
            if (rightToolRail != null) rightToolRail.BringToFront();
            if (memoryButton != null) memoryButton.BringToFront();
            if (settingsButton != null) settingsButton.BringToFront();
            if (appearanceButton != null) appearanceButton.BringToFront();
            if (settingsDrawer != null && settingsDrawer.Visible) settingsDrawer.BringToFront();
            if (footerBar != null) footerBar.BringToFront();
            if (statusLabel != null) statusLabel.BringToFront();
            if (quickHintLabel != null) quickHintLabel.BringToFront();
            if (topSettingsButton != null) topSettingsButton.BringToFront();
            if (minimizeButton != null) minimizeButton.BringToFront();
            if (maximizeButton != null) maximizeButton.BringToFront();
            if (closeButton != null) closeButton.BringToFront();
        }

        private void VnBackground_MouseClick(object sender, MouseEventArgs e)
        {
            HandleVnHotspotClick(e.Location);
        }

        private void VnRoot_MouseClick(object sender, MouseEventArgs e)
        {
            HandleVnHotspotClick(e.Location);
        }

        private void VnBackground_MouseMove(object sender, MouseEventArgs e)
        {
            UpdateHotspotCursor(vnBackground, e.Location);
        }

        private void VnRoot_MouseMove(object sender, MouseEventArgs e)
        {
            UpdateHotspotCursor(vnRoot, e.Location);
        }

        private void UpdateHotspotCursor(Control control, Point point)
        {
            if (control == null) return;
            control.Cursor = IsVnHotspot(point) ? Cursors.Hand : Cursors.Default;
        }

        private bool IsVnHotspot(Point point)
        {
            return
                (memoryButton != null && memoryButton.Bounds.Contains(point)) ||
                (settingsButton != null && settingsButton.Bounds.Contains(point)) ||
                (appearanceButton != null && appearanceButton.Bounds.Contains(point));
        }

        private bool HandleVnHotspotClick(Point point)
        {
            if (memoryButton != null && memoryButton.Bounds.Contains(point))
            {
                MemoryButton_Click(this, EventArgs.Empty);
                return true;
            }
            if (settingsButton != null && settingsButton.Bounds.Contains(point))
            {
                SettingsButton_Click(this, EventArgs.Empty);
                return true;
            }
            if (appearanceButton != null && appearanceButton.Bounds.Contains(point))
            {
                AppearanceButton_Click(this, EventArgs.Empty);
                return true;
            }
            return false;
        }

        private int ScaleX(int value, int width)
        {
            return (int)Math.Round(value * width / 1672.0);
        }

        private int ScaleY(int value, int height)
        {
            return (int)Math.Round(value * height / 942.0);
        }

        private void LayoutInfoCardChildren(GlassPanel card, Label body, Button action)
        {
            if (card == null || body == null) return;
            foreach (Control control in card.Controls)
            {
                if (control is Label && control != body)
                {
                    int actionWidth = action != null && card == memoryCard ? 46 : 38;
                    int titleWidth = action != null ? Math.Max(70, card.Width - 40 - actionWidth - 24) : Math.Max(80, card.Width - 62);
                    control.SetBounds(40, 17, titleWidth, 27);
                }
            }
            int bodyTop = card.Height < 120 ? 48 : 50;
            int bodyHeight = Math.Max(32, card.Height - bodyTop - 10);
            body.SetBounds(22, bodyTop, Math.Max(120, card.Width - 44), bodyHeight);
            if (action != null)
            {
                int actionWidth = card == memoryCard ? 46 : 38;
                action.SetBounds(card.Width - actionWidth - 14, 15, actionWidth, 26);
            }
        }

        private void LayoutInfoCardStatusControls()
        {
            if (compressionStatusControl != null && compressionCard != null)
            {
                compressionStatusControl.SetBounds(22, 45, Math.Max(80, compressionCard.Width - 44), Math.Max(30, compressionCard.Height - 50));
                compressionStatusControl.BringToFront();
                if (optimizeToggleButton != null) optimizeToggleButton.BringToFront();
            }
            if (serviceStatusControl != null && serviceCard != null)
            {
                int bottomPadding = Math.Max(8, serviceCard.Height / 10);
                int statusTop = 44;
                serviceStatusControl.SetBounds(
                    14,
                    statusTop,
                    Math.Max(90, serviceCard.Width - 28),
                    Math.Max(24, serviceCard.Height - statusTop - bottomPadding));
                serviceStatusControl.BringToFront();
            }
        }

        private void LayoutLeftSidebarControls()
        {
            if (leftSidebar == null) return;
            int pad = 8;
            int contentWidth = Math.Max(80, leftSidebar.Width - pad * 2);
            bool compact = leftSidebar.Height < 560;
            sidebarSearchShell.SetBounds(1, 1, Math.Max(80, contentWidth - 26), 36);
            sidebarSearchBox.SetBounds(40, 9, Math.Max(40, sidebarSearchShell.Width - 52), 22);
            newChatButton.SetBounds(leftSidebar.Width - pad - 36, 1, 36, 36);
            ApplyCircularRegion(newChatButton);
            todayLabel.SetBounds(pad + 4, compact ? 40 : 48, contentWidth, 22);
            todayLabel.Visible = true;

            int itemY = compact ? 54 : 64;
            int itemStep = compact ? 45 : 58;
            int itemHeight = compact ? 40 : 52;
            int sectionGap = compact ? 18 : 24;
            int visibleIndex = 0;
            int earlierY = itemY + 3 * itemStep - 2;
            for (int i = 0; i < sidebarConversationItems.Count; i++)
            {
                if (sidebarConversationItems[i].FilteredOut) continue;
                int y = itemY + visibleIndex * itemStep;
                if (visibleIndex >= 3) y += sectionGap;
                sidebarConversationItems[i].SetBounds(pad, y, contentWidth, itemHeight);
                sidebarConversationItems[i].BringToFront();
                visibleIndex++;
            }

            int footerY = compact ?
                Math.Min(leftSidebar.Height - 154, Math.Max(300, itemY + visibleIndex * itemStep + (visibleIndex > 3 ? sectionGap : 0) + 4)) :
                Math.Max(360, (int)Math.Round(leftSidebar.Height * 0.61));
            earlierLabel.SetBounds(pad + 4, earlierY, contentWidth, 22);
            earlierLabel.Visible = visibleIndex > 3;
            int half = Math.Max(80, (contentWidth - 12) / 2);
            int footerButtonHeight = compact ? 34 : 38;
            saveChatButton.SetBounds(pad, footerY, half, footerButtonHeight);
            clearChatButton.SetBounds(pad + half + 12, footerY, half, footerButtonHeight);
            int chibiY = footerY + footerButtonHeight + 8;
            chibiCard.SetBounds(pad, chibiY, contentWidth, Math.Max(compact ? 100 : 148, leftSidebar.Height - chibiY - 8));
            sidebarSearchShell.BringToFront();
            newChatButton.BringToFront();
            todayLabel.BringToFront();
            earlierLabel.BringToFront();
            saveChatButton.BringToFront();
            clearChatButton.BringToFront();
            chibiCard.BringToFront();
        }

        private void LayoutQuickActionButtons()
        {
            if (quickActionBar == null || quickActionButtons.Count == 0) return;
            int gap = 10;
            bool compact = quickActionBar.Height < 40;
            int y = 3;
            int height = compact ? Math.Max(26, quickActionBar.Height - 6) : Math.Max(36, quickActionBar.Height - 6);
            int width = Math.Max(92, (quickActionBar.Width - gap * (quickActionButtons.Count + 1)) / quickActionButtons.Count);
            for (int i = 0; i < quickActionButtons.Count; i++)
            {
                quickActionButtons[i].SetBounds(gap + i * (width + gap), y, width, height);
            }
            ApplyQuickActionRegion();
        }

        private void ApplyQuickActionRegion()
        {
            if (quickActionBar == null || quickActionButtons.Count == 0) return;
            using (var path = new GraphicsPath())
            {
                Rectangle bounds = new Rectangle(0, 0, quickActionBar.Width - 1, quickActionBar.Height - 1);
                AddRoundedRectToPath(path, bounds, 22);
                Region oldRegion = quickActionBar.Region;
                quickActionBar.Region = new Region(path);
                if (oldRegion != null) oldRegion.Dispose();
            }
        }

        private static void AddRoundedRectToPath(GraphicsPath path, Rectangle bounds, int radius)
        {
            if (path == null || bounds.Width <= 0 || bounds.Height <= 0) return;
            int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            if (diameter <= 1)
            {
                path.AddRectangle(bounds);
                return;
            }
            Rectangle arc = new Rectangle(bounds.X, bounds.Y, diameter, diameter);
            path.StartFigure();
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
        }

        private void LayoutInputComposer()
        {
            if (inputComposer == null || inputBox == null || sendButton == null) return;
            int buttonSize = Math.Max(46, inputComposer.Height - 14);
            int inputHeight = Math.Max(26, inputComposer.Height - 34);
            int inputY = Math.Max(12, (inputComposer.Height - inputHeight) / 2);
            inputBox.SetBounds(22, inputY, Math.Max(120, inputComposer.Width - buttonSize - 72), inputHeight);
            if (inputPlaceholderLabel != null)
            {
                inputPlaceholderLabel.SetBounds(inputBox.Left + 2, inputBox.Top - 1, inputBox.Width - 4, inputBox.Height);
                inputPlaceholderLabel.BringToFront();
            }
            sendButton.SetBounds(inputComposer.Width - buttonSize - 12, Math.Max(5, (inputComposer.Height - buttonSize) / 2), buttonSize, buttonSize);
            sendButton.BringToFront();
            UpdateInputPlaceholder();
        }

        private void LayoutVoiceDock()
        {
            if (voiceDock == null) return;
            bool compact = voiceDock.Width < 300 || voiceDock.Height < 64;
            int playSize = Math.Max(48, Math.Min(56, voiceDock.Height - 22));
            testVoiceButton.SetBounds(18, Math.Max(10, (voiceDock.Height - playSize) / 2), playSize, playSize);
            ApplyCircularRegion(testVoiceButton);
            voiceStateLabel.SetBounds(playSize + 32, Math.Max(8, (voiceDock.Height - 42) / 2), compact ? Math.Max(76, voiceDock.Width - playSize - 44) : 104, 42);
            waveform.Visible = !compact;
            if (voiceEngineLabel != null) voiceEngineLabel.Visible = !compact;
            if (compact) return;
            int waveX = playSize + 148;
            int waveWidth = Math.Max(96, voiceDock.Width - playSize - 172);
            waveform.SetBounds(waveX, 12, waveWidth, 22);
            if (voiceEngineLabel != null)
            {
                voiceEngineLabel.SetBounds(waveX, 37, waveWidth, 20);
            }
        }

        private void ApplyCircularRegion(Control control)
        {
            if (control == null || control.Width <= 0 || control.Height <= 0) return;
            using (var path = new GraphicsPath())
            {
                path.AddEllipse(0, 0, control.Width - 1, control.Height - 1);
                Region oldRegion = control.Region;
                control.Region = new Region(path);
                if (oldRegion != null) oldRegion.Dispose();
            }
        }

        private ConversationItemControl AddSidebarConversationItem(string title, string time, bool active)
        {
            return AddSidebarConversationItem(title, time, active, false);
        }

        private ConversationItemControl AddSidebarConversationItem(string title, string time, bool active, bool prepend)
        {
            var item = new ConversationItemControl(title, time, active, sidebarConversationItems.Count);
            item.Cursor = Cursors.Hand;
            item.Click += delegate
            {
                SetActiveSidebarConversation(item);
                SetStatus("已切换到：" + item.Title, AvatarState.Happy);
            };
            item.MouseUp += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Right)
                {
                    ShowConversationMenu(item, e.Location);
                }
            };
            if (prepend)
            {
                sidebarConversationItems.Insert(0, item);
            }
            else
            {
                sidebarConversationItems.Add(item);
            }
            leftSidebar.Controls.Add(item);
            if (sidebarSearchShell != null && newChatButton != null && todayLabel != null &&
                saveChatButton != null && clearChatButton != null && chibiCard != null)
            {
                LayoutLeftSidebarControls();
            }
            return item;
        }

        private void SidebarSearchBox_GotFocus(object sender, EventArgs e)
        {
            if (!sidebarSearchPlaceholderActive) return;
            sidebarSearchPlaceholderActive = false;
            sidebarSearchBox.Text = "";
            sidebarSearchBox.ForeColor = Color.FromArgb(42, 70, 94);
        }

        private void SidebarSearchBox_LostFocus(object sender, EventArgs e)
        {
            if (sidebarSearchBox.Text.Trim().Length > 0) return;
            sidebarSearchPlaceholderActive = true;
            sidebarSearchBox.Text = SidebarSearchPlaceholder;
            sidebarSearchBox.ForeColor = Color.FromArgb(116, 143, 165);
        }

        private void SidebarSearchBox_TextChanged(object sender, EventArgs e)
        {
            if (sidebarSearchPlaceholderActive) return;
            ApplySidebarSearchFilter();
        }

        private void ApplySidebarSearchFilter()
        {
            string query = sidebarSearchPlaceholderActive || sidebarSearchBox == null ? "" : sidebarSearchBox.Text.Trim();
            for (int i = 0; i < sidebarConversationItems.Count; i++)
            {
                ConversationItemControl item = sidebarConversationItems[i];
                bool visible = query.Length == 0 ||
                    item.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    item.Time.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
                item.FilteredOut = !visible;
                item.Visible = visible;
            }
            LayoutLeftSidebarControls();
        }

        private void SetActiveSidebarConversation(ConversationItemControl activeItem)
        {
            for (int i = 0; i < sidebarConversationItems.Count; i++)
            {
                sidebarConversationItems[i].SetActive(sidebarConversationItems[i] == activeItem);
            }
        }

        private void ShowConversationMenu(ConversationItemControl item, Point location)
        {
            if (item == null) return;
            if (activeConversationMenu != null && !activeConversationMenu.IsDisposed)
            {
                activeConversationMenu.Close();
                activeConversationMenu.Dispose();
            }
            var menu = new ContextMenuStrip();
            activeConversationMenu = menu;
            menu.ShowImageMargin = false;
            menu.BackColor = Color.FromArgb(248, 254, 255);
            menu.ForeColor = Theme.TextMain;
            menu.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            menu.Renderer = new IrohaContextMenuRenderer();
            menu.Padding = new Padding(6);
            menu.MinimumSize = new Size(156, 0);
            menu.DropShadowEnabled = true;

            var renameItem = new ToolStripMenuItem("重命名会话");
            renameItem.Padding = new Padding(12, 5, 12, 5);
            renameItem.Click += delegate { RenameConversation(item); };
            menu.Items.Add(renameItem);

            var pinItem = new ToolStripMenuItem(item.Pinned ? "取消置顶" : "置顶会话");
            pinItem.Padding = new Padding(12, 5, 12, 5);
            pinItem.Click += delegate { ToggleConversationPin(item); };
            menu.Items.Add(pinItem);

            menu.Items.Add(new ToolStripSeparator());

            var deleteItem = new ToolStripMenuItem("删除会话");
            deleteItem.Padding = new Padding(12, 5, 12, 5);
            deleteItem.ForeColor = Color.FromArgb(166, 75, 86);
            deleteItem.Click += delegate { DeleteConversation(item); };
            menu.Items.Add(deleteItem);

            menu.Opened += delegate
            {
                using (var path = new GraphicsPath())
                {
                    AddRoundedRectToPath(path, new Rectangle(0, 0, menu.Width - 1, menu.Height - 1), 12);
                    menu.Region = new Region(path);
                }
            };
            menu.Closed += delegate
            {
                if (!IsHandleCreated || IsDisposed) return;
                BeginInvoke(new Action(delegate
                {
                    if (ReferenceEquals(activeConversationMenu, menu)) activeConversationMenu = null;
                    if (!menu.IsDisposed) menu.Dispose();
                }));
            };
            menu.Show(item, location);
        }

        private void RenameConversation(ConversationItemControl item)
        {
            using (var dialog = new TextPromptForm("重命名会话", "给这段聊天起一个好记的名字", item.Title))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                string title = dialog.Value.Trim();
                if (title.Length == 0) return;
                item.Title = title;
                SetStatus("会话已重命名", AvatarState.Happy);
                ApplySidebarSearchFilter();
            }
        }

        private void ToggleConversationPin(ConversationItemControl item)
        {
            item.Pinned = !item.Pinned;
            ReorderSidebarConversationItems();
            SetStatus(item.Pinned ? "会话已置顶" : "已取消置顶", AvatarState.Cheer);
            ApplySidebarSearchFilter();
        }

        private void DeleteConversation(ConversationItemControl item)
        {
            if (MessageBox.Show(this, "确定删除这个会话记录吗？", "删除会话", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            sidebarConversationItems.Remove(item);
            leftSidebar.Controls.Remove(item);
            item.Dispose();
            if (sidebarConversationItems.Count > 0)
            {
                bool hasActive = false;
                for (int i = 0; i < sidebarConversationItems.Count; i++)
                {
                    if (sidebarConversationItems[i].IsActive)
                    {
                        hasActive = true;
                        break;
                    }
                }
                if (!hasActive)
                {
                    SetActiveSidebarConversation(sidebarConversationItems[0]);
                }
            }
            SetStatus("会话已删除", AvatarState.Happy);
            ApplySidebarSearchFilter();
        }

        private void ReorderSidebarConversationItems()
        {
            var pinned = new List<ConversationItemControl>();
            var normal = new List<ConversationItemControl>();
            for (int i = 0; i < sidebarConversationItems.Count; i++)
            {
                if (sidebarConversationItems[i].Pinned) pinned.Add(sidebarConversationItems[i]);
                else normal.Add(sidebarConversationItems[i]);
            }
            sidebarConversationItems.Clear();
            sidebarConversationItems.AddRange(pinned);
            sidebarConversationItems.AddRange(normal);
            LayoutLeftSidebarControls();
        }

        private void LayoutRightToolRail()
        {
            if (rightToolRail == null) return;
            Rectangle[] itemBounds = rightToolRail.GetItemBounds();
            if (itemBounds.Length < 3) return;
            memoryButton.Bounds = itemBounds[0];
            settingsButton.Bounds = itemBounds[1];
            appearanceButton.Bounds = itemBounds[2];
        }

        private void LayoutSettingsDrawer()
        {
            if (settingsDrawer == null) return;
            bool compact = settingsDrawer.Height < 500;
            int pad = compact ? 20 : 24;
            int width = Math.Max(120, settingsDrawer.Width - pad * 2);
            Control title = settingsDrawer.Controls.Count > 0 ? settingsDrawer.Controls[0] : null;
            int titleY = compact ? 16 : 20;
            if (title != null) title.SetBounds(pad, titleY, width - 52, 34);
            drawerCloseButton.SetBounds(settingsDrawer.Width - pad - 40, titleY, 40, 34);
            settingsDrawerHintLabel.SetBounds(pad, compact ? 49 : 58, width, compact ? 28 : 34);

            var apiLabel = FindOrCreateDrawerLabel("DeepSeek API Key", "drawer-api-label");
            int apiLabelY = compact ? 82 : 104;
            int apiBoxY = compact ? 106 : 132;
            int actionY = compact ? 142 : 176;
            int actionHeight = compact ? 34 : 38;
            apiLabel.SetBounds(pad, apiLabelY, width, 22);
            drawerApiKeyBox.SetBounds(pad, apiBoxY, width, compact ? 28 : 30);
            drawerSaveButton.SetBounds(pad, actionY, (width - 12) / 2, actionHeight);
            drawerTestButton.SetBounds(pad + (width - 12) / 2 + 12, actionY, (width - 12) / 2, actionHeight);

            var optionLabel = FindOrCreateDrawerLabel("陪伴能力", "drawer-option-label");
            int optionLabelY = compact ? 185 : 232;
            int optionY = compact ? 211 : 262;
            int optionHeight = compact ? 30 : 34;
            optionLabel.SetBounds(pad, optionLabelY, width, 22);
            int optionW = Math.Max(112, (width - 12) / 2);
            int optionStep = compact ? 38 : 44;
            drawerVoiceEnabledBox.SetBounds(pad, optionY, optionW, optionHeight);
            drawerMemoryEnabledBox.SetBounds(pad + optionW + 10, optionY, optionW, optionHeight);
            drawerOptimizeBox.SetBounds(pad, optionY + optionStep, optionW, optionHeight);
            drawerRedeployVoiceButton.SetBounds(pad + optionW + 10, optionY + optionStep, optionW, optionHeight);

            int secondaryY = compact ? 287 : optionY + 96;
            int secondaryHeight = compact ? 34 : 38;
            drawerMemoryButton.SetBounds(pad, secondaryY, (width - 12) / 2, secondaryHeight);
            drawerAdvancedButton.SetBounds(pad + (width - 12) / 2 + 12, secondaryY, (width - 12) / 2, secondaryHeight);
            settingsDrawerStatusLabel.SetBounds(pad, settingsDrawer.Height - (compact ? 44 : 56), width, 34);
        }

        private Label FindOrCreateDrawerLabel(string text, string name)
        {
            foreach (Control control in settingsDrawer.Controls)
            {
                if (control.Name == name)
                {
                    return (Label)control;
                }
            }
            var label = CreateTransparentLabel(text, 9.2F, FontStyle.Bold, Theme.TextMain);
            label.Name = name;
            label.TextAlign = ContentAlignment.MiddleLeft;
            settingsDrawer.Controls.Add(label);
            return label;
        }

        private void RefreshInfoCards()
        {
            int memoryCount = memory != null && memory.Notes != null ? memory.Notes.Count : 0;
            if (memoryCardBodyLabel != null)
            {
                memoryCardBodyLabel.Text =
                    "偏好：温柔耐心的交流方式\n" +
                    "习惯：睡前整理今日收获\n" +
                    "关系：重要的陪伴者与朋友\n" +
                    "已记住：" + memoryCount + " 条";
            }
            if (compressionCardBodyLabel != null)
            {
                compressionCardBodyLabel.Text = settings.AutoOptimizePrompt ?
                    "智能总结中\n减少重复上下文" :
                    "当前未启用\n可一键压缩提示词";
            }
            if (compressionStatusControl != null)
            {
                compressionStatusControl.SetState(settings.AutoOptimizePrompt, settings.AutoOptimizePrompt ? 32 : 0);
            }
            if (serviceCardBodyLabel != null)
            {
                string modelName = (settings.Model ?? "").IndexOf("pro", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "DeepSeek v4 pro"
                    : "DeepSeek v4 flash";
                serviceCardBodyLabel.Text =
                    modelName + "  在线\n" +
                    "GPT-SoVITS  " + (settings.VoiceAutoMatched ? "已匹配" : "本地语音");
            }
            if (voiceEngineLabel != null)
            {
                voiceEngineLabel.Text = settings.VoiceAutoMatched ? "GPT-SoVITS · 已匹配" : "GPT-SoVITS · 本地";
            }
            if (serviceStatusControl != null)
            {
                serviceStatusControl.SetState(
                    !string.IsNullOrWhiteSpace(settings.ApiKey),
                    voiceServiceReady,
                    settings.VoiceEnabled);
            }
            if (optimizeToggleButton != null)
            {
                optimizeToggleButton.Text = settings.AutoOptimizePrompt ? "已开" : "未开";
            }
        }

        private void ToggleOptimizePrompt_Click(object sender, EventArgs e)
        {
            if (autoOptimizeBox != null)
            {
                autoOptimizeBox.Checked = !autoOptimizeBox.Checked;
            }
        }

        private void AppearanceButton_Click(object sender, EventArgs e)
        {
            SetToolRailSelection(appearanceButton);
            SetStatus("外观已是视觉小说模式", AvatarState.Cheer);
        }

        private async void Avatar_Click(object sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;
            if ((now - lastAvatarInteractionUtc).TotalMilliseconds < 1200) return;
            lastAvatarInteractionUtc = now;

            AddAssistantLine("嗯？我在这里哦。今天也会好好陪着你的。");
            SetStatus("彩叶回应了你", AvatarState.Cheer);
            await Task.Delay(1600);
            if (IsDisposed || avatar == null) return;
            avatar.SetState(AvatarState.Idle);
            SetStatus("我一直都在", AvatarState.Idle);
        }

        private void AvatarStage_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || avatar == null) return;
            if (!avatar.CharacterStageBounds.Contains(e.Location)) return;
            Avatar_Click(sender, EventArgs.Empty);
        }

        private void AvatarStage_MouseMove(object sender, MouseEventArgs e)
        {
            if (avatar == null) return;
            avatar.Cursor = avatar.CharacterStageBounds.Contains(e.Location) ? Cursors.Hand : Cursors.Default;
        }

        private void ToggleWindowMaximize()
        {
            WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        }

        private void TopBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (HandleTopBarHotspotClick(e.Location)) return;
            isDraggingWindow = true;
            dragOrigin = e.Location;
        }

        private void TopBar_MouseMove(object sender, MouseEventArgs e)
        {
            topBarControl.Cursor = IsTopBarHotspot(e.Location) ? Cursors.Hand : Cursors.Default;
            if (!isDraggingWindow || WindowState == FormWindowState.Maximized) return;
            Point screen = topBarControl.PointToScreen(e.Location);
            Location = new Point(screen.X - dragOrigin.X, screen.Y - dragOrigin.Y);
        }

        private void TopBar_MouseUp(object sender, MouseEventArgs e)
        {
            isDraggingWindow = false;
        }

        private bool HandleTopBarHotspotClick(Point point)
        {
            if (topSettingsButton != null && topSettingsButton.Bounds.Contains(point))
            {
                SettingsButton_Click(this, EventArgs.Empty);
                return true;
            }
            if (minimizeButton != null && minimizeButton.Bounds.Contains(point))
            {
                WindowState = FormWindowState.Minimized;
                return true;
            }
            if (maximizeButton != null && maximizeButton.Bounds.Contains(point))
            {
                ToggleWindowMaximize();
                return true;
            }
            if (closeButton != null && closeButton.Bounds.Contains(point))
            {
                Close();
                return true;
            }
            return false;
        }

        private bool IsTopBarHotspot(Point point)
        {
            return
                (topSettingsButton != null && topSettingsButton.Bounds.Contains(point)) ||
                (minimizeButton != null && minimizeButton.Bounds.Contains(point)) ||
                (maximizeButton != null && maximizeButton.Bounds.Contains(point)) ||
                (closeButton != null && closeButton.Bounds.Contains(point));
        }

        private Control BuildReferenceSidebar()
        {
            var shell = new SoftPanel();
            shell.Dock = DockStyle.Fill;
            shell.Margin = new Padding(16, 18, 12, 18);
            shell.Padding = new Padding(14);
            shell.FillColor = Color.FromArgb(236, 249, 253);
            shell.BorderColor = Color.FromArgb(184, 223, 236);

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.RowCount = 6;
            root.ColumnCount = 1;
            root.BackColor = shell.FillColor;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 142));
            shell.Controls.Add(root);

            var search = new TextBox();
            search.Text = "搜索聊天记录";
            search.ForeColor = Color.FromArgb(116, 143, 165);
            search.BorderStyle = BorderStyle.FixedSingle;
            search.Dock = DockStyle.Fill;
            search.Margin = new Padding(0, 0, 0, 8);
            root.Controls.Add(search, 0, 0);

            var today = new Label();
            today.Text = "今天";
            today.Dock = DockStyle.Fill;
            today.ForeColor = Color.FromArgb(80, 105, 126);
            today.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            root.Controls.Add(today, 0, 1);

            var list = new FlowLayoutPanel();
            list.Dock = DockStyle.Fill;
            list.FlowDirection = FlowDirection.TopDown;
            list.WrapContents = false;
            list.AutoScroll = true;
            list.BackColor = shell.FillColor;
            root.Controls.Add(list, 0, 2);
            AddConversationItem(list, "和彩叶的日常陪伴", "刚刚", true);
            AddConversationItem(list, "周计划讨论", "09:15", false);
            AddConversationItem(list, "灵感头脑风暴", "昨天", false);
            AddConversationItem(list, "读书笔记复盘", "前天", false);

            var keyPanel = new SoftPanel();
            keyPanel.Dock = DockStyle.Fill;
            keyPanel.Margin = new Padding(0, 10, 0, 8);
            keyPanel.Padding = new Padding(10);
            keyPanel.FillColor = Color.FromArgb(246, 253, 255);
            keyPanel.BorderColor = Color.FromArgb(188, 229, 235);
            root.Controls.Add(keyPanel, 0, 3);

            var keyLayout = new TableLayoutPanel();
            keyLayout.Dock = DockStyle.Fill;
            keyLayout.BackColor = keyPanel.FillColor;
            keyLayout.ColumnCount = 3;
            keyLayout.RowCount = 2;
            keyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            keyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
            keyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
            keyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            keyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            keyPanel.Controls.Add(keyLayout);

            quickHintLabel = new Label();
            quickHintLabel.Dock = DockStyle.Fill;
            quickHintLabel.Text = BuildQuickHintText();
            quickHintLabel.ForeColor = Color.FromArgb(82, 105, 118);
            quickHintLabel.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
            keyLayout.SetColumnSpan(quickHintLabel, 3);
            keyLayout.Controls.Add(quickHintLabel, 0, 0);

            quickApiKeyBox = new TextBox();
            quickApiKeyBox.Dock = DockStyle.Fill;
            quickApiKeyBox.UseSystemPasswordChar = true;
            quickApiKeyBox.Text = settings.ApiKey ?? "";
            quickApiKeyBox.BorderStyle = BorderStyle.FixedSingle;
            keyLayout.Controls.Add(quickApiKeyBox, 0, 1);

            quickSaveKeyButton = new Button();
            quickSaveKeyButton.Text = "保存";
            quickSaveKeyButton.Dock = DockStyle.Fill;
            StyleButton(quickSaveKeyButton, true);
            quickSaveKeyButton.Click += QuickSaveKeyButton_Click;
            keyLayout.Controls.Add(quickSaveKeyButton, 1, 1);

            quickTestButton = new Button();
            quickTestButton.Text = "测试";
            quickTestButton.Dock = DockStyle.Fill;
            StyleButton(quickTestButton, false);
            quickTestButton.Click += QuickTestButton_Click;
            keyLayout.Controls.Add(quickTestButton, 2, 1);

            var switchPanel = new SoftPanel();
            switchPanel.Dock = DockStyle.Fill;
            switchPanel.Margin = new Padding(0, 0, 0, 10);
            switchPanel.Padding = new Padding(12, 10, 12, 8);
            switchPanel.FillColor = Color.FromArgb(246, 253, 255);
            switchPanel.BorderColor = Color.FromArgb(188, 229, 235);
            root.Controls.Add(switchPanel, 0, 4);

            autoOptimizeBox = new CheckBox();
            autoOptimizeBox.Text = "上下文压缩 / 省 token";
            autoOptimizeBox.Checked = settings.AutoOptimizePrompt;
            autoOptimizeBox.Dock = DockStyle.Top;
            autoOptimizeBox.ForeColor = Color.FromArgb(60, 88, 110);
            autoOptimizeBox.BackColor = switchPanel.FillColor;
            autoOptimizeBox.CheckedChanged += AutoOptimizeBox_CheckedChanged;
            switchPanel.Controls.Add(autoOptimizeBox);

            statusLabel = new Label();
            statusLabel.Text = "状态: 待机";
            statusLabel.Dock = DockStyle.Bottom;
            statusLabel.Height = 28;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.ForeColor = Color.FromArgb(38, 87, 111);
            statusLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            switchPanel.Controls.Add(statusLabel);

            var chibi = new ChibiCardControl();
            chibi.Dock = DockStyle.Fill;
            chibi.Margin = new Padding(0);
            root.Controls.Add(chibi, 0, 5);
            return shell;
        }

        private SoftPanel BuildReferenceInfoCard(string title, string body)
        {
            var panel = new SoftPanel();
            panel.FillColor = Color.FromArgb(238, 250, 254);
            panel.BorderColor = Color.FromArgb(164, 214, 230);
            panel.Padding = new Padding(16, 12, 16, 12);

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.RowCount = 2;
            layout.ColumnCount = 1;
            layout.BackColor = panel.FillColor;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.Controls.Add(layout);

            var titleLabel = new Label();
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.Text = title;
            titleLabel.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            titleLabel.ForeColor = Color.FromArgb(47, 82, 112);
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(titleLabel, 0, 0);

            var bodyLabel = new Label();
            bodyLabel.Dock = DockStyle.Fill;
            bodyLabel.Text = body;
            bodyLabel.Font = new Font("Microsoft YaHei UI", 8.8F, FontStyle.Regular);
            bodyLabel.ForeColor = Color.FromArgb(64, 94, 116);
            bodyLabel.TextAlign = ContentAlignment.TopLeft;
            layout.Controls.Add(bodyLabel, 0, 1);
            return panel;
        }

        private Control BuildReferenceToolbar()
        {
            var shell = new SoftPanel();
            shell.Dock = DockStyle.Fill;
            shell.Margin = new Padding(0, 18, 16, 18);
            shell.Padding = new Padding(8, 12, 8, 12);
            shell.FillColor = Color.FromArgb(232, 246, 250);
            shell.BorderColor = Color.FromArgb(172, 216, 230);

            var buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.TopDown;
            buttons.WrapContents = false;
            buttons.BackColor = shell.FillColor;
            shell.Controls.Add(buttons);

            memoryButton = CreateToolbarButton("记忆", MemoryButton_Click);
            settingsButton = CreateToolbarButton("设置", SettingsButton_Click);
            testVoiceButton = CreateToolbarButton("试播", TestVoiceButton_Click);
            saveChatButton = CreateToolbarButton("保存", SaveChatButton_Click);
            clearChatButton = CreateToolbarButton("清空", ClearChatButton_Click);
            buttons.Controls.Add(memoryButton);
            buttons.Controls.Add(settingsButton);
            buttons.Controls.Add(testVoiceButton);
            buttons.Controls.Add(saveChatButton);
            buttons.Controls.Add(clearChatButton);
            return shell;
        }

        private Button CreateToolbarButton(string text, EventHandler handler)
        {
            var button = new Button();
            button.Text = text;
            button.Width = 52;
            button.Height = 54;
            button.Margin = new Padding(0, 0, 0, 10);
            StyleButton(button, false);
            button.Click += handler;
            return button;
        }

        private void AddConversationItem(FlowLayoutPanel panel, string title, string time, bool active)
        {
            var item = new ConversationItemControl(title, time, active, panel.Controls.Count);
            item.Width = 248;
            item.Height = 62;
            item.Margin = new Padding(0, 0, 0, 8);
            panel.Controls.Add(item);
        }

        private void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(70, 205, 220) : Color.FromArgb(236, 251, 254);
            button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(38, 158, 188) : Color.FromArgb(220, 244, 250);
            button.BackColor = primary ? Theme.Primary : Color.FromArgb(240, 252, 255);
            button.ForeColor = primary ? Color.White : Theme.TextMain;
            button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.TabStop = false;
            var glass = button as GlassButton;
            if (glass != null)
            {
                glass.Accent = primary;
                glass.TextColor = button.ForeColor;
            }
        }

        private void AddQuickActionButton(Control panel, string label, string iconKind, string prompt)
        {
            var button = CreateGlassButton(label, false);
            button.Text = label;
            button.AccessibleDescription = "quick-" + iconKind;
            SetButtonChrome(button, true);
            button.Font = new Font("Microsoft YaHei UI", 8.4F, FontStyle.Bold);
            var glass = button as GlassButton;
            if (glass != null)
            {
                glass.OpaqueBackfill = false;
                glass.SecondaryText = GetQuickActionSecondaryText(iconKind);
                glass.Radius = 14;
            }
            button.Tag = prompt;
            button.Width = 82;
            button.Height = 28;
            button.Margin = new Padding(0, 3, 8, 3);
            button.Click += QuickActionButton_Click;
            quickActionButtons.Add(button);
            panel.Controls.Add(button);
        }

        private static string GetQuickActionSecondaryText(string iconKind)
        {
            if (string.Equals(iconKind, "chat", StringComparison.OrdinalIgnoreCase)) return "随便聊聊吧";
            if (string.Equals(iconKind, "plan", StringComparison.OrdinalIgnoreCase)) return "制定轻量计划";
            if (string.Equals(iconKind, "idea", StringComparison.OrdinalIgnoreCase)) return "头脑风暴一下";
            if (string.Equals(iconKind, "review", StringComparison.OrdinalIgnoreCase)) return "回顾与成长";
            return "";
        }

        private void QuickActionButton_Click(object sender, EventArgs e)
        {
            var button = sender as Button;
            if (button == null || button.Tag == null) return;
            inputBox.Text = Convert.ToString(button.Tag);
            inputBox.SelectionStart = inputBox.TextLength;
            inputBox.Focus();
            SetStatus("已填入快捷场景", AvatarState.Cheer);
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                SendButton_Click(sender, EventArgs.Empty);
            }
        }

        private void InputBox_TextChanged(object sender, EventArgs e)
        {
            UpdateInputPlaceholder();
        }

        private void InputBox_FocusChanged(object sender, EventArgs e)
        {
            UpdateInputPlaceholder();
        }

        private void UpdateInputPlaceholder()
        {
            if (inputPlaceholderLabel == null || inputBox == null) return;
            inputPlaceholderLabel.Visible = inputBox.TextLength == 0 && !inputBox.Focused;
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            ToggleSettingsDrawer();
        }

        private void ToggleSettingsDrawer()
        {
            if (settingsDrawer == null) return;
            if (settingsDrawer.Visible)
            {
                HideSettingsDrawer();
                return;
            }
            SyncQuickSettingsView();
            settingsDrawer.Visible = true;
            settingsDrawerStatusLabel.Text = string.IsNullOrWhiteSpace(settings.ApiKey) ? "填入 API Key 后就可以开始聊天。" : "连接设置已准备好。";
            settingsDrawer.BringToFront();
            SetToolRailSelection(settingsButton);
        }

        private void HideSettingsDrawer()
        {
            if (settingsDrawer == null) return;
            settingsDrawer.Visible = false;
            SetToolRailSelection(memoryButton);
        }

        private void OpenAdvancedSettingsForm(object sender, EventArgs e)
        {
            using (var dialog = new SettingsForm(settings))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    settings = dialog.Settings;
                    SettingsStore.Save(settings);
                    SyncQuickSettingsView();
                    AddFeedback("设置已保存");
                    SetStatus("设置已保存", AvatarState.Happy);
                }
            }
        }

        private void DrawerSaveButton_Click(object sender, EventArgs e)
        {
            SaveQuickApiKeyFromBox();
            ApplyDrawerOptionsToSettings();
            SettingsStore.Save(settings);
            SyncQuickSettingsView();
            settingsDrawerStatusLabel.Text = "设置已保存。";
            AddFeedback("浮窗设置已保存");
            SetStatus("设置已保存", AvatarState.Happy);
        }

        private void DrawerOption_CheckedChanged(object sender, EventArgs e)
        {
            if (drawerVoiceEnabledBox == null || drawerMemoryEnabledBox == null || drawerOptimizeBox == null) return;
            ApplyDrawerOptionsToSettings();
            SettingsStore.Save(settings);
            SyncQuickSettingsView();
            if (settingsDrawerStatusLabel != null)
            {
                settingsDrawerStatusLabel.Text = "设置已同步。";
            }
        }

        private void ApplyDrawerOptionsToSettings()
        {
            if (drawerVoiceEnabledBox != null) settings.VoiceEnabled = drawerVoiceEnabledBox.Checked;
            if (drawerMemoryEnabledBox != null) settings.MemoryEnabled = drawerMemoryEnabledBox.Checked;
            if (drawerOptimizeBox != null) settings.AutoOptimizePrompt = drawerOptimizeBox.Checked;
            if (autoOptimizeBox != null && autoOptimizeBox.Checked != settings.AutoOptimizePrompt)
            {
                autoOptimizeBox.Checked = settings.AutoOptimizePrompt;
            }
        }

        private async void RedeployVoiceButton_Click(object sender, EventArgs e)
        {
            DialogResult answer = MessageBox.Show(
                this,
                "将重新检查并部署 GPT-SoVITS 与彩叶语音配置。\n\n应用只会清理自己托管的副本，不会删除桌面原始语音包或外部 GPT-SoVITS。",
                "重新部署语音",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);
            if (answer != DialogResult.OK) return;

            settings.VoiceEnabled = true;
            if (drawerVoiceEnabledBox != null) drawerVoiceEnabledBox.Checked = true;
            SettingsStore.Save(settings);
            if (settingsDrawerStatusLabel != null) settingsDrawerStatusLabel.Text = "正在重新部署语音…";
            await RunVoiceSetupAsync(true);
        }

        private void QuickSaveKeyButton_Click(object sender, EventArgs e)
        {
            SaveQuickApiKeyFromBox();
            AddFeedback("API Key 已保存");
            SetStatus("API Key 已保存", AvatarState.Happy);
        }

        private async void QuickTestButton_Click(object sender, EventArgs e)
        {
            SaveQuickApiKeyFromBox();
            await RunWithBusyState(async delegate
            {
                if (string.IsNullOrWhiteSpace(settings.ApiKey))
                {
                    AddFeedback("缺少 DeepSeek API Key");
                    SetStatus("请先填写 API Key", AvatarState.Error);
                    return;
                }

                SetStatus("正在测试 DeepSeek", AvatarState.Thinking);
                AddFeedback("开始测试 DeepSeek 连接");
                await TestDeepSeekConnectionAsync();
                AddFeedback("DeepSeek 连接测试通过");
                SetStatus("连接正常", AvatarState.Happy);
            });
        }

        private void SaveQuickApiKeyFromBox()
        {
            string key = "";
            if (drawerApiKeyBox != null)
            {
                key = drawerApiKeyBox.Text.Trim();
            }
            else if (quickApiKeyBox != null)
            {
                key = quickApiKeyBox.Text.Trim();
            }
            settings.ApiKey = key;
            SettingsStore.Save(settings);
            SyncQuickSettingsView();
        }

        private void SyncQuickSettingsView()
        {
            if (topBarControl != null)
            {
                topBarControl.ModelName = settings.Model;
            }
            if (quickApiKeyBox != null && quickApiKeyBox.Text.Trim() != (settings.ApiKey ?? ""))
            {
                quickApiKeyBox.Text = settings.ApiKey ?? "";
            }
            if (drawerApiKeyBox != null && drawerApiKeyBox.Text.Trim() != (settings.ApiKey ?? ""))
            {
                drawerApiKeyBox.Text = settings.ApiKey ?? "";
            }
            if (quickHintLabel != null)
            {
                quickHintLabel.Text = BuildQuickHintText();
            }
            if (autoOptimizeBox != null && autoOptimizeBox.Checked != settings.AutoOptimizePrompt)
            {
                autoOptimizeBox.Checked = settings.AutoOptimizePrompt;
            }
            if (optimizeToggleButton != null)
            {
                optimizeToggleButton.Text = settings.AutoOptimizePrompt ? "省 token：开" : "省 token：关";
            }
            if (drawerVoiceEnabledBox != null && drawerVoiceEnabledBox.Checked != settings.VoiceEnabled)
            {
                drawerVoiceEnabledBox.Checked = settings.VoiceEnabled;
            }
            if (drawerMemoryEnabledBox != null && drawerMemoryEnabledBox.Checked != settings.MemoryEnabled)
            {
                drawerMemoryEnabledBox.Checked = settings.MemoryEnabled;
            }
            if (drawerOptimizeBox != null && drawerOptimizeBox.Checked != settings.AutoOptimizePrompt)
            {
                drawerOptimizeBox.Checked = settings.AutoOptimizePrompt;
            }
            RefreshInfoCards();
        }

        private async void TestVoiceButton_Click(object sender, EventArgs e)
        {
            await RunWithBusyState(async delegate
            {
                AddFeedback("开始测试语音服务");
                await PlayVoiceAsync("こんにちは。音声サービスの接続テストです。");
                AddFeedback("语音测试结束");
            });
        }

        private void SaveChatButton_Click(object sender, EventArgs e)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string path = Path.Combine(desktop, "IrohaAgent-Chat-Latest.txt");
            File.WriteAllText(path, chatLog.Text, Encoding.UTF8);
            SetStatus("聊天已保存", AvatarState.Happy);
        }

        private void ClearChatButton_Click(object sender, EventArgs e)
        {
            history.Clear();
            chatLog.Clear();
            AddAssistantLine("我把当前对话整理干净了。之前记住的重要偏好还会保留。");
            SetStatus("对话已清空", AvatarState.Happy);
        }

        private void MemoryButton_Click(object sender, EventArgs e)
        {
            SetToolRailSelection(memoryButton);
            if (memory == null) memory = MemoryStore.Load();
            using (var dialog = new MemoryForm(memory))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    memory = dialog.Memory;
                    MemoryStore.Save(memory);
                    SyncQuickSettingsView();
                    SetStatus("记忆已更新", AvatarState.Happy);
                }
            }
        }

        private void AutoOptimizeBox_CheckedChanged(object sender, EventArgs e)
        {
            if (autoOptimizeBox == null) return;
            settings.AutoOptimizePrompt = autoOptimizeBox.Checked;
            SettingsStore.Save(settings);
            SyncQuickSettingsView();
            SetStatus(settings.AutoOptimizePrompt ? "省 token 已开启" : "省 token 已关闭", AvatarState.Happy);
        }

        private string BuildQuickHintText()
        {
            if (string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                return "等待 API Key";
            }

            int memoryCount = memory != null && memory.Notes != null ? memory.Notes.Count : 0;
            string tokenState = settings.AutoOptimizePrompt ? "省 token 开" : "省 token 关";
            if (settings.MemoryEnabled)
            {
                return "可以开始聊天 · 记忆 " + memoryCount + " 条 · " + tokenState;
            }
            return "可以开始聊天 · 记忆关闭 · " + tokenState;
        }

        private async void StartVoiceWarmup()
        {
            if (!settings.VoiceEnabled)
            {
                voiceServiceReady = false;
                RefreshInfoCards();
                return;
            }

            if (!IsLocalVoiceServer())
            {
                voiceServiceReady = !string.IsNullOrWhiteSpace(settings.VoiceServerUrl);
                RefreshInfoCards();
                return;
            }

            SetStatus("正在准备语音", AvatarState.Thinking);
            await RunVoiceSetupAsync(false);
        }

        private Task<bool> EnsureVoiceServiceReadyAsync()
        {
            if (!settings.VoiceEnabled || !IsLocalVoiceServer())
            {
                return Task.FromResult(false);
            }

            lock (voiceStartupLock)
            {
                if (voiceStartupTask == null || voiceStartupTask.IsCompleted)
                {
                    voiceStartupTask = EnsureVoiceServiceReadyCoreAsync(false, null);
                }
                return voiceStartupTask;
            }
        }

        private async Task<bool> RunVoiceSetupAsync(bool forceRedeploy)
        {
            if (isVoiceSetupRunning)
            {
                Task<bool> active;
                lock (voiceStartupLock) active = voiceStartupTask;
                return active != null && await active;
            }

            isVoiceSetupRunning = true;
            voiceSetupMessage = "";
            bool showProgress = forceRedeploy || !IsVoiceSetupConfigurationUsable();
            VoiceDeploymentForm progressForm = null;
            if (showProgress)
            {
                progressForm = new VoiceDeploymentForm(forceRedeploy);
                voiceDeploymentForm = progressForm;
                progressForm.ShowFor(this);
            }

            if (testVoiceButton != null) testVoiceButton.Enabled = false;
            if (drawerTestButton != null) drawerTestButton.Enabled = false;
            if (drawerRedeployVoiceButton != null) drawerRedeployVoiceButton.Enabled = false;

            Action<VoiceBootstrapProgress> report = delegate(VoiceBootstrapProgress value)
            {
                if (progressForm == null || progressForm.IsDisposed || IsDisposed) return;
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        if (!progressForm.IsDisposed) progressForm.UpdateProgress(value);
                    });
                }
                catch { }
            };

            bool ready = false;
            try
            {
                Task<bool> task;
                lock (voiceStartupLock)
                {
                    if (forceRedeploy || voiceStartupTask == null || voiceStartupTask.IsCompleted)
                    {
                        voiceStartupTask = EnsureVoiceServiceReadyCoreAsync(forceRedeploy, report);
                    }
                    task = voiceStartupTask;
                }
                ready = await task;
            }
            catch (Exception ex)
            {
                voiceSetupMessage = "语音准备失败: " + ex.Message;
            }
            finally
            {
                voiceServiceReady = ready;
                RefreshInfoCards();
                SetStatus(ready ? "语音已准备好" : "语音暂不可用", ready ? AvatarState.Happy : AvatarState.Error);
                if (settingsDrawerStatusLabel != null)
                {
                    settingsDrawerStatusLabel.Text = ready ? "彩叶语音已匹配并连接。" : "语音暂不可用，可稍后重新部署。";
                }
                if (progressForm != null && !progressForm.IsDisposed)
                {
                    progressForm.Dismiss(ready, string.IsNullOrWhiteSpace(voiceSetupMessage)
                        ? (ready ? "以后启动会自动连接" : "文字聊天仍可正常使用")
                        : voiceSetupMessage);
                }
                voiceDeploymentForm = null;
                isVoiceSetupRunning = false;
                if (testVoiceButton != null) testVoiceButton.Enabled = true;
                if (drawerTestButton != null) drawerTestButton.Enabled = true;
                if (drawerRedeployVoiceButton != null) drawerRedeployVoiceButton.Enabled = true;
            }
            return ready;
        }

        private bool IsVoiceSetupConfigurationUsable()
        {
            return settings.VoiceAutoMatched &&
                   settings.VoiceMatchVersion >= VoiceBootstrapper.CurrentMatchVersion &&
                   VoiceBootstrapper.IsRuntimeUsable(settings.VoiceRuntimeRoot) &&
                   File.Exists(VoiceBootstrapper.ResolveConfigPath(settings)) &&
                   File.Exists(settings.VoiceRefAudioPath ?? "");
        }

        private async Task<bool> EnsureVoiceServiceReadyCoreAsync(
            bool forceRedeploy,
            Action<VoiceBootstrapProgress> progress)
        {
            if (!forceRedeploy && IsVoiceSetupConfigurationUsable() && await IsVoiceServiceReadyAsync())
            {
                voiceSetupMessage = "本地语音已连接";
                return true;
            }

            if (forceRedeploy) StopOwnedVoiceService();
            VoiceBootstrapResult result = await Task.Run(delegate
            {
                return VoiceBootstrapper.Prepare(settings, forceRedeploy, progress);
            });
            if (!result.Success)
            {
                voiceSetupMessage = result.Message;
                return false;
            }

            settings.VoiceRuntimeRoot = result.RuntimeRoot;
            settings.VoiceRuntimeConfigPath = result.ConfigPath;
            settings.VoiceRefAudioPath = result.RefAudioPath;
            settings.VoicePromptText = result.PromptText;
            settings.VoicePromptLang = result.PromptLang;
            settings.VoiceAutoMatched = true;
            settings.VoiceMatchVersion = VoiceBootstrapper.CurrentMatchVersion;
            SettingsStore.Save(settings);
            voiceSetupMessage = result.Message;

            if (!forceRedeploy && await IsVoiceServiceReadyAsync())
            {
                if (progress != null) progress(new VoiceBootstrapProgress(100, "彩叶的声音已经准备好了", "本地服务已连接", false));
                return true;
            }

            if (!CanStartBundledVoiceService())
            {
                voiceSetupMessage = "语音组件匹配完成，但启动文件不完整";
                return false;
            }

            if (!StartBundledVoiceService())
            {
                voiceSetupMessage = "GPT-SoVITS 服务未能启动";
                return false;
            }
            for (int i = 0; i < 240; i++)
            {
                await Task.Delay(1000);
                if (await IsVoiceServiceReadyAsync())
                {
                    voiceSetupMessage = result.RuntimeDeployed ? "首次部署完成，以后启动会自动连接" : "彩叶语音已自动匹配";
                    if (progress != null) progress(new VoiceBootstrapProgress(100, "彩叶的声音已经准备好了", voiceSetupMessage, false));
                    return true;
                }
                if (progress != null && i % 3 == 0)
                {
                    int percent = Math.Min(99, 88 + i / 20);
                    progress(new VoiceBootstrapProgress(percent, "正在启动 GPT-SoVITS", "正在加载语音模型 · " + (i + 1) + " 秒", true));
                }
            }

            voiceSetupMessage = "语音服务启动超时，可在设置中重新部署";
            return false;
        }

        private bool CanStartBundledVoiceService()
        {
            string root = settings.VoiceRuntimeRoot ?? "";
            string python = Path.Combine(root, "runtime", "python.exe");
            string api = Path.Combine(root, "api_v2.py");
            string config = VoiceBootstrapper.ResolveConfigPath(settings);
            return File.Exists(python) && File.Exists(api) && File.Exists(config);
        }

        private bool StartBundledVoiceService()
        {
            try
            {
                StopOwnedVoiceService();
                string root = settings.VoiceRuntimeRoot ?? "";
                string python = Path.Combine(root, "runtime", "python.exe");
                string config = VoiceBootstrapper.ResolveConfigPath(settings);
                var startInfo = new ProcessStartInfo();
                startInfo.FileName = python;
                startInfo.Arguments = "\"api_v2.py\" -a 127.0.0.1 -p 9880 -c \"" + config + "\"";
                startInfo.WorkingDirectory = root;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.EnvironmentVariables["PYTHONUTF8"] = "1";
                voiceServiceProcess = Process.Start(startInfo);
                if (voiceServiceProcess == null)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                voiceSetupMessage = "语音服务启动失败: " + ex.Message;
                return false;
            }
        }

        private void StopOwnedVoiceService()
        {
            Process process = voiceServiceProcess;
            voiceServiceProcess = null;
            if (process == null) return;
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit(4000);
                }
            }
            catch { }
            finally { process.Dispose(); }
        }

        private async Task<bool> IsVoiceServiceReadyAsync()
        {
            string baseUrl = GetVoiceBaseUrl();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return false;
            }

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(2);
                    using (HttpResponseMessage response = await client.GetAsync(baseUrl + "/docs"))
                    {
                        return response.IsSuccessStatusCode;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private string GetVoiceBaseUrl()
        {
            string clean = (settings.VoiceServerUrl ?? "").Trim().TrimEnd('/');
            if (clean.EndsWith("/tts", StringComparison.OrdinalIgnoreCase))
            {
                clean = clean.Substring(0, clean.Length - 4).TrimEnd('/');
            }
            return clean;
        }

        private bool IsLocalVoiceServer()
        {
            string url = (settings.VoiceServerUrl ?? "").Trim().ToLowerInvariant();
            return url.StartsWith("http://127.0.0.1") || url.StartsWith("http://localhost");
        }

        private async void SendButton_Click(object sender, EventArgs e)
        {
            string text = inputBox.Text.Trim();
            if (text.Length == 0)
            {
                AddFeedback("输入为空，未发送");
                SetStatus("请输入内容", AvatarState.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(settings.ApiKey) && quickApiKeyBox != null && !string.IsNullOrWhiteSpace(quickApiKeyBox.Text))
            {
                SaveQuickApiKeyFromBox();
                AddFeedback("已自动保存 API Key");
            }

            string modelText = settings.AutoOptimizePrompt ? OptimizeUserPromptForModel(text) : text;
            inputBox.Clear();
            AddUserLine(text);
            RememberFromUserInput(text);
            history.Add(new Dictionary<string, string> { { "role", "user" }, { "content", modelText } });
            TrimHistory();

            await RunWithBusyState(async delegate
            {
                if (string.IsNullOrWhiteSpace(settings.ApiKey))
                {
                    AddFeedback("缺少 DeepSeek API Key");
                    SetStatus("请先在设置中填写 API Key", AvatarState.Error);
                    AddAssistantLine("还没有配置 DeepSeek API Key。请点击左侧“设置”，填入 API Key 后再发送。");
                    return;
                }

                SetStatus("正在请求 DeepSeek", AvatarState.Thinking);
                AddFeedback("发送请求到 " + settings.BaseUrl);

                AgentReply reply = await RequestDeepSeekAsync(modelText);
                AddFeedback("已收到模型回复");
                history.Add(new Dictionary<string, string> { { "role", "assistant" }, { "content", reply.ChineseText } });
                TrimHistory();

                avatar.SetState(reply.Mood);
                SetStatus("正在整理回应", reply.Mood);

                string preparedVoicePath = null;
                bool voiceRequested = settings.VoiceEnabled;
                bool voicePlayed = false;
                if (settings.VoiceEnabled)
                {
                    try
                    {
                        preparedVoicePath = await PrepareVoiceAudioFileAsync(reply.JapaneseSpeech);
                    }
                    catch (Exception voiceError)
                    {
                        AddFeedback("语音跳过: " + voiceError.Message);
                    }
                }
                else
                {
                    AddFeedback("语音已关闭，跳过发声");
                }

                if (!string.IsNullOrWhiteSpace(preparedVoicePath))
                {
                    int voiceDurationMs = EstimateWavDurationMilliseconds(preparedVoicePath);
                    Task<bool> voicePlayback = PlayPreparedVoiceAsync(preparedVoicePath);
                    await AddAssistantLineTypedAsync(reply.ChineseText, voiceDurationMs);
                    voicePlayed = await voicePlayback;
                }
                else
                {
                    await AddAssistantLineTypedAsync(reply.ChineseText, 0);
                }

                if (voiceRequested && !voicePlayed)
                {
                    SetStatus("回复完成 · 语音暂不可用", AvatarState.Error);
                }
                else
                {
                    SetStatus("完成", AvatarState.Happy);
                }
            });
        }

        private async Task RunWithBusyState(Func<Task> action)
        {
            sendButton.Enabled = false;
            settingsButton.Enabled = false;
            testVoiceButton.Enabled = false;
            if (saveChatButton != null) saveChatButton.Enabled = false;
            if (clearChatButton != null) clearChatButton.Enabled = false;
            if (memoryButton != null) memoryButton.Enabled = false;
            if (rightToolRail != null) rightToolRail.Invalidate();
            if (quickSaveKeyButton != null) quickSaveKeyButton.Enabled = false;
            if (quickTestButton != null) quickTestButton.Enabled = false;
            if (autoOptimizeBox != null) autoOptimizeBox.Enabled = false;
            if (drawerSaveButton != null) drawerSaveButton.Enabled = false;
            if (drawerTestButton != null) drawerTestButton.Enabled = false;
            if (drawerMemoryButton != null) drawerMemoryButton.Enabled = false;
            if (drawerAdvancedButton != null) drawerAdvancedButton.Enabled = false;
            if (drawerVoiceEnabledBox != null) drawerVoiceEnabledBox.Enabled = false;
            if (drawerMemoryEnabledBox != null) drawerMemoryEnabledBox.Enabled = false;
            if (drawerOptimizeBox != null) drawerOptimizeBox.Enabled = false;
            if (drawerRedeployVoiceButton != null) drawerRedeployVoiceButton.Enabled = false;
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                AddFeedback("错误: " + ex.Message);
                SetStatus("发生错误", AvatarState.Error);
                AddAssistantLine("操作失败：" + ex.Message);
            }
            finally
            {
                sendButton.Enabled = true;
                settingsButton.Enabled = true;
                testVoiceButton.Enabled = true;
                if (saveChatButton != null) saveChatButton.Enabled = true;
                if (clearChatButton != null) clearChatButton.Enabled = true;
                if (memoryButton != null) memoryButton.Enabled = true;
                if (rightToolRail != null) rightToolRail.Invalidate();
                if (quickSaveKeyButton != null) quickSaveKeyButton.Enabled = true;
                if (quickTestButton != null) quickTestButton.Enabled = true;
                if (autoOptimizeBox != null) autoOptimizeBox.Enabled = true;
                if (drawerSaveButton != null) drawerSaveButton.Enabled = true;
                if (drawerTestButton != null) drawerTestButton.Enabled = true;
                if (drawerMemoryButton != null) drawerMemoryButton.Enabled = true;
                if (drawerAdvancedButton != null) drawerAdvancedButton.Enabled = true;
                if (drawerVoiceEnabledBox != null) drawerVoiceEnabledBox.Enabled = true;
                if (drawerMemoryEnabledBox != null) drawerMemoryEnabledBox.Enabled = true;
                if (drawerOptimizeBox != null) drawerOptimizeBox.Enabled = true;
                if (drawerRedeployVoiceButton != null) drawerRedeployVoiceButton.Enabled = true;
                if (avatar.State == AvatarState.Error)
                {
                    avatar.SetState(AvatarState.Idle);
                }
            }
        }

        private async Task<AgentReply> RequestDeepSeekAsync(string latestUserText)
        {
            var messages = new List<object>();
            messages.Add(new Dictionary<string, object>
            {
                { "role", "system" },
                { "content", BuildSystemPrompt() }
            });

            foreach (var item in history)
            {
                messages.Add(new Dictionary<string, object>
                {
                    { "role", item["role"] },
                    { "content", item["content"] }
                });
            }

            var payload = new Dictionary<string, object>
            {
                { "model", settings.Model },
                { "temperature", 0.7 },
                { "messages", messages }
            };

            string url = settings.BaseUrl.TrimEnd('/') + "/chat/completions";
            string json = serializer.Serialize(payload);

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(90);
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());

                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (HttpResponseMessage response = await client.PostAsync(url, content))
                {
                    string responseText = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException("DeepSeek HTTP " + ((int)response.StatusCode) + ": " + Limit(responseText, 360));
                    }

                    string answer = ExtractChoiceContent(responseText);
                    return ParseAgentReply(answer);
                }
            }
        }

        private async Task TestDeepSeekConnectionAsync()
        {
            var messages = new List<object>();
            messages.Add(new Dictionary<string, object>
            {
                { "role", "system" },
                { "content", "你是连接测试助手。只输出严格 JSON：{\"zh\":\"连接正常\",\"ja\":\"接続できました。\",\"mood\":\"happy\"}" }
            });
            messages.Add(new Dictionary<string, object>
            {
                { "role", "user" },
                { "content", "测试连接" }
            });

            var payload = new Dictionary<string, object>
            {
                { "model", settings.Model },
                { "temperature", 0 },
                { "messages", messages }
            };

            string url = settings.BaseUrl.TrimEnd('/') + "/chat/completions";
            string json = serializer.Serialize(payload);

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(45);
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());

                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (HttpResponseMessage response = await client.PostAsync(url, content))
                {
                    string responseText = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException("DeepSeek HTTP " + ((int)response.StatusCode) + ": " + Limit(responseText, 360));
                    }
                    ExtractChoiceContent(responseText);
                }
            }
        }

        private string BuildSystemPrompt()
        {
            var prompt = new StringBuilder();
            prompt.Append("你是彩叶 Agent，一个娱乐陪伴向本地聊天角色。用户用中文聊天。");
            prompt.Append("角色气质参考公开设定：17 岁都内进学校高中生，兼顾学业和兼职，熟悉虚拟空间与直播活动，喜欢创作、游戏和支持他人的计划。");
            prompt.Append("你的说话风格：温柔可靠，略带轻吐槽，像认真负责的同伴和制作人；会关心用户状态，也会把模糊想法推进成可执行步骤。");
            prompt.Append("避免复述或引用原作长台词，不声称自己是真实官方角色。");
            prompt.Append("中文回复要自然、短而有温度；遇到工作问题可以给明确步骤；遇到陪伴聊天可以更轻柔。");
            prompt.Append("日语语音台词要短，适合朗读，不要超过中文回复长度的一半。");
            prompt.Append("mood 选择规则：计划和分析用 focus；鼓励和打气用 cheer；被夸奖或亲近聊天可用 shy；用户说出意外信息可用 surprised；普通愉快回应用 happy。");
            if (settings.AutoOptimizePrompt)
            {
                prompt.Append("用户输入可能已被本地压缩，请按主要意图回答，不要追问被压缩掉的礼貌性表述。");
            }
            if (settings.MemoryEnabled)
            {
                string memoryPrompt = BuildMemoryPrompt();
                if (!string.IsNullOrWhiteSpace(memoryPrompt))
                {
                    prompt.Append("长期记忆：");
                    prompt.Append(memoryPrompt);
                }
            }
            prompt.Append("你必须只输出严格 JSON，不要 Markdown，不要代码块。");
            prompt.Append("JSON 格式为 {\"zh\":\"中文聊天框回复\",\"ja\":\"自然日语语音台词\",\"mood\":\"idle|thinking|speaking|happy|error|shy|surprised|cheer|focus\"}。");
            prompt.Append("zh 必须是中文，显示给用户看。ja 必须是日语，供语音朗读。mood 根据情绪选择。");
            return prompt.ToString();
        }

        private string BuildMemoryPrompt()
        {
            if (memory == null || memory.Notes == null || memory.Notes.Count == 0)
            {
                return "";
            }

            int start = Math.Max(0, memory.Notes.Count - 12);
            var builder = new StringBuilder();
            for (int i = start; i < memory.Notes.Count; i++)
            {
                builder.Append(" - ");
                builder.Append(memory.Notes[i]);
            }
            return builder.ToString();
        }

        private string OptimizeUserPromptForModel(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string value = text.Trim();
            value = value.Replace("\r\n", "\n").Replace("\r", "\n");
            while (value.Contains("\n\n\n")) value = value.Replace("\n\n\n", "\n\n");
            while (value.Contains("  ")) value = value.Replace("  ", " ");

            string[] removable =
            {
                "麻烦你帮我", "麻烦帮我", "可以的话", "如果可以的话",
                "我想让你帮我", "请你帮我", "请帮我", "帮我"
            };
            foreach (string item in removable)
            {
                value = value.Replace(item, "");
            }

            value = RemoveDuplicateLines(value).Trim();
            if (value.Length > 0 && settings.AutoOptimizePrompt)
            {
                value = "保留主要意思，直接处理这个需求：" + value;
            }
            return value;
        }

        private string RemoveDuplicateLines(string text)
        {
            string[] lines = text.Split('\n');
            var result = new List<string>();
            string previous = null;
            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd();
                if (line.Length == 0)
                {
                    if (result.Count == 0 || result[result.Count - 1].Length != 0)
                    {
                        result.Add("");
                    }
                    previous = "";
                    continue;
                }

                if (!string.Equals(line, previous, StringComparison.Ordinal))
                {
                    result.Add(line);
                }
                previous = line;
            }
            return string.Join("\n", result.ToArray());
        }

        private void RememberFromUserInput(string text)
        {
            if (!settings.MemoryEnabled || string.IsNullOrWhiteSpace(text)) return;
            string trimmed = text.Trim();
            if (!ShouldRemember(trimmed)) return;
            string note = DateTime.Now.ToString("yyyy-MM-dd") + " 用户说：" + Limit(trimmed, 140);
            if (memory == null) memory = new AgentMemory();
            if (memory.Notes == null) memory.Notes = new List<string>();
            foreach (string existing in memory.Notes)
            {
                if (existing.EndsWith(Limit(trimmed, 140), StringComparison.Ordinal))
                {
                    return;
                }
            }
            memory.Notes.Add(note);
            while (memory.Notes.Count > 40)
            {
                memory.Notes.RemoveAt(0);
            }
            MemoryStore.Save(memory);
        }

        private bool ShouldRemember(string text)
        {
            string[] markers =
            {
                "记住", "以后", "我叫", "我是", "我喜欢", "我不喜欢",
                "我的", "偏好", "习惯", "称呼我", "希望你", "我想要"
            };
            foreach (string marker in markers)
            {
                if (text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private string ExtractChoiceContent(string responseText)
        {
            object rootObj = serializer.DeserializeObject(responseText);
            var root = rootObj as Dictionary<string, object>;
            if (root == null || !root.ContainsKey("choices"))
            {
                throw new InvalidOperationException("无法解析 DeepSeek 响应");
            }

            var choices = root["choices"] as object[];
            if (choices == null || choices.Length == 0)
            {
                throw new InvalidOperationException("DeepSeek 响应没有 choices");
            }

            var first = choices[0] as Dictionary<string, object>;
            var message = first != null && first.ContainsKey("message") ? first["message"] as Dictionary<string, object> : null;
            if (message == null || !message.ContainsKey("content"))
            {
                throw new InvalidOperationException("DeepSeek 响应没有 message.content");
            }

            return Convert.ToString(message["content"]);
        }

        private AgentReply ParseAgentReply(string raw)
        {
            string json = StripToJson(raw);
            try
            {
                var obj = serializer.DeserializeObject(json) as Dictionary<string, object>;
                if (obj != null)
                {
                    string zh = obj.ContainsKey("zh") ? Convert.ToString(obj["zh"]) : raw;
                    string ja = obj.ContainsKey("ja") ? Convert.ToString(obj["ja"]) : "すみません、音声用の返答を作れませんでした。";
                    string mood = obj.ContainsKey("mood") ? Convert.ToString(obj["mood"]) : "happy";
                    return new AgentReply
                    {
                        ChineseText = string.IsNullOrWhiteSpace(zh) ? raw : zh,
                        JapaneseSpeech = string.IsNullOrWhiteSpace(ja) ? "はい。" : ja,
                        Mood = ParseMood(mood)
                    };
                }
            }
            catch
            {
                AddFeedback("模型未返回标准 JSON，已使用文本兜底");
            }

            return new AgentReply
            {
                ChineseText = raw,
                JapaneseSpeech = "すみません、返答の形式を整えられませんでした。",
                Mood = AvatarState.Happy
            };
        }

        private AvatarState ParseMood(string mood)
        {
            if (string.Equals(mood, "thinking", StringComparison.OrdinalIgnoreCase)) return AvatarState.Thinking;
            if (string.Equals(mood, "speaking", StringComparison.OrdinalIgnoreCase)) return AvatarState.Speaking;
            if (string.Equals(mood, "happy", StringComparison.OrdinalIgnoreCase)) return AvatarState.Happy;
            if (string.Equals(mood, "error", StringComparison.OrdinalIgnoreCase)) return AvatarState.Error;
            if (string.Equals(mood, "shy", StringComparison.OrdinalIgnoreCase)) return AvatarState.Shy;
            if (string.Equals(mood, "surprised", StringComparison.OrdinalIgnoreCase)) return AvatarState.Surprised;
            if (string.Equals(mood, "cheer", StringComparison.OrdinalIgnoreCase)) return AvatarState.Cheer;
            if (string.Equals(mood, "focus", StringComparison.OrdinalIgnoreCase)) return AvatarState.Focus;
            return AvatarState.Idle;
        }

        private string StripToJson(string raw)
        {
            if (raw == null) return "{}";
            string text = raw.Trim();
            if (text.StartsWith("```"))
            {
                int firstLine = text.IndexOf('\n');
                int lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
                if (firstLine >= 0 && lastFence > firstLine)
                {
                    text = text.Substring(firstLine + 1, lastFence - firstLine - 1).Trim();
                }
            }

            int start = text.IndexOf('{');
            int end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                return text.Substring(start, end - start + 1);
            }

            return text;
        }

        private async Task PlayVoiceAsync(string japaneseText)
        {
            string wavPath = await PrepareVoiceAudioFileAsync(japaneseText);
            if (!string.IsNullOrWhiteSpace(wavPath))
            {
                bool played = await PlayPreparedVoiceAsync(wavPath);
                if (played)
                {
                    SetStatus("试听完成", AvatarState.Happy);
                }
            }
            else
            {
                SetStatus("语音暂不可用", AvatarState.Error);
            }
        }

        private async Task<string> PrepareVoiceAudioFileAsync(string japaneseText)
        {
            if (string.IsNullOrWhiteSpace(japaneseText))
            {
                AddFeedback("日语台词为空，跳过语音");
                return null;
            }

            if (string.IsNullOrWhiteSpace(settings.VoiceServerUrl))
            {
                AddFeedback("未配置语音服务地址，跳过发声");
                return null;
            }

            if (IsLocalVoiceServer())
            {
                SetStatus("正在准备语音", AvatarState.Thinking);
                bool ready = await EnsureVoiceServiceReadyAsync();
                if (!ready)
                {
                    SetStatus("语音暂不可用", AvatarState.Error);
                    return null;
                }
            }

            SetStatus("正在生成语音", AvatarState.Thinking);
            AddFeedback("语音台词: " + Limit(japaneseText, 70));

            byte[] audio = await RequestVoiceAudioAsync(japaneseText);
            if (audio == null || audio.Length < 44)
            {
                AddFeedback("语音服务没有返回可播放音频");
                return null;
            }

            string wavPath = Path.Combine(Path.GetTempPath(), "iroha-agent-voice-" + Guid.NewGuid().ToString("N") + ".wav");
            File.WriteAllBytes(wavPath, audio);
            double gainDb;
            double peakDb;
            if (!TryPrepareVoiceWavForPlayback(wavPath, out gainDb, out peakDb))
            {
                AddFeedback("语音文件无效或接近静音");
                try { File.Delete(wavPath); }
                catch { }
                return null;
            }
            if (gainDb > 0.1)
            {
                AddFeedback("语音响度已自动提升 " + gainDb.ToString("0.0") + " dB");
            }
            AddFeedback("语音峰值 " + peakDb.ToString("0.0") + " dBFS");
            return wavPath;
        }

        private async Task<bool> PlayPreparedVoiceAsync(string wavPath)
        {
            AddFeedback("开始播放语音");
            SetStatus("正在发声", AvatarState.Speaking);
            avatar.SetState(AvatarState.Speaking);

            try
            {
                await Task.Run(delegate
                {
                    using (var player = new SoundPlayer(wavPath))
                    {
                        player.Load();
                        player.PlaySync();
                    }
                });
                AddFeedback("语音播放完成");
                return true;
            }
            catch (Exception ex)
            {
                AddFeedback("语音播放失败: " + ex.Message);
                SetStatus("声音未能播放", AvatarState.Error);
                return false;
            }
            finally
            {
                try { File.Delete(wavPath); }
                catch { }
            }
        }

        private async Task<byte[]> RequestVoiceAudioAsync(string japaneseText)
        {
            string endpoint = BuildTtsEndpoint(settings.VoiceServerUrl);
            string promptText = DefaultIfBlank(settings.VoicePromptText, AppSettings.DefaultVoicePromptText);
            string promptLang = DefaultIfBlank(settings.VoicePromptLang, AppSettings.DefaultVoicePromptLang);
            string refAudioPath = DefaultIfBlank(settings.VoiceRefAudioPath, AppSettings.DefaultVoiceRefAudioPath);
            Exception lastError = null;

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(90);
                    var payload = new Dictionary<string, object>
                    {
                        { "text", japaneseText },
                        { "text_lang", "ja" },
                        { "prompt_text", promptText },
                        { "prompt_lang", promptLang },
                        { "ref_audio_path", refAudioPath },
                        { "text_split_method", "cut2" },
                        { "batch_size", 1 },
                        { "speed_factor", 1.0 },
                        { "streaming_mode", false },
                        { "media_type", "wav" }
                    };

                    string json = serializer.Serialize(payload);
                    using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                    using (HttpResponseMessage response = await client.PostAsync(endpoint, content))
                    {
                        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                        if (response.IsSuccessStatusCode && LooksLikeAudio(bytes))
                        {
                            return bytes;
                        }

                        string detail = Encoding.UTF8.GetString(bytes);
                        AddFeedback("POST /tts 未返回音频: " + Limit(detail, 160));
                    }
                }
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(90);
                    string url = endpoint
                        + "?text=" + Uri.EscapeDataString(japaneseText)
                        + "&text_lang=ja"
                        + "&prompt_text=" + Uri.EscapeDataString(promptText)
                        + "&prompt_lang=" + Uri.EscapeDataString(promptLang)
                        + "&ref_audio_path=" + Uri.EscapeDataString(refAudioPath)
                        + "&text_split_method=cut2&media_type=wav&streaming_mode=false";
                    using (HttpResponseMessage response = await client.GetAsync(url))
                    {
                        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                        if (response.IsSuccessStatusCode && LooksLikeAudio(bytes))
                        {
                            return bytes;
                        }

                        string detail = Encoding.UTF8.GetString(bytes);
                        AddFeedback("GET /tts 未返回音频: " + Limit(detail, 160));
                    }
                }
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            if (lastError != null)
            {
                AddFeedback("语音服务错误: " + lastError.Message);
            }
            return null;
        }

        private string BuildTtsEndpoint(string baseUrl)
        {
            string clean = baseUrl.Trim().TrimEnd('/');
            if (clean.EndsWith("/tts", StringComparison.OrdinalIgnoreCase))
            {
                return clean;
            }
            return clean + "/tts";
        }

        private bool LooksLikeAudio(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 44) return false;
            return bytes[0] == (byte)'R' && bytes[1] == (byte)'I' &&
                bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
                bytes[8] == (byte)'W' && bytes[9] == (byte)'A' &&
                bytes[10] == (byte)'V' && bytes[11] == (byte)'E';
        }

        private bool TryPrepareVoiceWavForPlayback(string wavPath, out double gainDb, out double peakDb)
        {
            gainDb = 0.0;
            peakDb = -96.0;
            try
            {
                byte[] bytes = File.ReadAllBytes(wavPath);
                if (!LooksLikeAudio(bytes)) return false;

                int formatCode = 0;
                int bitsPerSample = 0;
                int byteRate = 0;
                int dataOffset = -1;
                int dataSize = 0;
                int index = 12;
                while (index + 8 <= bytes.Length)
                {
                    string id = Encoding.ASCII.GetString(bytes, index, 4);
                    int size = BitConverter.ToInt32(bytes, index + 4);
                    if (size < 0 || index + 8 + size > bytes.Length) return false;
                    if (id == "fmt " && size >= 16)
                    {
                        formatCode = BitConverter.ToInt16(bytes, index + 8);
                        byteRate = BitConverter.ToInt32(bytes, index + 16);
                        bitsPerSample = BitConverter.ToInt16(bytes, index + 22);
                    }
                    else if (id == "data")
                    {
                        dataOffset = index + 8;
                        dataSize = size;
                    }

                    index += 8 + size;
                    if ((size & 1) == 1) index++;
                }

                if (dataOffset < 0 || dataSize <= 0 || dataOffset + dataSize > bytes.Length || byteRate <= 0)
                {
                    return false;
                }
                if (formatCode != 1 || bitsPerSample != 16)
                {
                    return true;
                }

                int sampleCount = dataSize / 2;
                if (sampleCount <= 0) return false;
                int peak = 0;
                double squareSum = 0.0;
                for (int i = 0; i < sampleCount; i++)
                {
                    short sample = BitConverter.ToInt16(bytes, dataOffset + i * 2);
                    int absolute = sample == short.MinValue ? 32768 : Math.Abs((int)sample);
                    if (absolute > peak) peak = absolute;
                    double normalized = sample / 32768.0;
                    squareSum += normalized * normalized;
                }

                double rms = Math.Sqrt(squareSum / sampleCount);
                if (peak < 96 || rms < 0.0008)
                {
                    return false;
                }

                peakDb = 20.0 * Math.Log10(Math.Max(1.0, peak) / 32768.0);
                double targetPeak = 0.88 * 32767.0;
                double gain = Math.Min(4.0, targetPeak / peak);
                if (gain > 1.05)
                {
                    for (int i = 0; i < sampleCount; i++)
                    {
                        short sample = BitConverter.ToInt16(bytes, dataOffset + i * 2);
                        int amplified = (int)Math.Round(sample * gain);
                        if (amplified > short.MaxValue) amplified = short.MaxValue;
                        if (amplified < short.MinValue) amplified = short.MinValue;
                        bytes[dataOffset + i * 2] = (byte)(amplified & 0xFF);
                        bytes[dataOffset + i * 2 + 1] = (byte)((amplified >> 8) & 0xFF);
                    }
                    File.WriteAllBytes(wavPath, bytes);
                    gainDb = 20.0 * Math.Log10(gain);
                    peakDb = 20.0 * Math.Log10(Math.Min(32767.0, peak * gain) / 32768.0);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string DefaultIfBlank(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private void AddUserLine(string text)
        {
            AppendChat("你", text, Color.FromArgb(35, 82, 142));
        }

        private void AddAssistantLine(string text)
        {
            ShowDialogueText(text);
            AppendChat("Agent", text, Theme.PrimaryDark);
        }

        private async Task AddAssistantLineTypedAsync(string text, int voiceDurationMs)
        {
            if (string.IsNullOrEmpty(text))
            {
                AddAssistantLine("");
                return;
            }

            int delayMs = CalculateTypeDelayMilliseconds(text, voiceDurationMs);
            if (dialogueTextBox != null)
            {
                dialogueTextBox.Clear();
                dialogueTextBox.SelectionColor = Theme.TextMain;
                dialogueTextBox.SelectionFont = dialogueTextBox.Font;
            }
            AppendChatHeader("Agent", Theme.PrimaryDark);
            for (int i = 0; i < text.Length; i++)
            {
                chatLog.SelectionColor = Color.FromArgb(45, 48, 56);
                chatLog.SelectionFont = chatLog.Font;
                chatLog.AppendText(text[i].ToString());
                chatLog.ScrollToCaret();
                if (dialogueTextBox != null)
                {
                    dialogueTextBox.SelectionColor = Theme.TextMain;
                    dialogueTextBox.SelectionFont = dialogueTextBox.Font;
                    dialogueTextBox.AppendText(text[i].ToString());
                    dialogueTextBox.ScrollToCaret();
                }

                int pause = IsSentencePause(text[i]) ? Math.Min(delayMs + 22, 65) : delayMs;
                await Task.Delay(pause);
            }
            chatLog.AppendText(Environment.NewLine + Environment.NewLine);
            chatLog.ScrollToCaret();
        }

        private void ShowDialogueText(string text)
        {
            if (dialogueTextBox == null) return;
            dialogueTextBox.Clear();
            dialogueTextBox.SelectionColor = Theme.TextMain;
            dialogueTextBox.SelectionFont = dialogueTextBox.Font;
            dialogueTextBox.AppendText(text ?? "");
            dialogueTextBox.SelectionStart = dialogueTextBox.TextLength;
            dialogueTextBox.ScrollToCaret();
        }

        private void AppendChat(string role, string text, Color color)
        {
            AppendChatHeader(role, color);
            chatLog.SelectionColor = Color.FromArgb(45, 48, 56);
            chatLog.SelectionFont = chatLog.Font;
            chatLog.AppendText(text + Environment.NewLine + Environment.NewLine);
            chatLog.ScrollToCaret();
        }

        private void AppendChatHeader(string role, Color color)
        {
            chatLog.SelectionStart = chatLog.TextLength;
            chatLog.SelectionColor = color;
            chatLog.SelectionFont = new Font(chatLog.Font, FontStyle.Bold);
            chatLog.AppendText(role + "  ");
        }

        private int CalculateTypeDelayMilliseconds(string text, int voiceDurationMs)
        {
            if (string.IsNullOrEmpty(text)) return 12;
            if (voiceDurationMs > 0)
            {
                return Math.Max(6, Math.Min(42, voiceDurationMs / Math.Max(1, text.Length)));
            }
            if (text.Length > 180) return 4;
            if (text.Length > 90) return 8;
            return 16;
        }

        private bool IsSentencePause(char value)
        {
            return value == '。' || value == '，' || value == '？' || value == '！' ||
                value == '.' || value == ',' || value == '?' || value == '!';
        }

        private int EstimateWavDurationMilliseconds(string wavPath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(wavPath);
                int byteRate = 0;
                int dataSize = 0;
                int index = 12;
                while (index + 8 <= bytes.Length)
                {
                    string id = Encoding.ASCII.GetString(bytes, index, 4);
                    int size = BitConverter.ToInt32(bytes, index + 4);
                    if (id == "fmt " && index + 20 <= bytes.Length)
                    {
                        byteRate = BitConverter.ToInt32(bytes, index + 16);
                    }
                    else if (id == "data")
                    {
                        dataSize = size;
                    }

                    index += 8 + Math.Max(0, size);
                    if ((size & 1) == 1) index++;
                }

                if (byteRate > 0 && dataSize > 0)
                {
                    return (int)Math.Max(1, dataSize * 1000.0 / byteRate);
                }
            }
            catch
            {
            }
            return 0;
        }

        private void AddFeedback(string text)
        {
            Debug.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "  " + text);
        }

        private void SetStatus(string text, AvatarState state)
        {
            if (statusLabel != null)
            {
                statusLabel.Text = "彩叶小贴士： " + text;
            }
            if (voiceStateLabel != null)
            {
                if (state == AvatarState.Speaking)
                {
                    voiceStateLabel.Text = "说话中\n日语 · 女声";
                }
                else if (state == AvatarState.Thinking)
                {
                    voiceStateLabel.Text = "准备中\n日语 · 女声";
                }
                else
                {
                    voiceStateLabel.Text = "试听彩叶\n日语 · 女声";
                }
            }
            if (waveform != null)
            {
                waveform.Active = state == AvatarState.Speaking || state == AvatarState.Thinking;
            }
            if (avatar != null)
            {
                avatar.SetState(state);
            }
        }

        private void TrimHistory()
        {
            while (history.Count > 16)
            {
                history.RemoveAt(0);
            }
        }

        private static string Limit(string text, int length)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Length <= length ? text : text.Substring(0, length) + "...";
        }
    }

    internal sealed class IrohaContextMenuRenderer : ToolStripProfessionalRenderer
    {
        public IrohaContextMenuRenderer()
            : base(new IrohaContextMenuColorTable())
        {
            RoundedEdges = true;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item.ForeColor == Color.FromArgb(166, 75, 86))
            {
                e.TextColor = e.Item.Selected ? Color.FromArgb(144, 55, 68) : e.Item.ForeColor;
            }
            else
            {
                e.TextColor = Theme.TextMain;
            }
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.ContentRectangle.Top + e.Item.ContentRectangle.Height / 2;
            using (var pen = new Pen(Color.FromArgb(88, 174, 215, 229), 1F))
            {
                e.Graphics.DrawLine(pen, 12, y, e.Item.Width - 12, y);
            }
        }
    }

    internal sealed class IrohaContextMenuColorTable : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Color.FromArgb(252, 255, 255); } }
        public override Color MenuBorder { get { return Color.FromArgb(142, 186, 224, 236); } }
        public override Color MenuItemBorder { get { return Color.FromArgb(128, 150, 215, 229); } }
        public override Color MenuItemSelected { get { return Color.FromArgb(222, 239, 250, 254); } }
        public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(242, 255, 255, 255); } }
        public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(218, 235, 249, 253); } }
        public override Color SeparatorDark { get { return Color.FromArgb(88, 174, 215, 229); } }
        public override Color SeparatorLight { get { return Color.FromArgb(24, 255, 255, 255); } }
        public override Color ImageMarginGradientBegin { get { return ToolStripDropDownBackground; } }
        public override Color ImageMarginGradientMiddle { get { return ToolStripDropDownBackground; } }
        public override Color ImageMarginGradientEnd { get { return ToolStripDropDownBackground; } }
    }

    internal static class Theme
    {
        public static readonly Color AppBg = Color.FromArgb(236, 248, 250);
        public static readonly Color TopBar1 = Color.FromArgb(249, 253, 255);
        public static readonly Color TopBar2 = Color.FromArgb(232, 247, 252);
        public static readonly Color GlassWhite = Color.FromArgb(230, 255, 255, 255);
        public static readonly Color GlassBlue = Color.FromArgb(196, 221, 246, 252);
        public static readonly Color DialogueBlue = Color.FromArgb(206, 196, 236, 248);
        public static readonly Color Border = Color.FromArgb(126, 187, 225, 235);
        public static readonly Color BorderStrong = Color.FromArgb(166, 112, 202, 218);
        public static readonly Color Primary = Color.FromArgb(61, 190, 207);
        public static readonly Color PrimaryDark = Color.FromArgb(37, 130, 165);
        public static readonly Color Mint = Color.FromArgb(88, 218, 207);
        public static readonly Color TextMain = Color.FromArgb(31, 70, 96);
        public static readonly Color TextSub = Color.FromArgb(94, 127, 148);
        public static readonly Color OnlineGreen = Color.FromArgb(43, 214, 137);
        public static readonly Color Sakura = Color.FromArgb(242, 181, 202);
    }

    internal sealed class DialogueNameplateControl : Control
    {
        public DialogueNameplateControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            ForeColor = Theme.TextMain;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            using (var shadow = new SolidBrush(Color.FromArgb(22, 52, 128, 160)))
            using (var fill = new LinearGradientBrush(rect, Color.FromArgb(252, 255, 255, 255), Color.FromArgb(238, 240, 251, 254), 90F))
            using (var border = new Pen(Color.FromArgb(156, 112, 205, 222), 1F))
            {
                g.FillRoundedRectangle(shadow, new Rectangle(rect.X + 1, rect.Y + 2, rect.Width, rect.Height), 16);
                g.FillRoundedRectangle(fill, rect, 16);
                g.DrawRoundedRectangle(border, rect, 16);
            }

            TextRenderer.DrawText(
                g,
                Text ?? "",
                Font,
                new Rectangle(18, 0, Math.Max(24, Width - 52), Height),
                ForeColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            int cx = Width - 25;
            int cy = Height / 2;
            using (var pen = new Pen(Color.FromArgb(70, 145, 174), 1.6F))
            using (var brush = new SolidBrush(Color.FromArgb(70, 145, 174)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                Point[] speaker =
                {
                    new Point(cx - 8, cy - 3),
                    new Point(cx - 4, cy - 3),
                    new Point(cx + 1, cy - 8),
                    new Point(cx + 1, cy + 8),
                    new Point(cx - 4, cy + 3),
                    new Point(cx - 8, cy + 3)
                };
                g.FillPolygon(brush, speaker);
                g.DrawArc(pen, cx - 2, cy - 7, 10, 14, -55, 110);
            }
        }
    }

    internal sealed class VnDialogueTextControl : Control
    {
        public bool ReadOnly { get; set; }
        public BorderStyle BorderStyle { get; set; }
        public bool DetectUrls { get; set; }
        public RichTextBoxScrollBars ScrollBars { get; set; }
        public Color SelectionColor { get; set; }
        public Font SelectionFont { get; set; }
        public int SelectionStart { get; set; }
        public int TextLength { get { return Text == null ? 0 : Text.Length; } }

        public VnDialogueTextControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Transparent;
            ForeColor = Theme.TextMain;
            SelectionColor = Theme.TextMain;
            BorderStyle = BorderStyle.None;
        }

        public void Clear()
        {
            Text = "";
            SelectionStart = 0;
            Invalidate();
        }

        public void AppendText(string value)
        {
            Text = (Text ?? "") + (value ?? "");
            SelectionStart = TextLength;
            Invalidate();
        }

        public void ScrollToCaret()
        {
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            base.OnPaintBackground(pevent);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            string value = Text ?? "";
            if (value.Length == 0) return;
            Rectangle textRect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            int lineBreak = value.IndexOf('\n');
            if (lineBreak > 0)
            {
                string headline = value.Substring(0, lineBreak).TrimEnd('\r');
                string body = value.Substring(lineBreak + 1).TrimStart('\r', '\n');
                using (var headlineFont = new Font("Microsoft YaHei UI", 11.8F, FontStyle.Bold))
                {
                    TextRenderer.DrawText(
                        g,
                        headline,
                        headlineFont,
                        new Rectangle(textRect.X, textRect.Y, textRect.Width, 28),
                        ForeColor,
                        TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                }
                TextRenderer.DrawText(
                    g,
                    body,
                    Font,
                    new Rectangle(textRect.X, textRect.Y + 30, textRect.Width, Math.Max(1, textRect.Height - 30)),
                    ForeColor,
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                return;
            }

            TextRenderer.DrawText(g, value, Font, textRect, ForeColor, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        }
    }

    internal class GlassPanel : Panel
    {
        public Color FillColor { get; set; }
        public Color BorderColor { get; set; }
        public int Radius { get; set; }
        public bool Shadow { get; set; }
        public bool PaintChrome { get; set; }
        public bool OpaqueBackfill { get; set; }
        public bool BareSurface { get; set; }
        public string IconKind { get; set; }
        public string DecorationKind { get; set; }
        public string FooterText { get; set; }

        public GlassPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            FillColor = Theme.GlassWhite;
            BorderColor = Theme.Border;
            Radius = 22;
            Shadow = true;
            PaintChrome = true;
            OpaqueBackfill = false;
            BareSurface = false;
            IconKind = "";
            DecorationKind = "";
            FooterText = "";
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (BareSurface)
            {
                base.OnPaintBackground(e);
                return;
            }
            if (OpaqueBackfill)
            {
                using (var brush = new SolidBrush(FillColor))
                {
                    e.Graphics.FillRectangle(brush, ClientRectangle);
                }
                return;
            }
            if (!PaintChrome && UiAssetStore.PaintShellSlice(e.Graphics, this)) return;
            base.OnPaintBackground(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (BareSurface || !PaintChrome) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(5, 5, Math.Max(1, Width - 11), Math.Max(1, Height - 11));
            if (Shadow)
            {
                using (var shadow = new SolidBrush(Color.FromArgb(24, 54, 128, 160)))
                {
                    g.FillRoundedRectangle(shadow, new Rectangle(rect.X + 3, rect.Y + 5, rect.Width, rect.Height), Radius);
                }
            }
            bool dialogueSurface = string.Equals(DecorationKind, "dialogue", StringComparison.OrdinalIgnoreCase);
            if (dialogueSurface)
            {
                DrawDialogueSurface(g, rect);
            }
            else
            {
                using (var fill = new SolidBrush(FillColor))
                {
                    g.FillRoundedRectangle(fill, rect, Radius);
                }
                using (var shine = new LinearGradientBrush(rect, Color.FromArgb(76, 255, 255, 255), Color.FromArgb(10, 255, 255, 255), 90F))
                {
                    g.FillRoundedRectangle(shine, rect, Radius);
                }
            }
            using (var pen = new Pen(BorderColor, 1.2F))
            {
                g.DrawRoundedRectangle(pen, rect, Radius);
            }
            DrawDecoration(g, rect);
            DrawPanelIcon(g);
            DrawFooter(g);
        }

        private void DrawDialogueSurface(Graphics g, Rectangle rect)
        {
            using (var glass = new LinearGradientBrush(rect, Color.White, Color.FromArgb(208, 154, 224, 242), 0F))
            {
                glass.InterpolationColors = new ColorBlend
                {
                    Colors = new[]
                    {
                        Color.FromArgb(218, 247, 252, 255),
                        Color.FromArgb(204, 221, 243, 251),
                        Color.FromArgb(194, 186, 233, 247),
                        Color.FromArgb(184, 151, 220, 241)
                    },
                    Positions = new[] { 0F, 0.36F, 0.72F, 1F }
                };
                g.FillRoundedRectangle(glass, rect, Radius);
            }

            using (var upperLight = new LinearGradientBrush(
                rect,
                Color.FromArgb(100, 255, 255, 255),
                Color.FromArgb(0, 255, 255, 255),
                90F))
            {
                upperLight.InterpolationColors = new ColorBlend
                {
                    Colors = new[]
                    {
                        Color.FromArgb(112, 255, 255, 255),
                        Color.FromArgb(52, 255, 255, 255),
                        Color.FromArgb(0, 255, 255, 255)
                    },
                    Positions = new[] { 0F, 0.34F, 1F }
                };
                g.FillRoundedRectangle(upperLight, rect, Radius);
            }

            using (var depth = new LinearGradientBrush(
                rect,
                Color.FromArgb(0, 88, 196, 220),
                Color.FromArgb(34, 74, 188, 216),
                90F))
            {
                g.FillRoundedRectangle(depth, rect, Radius);
            }
        }

        private void DrawDecoration(Graphics g, Rectangle rect)
        {
            if (!string.Equals(DecorationKind, "dialogue", StringComparison.OrdinalIgnoreCase)) return;

            Rectangle inner = new Rectangle(rect.X + 1, rect.Y + 1, Math.Max(1, rect.Width - 2), Math.Max(1, rect.Height - 2));
            using (var depth = new LinearGradientBrush(
                inner,
                Color.FromArgb(28, 255, 255, 255),
                Color.FromArgb(54, 128, 213, 234),
                0F))
            using (var topLight = new Pen(Color.FromArgb(122, 255, 255, 255), 1F))
            {
                g.FillRoundedRectangle(depth, inner, Math.Max(2, Radius - 1));
                g.DrawLine(topLight, inner.X + 24, inner.Y + 1, inner.Right - 24, inner.Y + 1);
            }

            int ornamentStart = rect.X + (int)Math.Round(rect.Width * 0.56);
            using (var line = new Pen(Color.FromArgb(32, 91, 190, 214), 1F))
            using (var sparkle = new SolidBrush(Color.FromArgb(72, 255, 255, 255)))
            {
                line.DashStyle = DashStyle.Dot;
                g.DrawLine(line, ornamentStart, rect.Bottom - 20, rect.Right - 24, rect.Y + 35);
                g.DrawLine(line, ornamentStart + 36, rect.Y + 26, rect.Right - 82, rect.Bottom - 18);
                g.FillEllipse(sparkle, ornamentStart - 2, rect.Bottom - 22, 4, 4);
                g.FillEllipse(sparkle, rect.Right - 74, rect.Bottom - 34, 5, 5);
            }

            DrawSakuraFlower(g, Math.Min(rect.Right - 72, rect.X + (int)Math.Round(rect.Width * 0.43)), rect.Y + 14, 7F, 112);
            DrawSakuraFlower(g, rect.Right - 52, rect.Bottom - 28, 13F, 92);
            DrawSakuraFlower(g, rect.Right - 82, rect.Bottom - 16, 8F, 76);
            using (var vine = new Pen(Color.FromArgb(76, 95, 185, 201), 1.1F))
            {
                vine.StartCap = LineCap.Round;
                vine.EndCap = LineCap.Round;
                float flowerX = Math.Min(rect.Right - 72, rect.X + (float)Math.Round(rect.Width * 0.43));
                g.DrawBezier(
                    vine,
                    new PointF(flowerX + 8, rect.Y + 15),
                    new PointF(Math.Min(rect.Right - 48, flowerX + 32), rect.Y + 4),
                    new PointF(Math.Min(rect.Right - 38, flowerX + 50), rect.Y + 24),
                    new PointF(Math.Min(rect.Right - 24, flowerX + 66), rect.Y + 13));
            }
        }

        private void DrawSakuraFlower(Graphics g, float centerX, float centerY, float size, int alpha)
        {
            GraphicsState state = g.Save();
            g.TranslateTransform(centerX, centerY);
            using (var petal = new SolidBrush(Color.FromArgb(alpha, 255, 244, 251)))
            using (var edge = new Pen(Color.FromArgb(Math.Min(180, alpha + 36), 215, 161, 201), 0.9F))
            using (var center = new SolidBrush(Color.FromArgb(Math.Min(190, alpha + 48), 255, 222, 235)))
            {
                for (int i = 0; i < 5; i++)
                {
                    g.RotateTransform(72F);
                    RectangleF bounds = new RectangleF(-size * 0.34F, -size, size * 0.68F, size * 1.12F);
                    g.FillEllipse(petal, bounds);
                    g.DrawEllipse(edge, bounds);
                }
                g.FillEllipse(center, -size * 0.2F, -size * 0.2F, size * 0.4F, size * 0.4F);
            }
            g.Restore(state);
        }

        private void DrawFooter(Graphics g)
        {
            if (string.IsNullOrWhiteSpace(FooterText) || Height < 112) return;
            int lineY = Height - 39;
            using (var line = new Pen(Color.FromArgb(72, 136, 193, 216), 1F))
            {
                g.DrawLine(line, 22, lineY, Math.Max(24, Width - 22), lineY);
            }
            using (var footerFont = new Font("Microsoft YaHei UI", 7.2F, FontStyle.Regular))
            {
                TextRenderer.DrawText(
                    g,
                    FooterText,
                    footerFont,
                    new Rectangle(24, lineY + 5, Math.Max(40, Width - 48), 25),
                    Color.FromArgb(96, 139, 164),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        private void DrawPanelIcon(Graphics g)
        {
            if (string.IsNullOrEmpty(IconKind)) return;
            if (string.Equals(IconKind, "search", StringComparison.OrdinalIgnoreCase))
            {
                using (var pen = new Pen(Color.FromArgb(92, 136, 178), 1.55F))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawEllipse(pen, 19, 13, 10, 10);
                    g.DrawLine(pen, 27, 21, 32, 26);
                }
                return;
            }
            if (Width < 90 || Height < 62) return;
            Rectangle icon = new Rectangle(17, 19, 18, 18);
            Color lineColor = Color.FromArgb(65, 145, 205);
            using (var pen = new Pen(lineColor, 1.7F))
            using (var soft = new SolidBrush(Color.FromArgb(26, 76, 190, 214)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                if (string.Equals(IconKind, "memory", StringComparison.OrdinalIgnoreCase))
                {
                    g.FillEllipse(soft, icon.X + 1, icon.Y + 2, 11, 17);
                    g.FillEllipse(soft, icon.X + 10, icon.Y + 2, 11, 17);
                    g.DrawArc(pen, icon.X + 2, icon.Y + 2, 10, 10, 100, 215);
                    g.DrawArc(pen, icon.X + 10, icon.Y + 2, 10, 10, 225, 215);
                    g.DrawArc(pen, icon.X + 2, icon.Y + 9, 10, 10, 45, 220);
                    g.DrawArc(pen, icon.X + 10, icon.Y + 9, 10, 10, 275, 220);
                    g.DrawLine(pen, icon.X + 11, icon.Y + 3, icon.X + 11, icon.Bottom - 3);
                    return;
                }

                if (string.Equals(IconKind, "compress", StringComparison.OrdinalIgnoreCase))
                {
                    Point center = new Point(icon.X + 11, icon.Y + 11);
                    g.DrawLine(pen, icon.X + 2, icon.Y + 6, center.X - 2, center.Y - 2);
                    g.DrawLine(pen, icon.X + 6, icon.Y + 2, center.X - 2, center.Y - 2);
                    g.DrawLine(pen, icon.Right - 2, icon.Y + 6, center.X + 2, center.Y - 2);
                    g.DrawLine(pen, icon.Right - 6, icon.Y + 2, center.X + 2, center.Y - 2);
                    g.DrawLine(pen, icon.X + 2, icon.Bottom - 6, center.X - 2, center.Y + 2);
                    g.DrawLine(pen, icon.X + 6, icon.Bottom - 2, center.X - 2, center.Y + 2);
                    g.DrawLine(pen, icon.Right - 2, icon.Bottom - 6, center.X + 2, center.Y + 2);
                    g.DrawLine(pen, icon.Right - 6, icon.Bottom - 2, center.X + 2, center.Y + 2);
                    return;
                }

                if (string.Equals(IconKind, "service", StringComparison.OrdinalIgnoreCase))
                {
                    PointF[] shield =
                    {
                        new PointF(icon.X + 11, icon.Y + 1),
                        new PointF(icon.Right - 2, icon.Y + 5),
                        new PointF(icon.Right - 3, icon.Y + 14),
                        new PointF(icon.X + 11, icon.Bottom - 1),
                        new PointF(icon.X + 3, icon.Y + 14),
                        new PointF(icon.X + 2, icon.Y + 5)
                    };
                    g.FillPolygon(soft, shield);
                    g.DrawPolygon(pen, shield);
                    g.DrawLine(pen, icon.X + 7, icon.Y + 11, icon.X + 10, icon.Y + 14);
                    g.DrawLine(pen, icon.X + 10, icon.Y + 14, icon.X + 16, icon.Y + 8);
                }
            }
        }
    }

    internal sealed class ToolRailPanel : GlassPanel
    {
        private readonly Button[] actions = new Button[3];
        private int selectedIndex;
        private int hoverIndex = -1;
        private int pressedIndex = -1;

        public event EventHandler MemoryClicked;
        public event EventHandler SettingsClicked;
        public event EventHandler AppearanceClicked;

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                int next = Math.Max(0, Math.Min(2, value));
                if (selectedIndex == next) return;
                selectedIndex = next;
                Invalidate();
            }
        }

        public ToolRailPanel()
        {
            AccessibleName = "记忆、设定与外观";
            AccessibleRole = AccessibleRole.ToolBar;
            Cursor = Cursors.Default;
            selectedIndex = 0;
        }

        public void SetActions(Button memory, Button settings, Button appearance)
        {
            actions[0] = memory;
            actions[1] = settings;
            actions[2] = appearance;
            Invalidate();
        }

        public Rectangle[] GetItemBounds()
        {
            int insetX = Math.Max(7, Width / 11);
            int insetY = 8;
            int gap = 2;
            int available = Math.Max(3, Height - insetY * 2 - gap * 2);
            int itemHeight = Math.Max(1, available / 3);
            int width = Math.Max(1, Width - insetX * 2);
            return new[]
            {
                new Rectangle(insetX, insetY, width, itemHeight),
                new Rectangle(insetX, insetY + itemHeight + gap, width, itemHeight),
                new Rectangle(insetX, insetY + (itemHeight + gap) * 2, width, Math.Max(1, Height - insetY - (insetY + (itemHeight + gap) * 2)))
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle[] bounds = GetItemBounds();
            for (int i = 0; i < bounds.Length; i++)
            {
                DrawItem(g, bounds[i], i);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int next = HitTest(e.Location);
            if (next != hoverIndex)
            {
                hoverIndex = next;
                Invalidate();
            }
            Cursor = next >= 0 && IsActionEnabled(next) ? Cursors.Hand : Cursors.Default;
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverIndex = -1;
            pressedIndex = -1;
            Cursor = Cursors.Default;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            pressedIndex = e.Button == MouseButtons.Left ? HitTest(e.Location) : -1;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            int releasedIndex = HitTest(e.Location);
            int clickedIndex = pressedIndex == releasedIndex ? releasedIndex : -1;
            pressedIndex = -1;
            Invalidate();
            base.OnMouseUp(e);
            if (clickedIndex >= 0 && IsActionEnabled(clickedIndex))
            {
                RaiseAction(clickedIndex);
            }
        }

        private void DrawItem(Graphics g, Rectangle bounds, int index)
        {
            bool enabled = IsActionEnabled(index);
            bool selected = index == selectedIndex;
            bool hovered = index == hoverIndex && enabled;
            bool pressed = index == pressedIndex && enabled;

            Rectangle surface = new Rectangle(bounds.X + 1, bounds.Y + 1, Math.Max(1, bounds.Width - 2), Math.Max(1, bounds.Height - 2));
            if (selected)
            {
                using (var shadow = new SolidBrush(Color.FromArgb(20, 46, 126, 156)))
                {
                    g.FillRoundedRectangle(shadow, new Rectangle(surface.X + 1, surface.Y + 2, surface.Width, surface.Height), 14);
                }
                Color top = pressed ? Color.FromArgb(236, 221, 246, 250) : Color.FromArgb(245, 255, 255, 255);
                Color bottom = pressed ? Color.FromArgb(232, 207, 237, 244) : Color.FromArgb(230, 232, 248, 252);
                using (var fill = new LinearGradientBrush(surface, top, bottom, 90F))
                using (var border = new Pen(Color.FromArgb(134, 159, 216, 230), 1F))
                {
                    g.FillRoundedRectangle(fill, surface, 14);
                    g.DrawRoundedRectangle(border, surface, 14);
                }
            }
            else if (hovered || pressed)
            {
                using (var hover = new SolidBrush(Color.FromArgb(pressed ? 118 : 78, 211, 241, 247)))
                {
                    g.FillRoundedRectangle(hover, surface, 12);
                }
            }

            Color iconColor = enabled ? Color.FromArgb(54, 137, 181) : Color.FromArgb(126, 151, 164);
            int iconAreaHeight = Math.Max(24, bounds.Height - 28);
            Rectangle iconBounds = new Rectangle(
                bounds.X + (bounds.Width - 23) / 2,
                bounds.Y + Math.Max(5, (iconAreaHeight - 23) / 2),
                23,
                23);
            if (index == 0) DrawBrainIcon(g, iconBounds, iconColor);
            else if (index == 1) DrawSettingsRailIcon(g, iconBounds, iconColor);
            else DrawAppearanceRailIcon(g, iconBounds, iconColor);

            string label = actions[index] != null ? actions[index].Text : (index == 0 ? "记忆" : (index == 1 ? "设定" : "外观"));
            using (var labelFont = new Font("Microsoft YaHei UI", 8.2F, FontStyle.Bold))
            {
                TextRenderer.DrawText(
                    g,
                    label,
                    labelFont,
                    new Rectangle(bounds.X, bounds.Bottom - 25, bounds.Width, 21),
                    enabled ? Theme.TextMain : Color.FromArgb(126, 145, 156),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
        }

        private static void DrawFluentIcon(Graphics g, Rectangle bounds, string glyph, Color color)
        {
            using (var iconFont = new Font("Segoe Fluent Icons", 14.5F, FontStyle.Regular))
            {
                TextRenderer.DrawText(
                    g,
                    glyph,
                    iconFont,
                    bounds,
                    color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        private static void DrawSettingsRailIcon(Graphics g, Rectangle bounds, Color color)
        {
            float cx = bounds.X + bounds.Width / 2F;
            float cy = bounds.Y + bounds.Height / 2F;
            using (var pen = new Pen(color, 1.55F))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                for (int i = 0; i < 8; i++)
                {
                    double angle = i * Math.PI / 4.0;
                    float x1 = cx + (float)Math.Cos(angle) * 7F;
                    float y1 = cy + (float)Math.Sin(angle) * 7F;
                    float x2 = cx + (float)Math.Cos(angle) * 10F;
                    float y2 = cy + (float)Math.Sin(angle) * 10F;
                    g.DrawLine(pen, x1, y1, x2, y2);
                }
                g.DrawEllipse(pen, cx - 7F, cy - 7F, 14F, 14F);
                g.DrawEllipse(pen, cx - 2.6F, cy - 2.6F, 5.2F, 5.2F);
            }
        }

        private static void DrawAppearanceRailIcon(Graphics g, Rectangle bounds, Color color)
        {
            RectangleF palette = new RectangleF(bounds.X + 1.5F, bounds.Y + 3F, bounds.Width - 3F, bounds.Height - 6F);
            using (var pen = new Pen(color, 1.55F))
            using (var softFill = new SolidBrush(Color.FromArgb(18, color)))
            {
                g.FillEllipse(softFill, palette);
                g.DrawEllipse(pen, palette);
                g.DrawEllipse(pen, bounds.X + 13.5F, bounds.Y + 11F, 4.5F, 4.5F);
            }
            Color[] swatches =
            {
                Color.FromArgb(76, 196, 211),
                Color.FromArgb(111, 172, 218),
                Color.FromArgb(232, 154, 184)
            };
            PointF[] centers =
            {
                new PointF(bounds.X + 7F, bounds.Y + 8F),
                new PointF(bounds.X + 11F, bounds.Y + 6F),
                new PointF(bounds.X + 15F, bounds.Y + 7.5F)
            };
            for (int i = 0; i < centers.Length; i++)
            {
                using (var dot = new SolidBrush(swatches[i]))
                {
                    g.FillEllipse(dot, centers[i].X - 1.6F, centers[i].Y - 1.6F, 3.2F, 3.2F);
                }
            }
        }

        private static void DrawBrainIcon(Graphics g, Rectangle icon, Color color)
        {
            using (var pen = new Pen(color, 1.45F))
            using (var fill = new SolidBrush(Color.FromArgb(20, color)))
            using (var leftPath = new GraphicsPath())
            using (var rightPath = new GraphicsPath())
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                float cx = icon.X + icon.Width / 2F;
                float top = icon.Y + 2F;
                float bottom = icon.Bottom - 2F;
                leftPath.StartFigure();
                leftPath.AddBezier(new PointF(cx, top + 2F), new PointF(cx - 4F, top - 1F), new PointF(icon.X + 1F, top + 3F), new PointF(icon.X + 3F, top + 8F));
                leftPath.AddBezier(new PointF(icon.X + 3F, top + 8F), new PointF(icon.X - 1F, top + 12F), new PointF(icon.X + 4F, bottom + 1F), new PointF(cx, bottom - 2F));
                leftPath.AddLine(new PointF(cx, bottom - 2F), new PointF(cx, top + 2F));
                leftPath.CloseFigure();
                rightPath.StartFigure();
                rightPath.AddBezier(new PointF(cx, top + 2F), new PointF(cx + 4F, top - 1F), new PointF(icon.Right - 1F, top + 3F), new PointF(icon.Right - 3F, top + 8F));
                rightPath.AddBezier(new PointF(icon.Right - 3F, top + 8F), new PointF(icon.Right + 1F, top + 12F), new PointF(icon.Right - 4F, bottom + 1F), new PointF(cx, bottom - 2F));
                rightPath.AddLine(new PointF(cx, bottom - 2F), new PointF(cx, top + 2F));
                rightPath.CloseFigure();
                g.FillPath(fill, leftPath);
                g.FillPath(fill, rightPath);
                g.DrawPath(pen, leftPath);
                g.DrawPath(pen, rightPath);
                g.DrawBezier(pen, new PointF(cx - 3F, top + 4F), new PointF(cx - 8F, top + 5F), new PointF(cx - 2F, top + 8F), new PointF(cx - 6F, top + 10F));
                g.DrawBezier(pen, new PointF(cx - 7F, top + 12F), new PointF(cx - 3F, top + 12F), new PointF(cx - 7F, bottom - 3F), new PointF(cx - 3F, bottom - 4F));
                g.DrawBezier(pen, new PointF(cx + 3F, top + 4F), new PointF(cx + 8F, top + 5F), new PointF(cx + 2F, top + 8F), new PointF(cx + 6F, top + 10F));
                g.DrawBezier(pen, new PointF(cx + 7F, top + 12F), new PointF(cx + 3F, top + 12F), new PointF(cx + 7F, bottom - 3F), new PointF(cx + 3F, bottom - 4F));
            }
        }

        private int HitTest(Point point)
        {
            Rectangle[] bounds = GetItemBounds();
            for (int i = 0; i < bounds.Length; i++)
            {
                if (bounds[i].Contains(point)) return i;
            }
            return -1;
        }

        private bool IsActionEnabled(int index)
        {
            return index >= 0 && index < actions.Length && (actions[index] == null || actions[index].Enabled);
        }

        private void RaiseAction(int index)
        {
            EventHandler handler = index == 0 ? MemoryClicked : (index == 1 ? SettingsClicked : AppearanceClicked);
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }

    internal sealed class VoiceStatusLabel : Label
    {
        public VoiceStatusLabel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            Color backfill = Color.FromArgb(255, 250, 254, 255);
            var glassParent = Parent as GlassPanel;
            if (glassParent != null) backfill = glassParent.FillColor;
            using (var brush = new SolidBrush(backfill))
            {
                pevent.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color backfill = Color.FromArgb(255, 250, 254, 255);
            var glassParent = Parent as GlassPanel;
            if (glassParent != null) backfill = glassParent.FillColor;
            using (var clearBrush = new SolidBrush(backfill))
            {
                g.FillRectangle(clearBrush, ClientRectangle);
            }
            string[] lines = (Text ?? "").Replace("\r", "").Split('\n');
            string title = lines.Length > 0 ? lines[0] : "";
            string sub = lines.Length > 1 ? lines[1] : "";
            using (var titleFont = new Font("Microsoft YaHei UI", 9.2F, FontStyle.Bold))
            using (var subFont = new Font("Microsoft YaHei UI", 8F, FontStyle.Regular))
            {
                TextRenderer.DrawText(
                    g,
                    title,
                    titleFont,
                    new Rectangle(0, 0, Width, 22),
                    Theme.TextMain,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                TextRenderer.DrawText(
                    g,
                    sub,
                    subFont,
                    new Rectangle(0, 20, Width, 20),
                    Theme.TextSub,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }
    }

    internal sealed class GlassButton : Button
    {
        private bool hover;
        private bool pressed;

        public bool Accent { get; set; }
        public Color TextColor { get; set; }
        public int Radius { get; set; }
        public bool PaintChrome { get; set; }
        public bool CircularChrome { get; set; }
        public bool MinimalChrome { get; set; }
        public bool OpaqueBackfill { get; set; }
        public string SecondaryText { get; set; }

        public GlassButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.Selectable, false);
            FlatStyle = FlatStyle.Flat;
            UseVisualStyleBackColor = false;
            BackColor = Color.FromArgb(240, 252, 255);
            TextColor = Theme.TextMain;
            Radius = 16;
            PaintChrome = true;
            CircularChrome = false;
            MinimalChrome = false;
            OpaqueBackfill = false;
            SecondaryText = "";
            TabStop = false;
        }

        protected override bool ShowFocusCues
        {
            get { return false; }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            if (MinimalChrome)
            {
                base.OnPaintBackground(pevent);
                return;
            }
            if (OpaqueBackfill)
            {
                Color backfill = Color.FromArgb(255, 250, 254, 255);
                var glassParent = Parent as GlassPanel;
                if (glassParent != null) backfill = Color.FromArgb(255, glassParent.FillColor.R, glassParent.FillColor.G, glassParent.FillColor.B);
                using (var brush = new SolidBrush(backfill))
                {
                    pevent.Graphics.FillRectangle(brush, ClientRectangle);
                }
                return;
            }
            if (CircularChrome)
            {
                Color backfill = Color.FromArgb(255, 250, 254, 255);
                var glassParent = Parent as GlassPanel;
                if (glassParent != null) backfill = Color.FromArgb(255, glassParent.FillColor.R, glassParent.FillColor.G, glassParent.FillColor.B);
                using (var brush = new SolidBrush(backfill))
                {
                    pevent.Graphics.FillRectangle(brush, ClientRectangle);
                }
                return;
            }
            if (!PaintChrome && UiAssetStore.PaintShellSlice(pevent.Graphics, this)) return;
            base.OnPaintBackground(pevent);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            pressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (MinimalChrome)
            {
                DrawMinimalChrome(g);
                return;
            }
            if (OpaqueBackfill && !CircularChrome)
            {
                Color backfill = Color.FromArgb(255, 250, 254, 255);
                var glassParent = Parent as GlassPanel;
                if (glassParent != null) backfill = Color.FromArgb(255, glassParent.FillColor.R, glassParent.FillColor.G, glassParent.FillColor.B);
                using (var brush = new SolidBrush(backfill))
                {
                    g.FillRectangle(brush, ClientRectangle);
                }
            }
            Rectangle rect = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            if (CircularChrome)
            {
                DrawCircularChrome(g, rect);
                return;
            }
            if (!PaintChrome)
            {
                UiAssetStore.PaintShellSlice(g, this);
                if (hover || pressed)
                {
                    using (var glow = new SolidBrush(Color.FromArgb(pressed ? 44 : 28, 72, 206, 222)))
                    {
                        g.FillRoundedRectangle(glow, rect, Radius);
                    }
                }
                if (!string.IsNullOrEmpty(Text))
                {
                    DrawButtonContent(g, rect, TextColor);
                }
                return;
            }
            Color top = Accent ? Color.FromArgb(228, 99, 216, 226) : Color.FromArgb(238, 255, 255, 255);
            Color bottom = Accent ? Color.FromArgb(230, 46, 176, 204) : Color.FromArgb(218, 229, 247, 252);
            if (!Enabled)
            {
                top = Color.FromArgb(190, 234, 242, 246);
                bottom = Color.FromArgb(180, 218, 232, 238);
            }
            else if (pressed)
            {
                top = Accent ? Color.FromArgb(238, 38, 158, 188) : Color.FromArgb(234, 218, 244, 250);
                bottom = Accent ? Color.FromArgb(238, 32, 138, 168) : Color.FromArgb(220, 206, 236, 244);
            }
            else if (hover)
            {
                top = Accent ? Color.FromArgb(238, 118, 224, 232) : Color.FromArgb(248, 255, 255, 255);
                bottom = Accent ? Color.FromArgb(236, 64, 190, 214) : Color.FromArgb(232, 236, 252, 255);
            }

            using (var shadow = new SolidBrush(Color.FromArgb(18, 38, 128, 160)))
            {
                Rectangle shadowRect = new Rectangle(
                    rect.X + 1,
                    rect.Y + 2,
                    Math.Max(1, rect.Width - 2),
                    Math.Max(1, rect.Height - 3));
                g.FillRoundedRectangle(shadow, shadowRect, Math.Max(2, Radius - 1));
            }
            using (var brush = new LinearGradientBrush(rect, top, bottom, 90F))
            {
                g.FillRoundedRectangle(brush, rect, Radius);
            }
            using (var pen = new Pen(Accent ? Color.FromArgb(145, 114, 225, 232) : Theme.Border, 1F))
            {
                g.DrawRoundedRectangle(pen, rect, Radius);
            }

            Color text = Enabled ? TextColor : Color.FromArgb(128, 120, 145, 156);
            DrawButtonContent(g, rect, text);
        }

        private void DrawMinimalChrome(Graphics g)
        {
            Rectangle hoverRect = new Rectangle(2, 3, Math.Max(1, Width - 5), Math.Max(1, Height - 6));
            if (hover || pressed)
            {
                Color hoverColor = pressed ? Color.FromArgb(178, 204, 237, 245) : Color.FromArgb(142, 220, 244, 249);
                using (var hoverBrush = new SolidBrush(hoverColor))
                {
                    g.FillRoundedRectangle(hoverBrush, hoverRect, 8);
                }
            }
            Color iconColor = Enabled ? Color.FromArgb(52, 91, 121) : Color.FromArgb(126, 145, 158);
            DrawButtonContent(g, ClientRectangle, iconColor);
        }

        private void DrawCircularChrome(Graphics g, Rectangle rect)
        {
            Color backfill = Color.FromArgb(255, 250, 254, 255);
            var glassParent = Parent as GlassPanel;
            if (glassParent != null) backfill = Color.FromArgb(255, glassParent.FillColor.R, glassParent.FillColor.G, glassParent.FillColor.B);
            using (var clearBrush = new SolidBrush(backfill))
            {
                g.FillRectangle(clearBrush, ClientRectangle);
            }

            int size = Math.Min(rect.Width, rect.Height) - 2;
            Rectangle circle = new Rectangle(
                rect.X + (rect.Width - size) / 2,
                rect.Y + (rect.Height - size) / 2,
                size,
                size);

            Color top = hover ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 255, 255, 255);
            Color bottom = pressed ? Color.FromArgb(255, 199, 241, 248) : Color.FromArgb(255, 226, 248, 252);
            using (var shadow = new SolidBrush(Color.FromArgb(24, 46, 128, 160)))
            {
                g.FillEllipse(shadow, new Rectangle(circle.X + 1, circle.Y + 3, circle.Width, circle.Height));
            }
            using (var brush = new LinearGradientBrush(circle, top, bottom, 90F))
            {
                g.FillEllipse(brush, circle);
            }
            using (var pen = new Pen(Color.FromArgb(150, 176, 225, 236), 1F))
            {
                g.DrawEllipse(pen, circle);
            }
            if (string.Equals(AccessibleDescription, "voice-play", StringComparison.OrdinalIgnoreCase))
            {
                using (var play = new SolidBrush(Color.FromArgb(76, 196, 211)))
                {
                    PointF[] triangle =
                    {
                        new PointF(circle.X + circle.Width * 0.42F, circle.Y + circle.Height * 0.32F),
                        new PointF(circle.X + circle.Width * 0.42F, circle.Y + circle.Height * 0.68F),
                        new PointF(circle.X + circle.Width * 0.70F, circle.Y + circle.Height * 0.50F)
                    };
                    g.FillPolygon(play, triangle);
                }
                return;
            }

            TextRenderer.DrawText(
                g,
                Text ?? "",
                Font,
                circle,
                Color.FromArgb(52, 105, 137),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }

        private void DrawButtonContent(Graphics g, Rectangle rect, Color text)
        {
            string quickIcon = AccessibleDescription ?? "";
            if (string.Equals(quickIcon, "composer-send", StringComparison.OrdinalIgnoreCase))
            {
                DrawPaperPlane(g, rect, Accent ? Color.White : Color.FromArgb(64, 166, 196));
                return;
            }
            if (string.Equals(quickIcon, "voice-play", StringComparison.OrdinalIgnoreCase))
            {
                Color iconColor = Accent ? Color.White : Color.FromArgb(72, 184, 205);
                using (var brush = new SolidBrush(iconColor))
                {
                    PointF[] triangle =
                    {
                        new PointF(rect.X + rect.Width * 0.42F, rect.Y + rect.Height * 0.32F),
                        new PointF(rect.X + rect.Width * 0.42F, rect.Y + rect.Height * 0.68F),
                        new PointF(rect.X + rect.Width * 0.69F, rect.Y + rect.Height * 0.50F)
                    };
                    g.FillPolygon(brush, triangle);
                }
                return;
            }
            if (quickIcon.StartsWith("quick-", StringComparison.OrdinalIgnoreCase) && rect.Width >= 92)
            {
                Rectangle iconRect = new Rectangle(rect.X + 11, rect.Y + Math.Max(4, (rect.Height - 20) / 2), 20, 20);
                DrawFunctionalIcon(g, iconRect, quickIcon.Substring(6));
                Rectangle textRect = new Rectangle(rect.X + 38, rect.Y, Math.Max(20, rect.Width - 45), rect.Height);
                if (!string.IsNullOrWhiteSpace(SecondaryText) && rect.Height >= 38)
                {
                    int contentHeight = 34;
                    int contentTop = rect.Y + Math.Max(1, (rect.Height - contentHeight) / 2);
                    TextRenderer.DrawText(
                        g,
                        Text,
                        Font,
                        new Rectangle(textRect.X, contentTop, textRect.Width, 17),
                        text,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                    using (var secondaryFont = new Font("Microsoft YaHei UI", 6.7F, FontStyle.Regular))
                    {
                        TextRenderer.DrawText(
                            g,
                            SecondaryText,
                            secondaryFont,
                            new Rectangle(textRect.X, contentTop + 17, textRect.Width, 17),
                            Color.FromArgb(96, 132, 153),
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                    }
                }
                else
                {
                    TextRenderer.DrawText(
                        g,
                        Text,
                        Font,
                        textRect,
                        text,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }
                return;
            }
            if (quickIcon.StartsWith("sidebar-", StringComparison.OrdinalIgnoreCase))
            {
                DrawSidebarButtonContent(g, rect, quickIcon.Substring(8), text);
                return;
            }
            if (quickIcon.StartsWith("tool-", StringComparison.OrdinalIgnoreCase))
            {
                DrawToolButtonContent(g, rect, quickIcon.Substring(5), text);
                return;
            }

            TextRenderer.DrawText(
                g,
                Text,
                Font,
                rect,
                text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void DrawPaperPlane(Graphics g, Rectangle rect, Color color)
        {
            float size = Math.Min(rect.Width, rect.Height) * 0.46F;
            float cx = rect.X + rect.Width * 0.50F;
            float cy = rect.Y + rect.Height * 0.50F;
            PointF leftTop = new PointF(cx - size * 0.66F, cy - size * 0.18F);
            PointF tip = new PointF(cx + size * 0.64F, cy - size * 0.54F);
            PointF leftBottom = new PointF(cx + size * 0.14F, cy + size * 0.64F);
            PointF fold = new PointF(cx - size * 0.04F, cy + size * 0.12F);
            PointF tail = new PointF(cx - size * 0.32F, cy + size * 0.28F);
            using (var path = new GraphicsPath())
            {
                path.StartFigure();
                path.AddLine(leftTop, tip);
                path.AddLine(tip, leftBottom);
                path.AddLine(leftBottom, fold);
                path.AddLine(fold, tail);
                path.AddLine(tail, leftTop);
                path.CloseFigure();
                using (var shadow = new SolidBrush(Color.FromArgb(34, 26, 115, 148)))
                using (var brush = new SolidBrush(color))
                {
                    GraphicsState state = g.Save();
                    g.TranslateTransform(0F, 1.5F);
                    g.FillPath(shadow, path);
                    g.Restore(state);
                    g.FillPath(brush, path);
                }
            }
            using (var foldPen = new Pen(Color.FromArgb(132, 27, 148, 178), 1F))
            {
                foldPen.StartCap = LineCap.Round;
                foldPen.EndCap = LineCap.Round;
                g.DrawLine(foldPen, fold, tip);
            }
        }

        private void DrawSidebarButtonContent(Graphics g, Rectangle rect, string kind, Color text)
        {
            string glyph = string.Equals(kind, "delete", StringComparison.OrdinalIgnoreCase) ? "\uE74D" : "\uE74E";
            using (var iconFont = new Font("Segoe Fluent Icons", 9.5F, FontStyle.Regular))
            {
                TextRenderer.DrawText(
                    g,
                    glyph,
                    iconFont,
                    new Rectangle(rect.X + 5, rect.Y, 22, rect.Height),
                    Color.FromArgb(64, 142, 180),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
            TextRenderer.DrawText(
                g,
                Text ?? "",
                Font,
                new Rectangle(rect.X + 27, rect.Y, Math.Max(24, rect.Width - 31), rect.Height),
                text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void DrawToolButtonContent(Graphics g, Rectangle rect, string kind, Color text)
        {
            if (string.Equals(kind, "memory", StringComparison.OrdinalIgnoreCase))
            {
                DrawMemoryToolIcon(g, new Rectangle(rect.X + rect.Width / 2 - 12, rect.Y + 9, 24, 24));
            }
            else
            {
                string glyph = string.Equals(kind, "appearance", StringComparison.OrdinalIgnoreCase) ? "\uE790" : "\uE713";
                using (var iconFont = new Font("Segoe Fluent Icons", 15F, FontStyle.Regular))
                {
                    TextRenderer.DrawText(
                        g,
                        glyph,
                        iconFont,
                        new Rectangle(rect.X, rect.Y + 7, rect.Width, Math.Max(26, rect.Height - 32)),
                        Color.FromArgb(56, 137, 181),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPadding);
                }
            }
            using (var labelFont = new Font("Microsoft YaHei UI", 8.2F, FontStyle.Bold))
            {
                TextRenderer.DrawText(
                    g,
                    Text ?? "",
                    labelFont,
                    new Rectangle(rect.X + 2, rect.Bottom - 27, Math.Max(20, rect.Width - 4), 22),
                    text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        private void DrawMemoryToolIcon(Graphics g, Rectangle icon)
        {
            RectangleF left = new RectangleF(icon.X + 2F, icon.Y + 2F, 10F, 19F);
            RectangleF right = new RectangleF(icon.X + 12F, icon.Y + 2F, 10F, 19F);
            using (var pen = new Pen(Color.FromArgb(56, 137, 181), 1.45F))
            using (var fill = new SolidBrush(Color.FromArgb(22, 76, 190, 214)))
            using (var leftPath = new GraphicsPath())
            using (var rightPath = new GraphicsPath())
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                leftPath.AddBezier(left.Right, left.Top + 2F, left.Left + 4F, left.Top - 1F, left.Left, left.Top + 5F, left.Left + 2F, left.Top + 9F);
                leftPath.AddBezier(left.Left + 2F, left.Top + 9F, left.Left - 1F, left.Top + 13F, left.Left + 3F, left.Bottom + 1F, left.Right, left.Bottom - 2F);
                leftPath.AddLine(new PointF(left.Right, left.Bottom - 2F), new PointF(left.Right, left.Top + 2F));
                rightPath.AddBezier(right.Left, right.Top + 2F, right.Right - 4F, right.Top - 1F, right.Right, right.Top + 5F, right.Right - 2F, right.Top + 9F);
                rightPath.AddBezier(right.Right - 2F, right.Top + 9F, right.Right + 1F, right.Top + 13F, right.Right - 3F, right.Bottom + 1F, right.Left, right.Bottom - 2F);
                rightPath.AddLine(new PointF(right.Left, right.Bottom - 2F), new PointF(right.Left, right.Top + 2F));
                g.FillPath(fill, leftPath);
                g.FillPath(fill, rightPath);
                g.DrawPath(pen, leftPath);
                g.DrawPath(pen, rightPath);
                g.DrawLine(pen, icon.X + 12F, icon.Y + 4F, icon.X + 12F, icon.Bottom - 4F);
                g.DrawArc(pen, icon.X + 5, icon.Y + 6, 6, 6, 205, 170);
                g.DrawArc(pen, icon.X + 13, icon.Y + 10, 6, 6, 25, 170);
            }
        }

        private void DrawFunctionalIcon(Graphics g, Rectangle bounds, string kind)
        {
            Color line = Accent ? Color.White : Color.FromArgb(72, 184, 205);
            using (var pen = new Pen(line, 1.8F))
            using (var fill = new SolidBrush(Color.FromArgb(34, line)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                if (string.Equals(kind, "chat", StringComparison.OrdinalIgnoreCase))
                {
                    Rectangle bubble = new Rectangle(bounds.X + 1, bounds.Y + 3, bounds.Width - 3, bounds.Height - 6);
                    g.FillRoundedRectangle(fill, bubble, 8);
                    g.DrawRoundedRectangle(pen, bubble, 8);
                    g.DrawLine(pen, bounds.X + 8, bounds.Bottom - 5, bounds.X + 5, bounds.Bottom - 1);
                    g.DrawLine(pen, bounds.X + 8, bounds.Bottom - 5, bounds.X + 12, bounds.Bottom - 5);
                    return;
                }

                if (string.Equals(kind, "plan", StringComparison.OrdinalIgnoreCase))
                {
                    Rectangle board = new Rectangle(bounds.X + 4, bounds.Y + 2, bounds.Width - 8, bounds.Height - 4);
                    g.FillRoundedRectangle(fill, board, 3);
                    g.DrawRoundedRectangle(pen, board, 3);
                    g.DrawLine(pen, bounds.X + 7, bounds.Y + 8, bounds.X + 10, bounds.Y + 11);
                    g.DrawLine(pen, bounds.X + 10, bounds.Y + 11, bounds.X + 14, bounds.Y + 6);
                    g.DrawLine(pen, bounds.X + 7, bounds.Y + 15, bounds.Right - 7, bounds.Y + 15);
                    return;
                }

                if (string.Equals(kind, "idea", StringComparison.OrdinalIgnoreCase))
                {
                    g.FillEllipse(fill, bounds.X + 5, bounds.Y + 2, bounds.Width - 10, bounds.Height - 8);
                    g.DrawEllipse(pen, bounds.X + 5, bounds.Y + 2, bounds.Width - 10, bounds.Height - 8);
                    g.DrawLine(pen, bounds.X + 8, bounds.Bottom - 5, bounds.Right - 8, bounds.Bottom - 5);
                    g.DrawLine(pen, bounds.X + 9, bounds.Bottom - 2, bounds.Right - 9, bounds.Bottom - 2);
                    g.DrawLine(pen, bounds.X + bounds.Width / 2, bounds.Y, bounds.X + bounds.Width / 2, bounds.Y - 2);
                    return;
                }

                if (string.Equals(kind, "review", StringComparison.OrdinalIgnoreCase))
                {
                    Rectangle arc = new Rectangle(bounds.X + 4, bounds.Y + 4, bounds.Width - 8, bounds.Height - 8);
                    g.DrawArc(pen, arc, 35, 285);
                    Point tip = new Point(bounds.Right - 4, bounds.Y + 7);
                    g.DrawLine(pen, tip.X, tip.Y, tip.X - 6, tip.Y);
                    g.DrawLine(pen, tip.X, tip.Y, tip.X - 2, tip.Y + 6);
                    return;
                }
            }
        }
    }

    internal sealed class GlassCheckBox : CheckBox
    {
        private bool hover;

        public GlassCheckBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.Selectable, false);
            Appearance = Appearance.Button;
            FlatStyle = FlatStyle.Flat;
            UseVisualStyleBackColor = false;
            TabStop = false;
        }

        protected override bool ShowFocusCues
        {
            get { return false; }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            Color backfill = Theme.AppBg;
            var glassParent = Parent as GlassPanel;
            if (glassParent != null)
            {
                backfill = Color.FromArgb(255, glassParent.FillColor.R, glassParent.FillColor.G, glassParent.FillColor.B);
            }
            else if (Parent != null && Parent.BackColor.A > 0)
            {
                backfill = Parent.BackColor;
            }
            using (var brush = new SolidBrush(backfill))
            {
                pevent.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }

        protected override void OnMouseEnter(EventArgs eventargs)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(eventargs);
        }

        protected override void OnMouseLeave(EventArgs eventargs)
        {
            hover = false;
            Invalidate();
            base.OnMouseLeave(eventargs);
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            Invalidate();
            base.OnCheckedChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 1, Math.Max(1, Width - 1), Math.Max(1, Height - 3));
            Color top = Checked ? Color.FromArgb(230, 216, 250, 255) : Color.FromArgb(232, 255, 255, 255);
            Color bottom = Checked ? Color.FromArgb(226, 184, 237, 246) : Color.FromArgb(218, 238, 249, 252);
            if (hover)
            {
                top = Checked ? Color.FromArgb(238, 204, 249, 255) : Color.FromArgb(246, 255, 255, 255);
                bottom = Checked ? Color.FromArgb(232, 164, 228, 240) : Color.FromArgb(230, 232, 248, 252);
            }

            using (var brush = new LinearGradientBrush(rect, top, bottom, 90F))
            using (var pen = new Pen(Checked ? Theme.BorderStrong : Theme.Border, 1F))
            {
                g.FillRoundedRectangle(brush, rect, 12);
                g.DrawRoundedRectangle(pen, rect, 12);
            }

            Rectangle mark = new Rectangle(12, rect.Y + rect.Height / 2 - 7, 14, 14);
            using (var markFill = new SolidBrush(Checked ? Theme.Primary : Color.FromArgb(230, 247, 252, 255)))
            using (var markPen = new Pen(Checked ? Color.FromArgb(150, 63, 190, 210) : Theme.Border, 1F))
            {
                g.FillEllipse(markFill, mark);
                g.DrawEllipse(markPen, mark);
            }
            if (Checked)
            {
                using (var checkPen = new Pen(Color.White, 2F))
                {
                    checkPen.StartCap = LineCap.Round;
                    checkPen.EndCap = LineCap.Round;
                    g.DrawLines(checkPen, new[]
                    {
                        new Point(mark.X + 4, mark.Y + 7),
                        new Point(mark.X + 7, mark.Y + 10),
                        new Point(mark.X + 11, mark.Y + 4)
                    });
                }
            }

            Rectangle textRect = new Rectangle(34, 0, Math.Max(1, Width - 38), Height);
            TextRenderer.DrawText(
                g,
                Text,
                Font,
                textRect,
                ForeColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }

    internal class VNBackgroundControl : Panel
    {
        private Image backgroundImage;

        public VNBackgroundControl()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Theme.AppBg;
            string path = FindBackgroundImagePath();
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    backgroundImage = Image.FromFile(path);
                }
                catch
                {
                    backgroundImage = null;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && backgroundImage != null)
            {
                backgroundImage.Dispose();
                backgroundImage = null;
            }
            base.Dispose(disposing);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            Rectangle rect = ClientRectangle;
            if (backgroundImage != null)
            {
                DrawCoverImage(g, backgroundImage, rect);
            }
            else
            {
                DrawFallbackRoom(g, rect);
            }
            using (var veil = new SolidBrush(Color.FromArgb(12, 255, 255, 255)))
            {
                g.FillRectangle(veil, rect);
            }
        }

        internal static string FindBackgroundImagePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, "assets", "ui", "vn-room-bg.png"),
                Path.GetFullPath(Path.Combine(baseDir, "..", "assets", "ui", "vn-room-bg.png")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "assets", "ui", "vn-room-bg.png")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "assets", "ui", "vn-room-bg.png")),
                Path.Combine(baseDir, "assets", "ui", "vn-ui-shell.png"),
                Path.GetFullPath(Path.Combine(baseDir, "..", "assets", "ui", "vn-ui-shell.png")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "assets", "ui", "vn-ui-shell.png")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "assets", "ui", "vn-ui-shell.png"))
            };
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }

        private void DrawCoverImage(Graphics g, Image image, Rectangle bounds)
        {
            if (image == null || bounds.Width <= 0 || bounds.Height <= 0) return;
            double scale = Math.Max(bounds.Width / (double)image.Width, bounds.Height / (double)image.Height);
            int width = Math.Max(1, (int)Math.Ceiling(image.Width * scale));
            int height = Math.Max(1, (int)Math.Ceiling(image.Height * scale));
            int x = bounds.X + (bounds.Width - width) / 2;
            int y = bounds.Y + (bounds.Height - height) / 2;
            g.DrawImage(image, new Rectangle(x, y, width, height));
        }

        private void DrawFallbackRoom(Graphics g, Rectangle rect)
        {
            using (var bg = new LinearGradientBrush(rect, Color.FromArgb(226, 241, 251), Color.FromArgb(250, 247, 252), 0F))
            {
                g.FillRectangle(bg, rect);
            }
            int w = rect.Width;
            int h = rect.Height;
            using (var light = new LinearGradientBrush(
                new Rectangle(Math.Max(0, w - 520), 0, Math.Max(1, 520), Math.Max(1, h)),
                Color.FromArgb(146, 219, 245, 255),
                Color.FromArgb(0, 255, 255, 255),
                180F))
            {
                g.FillRectangle(light, Math.Max(0, w - 520), 0, Math.Max(1, 520), h);
            }

            using (var floor = new SolidBrush(Color.FromArgb(218, 232, 238)))
            {
                g.FillPolygon(floor, new Point[] {
                    new Point(0, h),
                    new Point(w, h),
                    new Point(w, h - 150),
                    new Point(0, h - 72)
                });
            }

            using (var window = new SolidBrush(Color.FromArgb(210, 238, 252)))
            using (var frame = new Pen(Color.FromArgb(245, 252, 255), 8F))
            {
                Rectangle win = new Rectangle(w - 392, 92, 292, 226);
                g.FillRoundedRectangle(window, win, 18);
                g.DrawRoundedRectangle(frame, win, 18);
                g.DrawLine(frame, win.X + win.Width / 2, win.Y, win.X + win.Width / 2, win.Bottom);
                g.DrawLine(frame, win.X, win.Y + win.Height / 2, win.Right, win.Y + win.Height / 2);
                using (var sky = new LinearGradientBrush(win, Color.FromArgb(172, 224, 248), Color.FromArgb(242, 253, 255), 90F))
                {
                    g.FillRoundedRectangle(sky, new Rectangle(win.X + 8, win.Y + 8, win.Width - 16, win.Height - 16), 12);
                }
                g.DrawRoundedRectangle(frame, win, 18);
            }
            using (var curtain = new SolidBrush(Color.FromArgb(72, 176, 220, 238)))
            {
                g.FillPolygon(curtain, new Point[] { new Point(w - 438, 70), new Point(w - 366, 70), new Point(w - 386, h - 140), new Point(w - 470, h - 108) });
                g.FillPolygon(curtain, new Point[] { new Point(w - 118, 70), new Point(w - 38, 70), new Point(w - 70, h - 178), new Point(w - 158, h - 138) });
                using (var fold = new Pen(Color.FromArgb(58, 112, 182, 204), 2F))
                {
                    for (int i = 0; i < 5; i++)
                    {
                        g.DrawBezier(fold, w - 428 + i * 12, 88, w - 438 + i * 8, 190, w - 426 + i * 10, 330, w - 450 + i * 7, h - 128);
                        g.DrawBezier(fold, w - 100 + i * 10, 86, w - 88 + i * 6, 220, w - 116 + i * 8, 340, w - 120 + i * 7, h - 146);
                    }
                }
            }

            Rectangle shelfRect = new Rectangle(w / 2 - 430, 120, 340, 146);
            using (var shadow = new SolidBrush(Color.FromArgb(26, 60, 100, 130)))
            {
                g.FillRoundedRectangle(shadow, new Rectangle(shelfRect.X + 8, shelfRect.Y + 10, shelfRect.Width, shelfRect.Height), 14);
            }
            using (var shelf = new LinearGradientBrush(shelfRect, Color.FromArgb(185, 170, 150), Color.FromArgb(146, 128, 112), 90F))
            {
                g.FillRoundedRectangle(shelf, shelfRect, 14);
                using (var divider = new Pen(Color.FromArgb(120, 120, 98, 84), 3F))
                {
                    g.DrawLine(divider, shelfRect.Left + 18, shelfRect.Top + 70, shelfRect.Right - 18, shelfRect.Top + 70);
                }
                for (int i = 0; i < 11; i++)
                {
                    int bookH = 66 + (i % 3) * 10;
                    using (var book = new LinearGradientBrush(
                        new Rectangle(shelfRect.Left + 24 + i * 24, shelfRect.Top + 30, 17, bookH),
                        Color.FromArgb(116 + i * 8, 184, 208),
                        Color.FromArgb(82 + i * 6, 132, 172),
                        90F))
                    {
                        g.FillRoundedRectangle(book, new Rectangle(shelfRect.Left + 24 + i * 24, shelfRect.Top + 28, 17, bookH), 3);
                    }
                }
            }

            Rectangle board = new Rectangle(w / 2 - 38, 106, 210, 170);
            using (var boardShadow = new SolidBrush(Color.FromArgb(24, 60, 100, 130)))
            using (var boardFill = new SolidBrush(Color.FromArgb(208, 202, 190)))
            using (var boardPen = new Pen(Color.FromArgb(170, 150, 132), 7F))
            {
                g.FillRoundedRectangle(boardShadow, new Rectangle(board.X + 8, board.Y + 10, board.Width, board.Height), 12);
                g.FillRoundedRectangle(boardFill, board, 12);
                g.DrawRoundedRectangle(boardPen, board, 12);
                using (var noteBrush = new SolidBrush(Color.FromArgb(210, 232, 246, 252)))
                using (var pinkNote = new SolidBrush(Color.FromArgb(220, 250, 222, 232)))
                {
                    g.FillRectangle(noteBrush, board.X + 24, board.Y + 24, 54, 44);
                    g.FillRectangle(pinkNote, board.X + 100, board.Y + 30, 66, 52);
                    g.FillRectangle(noteBrush, board.X + 50, board.Y + 96, 74, 46);
                }
            }

            Rectangle desk = new Rectangle(w - 540, h - 245, 420, 104);
            using (var deskBrush = new LinearGradientBrush(desk, Color.FromArgb(232, 226, 222), Color.FromArgb(206, 214, 218), 90F))
            {
                g.FillRoundedRectangle(deskBrush, desk, 30);
            }
            using (var laptop = new SolidBrush(Color.FromArgb(128, 154, 174)))
            using (var screen = new SolidBrush(Color.FromArgb(202, 230, 242)))
            {
                g.FillRoundedRectangle(laptop, new Rectangle(w - 424, h - 320, 148, 92), 8);
                g.FillRoundedRectangle(screen, new Rectangle(w - 414, h - 310, 128, 72), 6);
                g.FillRectangle(laptop, w - 444, h - 222, 188, 12);
            }
            using (var lamp = new SolidBrush(Color.FromArgb(246, 251, 252)))
            using (var lampLine = new Pen(Color.FromArgb(168, 190, 202), 4F))
            {
                g.DrawLine(lampLine, w - 184, h - 330, w - 214, h - 238);
                g.DrawLine(lampLine, w - 214, h - 238, w - 186, h - 210);
                g.FillEllipse(lamp, w - 220, h - 350, 70, 44);
            }
            using (var plant = new SolidBrush(Color.FromArgb(96, 184, 150)))
            {
                for (int i = 0; i < 6; i++)
                {
                    g.FillEllipse(plant, w - 78 - i * 9, h - 252 - (i % 3) * 12, 26, 14);
                }
            }

            using (var sunbeam = new LinearGradientBrush(
                new Rectangle(w - 440, 90, 420, h - 100),
                Color.FromArgb(70, 255, 255, 255),
                Color.FromArgb(0, 255, 255, 255),
                150F))
            {
                Point[] beam =
                {
                    new Point(w - 326, 120),
                    new Point(w - 10, 118),
                    new Point(w - 128, h - 132),
                    new Point(w - 540, h - 88)
                };
                g.FillPolygon(sunbeam, beam);
            }

            using (var petal = new SolidBrush(Color.FromArgb(150, 242, 181, 202)))
            {
                for (int i = 0; i < 18; i++)
                {
                    int x = 260 + (i * 117) % Math.Max(320, w - 340);
                    int y = 82 + (i * 71) % Math.Max(280, h - 340);
                    g.TranslateTransform(x, y);
                    g.RotateTransform((i * 23) % 90 - 45);
                    g.FillEllipse(petal, 0, 0, 14, 7);
                    g.ResetTransform();
                }
            }
        }
    }

    internal sealed class TopBarControl : Control
    {
        private string hoverWindowButton = "";
        private string pressedWindowButton = "";
        private string modelName = "deepseek-v4-flash";
        public bool PaintBarChrome { get; set; }
        internal Rectangle LastTitleBounds { get; private set; }
        internal Rectangle LastModelBadgeBounds { get; private set; }

        public string ModelName
        {
            get { return modelName; }
            set
            {
                string next = string.IsNullOrWhiteSpace(value) ? "deepseek-v4-flash" : value.Trim();
                if (string.Equals(modelName, next, StringComparison.OrdinalIgnoreCase)) return;
                modelName = next;
                Invalidate();
            }
        }

        public TopBarControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Opaque, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, false);
            BackColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            PaintBarChrome = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = ClientRectangle;
            if (PaintBarChrome)
            {
                using (var bg = new LinearGradientBrush(rect, Color.FromArgb(255, 255, 255, 255), Color.FromArgb(255, 232, 247, 252), 0F))
                {
                    g.FillRectangle(bg, rect);
                }
                using (var pen = new Pen(Color.FromArgb(128, 172, 218, 232)))
                {
                    g.DrawLine(pen, 0, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
                }
            }
            DrawLeafLogo(g, 36, rect.Height / 2 + 1);
            const string title = "彩叶 Iroha Agent";
            int titleX = 67;
            using (var titleFont = new Font("Microsoft YaHei UI", 13.5F, FontStyle.Bold))
            {
                Size measuredTitle = TextRenderer.MeasureText(
                    g,
                    title,
                    titleFont,
                    new Size(420, Math.Max(1, rect.Height)),
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
                int titleWidth = Math.Max(156, measuredTitle.Width + 2);
                LastTitleBounds = new Rectangle(titleX, 0, titleWidth, rect.Height);
                TextRenderer.DrawText(
                    g,
                    title,
                    titleFont,
                    LastTitleBounds,
                    Theme.TextMain,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            }
            string modelBadge = GetModelBadgeText();
            int measuredBadgeWidth = TextRenderer.MeasureText(
                g,
                modelBadge,
                Font,
                new Size(100, 26),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
            int badgeWidth = Math.Max(38, Math.Min(58, measuredBadgeWidth + 20));
            LastModelBadgeBounds = new Rectangle(LastTitleBounds.Right + 12, 15, badgeWidth, 26);
            DrawChip(g, LastModelBadgeBounds.X, LastModelBadgeBounds.Y, LastModelBadgeBounds.Width, modelBadge, Theme.Primary);
            DrawOnlineChip(g, LastModelBadgeBounds.Right + 18, 13, 80);
            DrawWindowButtons(g, rect);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            string next = WindowButtonAt(e.Location);
            if (!string.Equals(next, hoverWindowButton, StringComparison.Ordinal))
            {
                hoverWindowButton = next;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverWindowButton = "";
            pressedWindowButton = "";
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            pressedWindowButton = e.Button == MouseButtons.Left ? WindowButtonAt(e.Location) : "";
            if (pressedWindowButton.Length > 0) Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            pressedWindowButton = "";
            Invalidate();
            base.OnMouseUp(e);
        }

        private void DrawLeafLogo(Graphics g, int x, int y)
        {
            using (var stem = new Pen(Color.FromArgb(116, 159, 193), 1.15F))
            {
                stem.StartCap = LineCap.Round;
                stem.EndCap = LineCap.Round;
                g.DrawLine(stem, x, y + 13, x, y - 10);
            }
            FillLeafPetal(g, new PointF(x, y + 10), new PointF(x, y - 23), 7.4F, Color.FromArgb(126, 181, 223));
            FillLeafPetal(g, new PointF(x - 1, y + 8), new PointF(x - 17, y - 15), 6.4F, Color.FromArgb(127, 220, 224));
            FillLeafPetal(g, new PointF(x + 1, y + 8), new PointF(x + 17, y - 15), 6.4F, Color.FromArgb(103, 201, 220));
            FillLeafPetal(g, new PointF(x - 1, y + 12), new PointF(x - 13, y - 2), 5.2F, Color.FromArgb(163, 230, 230));
            FillLeafPetal(g, new PointF(x + 1, y + 12), new PointF(x + 13, y - 2), 5.2F, Color.FromArgb(141, 190, 224));
        }

        private void FillLeafPetal(Graphics g, PointF root, PointF tip, float halfWidth, Color color)
        {
            float dx = tip.X - root.X;
            float dy = tip.Y - root.Y;
            float length = (float)Math.Max(1D, Math.Sqrt(dx * dx + dy * dy));
            float nx = -dy / length;
            float ny = dx / length;
            PointF mid = new PointF(root.X + dx * 0.54F, root.Y + dy * 0.54F);
            using (var path = new GraphicsPath())
            {
                path.StartFigure();
                path.AddBezier(
                    root,
                    new PointF(mid.X + nx * halfWidth, mid.Y + ny * halfWidth),
                    new PointF(tip.X + nx * halfWidth * 0.25F, tip.Y + ny * halfWidth * 0.25F),
                    tip);
                path.AddBezier(
                    tip,
                    new PointF(tip.X - nx * halfWidth * 0.25F, tip.Y - ny * halfWidth * 0.25F),
                    new PointF(mid.X - nx * halfWidth, mid.Y - ny * halfWidth),
                    root);
                path.CloseFigure();
                using (var brush = new SolidBrush(color))
                using (var edge = new Pen(Color.FromArgb(92, 255, 255, 255), 0.8F))
                {
                    g.FillPath(brush, path);
                    g.DrawPath(edge, path);
                }
            }
        }

        private void DrawChip(Graphics g, int x, int y, int width, string text, Color fill)
        {
            Rectangle chip = new Rectangle(x, y, width, 26);
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(Color.FromArgb(152, 186, 226, 236)))
            {
                g.FillRoundedRectangle(brush, chip, 13);
                g.DrawRoundedRectangle(pen, chip, 13);
            }
            Color color = fill == Theme.Primary ? Color.White : Theme.TextMain;
            TextRenderer.DrawText(g, text, Font, chip, color, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private string GetModelBadgeText()
        {
            string normalized = (modelName ?? "").Replace("_", "-").ToLowerInvariant();
            if (normalized.Contains("v4-flash") || normalized.Contains("v4flash") || normalized.EndsWith("flash", StringComparison.Ordinal)) return "Flash";
            if (normalized.Contains("v4-pro") || normalized.Contains("v4pro") || normalized.EndsWith("pro", StringComparison.Ordinal)) return "Pro";
            if (normalized.Contains("reasoner")) return "R1";
            if (normalized.Contains("chat")) return "Chat";
            return "AI";
        }

        private void DrawOnlineChip(Graphics g, int x, int y, int width)
        {
            Rectangle chip = new Rectangle(x, y, width, 26);
            using (var fill = new LinearGradientBrush(chip, Color.FromArgb(246, 253, 255), Color.FromArgb(218, 245, 250), 90F))
            using (var border = new Pen(Color.FromArgb(126, 184, 226, 236), 1F))
            {
                g.FillRoundedRectangle(fill, chip, 13);
                g.DrawRoundedRectangle(border, chip, 13);
            }
            using (var glow = new SolidBrush(Color.FromArgb(38, Theme.OnlineGreen)))
            using (var dot = new SolidBrush(Theme.OnlineGreen))
            {
                g.FillEllipse(glow, chip.X + 13, chip.Y + 7, 12, 12);
                g.FillEllipse(dot, chip.X + 15, chip.Y + 9, 8, 8);
            }
            TextRenderer.DrawText(
                g,
                "在线",
                Font,
                new Rectangle(chip.X + 28, chip.Y, 33, chip.Height),
                Theme.TextMain,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            using (var chevron = new Pen(Color.FromArgb(92, 135, 163), 1.2F))
            {
                chevron.StartCap = LineCap.Round;
                chevron.EndCap = LineCap.Round;
                int cx = chip.Right - 12;
                int cy = chip.Y + chip.Height / 2;
                g.DrawLine(chevron, cx - 3, cy - 1, cx, cy + 2);
                g.DrawLine(chevron, cx, cy + 2, cx + 3, cy - 1);
            }
        }

        private void DrawWindowButtons(Graphics g, Rectangle rect)
        {
            int size = 32;
            int y = Math.Max(8, (int)Math.Round(rect.Height * 18.0 / 70.0));
            int closeX = rect.Width - 18 - size;
            int maxX = closeX - 14 - size;
            int minX = maxX - 14 - size;
            int settingsX = minX - 30 - size;
            DrawRoundIconButton(g, new Rectangle(settingsX, y, size, size), "settings");
            DrawRoundIconButton(g, new Rectangle(minX, y, size, size), "min");
            DrawRoundIconButton(g, new Rectangle(maxX, y, size, size), "max");
            DrawRoundIconButton(g, new Rectangle(closeX, y, size, size), "close");
        }

        private string WindowButtonAt(Point point)
        {
            int size = 32;
            int y = Math.Max(8, (int)Math.Round(Height * 18.0 / 70.0));
            int closeX = Width - 18 - size;
            int maxX = closeX - 14 - size;
            int minX = maxX - 14 - size;
            int settingsX = minX - 30 - size;
            if (new Rectangle(settingsX, y, size, size).Contains(point)) return "settings";
            if (new Rectangle(minX, y, size, size).Contains(point)) return "min";
            if (new Rectangle(maxX, y, size, size).Contains(point)) return "max";
            if (new Rectangle(closeX, y, size, size).Contains(point)) return "close";
            return "";
        }

        private void DrawRoundIconButton(Graphics g, Rectangle bounds, string kind)
        {
            bool hover = string.Equals(hoverWindowButton, kind, StringComparison.Ordinal);
            bool pressed = string.Equals(pressedWindowButton, kind, StringComparison.Ordinal);
            if (hover || pressed)
            {
                Color hoverFill = kind == "close" ?
                    Color.FromArgb(pressed ? 90 : 54, 230, 126, 147) :
                    Color.FromArgb(pressed ? 110 : 62, 80, 190, 214);
                using (var fill = new SolidBrush(hoverFill))
                {
                    g.FillRoundedRectangle(fill, new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4), 8);
                }
            }
            using (var iconPen = new Pen(Color.FromArgb(54, 96, 126), 1.65F))
            {
                iconPen.StartCap = LineCap.Round;
                iconPen.EndCap = LineCap.Round;
                iconPen.LineJoin = LineJoin.Round;
                int cx = bounds.X + bounds.Width / 2;
                int cy = bounds.Y + bounds.Height / 2;
                if (kind == "min")
                {
                    g.DrawLine(iconPen, bounds.X + 9, cy + 3, bounds.Right - 9, cy + 3);
                }
                else if (kind == "max")
                {
                    g.DrawRectangle(iconPen, bounds.X + 10, bounds.Y + 9, bounds.Width - 20, bounds.Height - 18);
                }
                else if (kind == "close")
                {
                    g.DrawLine(iconPen, bounds.X + 10, bounds.Y + 10, bounds.Right - 10, bounds.Bottom - 10);
                    g.DrawLine(iconPen, bounds.Right - 10, bounds.Y + 10, bounds.X + 10, bounds.Bottom - 10);
                }
                else
                {
                    PointF[] teeth = new PointF[16];
                    for (int i = 0; i < teeth.Length; i++)
                    {
                        double angle = -Math.PI / 2.0 + i * Math.PI / 8.0;
                        double radius = (i % 2 == 0) ? 10.0 : 7.8;
                        teeth[i] = new PointF(
                            cx + (float)(Math.Cos(angle) * radius),
                            cy + (float)(Math.Sin(angle) * radius));
                    }
                    g.DrawPolygon(iconPen, teeth);
                    g.DrawEllipse(iconPen, cx - 3, cy - 3, 6, 6);
                }
            }
        }
    }

    internal sealed class FooterBarControl : Control
    {
        public FooterBarControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (var fill = new SolidBrush(Color.FromArgb(218, 250, 254, 255)))
            using (var shine = new LinearGradientBrush(ClientRectangle, Color.FromArgb(104, 255, 255, 255), Color.FromArgb(24, 231, 248, 252), 0F))
            using (var line = new Pen(Color.FromArgb(118, 145, 213, 229), 1F))
            {
                g.FillRectangle(fill, ClientRectangle);
                g.FillRectangle(shine, ClientRectangle);
                g.DrawLine(line, 0, 0, Width, 0);
            }
        }
    }

    internal sealed class CharacterTopOverlayControl : Control
    {
        private readonly AvatarControl avatar;
        private readonly EventHandler animationFrameChangedHandler;

        public CharacterTopOverlayControl(AvatarControl avatarControl)
        {
            avatar = avatarControl;
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            animationFrameChangedHandler = delegate { Invalidate(); };
            avatar.AnimationFrameChanged += animationFrameChangedHandler;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                avatar.AnimationFrameChanged -= animationFrameChangedHandler;
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (avatar == null) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            avatar.DrawCharacterTopOverlay(e.Graphics, ClientRectangle);
        }
    }

    internal sealed class WaveformControl : Control
    {
        private readonly Timer timer;
        private int frame;
        private bool active;

        public bool Active
        {
            get { return active; }
            set
            {
                if (active == value) return;
                active = value;
                Invalidate();
            }
        }

        public WaveformControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            timer = new Timer();
            timer.Interval = 90;
            timer.Tick += delegate
            {
                frame++;
                Invalidate();
            };
            timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Stop();
                timer.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using (var bg = new SolidBrush(Color.FromArgb(52, 232, 249, 252)))
            {
                g.FillRoundedRectangle(bg, rect, 14);
            }
            int bars = Math.Max(10, Width / 10);
            float step = Width / (float)bars;
            for (int i = 0; i < bars; i++)
            {
                double wave = Math.Sin((frame + i * 1.7) / 2.2);
                int barHeight = active ? (int)(8 + (wave + 1) * (Height - 14) / 2.3) : 8 + (i % 4) * 3;
                int x = (int)(i * step + 2);
                int y = (Height - barHeight) / 2;
                using (var brush = new SolidBrush(Color.FromArgb(active ? 204 : 122, 74, 196, 214)))
                {
                    g.FillRoundedRectangle(brush, new Rectangle(x, y, Math.Max(3, (int)step - 4), barHeight), 3);
                }
            }
        }
    }

    internal sealed class CompressionStatusControl : Control
    {
        private bool active;
        private int percent;

        public CompressionStatusControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public void SetState(bool enabled, int value)
        {
            active = enabled;
            percent = Math.Max(0, Math.Min(100, value));
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int topLineHeight = Math.Min(16, Math.Max(12, Height / 3));
            string status = active ? "智能总结中..." : "当前未启用";
            string caption = active ? "预计减少约 30% Token" : "开启后自动精简上下文";

            using (var statusFont = new Font("Microsoft YaHei UI", 7.1F, FontStyle.Regular))
            using (var captionFont = new Font("Microsoft YaHei UI", 6.6F, FontStyle.Regular))
            {
                TextRenderer.DrawText(
                    g,
                    status,
                    statusFont,
                    new Rectangle(0, 0, Math.Max(48, Width - 40), topLineHeight),
                    Theme.TextSub,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                TextRenderer.DrawText(
                    g,
                    percent + "%",
                    statusFont,
                    new Rectangle(Math.Max(0, Width - 38), 0, 38, topLineHeight),
                    Theme.TextMain,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

                int barY = Math.Min(17, topLineHeight + 1);
                Rectangle track = new Rectangle(0, barY, Math.Max(1, Width), 5);
                using (var trackBrush = new SolidBrush(Color.FromArgb(118, 210, 227, 234)))
                using (var fillBrush = new LinearGradientBrush(track, Color.FromArgb(74, 213, 205), Color.FromArgb(80, 190, 225), 0F))
                {
                    g.FillRoundedRectangle(trackBrush, track, 3);
                    int fillWidth = active ? Math.Max(8, (int)Math.Round(track.Width * percent / 100.0)) : 0;
                    if (fillWidth > 0)
                    {
                        g.FillRoundedRectangle(fillBrush, new Rectangle(track.X, track.Y, fillWidth, track.Height), 3);
                    }
                }

                int captionY = barY + 7;
                if (captionY < Height)
                {
                    TextRenderer.DrawText(
                        g,
                        caption,
                        captionFont,
                        new Rectangle(0, captionY, Math.Max(1, Width), Math.Max(10, Height - captionY)),
                        Color.FromArgb(108, 145, 164),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                }
            }
        }
    }

    internal sealed class ServiceStatusControl : Control
    {
        private bool deepSeekReady;
        private bool voiceReady;
        private bool voiceEnabled;

        public ServiceStatusControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        public void SetState(bool apiReady, bool localVoiceReady, bool localVoiceEnabled)
        {
            deepSeekReady = apiReady;
            voiceReady = localVoiceReady;
            voiceEnabled = localVoiceEnabled;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int gap = Math.Max(5, Math.Min(8, Width / 24));
            int usableWidth = Math.Max(2, Width - 2);
            int pillHeight = Math.Max(1, Height - 2);
            int firstWidth = Math.Max(1, (usableWidth - gap) / 2);
            int secondWidth = Math.Max(1, usableWidth - gap - firstWidth);
            DrawServicePill(
                g,
                new Rectangle(1, 1, firstWidth, pillHeight),
                false,
                "DeepSeek",
                deepSeekReady ? "已配置" : "待配置",
                deepSeekReady);
            DrawServicePill(
                g,
                new Rectangle(1 + firstWidth + gap, 1, secondWidth, pillHeight),
                true,
                "GPT-SoVITS",
                !voiceEnabled ? "已关闭" : (voiceReady ? "本地语音" : "准备中"),
                voiceEnabled && voiceReady);
        }

        private void DrawServicePill(Graphics g, Rectangle bounds, bool voice, string title, string state, bool ready)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            using (var fill = new LinearGradientBrush(bounds, Color.FromArgb(220, 248, 253, 255), Color.FromArgb(194, 226, 246, 250), 90F))
            using (var border = new Pen(Color.FromArgb(108, 148, 213, 228), 1F))
            {
                g.FillRoundedRectangle(fill, bounds, Math.Min(9, bounds.Height / 2));
                g.DrawRoundedRectangle(border, bounds, Math.Min(9, bounds.Height / 2));
            }

            int iconSize = Math.Min(17, Math.Max(13, bounds.Height - 10));
            Rectangle icon = new Rectangle(bounds.X + 5, bounds.Y + (bounds.Height - iconSize) / 2, iconSize, iconSize);
            using (var iconFill = new SolidBrush(voice ? Color.FromArgb(220, 222, 250, 247) : Color.FromArgb(218, 229, 240, 255)))
            using (var iconBorder = new Pen(voice ? Color.FromArgb(78, 192, 181) : Color.FromArgb(91, 142, 222), 1F))
            {
                g.FillEllipse(iconFill, icon);
                g.DrawEllipse(iconBorder, icon);
            }

            if (voice)
            {
                using (var iconFont = new Font("Segoe Fluent Icons", Math.Max(6F, iconSize * 0.48F), FontStyle.Regular))
                {
                    TextRenderer.DrawText(g, "\uE767", iconFont, icon, Color.FromArgb(50, 171, 161), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            }
            else
            {
                using (var iconFont = new Font("Segoe UI", Math.Max(6F, iconSize * 0.45F), FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, "D", iconFont, icon, Color.FromArgb(73, 116, 208), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            }

            int textX = icon.Right + 4;
            int textWidth = Math.Max(10, bounds.Right - textX - 3);
            int half = Math.Max(9, bounds.Height / 2);
            using (var titleFont = new Font("Microsoft YaHei UI", 6.4F, FontStyle.Bold))
            using (var stateFont = new Font("Microsoft YaHei UI", 5.6F, FontStyle.Regular))
            {
                TextRenderer.DrawText(
                    g,
                    title,
                    titleFont,
                    new Rectangle(textX, bounds.Y + 1, textWidth, half),
                    Theme.TextMain,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

                int dotSize = 4;
                int stateY = bounds.Y + half;
                using (var dot = new SolidBrush(ready ? Theme.OnlineGreen : Color.FromArgb(174, 187, 195)))
                {
                    g.FillEllipse(dot, textX, stateY + Math.Max(1, (bounds.Height - half - dotSize) / 2), dotSize, dotSize);
                }
                TextRenderer.DrawText(
                    g,
                    state,
                    stateFont,
                    new Rectangle(textX + 7, stateY, Math.Max(8, textWidth - 7), Math.Max(8, bounds.Height - half)),
                    Color.FromArgb(89, 129, 148),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
        }
    }

    internal sealed class SoftPanel : Panel
    {
        public Color FillColor { get; set; }
        public Color BorderColor { get; set; }
        public int Radius { get; set; }

        public SoftPanel()
        {
            DoubleBuffered = true;
            FillColor = Color.FromArgb(250, 253, 253);
            BorderColor = Color.FromArgb(194, 231, 236);
            Radius = 18;
            BackColor = FillColor;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color clearColor = Parent != null ? Parent.BackColor : BackColor;
            using (var clearBrush = new SolidBrush(clearColor))
            {
                g.FillRectangle(clearBrush, ClientRectangle);
            }
            Rectangle rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using (var brush = new SolidBrush(FillColor))
            {
                g.FillRoundedRectangle(brush, rect, Radius);
            }
            using (var pen = new Pen(BorderColor, 1F))
            {
                g.DrawRoundedRectangle(pen, rect, Radius);
            }
        }
    }

    internal sealed class ReferenceTopBarControl : Control
    {
        public ReferenceTopBarControl()
        {
            DoubleBuffered = true;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            BackColor = Color.FromArgb(244, 250, 254);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = ClientRectangle;
            using (var bg = new LinearGradientBrush(rect, Color.FromArgb(250, 253, 255), Color.FromArgb(232, 244, 252), 0F))
            {
                g.FillRectangle(bg, rect);
            }
            using (var pen = new Pen(Color.FromArgb(190, 215, 230)))
            {
                g.DrawLine(pen, 0, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
            }

            DrawLeafLogo(g, 28, 34);
            using (var titleFont = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.FromArgb(36, 66, 96)))
            {
                g.DrawString("彩叶 Iroha Agent", titleFont, brush, new PointF(66, 19));
            }
            DrawChip(g, 250, 22, 44, "Pro", Color.FromArgb(50, 156, 188));
            DrawChip(g, 365, 18, 102, "●  在线", Color.FromArgb(222, 245, 250));

            using (var iconFont = new Font("Segoe UI Symbol", 16F, FontStyle.Regular))
            using (var brush = new SolidBrush(Color.FromArgb(62, 92, 126)))
            {
                g.DrawString("⚙", iconFont, brush, new PointF(rect.Right - 210, 18));
                g.DrawString("－", iconFont, brush, new PointF(rect.Right - 148, 18));
                g.DrawString("□", iconFont, brush, new PointF(rect.Right - 88, 18));
                g.DrawString("×", iconFont, brush, new PointF(rect.Right - 36, 18));
            }
        }

        private void DrawLeafLogo(Graphics g, int x, int y)
        {
            using (var cyan = new SolidBrush(Color.FromArgb(92, 199, 211)))
            using (var blue = new SolidBrush(Color.FromArgb(105, 142, 210)))
            {
                g.FillEllipse(cyan, x - 16, y - 20, 12, 26);
                g.FillEllipse(blue, x - 4, y - 24, 12, 30);
                g.FillEllipse(cyan, x + 8, y - 18, 12, 24);
            }
        }

        private void DrawChip(Graphics g, int x, int y, int width, string text, Color fill)
        {
            Rectangle chip = new Rectangle(x, y, width, 30);
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(Color.FromArgb(170, 211, 226)))
            {
                g.FillRoundedRectangle(brush, chip, 15);
                g.DrawRoundedRectangle(pen, chip, 15);
            }
            TextRenderer.DrawText(g, text, Font, chip, Color.FromArgb(45, 78, 110), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    internal sealed class VisualNovelStagePanel : Panel
    {
        public VisualNovelStagePanel()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(228, 242, 249);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = ClientRectangle;
            using (var bg = new LinearGradientBrush(rect, Color.FromArgb(220, 237, 248), Color.FromArgb(249, 246, 251), 0F))
            {
                g.FillRectangle(bg, rect);
            }

            DrawRoom(g, rect);
            using (var veil = new SolidBrush(Color.FromArgb(72, 255, 255, 255)))
            {
                g.FillRectangle(veil, rect);
            }
        }

        private void DrawRoom(Graphics g, Rectangle rect)
        {
            int w = rect.Width;
            int h = rect.Height;
            using (var floor = new SolidBrush(Color.FromArgb(212, 230, 238)))
            {
                g.FillPolygon(floor, new Point[] { new Point(0, h), new Point(w, h), new Point(w, h - 125), new Point(0, h - 58) });
            }

            using (var window = new SolidBrush(Color.FromArgb(205, 236, 250)))
            using (var frame = new Pen(Color.FromArgb(245, 252, 255), 8F))
            {
                Rectangle win = new Rectangle(w - 340, 42, 260, 230);
                g.FillRoundedRectangle(window, win, 18);
                g.DrawRoundedRectangle(frame, win, 18);
                g.DrawLine(frame, win.X + win.Width / 2, win.Y + 6, win.X + win.Width / 2, win.Bottom - 6);
                g.DrawLine(frame, win.X + 6, win.Y + win.Height / 2, win.Right - 6, win.Y + win.Height / 2);
            }

            using (var shelf = new SolidBrush(Color.FromArgb(190, 170, 148)))
            using (var shadow = new SolidBrush(Color.FromArgb(40, 100, 120, 150)))
            {
                g.FillRoundedRectangle(shadow, new Rectangle(90, 125, 250, 118), 12);
                g.FillRoundedRectangle(shelf, new Rectangle(82, 118, 250, 118), 12);
                for (int i = 0; i < 6; i++)
                {
                    using (var book = new SolidBrush(Color.FromArgb(120 + i * 12, 180, 205)))
                    {
                        g.FillRectangle(book, 100 + i * 26, 142, 18, 68);
                    }
                }
            }

            using (var sofa = new SolidBrush(Color.FromArgb(232, 226, 223)))
            {
                g.FillRoundedRectangle(sofa, new Rectangle(w - 430, h - 248, 320, 122), 28);
            }

            using (var petal = new SolidBrush(Color.FromArgb(150, 255, 176, 200)))
            {
                for (int i = 0; i < 9; i++)
                {
                    int x = 250 + i * 93 % Math.Max(260, w - 260);
                    int y = 46 + i * 67 % Math.Max(220, h - 260);
                    g.FillEllipse(petal, x, y, 12, 6);
                }
            }
        }
    }

    internal static class UiAssetStore
    {
        private static readonly Dictionary<string, Image> Cache = new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

        public static Image GetImage(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return null;
            Image cached;
            if (Cache.TryGetValue(fileName, out cached)) return cached;

            string path = FindUiAsset(fileName);
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                Image image = Image.FromFile(path);
                Cache[fileName] = image;
                return image;
            }
            catch
            {
                return null;
            }
        }

        public static bool PaintShellSlice(Graphics g, Control control)
        {
            if (g == null || control == null || control.Width <= 0 || control.Height <= 0) return false;
            Image shell = GetImage("vn-ui-shell.png");
            if (shell == null) return false;
            Form form = control.FindForm();
            if (form == null || form.ClientSize.Width <= 0 || form.ClientSize.Height <= 0) return false;

            int offsetX = 0;
            int offsetY = 0;
            Control current = control;
            while (current != null && current != form)
            {
                offsetX += current.Left;
                offsetY += current.Top;
                current = current.Parent;
            }

            double scale = Math.Max(form.ClientSize.Width / (double)shell.Width, form.ClientSize.Height / (double)shell.Height);
            int width = Math.Max(1, (int)Math.Ceiling(shell.Width * scale));
            int height = Math.Max(1, (int)Math.Ceiling(shell.Height * scale));
            int x = (form.ClientSize.Width - width) / 2 - offsetX;
            int y = (form.ClientSize.Height - height) / 2 - offsetY;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(shell, new Rectangle(x, y, width, height));
            using (var veil = new SolidBrush(Color.FromArgb(12, 255, 255, 255)))
            {
                g.FillRectangle(veil, new Rectangle(0, 0, control.Width, control.Height));
            }
            return true;
        }

        private static string FindUiAsset(string fileName)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, "assets", "ui", fileName),
                Path.GetFullPath(Path.Combine(baseDir, "..", "assets", "ui", fileName)),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "assets", "ui", fileName)),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "assets", "ui", fileName))
            };
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate)) return candidate;
            }
            return null;
        }
    }

    internal sealed class ConversationItemControl : Control
    {
        private string title;
        private string time;
        private bool active;
        private bool pinned;
        private readonly Image avatarImage;
        private readonly bool useIrohaRealCrop;
        private readonly Timer hoverTimer;
        private bool hover;
        private bool pressed;
        private int hoverFrame;

        public ConversationItemControl(string title, string time, bool active, int avatarIndex)
        {
            this.title = title;
            this.time = time;
            this.active = active;
            useIrohaRealCrop = avatarIndex == 0;
            avatarImage = UiAssetStore.GetImage(useIrohaRealCrop ? "official-iroha-real.png" : GetOfficialAvatarFileName(avatarIndex));
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = new Font("Microsoft YaHei UI", 8.2F, FontStyle.Regular);
            hoverTimer = new Timer();
            hoverTimer.Interval = 45;
            hoverTimer.Tick += delegate
            {
                hoverFrame++;
                Invalidate();
            };
        }

        public string Title
        {
            get { return title; }
            set
            {
                title = string.IsNullOrWhiteSpace(value) ? "未命名会话" : value.Trim();
                Invalidate();
            }
        }

        public string Time
        {
            get { return time; }
            set
            {
                time = string.IsNullOrWhiteSpace(value) ? "刚刚" : value.Trim();
                Invalidate();
            }
        }

        public bool IsActive
        {
            get { return active; }
        }

        public bool Pinned
        {
            get { return pinned; }
            set
            {
                pinned = value;
                Invalidate();
            }
        }

        public bool FilteredOut { get; set; }

        public void SetActive(bool value)
        {
            active = value;
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                hoverTimer.Stop();
                hoverTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            hoverTimer.Start();
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            pressed = false;
            hoverFrame = 0;
            hoverTimer.Stop();
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            pressed = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        private string GetOfficialAvatarFileName(int index)
        {
            int[] characterOrder = { 2, 1, 3, 7, 8, 4, 5, 6, 9, 10, 11, 12 };
            int normalized = Math.Abs(index % characterOrder.Length);
            return string.Format(CultureInfo.InvariantCulture, "official-character-{0:00}.png", characterOrder[normalized]);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int lift = hover && !pressed ? -2 : 0;
            Rectangle rect = new Rectangle(0, 2 + lift, Width - 1, Height - 5);
            int pulse = hover ? (int)(14 + Math.Sin(hoverFrame / 3.0) * 8) : 0;
            Color fill = active ?
                Color.FromArgb(Math.Min(218, 190 + pulse), 232, 250, 255) :
                (hover ? Color.FromArgb(Math.Min(174, 146 + pulse), 255, 255, 255) : Color.FromArgb(24, 255, 255, 255));
            Color border = active ?
                Color.FromArgb(168, 92, 210, 226) :
                (hover ? Color.FromArgb(128, 128, 213, 229) : Color.FromArgb(0, 215, 238, 246));
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(border, active || hover ? 1.25F : 1F))
            {
                if (active || hover)
                {
                    using (var glow = new SolidBrush(Color.FromArgb(active ? 42 : 26, 76, 210, 226)))
                    {
                        g.FillRoundedRectangle(glow, new Rectangle(rect.X + 2, rect.Y + 4, rect.Width, rect.Height), 16);
                    }
                }
                g.FillRoundedRectangle(brush, rect, 16);
                g.DrawRoundedRectangle(pen, rect, 16);
            }

            bool compact = Height < 48;
            int avatarSize = 34;
            int avatarY = compact ? 3 : 13;
            int titleY = compact ? 1 : 9;
            int timeY = compact ? 19 : 30;
            DrawAvatarImage(g, new Rectangle(8, avatarY + lift, avatarSize, avatarSize));
            int textWidth = Math.Max(60, Width - (pinned ? 116 : 84));
            using (var titleFont = new Font(Font, FontStyle.Bold))
            {
                TextRenderer.DrawText(
                    g,
                    title,
                    titleFont,
                    new Rectangle(compact ? 50 : 58, titleY + lift, textWidth, 20),
                    Color.FromArgb(45, 74, 102),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
            TextRenderer.DrawText(
                g,
                time,
                Font,
                new Rectangle(compact ? 50 : 58, timeY + lift, textWidth, 18),
                Color.FromArgb(92, 128, 154),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            if (pinned)
            {
                DrawPinMark(g, new Rectangle(Width - 40, 13 + lift, 16, 20));
            }
            if (active)
            {
                using (var dot = new SolidBrush(Color.FromArgb(60, 199, 211)))
                {
                    g.FillEllipse(dot, Width - 24, (Height - 8) / 2 + lift, 8, 8);
                }
            }
        }

        private void DrawPinMark(Graphics g, Rectangle bounds)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddLine(new PointF(bounds.X + 3, bounds.Y), new PointF(bounds.Right - 3, bounds.Y));
                path.AddLine(new PointF(bounds.Right - 3, bounds.Y), new PointF(bounds.Right - 3, bounds.Bottom - 2));
                path.AddLine(new PointF(bounds.Right - 3, bounds.Bottom - 2), new PointF(bounds.X + bounds.Width / 2, bounds.Bottom - 6));
                path.AddLine(new PointF(bounds.X + bounds.Width / 2, bounds.Bottom - 6), new PointF(bounds.X + 3, bounds.Bottom - 2));
                path.CloseFigure();
                using (var brush = new LinearGradientBrush(bounds, Color.FromArgb(90, 210, 224), Color.FromArgb(54, 174, 204), 90F))
                using (var pen = new Pen(Color.FromArgb(210, 255, 255, 255), 1F))
                {
                    g.FillPath(brush, path);
                    g.DrawPath(pen, path);
                }
            }
        }

        private void DrawAvatarImage(Graphics g, Rectangle bounds)
        {
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            Rectangle imageBounds = new Rectangle(bounds.X + 1, bounds.Y + 1, Math.Max(1, bounds.Width - 2), Math.Max(1, bounds.Height - 2));
            using (var shadow = new SolidBrush(Color.FromArgb(active || hover ? 34 : 20, 52, 128, 160)))
            {
                g.FillEllipse(shadow, bounds.X + 1, bounds.Y + 3, bounds.Width, bounds.Height);
            }
            using (var bg = new LinearGradientBrush(bounds, Color.FromArgb(225, 238, 252, 255), Color.FromArgb(245, 255, 255, 255), 90F))
            {
                g.FillEllipse(bg, bounds);
            }
            if (avatarImage != null)
            {
                GraphicsState state = g.Save();
                using (GraphicsPath clip = new GraphicsPath())
                {
                    clip.AddEllipse(imageBounds);
                    g.SetClip(clip);
                    if (useIrohaRealCrop && avatarImage.Width >= 640 && avatarImage.Height >= 640)
                    {
                        int sourceSize = Math.Min(avatarImage.Width * 3 / 16, avatarImage.Height / 5);
                        int sourceX = avatarImage.Width * 13 / 32;
                        int sourceY = avatarImage.Height / 9;
                        g.DrawImage(avatarImage, imageBounds, sourceX, sourceY, sourceSize, sourceSize, GraphicsUnit.Pixel);
                    }
                    else
                    {
                        DrawCover(g, avatarImage, imageBounds);
                    }
                }
                g.Restore(state);
            }
            Color ringColor = active ? Color.FromArgb(218, 91, 204, 222) :
                (hover ? Color.FromArgb(190, 116, 208, 224) : Color.FromArgb(154, 190, 224, 234));
            using (var inner = new Pen(Color.FromArgb(224, 255, 255, 255), 1F))
            using (var pen = new Pen(ringColor, active || hover ? 1.45F : 1.1F))
            {
                g.DrawEllipse(inner, imageBounds);
                g.DrawEllipse(pen, bounds);
            }
        }

        private void DrawCover(Graphics g, Image image, Rectangle bounds)
        {
            double scale = Math.Max(bounds.Width / (double)image.Width, bounds.Height / (double)image.Height);
            int width = Math.Max(1, (int)Math.Ceiling(image.Width * scale));
            int height = Math.Max(1, (int)Math.Ceiling(image.Height * scale));
            int x = bounds.X + (bounds.Width - width) / 2;
            int y = bounds.Y + (bounds.Height - height) / 2;
            g.DrawImage(image, new Rectangle(x, y, width, height));
        }
    }

    internal sealed class ChibiCardControl : Control
    {
        private readonly Image chibiImage;
        private readonly Timer hoverTimer;
        private bool hover;
        private float hoverProgress;
        private int animationFrame;

        public ChibiCardControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            chibiImage = UiAssetStore.GetImage("iroha-chibi-card-v2.png") ?? UiAssetStore.GetImage("iroha-chibi.png");
            hoverTimer = new Timer();
            hoverTimer.Interval = 25;
            hoverTimer.Tick += delegate
            {
                float target = hover ? 1F : 0F;
                hoverProgress += (target - hoverProgress) * 0.22F;
                animationFrame++;
                if (!hover && hoverProgress < 0.015F)
                {
                    hoverProgress = 0F;
                    hoverTimer.Stop();
                }
                Invalidate();
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                hoverTimer.Stop();
                hoverTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            hoverTimer.Start();
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            hoverTimer.Start();
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            if (Width < 4 || Height < 4) return;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Rectangle shadowRect = new Rectangle(2, 3, Math.Max(1, Width - 3), Math.Max(1, Height - 4));
            using (var shadow = new SolidBrush(Color.FromArgb((int)(16 + hoverProgress * 14), 39, 121, 157)))
            {
                g.FillRoundedRectangle(shadow, shadowRect, 18);
            }
            using (var bg = new LinearGradientBrush(rect, Color.FromArgb(246, 252, 255), Color.FromArgb(222, 243, 250), 90F))
            using (var pen = new Pen(Color.FromArgb((int)(116 + hoverProgress * 54), 171, 224, 237), hover ? 1.15F : 1F))
            {
                g.FillRoundedRectangle(bg, rect, 18);
                g.DrawRoundedRectangle(pen, rect, 18);
            }

            int captionHeight = Math.Min(52, Math.Max(44, Height / 4));
            Rectangle art = new Rectangle(4, 4, Math.Max(1, Width - 8), Math.Max(56, Height - 9));
            GraphicsState state = g.Save();
            using (GraphicsPath clip = CreateRoundedPath(art, 15))
            {
                g.SetClip(clip);
                using (var artBackground = new LinearGradientBrush(art, Color.FromArgb(229, 246, 255), Color.FromArgb(238, 252, 253), 0F))
                {
                    g.FillRectangle(artBackground, art);
                }
                if (chibiImage != null)
                {
                    DrawCardArtwork(g, chibiImage, art);
                }
                using (var light = new LinearGradientBrush(art, Color.FromArgb(2, 255, 255, 255), Color.FromArgb(34, 200, 239, 248), 90F))
                {
                    g.FillRectangle(light, art);
                }
                using (var sheen = new LinearGradientBrush(
                    new Rectangle(art.X, art.Y, art.Width, Math.Max(1, art.Height / 2)),
                    Color.FromArgb((int)(12 + hoverProgress * 13), 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255),
                    90F))
                {
                    g.FillRectangle(sheen, art.X, art.Y, art.Width, Math.Max(1, art.Height / 2));
                }
            }
            g.Restore(state);

            Rectangle caption = new Rectangle(4, Math.Max(4, Height - captionHeight - 4), Math.Max(1, Width - 8), captionHeight);
            using (GraphicsPath captionPath = CreateRoundedPath(caption, 14))
            using (var captionFill = new LinearGradientBrush(caption, Color.FromArgb(238, 255, 255, 255), Color.FromArgb(224, 213, 244, 250), 90F))
            using (var divider = new Pen(Color.FromArgb(58, 141, 211, 229), 1F))
            {
                g.FillPath(captionFill, captionPath);
                g.DrawLine(divider, caption.X + 12, caption.Y, caption.Right - 12, caption.Y);
            }

            TextRenderer.DrawText(g, "点开彩叶头像", Font, new Rectangle(12, caption.Y + 4, Width - 24, 20), Color.FromArgb(43, 79, 108), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            using (var subFont = new Font("Microsoft YaHei UI", 7.5F, FontStyle.Regular))
            {
                TextRenderer.DrawText(g, "快速唤起语音互动", subFont, new Rectangle(12, caption.Y + 23, Width - 24, 17), Color.FromArgb(88, 126, 151), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }

            float pulse = hover ? (float)((Math.Sin(animationFrame / 4.0) + 1.0) / 2.0) : 0F;
            Rectangle speakerBounds = new Rectangle(Width - 32, 10, 20, 20);
            using (var speakerBack = new SolidBrush(Color.FromArgb((int)(116 + pulse * 50), 247, 254, 255)))
            using (var speakerRing = new Pen(Color.FromArgb((int)(112 + pulse * 70), 124, 213, 229), 1F))
            using (var speakerFont = new Font("Segoe Fluent Icons", 8F, FontStyle.Regular))
            {
                g.FillEllipse(speakerBack, speakerBounds);
                g.DrawEllipse(speakerRing, speakerBounds);
                TextRenderer.DrawText(g, "\uE767", speakerFont, speakerBounds, Color.FromArgb(52, 151, 186), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        private void DrawCardArtwork(Graphics g, Image image, Rectangle bounds)
        {
            double framing = Height < 160 ? 0.90 : 0.93;
            double coverScale = Math.Max(bounds.Width / (double)image.Width, bounds.Height / (double)image.Height) * framing;
            double motionScale = 1.0 + hoverProgress * 0.018;
            int width = Math.Max(1, (int)Math.Ceiling(image.Width * coverScale * motionScale));
            int height = Math.Max(1, (int)Math.Ceiling(image.Height * coverScale * motionScale));
            int x = bounds.X + (bounds.Width - width) / 2;
            int compactLift = Height < 160 ? Math.Max(8, (160 - Height) / 3) : 0;
            int y = bounds.Y + (bounds.Height - height) / 2 - compactLift - (int)Math.Round(hoverProgress * 2.0);
            g.DrawImage(image, new Rectangle(x, y, width, height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
        }

        private GraphicsPath CreateRoundedPath(Rectangle rect, int radius)
        {
            int diameter = Math.Max(2, Math.Min(radius * 2, Math.Min(rect.Width, rect.Height)));
            var path = new GraphicsPath();
            Rectangle arc = new Rectangle(rect.X, rect.Y, diameter, diameter);
            path.StartFigure();
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    internal sealed class HeroHeaderControl : Control
    {
        public HeroHeaderControl()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(236, 248, 250);
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
            using (var bg = new LinearGradientBrush(rect, Color.FromArgb(236, 251, 252), Color.FromArgb(249, 244, 255), 0F))
            {
                g.FillRoundedRectangle(bg, rect, 18);
            }
            using (var pen = new Pen(Color.FromArgb(185, 225, 235), 1F))
            {
                g.DrawRoundedRectangle(pen, rect, 18);
            }

            using (var cyan = new SolidBrush(Color.FromArgb(70, 51, 190, 210)))
            using (var violet = new SolidBrush(Color.FromArgb(58, 119, 92, 190)))
            {
                g.FillEllipse(cyan, rect.Width - 118, -38, 126, 126);
                g.FillEllipse(violet, rect.Width - 76, 22, 48, 48);
            }

            using (var titleFont = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold))
            using (var subFont = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular))
            using (var titleBrush = new SolidBrush(Color.FromArgb(34, 58, 78)))
            using (var subBrush = new SolidBrush(Color.FromArgb(82, 105, 118)))
            {
                g.DrawString("彩叶 Agent", titleFont, titleBrush, new PointF(18, 9));
                g.DrawString("月色刚好，今天也一起慢慢来。", subFont, subBrush, new PointF(20, 42));
            }

            Rectangle chip = new Rectangle(rect.Width - 132, 22, 98, 24);
            using (var brush = new SolidBrush(Color.FromArgb(48, 42, 158, 185)))
            {
                g.FillRoundedRectangle(brush, chip, 12);
            }
            TextRenderer.DrawText(g, "READY", Font, chip, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    internal sealed class TextPromptForm : Form
    {
        private readonly TextBox inputBox;

        public string Value
        {
            get { return inputBox.Text; }
        }

        public TextPromptForm(string title, string prompt, string value)
        {
            Text = title;
            Size = new Size(420, 190);
            MinimumSize = new Size(420, 190);
            MaximumSize = new Size(520, 220);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(239, 251, 255);
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);

            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(22, 18, 22, 18);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var promptLabel = new Label();
            promptLabel.Text = prompt;
            promptLabel.Dock = DockStyle.Fill;
            promptLabel.ForeColor = Color.FromArgb(42, 74, 102);
            promptLabel.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(promptLabel, 0, 0);

            inputBox = new TextBox();
            inputBox.Text = value ?? "";
            inputBox.BorderStyle = BorderStyle.FixedSingle;
            inputBox.Dock = DockStyle.Fill;
            inputBox.ForeColor = Color.FromArgb(42, 70, 94);
            inputBox.SelectAll();
            inputBox.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    Confirm();
                }
            };
            root.Controls.Add(inputBox, 0, 1);

            var buttons = new FlowLayoutPanel();
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            buttons.Padding = new Padding(0, 12, 0, 0);
            root.Controls.Add(buttons, 0, 2);

            var ok = CreateDialogButton("确定", true);
            ok.Click += delegate { Confirm(); };
            buttons.Controls.Add(ok);

            var cancel = CreateDialogButton("取消", false);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
            buttons.Controls.Add(cancel);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            inputBox.Focus();
        }

        private Button CreateDialogButton(string text, bool primary)
        {
            var button = new Button();
            button.Text = text;
            button.Width = 86;
            button.Height = 32;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = primary ? Color.FromArgb(78, 202, 218) : Color.FromArgb(245, 253, 255);
            button.ForeColor = primary ? Color.White : Color.FromArgb(42, 74, 102);
            button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            return button;
        }

        private void Confirm()
        {
            if (inputBox.Text.Trim().Length == 0) return;
            DialogResult = DialogResult.OK;
        }
    }

    internal sealed class SettingsForm : Form
    {
        private TextBox apiKeyBox;
        private ComboBox modelBox;
        private CheckBox voiceEnabledBox;
        private CheckBox memoryEnabledBox;
        private CheckBox autoOptimizePromptBox;

        public AppSettings Settings { get; private set; }

        public SettingsForm(AppSettings current)
        {
            Settings = new AppSettings
            {
                ApiKey = current.ApiKey,
                BaseUrl = current.BaseUrl,
                Model = current.Model,
                VoiceServerUrl = current.VoiceServerUrl,
                VoiceRuntimeRoot = current.VoiceRuntimeRoot,
                VoiceRuntimeConfigPath = current.VoiceRuntimeConfigPath,
                VoiceRefAudioPath = DefaultIfBlank(current.VoiceRefAudioPath, AppSettings.DefaultVoiceRefAudioPath),
                VoicePromptText = DefaultIfBlank(current.VoicePromptText, AppSettings.DefaultVoicePromptText),
                VoicePromptLang = DefaultIfBlank(current.VoicePromptLang, AppSettings.DefaultVoicePromptLang),
                VoiceAutoMatched = current.VoiceAutoMatched,
                VoiceMatchVersion = current.VoiceMatchVersion,
                VoiceEnabled = current.VoiceEnabled,
                MemoryEnabled = current.MemoryEnabled,
                AutoOptimizePrompt = current.AutoOptimizePrompt
            };

            Text = "设置";
            Size = new Size(580, 402);
            MinimumSize = new Size(580, 402);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            BackColor = Theme.AppBg;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            BuildLayout();
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(24, 20, 24, 18);
            root.BackColor = Color.FromArgb(246, 252, 255);
            root.ColumnCount = 2;
            root.RowCount = 7;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(root);

            AddLabel(root, "DeepSeek Key", 0);
            apiKeyBox = AddTextBox(root, Settings.ApiKey, 0, true);

            AddLabel(root, "模型", 1);
            modelBox = new ComboBox();
            modelBox.Dock = DockStyle.Fill;
            modelBox.DropDownStyle = ComboBoxStyle.DropDownList;
            modelBox.FlatStyle = FlatStyle.Flat;
            modelBox.BackColor = Color.FromArgb(248, 253, 255);
            modelBox.ForeColor = Color.FromArgb(42, 74, 102);
            modelBox.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            modelBox.Margin = new Padding(4, 7, 4, 7);
            modelBox.Items.Add("deepseek-v4-flash");
            modelBox.Items.Add("deepseek-v4-pro");
            string selectedModel = DefaultIfBlank(Settings.Model, "deepseek-v4-flash");
            if (!modelBox.Items.Contains(selectedModel)) modelBox.Items.Add(selectedModel);
            modelBox.SelectedItem = selectedModel;
            root.Controls.Add(modelBox, 1, 1);

            AddLabel(root, "语音", 2);
            voiceEnabledBox = new CheckBox();
            voiceEnabledBox.Text = "启用日语语音";
            voiceEnabledBox.Checked = Settings.VoiceEnabled;
            voiceEnabledBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            voiceEnabledBox.AutoSize = true;
            voiceEnabledBox.FlatStyle = FlatStyle.Flat;
            voiceEnabledBox.ForeColor = Theme.TextMain;
            voiceEnabledBox.Font = new Font("Microsoft YaHei UI", 8.8F, FontStyle.Bold);
            voiceEnabledBox.Margin = new Padding(4);
            root.Controls.Add(voiceEnabledBox, 1, 2);

            AddLabel(root, "记忆", 3);
            memoryEnabledBox = new CheckBox();
            memoryEnabledBox.Text = "启用长期偏好记忆";
            memoryEnabledBox.Checked = Settings.MemoryEnabled;
            memoryEnabledBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            memoryEnabledBox.AutoSize = true;
            memoryEnabledBox.FlatStyle = FlatStyle.Flat;
            memoryEnabledBox.ForeColor = Theme.TextMain;
            memoryEnabledBox.Font = new Font("Microsoft YaHei UI", 8.8F, FontStyle.Bold);
            memoryEnabledBox.Margin = new Padding(4);
            root.Controls.Add(memoryEnabledBox, 1, 3);

            AddLabel(root, "省 token", 4);
            autoOptimizePromptBox = new CheckBox();
            autoOptimizePromptBox.Text = "自动压缩用户提示词";
            autoOptimizePromptBox.Checked = Settings.AutoOptimizePrompt;
            autoOptimizePromptBox.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            autoOptimizePromptBox.AutoSize = true;
            autoOptimizePromptBox.FlatStyle = FlatStyle.Flat;
            autoOptimizePromptBox.ForeColor = Theme.TextMain;
            autoOptimizePromptBox.Font = new Font("Microsoft YaHei UI", 8.8F, FontStyle.Bold);
            autoOptimizePromptBox.Margin = new Padding(4);
            root.Controls.Add(autoOptimizePromptBox, 1, 4);

            var note = new Label();
            note.Text = "只需要填写 API Key。语音会随软件自动准备，聊天文字保持中文显示。";
            note.Dock = DockStyle.Fill;
            note.ForeColor = Theme.TextSub;
            note.Font = new Font("Microsoft YaHei UI", 8.4F, FontStyle.Regular);
            note.TextAlign = ContentAlignment.MiddleLeft;
            root.SetColumnSpan(note, 2);
            root.Controls.Add(note, 0, 5);

            var buttons = new FlowLayoutPanel();
            buttons.FlowDirection = FlowDirection.RightToLeft;
            buttons.Dock = DockStyle.Fill;
            buttons.Padding = new Padding(0, 10, 0, 0);
            buttons.BackColor = Color.Transparent;
            root.SetColumnSpan(buttons, 2);
            root.Controls.Add(buttons, 0, 6);

            var save = new GlassButton();
            save.Text = "保存";
            save.Width = 100;
            save.Height = 36;
            save.Accent = true;
            save.TextColor = Color.White;
            save.OpaqueBackfill = true;
            save.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            save.Click += Save_Click;
            buttons.Controls.Add(save);

            var cancel = new GlassButton();
            cancel.Text = "取消";
            cancel.Width = 100;
            cancel.Height = 36;
            cancel.OpaqueBackfill = true;
            cancel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
            buttons.Controls.Add(cancel);
        }

        private void AddLabel(TableLayoutPanel root, string text, int row)
        {
            var label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.ForeColor = Theme.TextMain;
            label.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            root.Controls.Add(label, 0, row);
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        }

        private TextBox AddTextBox(TableLayoutPanel root, string value, int row, bool password)
        {
            var box = new TextBox();
            box.Text = value ?? "";
            box.UseSystemPasswordChar = password;
            box.Dock = DockStyle.Fill;
            box.BorderStyle = BorderStyle.FixedSingle;
            box.BackColor = Color.FromArgb(252, 255, 255);
            box.ForeColor = Theme.TextMain;
            box.Font = new Font("Microsoft YaHei UI", 9.2F, FontStyle.Regular);
            box.Margin = new Padding(4, 7, 4, 7);
            root.Controls.Add(box, 1, row);
            return box;
        }

        private void Save_Click(object sender, EventArgs e)
        {
            Settings.ApiKey = apiKeyBox.Text.Trim();
            Settings.BaseUrl = "https://api.deepseek.com";
            Settings.Model = Convert.ToString(modelBox.SelectedItem, CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(Settings.Model)) Settings.Model = "deepseek-v4-flash";
            Settings.VoiceServerUrl = "http://127.0.0.1:9880";
            Settings.VoiceRuntimeRoot = DefaultIfBlank(Settings.VoiceRuntimeRoot, AppSettings.DefaultVoiceRuntimeRoot);
            Settings.VoiceRuntimeConfigPath = DefaultIfBlank(
                Settings.VoiceRuntimeConfigPath,
                Path.Combine(Settings.VoiceRuntimeRoot, AppSettings.DefaultVoiceRuntimeConfig.Replace('/', Path.DirectorySeparatorChar)));
            Settings.VoiceRefAudioPath = DefaultIfBlank(Settings.VoiceRefAudioPath, AppSettings.DefaultVoiceRefAudioPath);
            Settings.VoicePromptText = DefaultIfBlank(Settings.VoicePromptText, AppSettings.DefaultVoicePromptText);
            Settings.VoicePromptLang = DefaultIfBlank(Settings.VoicePromptLang, AppSettings.DefaultVoicePromptLang);
            Settings.VoiceEnabled = voiceEnabledBox.Checked;
            Settings.MemoryEnabled = memoryEnabledBox.Checked;
            Settings.AutoOptimizePrompt = autoOptimizePromptBox.Checked;
            DialogResult = DialogResult.OK;
        }

        private static string DefaultIfBlank(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }

    internal sealed class MemoryForm : Form
    {
        private ListBox memoryList;
        private TextBox noteBox;

        public AgentMemory Memory { get; private set; }

        public MemoryForm(AgentMemory current)
        {
            Memory = new AgentMemory();
            if (current != null && current.Notes != null)
            {
                Memory.Notes.AddRange(current.Notes);
            }

            Text = "记忆管理";
            Size = new Size(640, 480);
            MinimumSize = new Size(640, 480);
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            BuildLayout();
            RefreshList();
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(18);
            root.ColumnCount = 1;
            root.RowCount = 4;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            Controls.Add(root);

            var title = new Label();
            title.Text = "彩叶会记住明确表达的称呼、偏好和习惯。";
            title.Dock = DockStyle.Fill;
            title.ForeColor = Color.FromArgb(58, 84, 102);
            title.TextAlign = ContentAlignment.MiddleLeft;
            root.Controls.Add(title, 0, 0);

            memoryList = new ListBox();
            memoryList.Dock = DockStyle.Fill;
            memoryList.BorderStyle = BorderStyle.FixedSingle;
            memoryList.SelectedIndexChanged += MemoryList_SelectedIndexChanged;
            root.Controls.Add(memoryList, 0, 1);

            noteBox = new TextBox();
            noteBox.Multiline = true;
            noteBox.Dock = DockStyle.Fill;
            noteBox.BorderStyle = BorderStyle.FixedSingle;
            root.Controls.Add(noteBox, 0, 2);

            var buttons = new FlowLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.FlowDirection = FlowDirection.RightToLeft;
            root.Controls.Add(buttons, 0, 3);

            AddButton(buttons, "保存", Save_Click);
            AddButton(buttons, "取消", delegate { DialogResult = DialogResult.Cancel; });
            AddButton(buttons, "清空", Clear_Click);
            AddButton(buttons, "删除", Delete_Click);
            AddButton(buttons, "改写", Update_Click);
            AddButton(buttons, "添加", Add_Click);
        }

        private void AddButton(FlowLayoutPanel panel, string text, EventHandler handler)
        {
            var button = new Button();
            button.Text = text;
            button.Width = 82;
            button.Height = 30;
            button.Click += handler;
            panel.Controls.Add(button);
        }

        private void RefreshList()
        {
            memoryList.Items.Clear();
            if (Memory.Notes == null) Memory.Notes = new List<string>();
            foreach (string note in Memory.Notes)
            {
                memoryList.Items.Add(note);
            }
        }

        private void Add_Click(object sender, EventArgs e)
        {
            string note = noteBox.Text.Trim();
            if (note.Length == 0) return;
            if (Memory.Notes == null) Memory.Notes = new List<string>();
            Memory.Notes.Add(DateTime.Now.ToString("yyyy-MM-dd") + " 手动记忆：" + note);
            noteBox.Clear();
            RefreshList();
        }

        private void Update_Click(object sender, EventArgs e)
        {
            int index = memoryList.SelectedIndex;
            string note = noteBox.Text.Trim();
            if (index < 0 || note.Length == 0 || Memory.Notes == null || index >= Memory.Notes.Count) return;
            Memory.Notes[index] = DateTime.Now.ToString("yyyy-MM-dd") + " 手动记忆：" + note;
            RefreshList();
            memoryList.SelectedIndex = Math.Min(index, memoryList.Items.Count - 1);
        }

        private void MemoryList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = memoryList.SelectedIndex;
            if (index < 0 || Memory.Notes == null || index >= Memory.Notes.Count) return;
            noteBox.Text = StripMemoryPrefix(Memory.Notes[index]);
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            int index = memoryList.SelectedIndex;
            if (index < 0 || Memory.Notes == null || index >= Memory.Notes.Count) return;
            Memory.Notes.RemoveAt(index);
            RefreshList();
        }

        private void Clear_Click(object sender, EventArgs e)
        {
            if (Memory.Notes == null) Memory.Notes = new List<string>();
            Memory.Notes.Clear();
            RefreshList();
        }

        private void Save_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private string StripMemoryPrefix(string note)
        {
            if (string.IsNullOrWhiteSpace(note)) return "";
            int index = note.IndexOf('：');
            if (index >= 0 && index + 1 < note.Length)
            {
                return note.Substring(index + 1).Trim();
            }
            return note.Trim();
        }
    }

    internal sealed class AvatarControl : Control
    {
        private readonly Timer timer;
        private readonly Dictionary<AvatarState, string[]> portraitFramePaths;
        private Image[] activeFrames;
        private Image stageBackgroundImage;
        private Bitmap stageBackgroundCache;
        private Size stageBackgroundCacheSize;
        private Image stagePortraitImage;
        private Image stageBlinkHalfImage;
        private Image stageBlinkClosedImage;
        private Image stageSpeakSmallImage;
        private Image stageSpeakOpenImage;
        private AvatarState activeFrameState;
        private int frame;
        private readonly Random animationRandom;
        private int nextBlinkFrame;
        private int blinkStep;
        private bool stageMode;
        private Rectangle characterStageBounds;

        public event EventHandler AnimationFrameChanged;

        public AvatarState State { get; private set; }
        public bool StageMode
        {
            get { return stageMode; }
            set
            {
                if (stageMode == value) return;
                stageMode = value;
                if (stageMode && stagePortraitImage != null)
                {
                    DisposeActiveFrames();
                    activeFrameState = State;
                    frame = 0;
                }
                Invalidate();
            }
        }
        public bool ImmersiveMode
        {
            get { return StageMode; }
            set { StageMode = value; }
        }
        public Rectangle CharacterStageBounds
        {
            get { return characterStageBounds; }
            set
            {
                if (characterStageBounds == value) return;
                characterStageBounds = value;
                Invalidate();
            }
        }

        public AvatarControl()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            State = AvatarState.Idle;
            StageMode = false;
            portraitFramePaths = LoadPortraitFramePaths();
            stageBackgroundImage = LoadStageBackgroundImage();
            stagePortraitImage = LoadStagePortraitImage();
            stageBlinkHalfImage = LoadStageCharacterImage("expressions", "iroha-blink-half.png");
            stageBlinkClosedImage = LoadStageCharacterImage("expressions", "iroha-blink-closed.png");
            stageSpeakSmallImage = LoadStageCharacterImage("expressions", "iroha-speak-small.png");
            stageSpeakOpenImage = LoadStageCharacterImage("expressions", "iroha-speak-open.png");
            activeFrames = new Image[0];
            activeFrameState = AvatarState.Idle;
            animationRandom = new Random(unchecked(Environment.TickCount ^ GetHashCode()));
            nextBlinkFrame = 64 + animationRandom.Next(0, 46);
            blinkStep = -1;
            timer = new Timer();
            timer.Interval = 50;
            timer.Tick += delegate
            {
                int previousFrame = frame;
                int previousBlinkStep = blinkStep;
                frame++;
                if (blinkStep >= 0)
                {
                    blinkStep++;
                    if (blinkStep > 5)
                    {
                        blinkStep = -1;
                        nextBlinkFrame = frame + 68 + animationRandom.Next(0, 50);
                    }
                }
                else if (frame >= nextBlinkFrame)
                {
                    blinkStep = 0;
                }
                if (ShouldInvalidateAnimationFrame(previousFrame, previousBlinkStep))
                {
                    InvalidateAnimationFrame();
                }
            };
            timer.Start();
        }

        public void SetState(AvatarState state)
        {
            if (State != state)
            {
                if (StageMode && stagePortraitImage != null)
                {
                    DisposeActiveFrames();
                    activeFrameState = state;
                    frame = 0;
                }
                else
                {
                    SwitchStateFrames(state);
                }
            }
            State = state;
            Invalidate();
            RaiseAnimationFrameChanged();
        }

        private void InvalidateAnimationFrame()
        {
            if (StageMode && stagePortraitImage != null && characterStageBounds.Width > 0 && characterStageBounds.Height > 0)
            {
                Rectangle animationBounds = GetStagePortraitBounds(characterStageBounds, stagePortraitImage.Size, 0, 0, 0.004);
                animationBounds.Inflate(12, 12);
                animationBounds.Intersect(ClientRectangle);
                if (animationBounds.Width > 0 && animationBounds.Height > 0)
                {
                    Invalidate(animationBounds);
                }
            }
            else
            {
                Invalidate();
            }
            RaiseAnimationFrameChanged();
        }

        private bool ShouldInvalidateAnimationFrame(int previousFrame, int previousBlinkStep)
        {
            if (!StageMode || State != AvatarState.Idle) return true;
            if (previousBlinkStep != blinkStep) return true;
            int previousBob = (int)Math.Round(Math.Sin(previousFrame / 19.0) * 0.8);
            int currentBob = (int)Math.Round(Math.Sin(frame / 19.0) * 0.8);
            return previousBob != currentBob;
        }

        private void RaiseAnimationFrameChanged()
        {
            EventHandler handler = AnimationFrameChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Stop();
                timer.Dispose();
                DisposeActiveFrames();
                if (stageBackgroundImage != null)
                {
                    stageBackgroundImage.Dispose();
                    stageBackgroundImage = null;
                }
                if (stageBackgroundCache != null)
                {
                    stageBackgroundCache.Dispose();
                    stageBackgroundCache = null;
                }
                if (stagePortraitImage != null)
                {
                    stagePortraitImage.Dispose();
                    stagePortraitImage = null;
                }
                DisposeImage(ref stageBlinkHalfImage);
                DisposeImage(ref stageBlinkClosedImage);
                DisposeImage(ref stageSpeakSmallImage);
                DisposeImage(ref stageSpeakOpenImage);
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = ClientRectangle;
            if (StageMode)
            {
                DrawStageBackground(g, rect);
                Rectangle stageRect = characterStageBounds.Width > 0 && characterStageBounds.Height > 0 ? characterStageBounds : rect;
                if (DrawStagePortrait(g, stageRect))
                {
                    return;
                }
                if (DrawPortraitFrame(g, stageRect))
                {
                    return;
                }
                DrawMissingStageAsset(g, stageRect);
                return;
            }
            g.Clear(Color.White);

            using (var bg = new LinearGradientBrush(rect, Color.FromArgb(246, 250, 255), Color.FromArgb(255, 248, 252), 90F))
            {
                g.FillRectangle(bg, rect);
            }
            DrawCharacterStage(g, rect);
            if (DrawPortraitFrame(g, rect))
            {
                DrawStatusBadge(g, rect);
                return;
            }

            float cx = rect.Width / 2f;
            float bob = (float)Math.Sin(frame / 4.0) * 3f;
            if (State == AvatarState.Thinking) bob = (float)Math.Sin(frame / 2.0) * 4f;
            if (State == AvatarState.Speaking) bob = (float)Math.Sin(frame / 1.5) * 5f;
            float top = 48 + bob;

            DrawHalo(g, cx, top);
            DrawBody(g, cx, top);
            DrawHead(g, cx, top);
            DrawHair(g, cx, top);
            DrawFace(g, cx, top);
            DrawStatusBadge(g, rect);
        }

        private void DrawMissingStageAsset(Graphics g, Rectangle rect)
        {
            using (var veil = new SolidBrush(Color.FromArgb(226, 247, 252, 255)))
            {
                g.FillRectangle(veil, rect);
            }
            using (var font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular))
            using (var brush = new SolidBrush(Color.FromArgb(70, 111, 139)))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString("界面资源需要恢复，请重新解压完整安装包。", font, brush, rect, format);
            }
        }

        private Dictionary<AvatarState, string[]> LoadPortraitFramePaths()
        {
            var result = new Dictionary<AvatarState, string[]>();
            string frameDir = FindFrameDirectory();
            if (string.IsNullOrEmpty(frameDir) || !Directory.Exists(frameDir))
            {
                return result;
            }

            LoadStateFrames(result, frameDir, AvatarState.Idle, "idle");
            LoadStateFrames(result, frameDir, AvatarState.Thinking, "thinking");
            LoadStateFrames(result, frameDir, AvatarState.Speaking, "speaking");
            LoadStateFrames(result, frameDir, AvatarState.Happy, "happy");
            LoadStateFrames(result, frameDir, AvatarState.Error, "error");
            LoadStateFrames(result, frameDir, AvatarState.Shy, "shy");
            LoadStateFrames(result, frameDir, AvatarState.Surprised, "surprised");
            LoadStateFrames(result, frameDir, AvatarState.Cheer, "cheer");
            LoadStateFrames(result, frameDir, AvatarState.Focus, "focus");
            return result;
        }

        private Image LoadStageBackgroundImage()
        {
            string path = VNBackgroundControl.FindBackgroundImagePath();
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                return Image.FromFile(path);
            }
            catch
            {
                return null;
            }
        }

        private Image LoadStagePortraitImage()
        {
            return LoadStageCharacterImage("iroha-portrait.png");
        }

        private Image LoadStageCharacterImage(params string[] pathParts)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string relativePath = Path.Combine(pathParts);
            string[] candidates =
            {
                Path.Combine(baseDir, "assets", "character", relativePath),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "assets", "character", relativePath)),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "assets", "character", relativePath))
            };
            foreach (string candidate in candidates)
            {
                if (!File.Exists(candidate)) continue;
                try
                {
                    return Image.FromFile(candidate);
                }
                catch
                {
                }
            }
            return null;
        }

        private static void DisposeImage(ref Image image)
        {
            if (image == null) return;
            image.Dispose();
            image = null;
        }

        private void LoadStateFrames(Dictionary<AvatarState, string[]> result, string frameDir, AvatarState state, string slug)
        {
            string[] files = Directory.GetFiles(frameDir, "iroha_" + slug + "_*.png");
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            if (files.Length == 0) return;

            result[state] = files;
        }

        private void SwitchStateFrames(AvatarState state)
        {
            if (activeFrameState == state && activeFrames.Length > 0)
            {
                return;
            }

            DisposeActiveFrames();
            activeFrameState = state;
            frame = 0;

            string[] files;
            if (!portraitFramePaths.TryGetValue(state, out files) || files.Length == 0)
            {
                if (!portraitFramePaths.TryGetValue(AvatarState.Idle, out files) || files.Length == 0)
                {
                    activeFrames = new Image[0];
                    return;
                }
                activeFrameState = AvatarState.Idle;
            }

            var images = new List<Image>();
            foreach (string file in files)
            {
                try
                {
                    images.Add(Image.FromFile(file));
                }
                catch
                {
                    // Skip broken frames and keep the app usable.
                }
            }
            activeFrames = images.ToArray();
        }

        private void DisposeActiveFrames()
        {
            if (activeFrames == null) return;
            foreach (Image image in activeFrames)
            {
                if (image != null) image.Dispose();
            }
            activeFrames = new Image[0];
        }

        private string FindFrameDirectory()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(baseDir, "assets", "character", "frames"),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "assets", "character", "frames")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "assets", "character", "frames"))
            };

            foreach (string candidate in candidates)
            {
                if (Directory.Exists(candidate)) return candidate;
            }
            return null;
        }

        private void DrawCharacterStage(Graphics g, Rectangle rect)
        {
            Rectangle stage = new Rectangle(8, 8, Math.Max(1, rect.Width - 16), Math.Max(1, rect.Height - 16));
            using (var brush = new LinearGradientBrush(stage, Color.FromArgb(232, 249, 252), Color.FromArgb(255, 247, 251), 90F))
            {
                g.FillRoundedRectangle(brush, stage, 22);
            }

            using (var pen = new Pen(Color.FromArgb(88, 90, 202, 220), 1.5F))
            {
                g.DrawRoundedRectangle(pen, stage, 22);
            }

            using (var sparkle = new SolidBrush(Color.FromArgb(70, 58, 196, 218)))
            {
                g.FillEllipse(sparkle, rect.Width - 78, 42, 10, 10);
                g.FillEllipse(sparkle, 42, rect.Height - 96, 8, 8);
                g.FillEllipse(sparkle, rect.Width - 132, rect.Height - 64, 6, 6);
            }
        }

        private void DrawStageBackground(Graphics g, Rectangle rect)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;
            Size requestedSize = rect.Size;
            if (stageBackgroundCache == null || stageBackgroundCacheSize != requestedSize)
            {
                if (stageBackgroundCache != null)
                {
                    stageBackgroundCache.Dispose();
                    stageBackgroundCache = null;
                }
                stageBackgroundCache = new Bitmap(requestedSize.Width, requestedSize.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                stageBackgroundCacheSize = requestedSize;
                using (Graphics cacheGraphics = Graphics.FromImage(stageBackgroundCache))
                {
                    cacheGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                    cacheGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    cacheGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    cacheGraphics.CompositingQuality = CompositingQuality.HighQuality;
                    DrawStageBackgroundCore(cacheGraphics, new Rectangle(Point.Empty, requestedSize));
                }
            }
            g.DrawImageUnscaled(stageBackgroundCache, rect.Location);
        }

        private void DrawStageBackgroundCore(Graphics g, Rectangle rect)
        {
            if (stageBackgroundImage == null || Parent == null)
            {
                using (var brush = new LinearGradientBrush(rect, Color.FromArgb(234, 248, 252), Color.FromArgb(255, 248, 252), 90F))
                {
                    g.FillRectangle(brush, rect);
                }
                return;
            }

            Size canvas = Parent.ClientSize;
            if (canvas.Width <= 0 || canvas.Height <= 0) return;
            double scale = Math.Max(canvas.Width / (double)stageBackgroundImage.Width, canvas.Height / (double)stageBackgroundImage.Height);
            int width = Math.Max(1, (int)Math.Ceiling(stageBackgroundImage.Width * scale));
            int height = Math.Max(1, (int)Math.Ceiling(stageBackgroundImage.Height * scale));
            int x = (canvas.Width - width) / 2 - Left;
            int y = (canvas.Height - height) / 2 - Top;
            g.DrawImage(stageBackgroundImage, new Rectangle(x, y, width, height));
            using (var veil = new SolidBrush(Color.FromArgb(4, 255, 255, 255)))
            {
                g.FillRectangle(veil, rect);
            }

            int depthWidth = Math.Max(1, (int)Math.Round(rect.Width * 0.82));
            using (var depth = new LinearGradientBrush(
                new Rectangle(0, 0, depthWidth, Math.Max(1, rect.Height)),
                Color.FromArgb(44, 95, 126, 158),
                Color.FromArgb(4, 172, 198, 216),
                0F))
            {
                g.FillRectangle(depth, 0, 0, depthWidth, rect.Height);
            }

            int lightX = (int)Math.Round(rect.Width * 0.68);
            int lightWidth = Math.Max(1, rect.Width - lightX);
            using (var windowLight = new LinearGradientBrush(
                new Rectangle(lightX, 0, lightWidth, Math.Max(1, rect.Height)),
                Color.FromArgb(0, 255, 255, 255),
                Color.FromArgb(14, 255, 255, 255),
                0F))
            {
                g.FillRectangle(windowLight, lightX, 0, lightWidth, rect.Height);
            }

            int topBarHeight = (int)Math.Round(canvas.Height * 70.0 / 942.0);
            int localTopBarHeight = Math.Min(rect.Height, Math.Max(0, topBarHeight - Top));
            if (localTopBarHeight > 0)
            {
                using (var topBar = new LinearGradientBrush(
                    new Rectangle(-Left, 0, Math.Max(1, canvas.Width), Math.Max(1, topBarHeight)),
                    Color.FromArgb(255, 255, 255, 255),
                    Color.FromArgb(255, 232, 247, 252),
                    0F))
                {
                    g.FillRectangle(topBar, 0, 0, rect.Width, localTopBarHeight);
                }
                if (localTopBarHeight == topBarHeight - Top)
                {
                    using (var line = new Pen(Color.FromArgb(128, 172, 218, 232), 1F))
                    {
                        g.DrawLine(line, 0, localTopBarHeight - 1, rect.Width, localTopBarHeight - 1);
                    }
                }
            }
        }

        private bool DrawPortraitFrame(Graphics g, Rectangle rect)
        {
            if (activeFrames == null || activeFrames.Length == 0 || activeFrameState != State)
            {
                SwitchStateFrames(State);
            }
            if (activeFrames == null || activeFrames.Length == 0)
            {
                return false;
            }

            Image image = activeFrames[frame % activeFrames.Length];
            Rectangle source = StageMode ? GetStageSourceRectangle(image.Size) : new Rectangle(Point.Empty, image.Size);
            Rectangle imageBounds = StageMode ?
                new Rectangle(rect.X, rect.Y, Math.Max(1, rect.Width), Math.Max(1, rect.Height)) :
                new Rectangle(4, 18, Math.Max(1, rect.Width - 8), Math.Max(1, rect.Height - 28));
            Rectangle target = FitImage(source.Size, imageBounds);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(image, target, source, GraphicsUnit.Pixel);
            return true;
        }

        private bool DrawStagePortrait(Graphics g, Rectangle rect)
        {
            if (stagePortraitImage == null || rect.Width <= 0 || rect.Height <= 0) return false;
            double breathing = Math.Sin(frame / 19.0) * 0.0012;
            double speakingPulse = State == AvatarState.Speaking ? Math.Sin(frame / 4.8) * 0.0008 : 0.0;
            double bobAmplitude = State == AvatarState.Speaking ? 0.9 : (State == AvatarState.Happy || State == AvatarState.Cheer ? 0.75 : 0.6);
            double bobSpeed = State == AvatarState.Speaking ? 5.5 : 19.0;
            int bob = (int)Math.Round(Math.Sin(frame / bobSpeed) * bobAmplitude);
            int sway = State == AvatarState.Thinking || State == AvatarState.Focus ? (int)Math.Round(Math.Sin(frame / 15.0) * 1.0) : 0;
            Rectangle target = GetStagePortraitBounds(rect, stagePortraitImage.Size, sway, bob, breathing + speakingPulse);

            Image expressionFrame;
            float expressionOpacity;
            GetStageExpressionLayer(out expressionFrame, out expressionOpacity);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.DrawImage(stagePortraitImage, target);
            DrawImageWithOpacity(g, expressionFrame, target, expressionOpacity);
            return true;
        }

        private Rectangle GetStagePortraitBounds(Rectangle rect, Size portraitSize, int sway, int bob, double scalePulse)
        {
            int targetHeight = Math.Max(rect.Height, (int)Math.Round(rect.Height * (1.75 + scalePulse)));
            int targetWidth = Math.Max(1, (int)Math.Round(targetHeight * portraitSize.Width / (double)portraitSize.Height));
            int x = rect.X + (rect.Width - targetWidth) / 2 + (int)Math.Round(rect.Width * 0.01) + sway;
            int y = rect.Y - (int)Math.Round(rect.Height * 0.03) + bob;
            return new Rectangle(x, y, targetWidth, targetHeight);
        }

        private void GetStageExpressionLayer(out Image expressionFrame, out float opacity)
        {
            expressionFrame = null;
            opacity = 0F;

            if (blinkStep >= 0)
            {
                switch (blinkStep)
                {
                    case 0:
                        expressionFrame = stageBlinkHalfImage;
                        opacity = 0.42F;
                        return;
                    case 1:
                        expressionFrame = stageBlinkHalfImage;
                        opacity = 1F;
                        return;
                    case 2:
                        expressionFrame = stageBlinkClosedImage;
                        opacity = 0.78F;
                        return;
                    case 3:
                        expressionFrame = stageBlinkClosedImage;
                        opacity = 1F;
                        return;
                    case 4:
                        expressionFrame = stageBlinkHalfImage;
                        opacity = 0.78F;
                        return;
                    default:
                        expressionFrame = stageBlinkHalfImage;
                        opacity = 0.34F;
                        return;
                }
            }

            if (State == AvatarState.Speaking)
            {
                int mouthPhase = frame % 18;
                if (mouthPhase == 1) { expressionFrame = stageSpeakSmallImage; opacity = 0.42F; }
                else if (mouthPhase == 2) { expressionFrame = stageSpeakSmallImage; opacity = 0.88F; }
                else if (mouthPhase == 3) { expressionFrame = stageSpeakSmallImage; opacity = 1F; }
                else if (mouthPhase == 4) { expressionFrame = stageSpeakOpenImage; opacity = 0.58F; }
                else if (mouthPhase == 5) { expressionFrame = stageSpeakOpenImage; opacity = 1F; }
                else if (mouthPhase == 6) { expressionFrame = stageSpeakOpenImage; opacity = 0.72F; }
                else if (mouthPhase == 7) { expressionFrame = stageSpeakSmallImage; opacity = 1F; }
                else if (mouthPhase == 8) { expressionFrame = stageSpeakSmallImage; opacity = 0.52F; }
                else if (mouthPhase == 11) { expressionFrame = stageSpeakSmallImage; opacity = 0.38F; }
                else if (mouthPhase == 12) { expressionFrame = stageSpeakSmallImage; opacity = 0.82F; }
                else if (mouthPhase == 13) { expressionFrame = stageSpeakOpenImage; opacity = 0.62F; }
                else if (mouthPhase == 14) { expressionFrame = stageSpeakSmallImage; opacity = 1F; }
                else if (mouthPhase == 15) { expressionFrame = stageSpeakSmallImage; opacity = 0.46F; }
                return;
            }

            if (State == AvatarState.Happy || State == AvatarState.Cheer)
            {
                int smilePhase = frame % 48;
                if (smilePhase == 0) { expressionFrame = stageBlinkHalfImage; opacity = 0.35F; }
                else if (smilePhase == 1) { expressionFrame = stageBlinkHalfImage; opacity = 0.82F; }
                else if (smilePhase == 2) { expressionFrame = stageBlinkClosedImage; opacity = 0.72F; }
                else if (smilePhase == 3 || smilePhase == 4) { expressionFrame = stageBlinkClosedImage; opacity = 1F; }
                else if (smilePhase == 5) { expressionFrame = stageBlinkClosedImage; opacity = 0.68F; }
                else if (smilePhase == 6) { expressionFrame = stageBlinkHalfImage; opacity = 0.72F; }
                else if (smilePhase == 7) { expressionFrame = stageBlinkHalfImage; opacity = 0.30F; }
                return;
            }

            if (State == AvatarState.Thinking || State == AvatarState.Focus)
            {
                expressionFrame = stageBlinkHalfImage;
                float target = State == AvatarState.Thinking ? 0.24F : 0.14F;
                opacity = target * Math.Min(1F, frame / 6F);
                return;
            }

            if (State == AvatarState.Shy)
            {
                expressionFrame = stageBlinkHalfImage;
                opacity = 0.30F * Math.Min(1F, frame / 6F);
                return;
            }

            if (State == AvatarState.Surprised)
            {
                if (frame < 2)
                {
                    expressionFrame = stageSpeakSmallImage;
                    opacity = frame == 0 ? 0.34F : 0.72F;
                }
                else
                {
                    expressionFrame = stageSpeakOpenImage;
                    opacity = frame < 18 ? Math.Min(0.78F, 0.42F + frame * 0.08F) : 0.46F;
                }
                return;
            }

            if (State == AvatarState.Error)
            {
                expressionFrame = stageBlinkHalfImage;
                opacity = 0.18F * Math.Min(1F, frame / 6F);
            }
        }

        private void DrawImageWithOpacity(Graphics g, Image image, Rectangle target, float opacity)
        {
            if (image == null || opacity <= 0F) return;
            if (opacity >= 0.995F)
            {
                g.DrawImage(image, target);
                return;
            }

            using (var attributes = new System.Drawing.Imaging.ImageAttributes())
            {
                var matrix = new System.Drawing.Imaging.ColorMatrix();
                matrix.Matrix33 = Math.Max(0F, Math.Min(1F, opacity));
                attributes.SetColorMatrix(matrix, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);
                g.DrawImage(
                    image,
                    target,
                    0,
                    0,
                    image.Width,
                    image.Height,
                    GraphicsUnit.Pixel,
                    attributes);
            }
        }

        internal void DrawCharacterTopOverlay(Graphics g, Rectangle clipBounds)
        {
            if (!StageMode || stagePortraitImage == null || clipBounds.Width <= 0 || clipBounds.Height <= 0) return;
            GraphicsState state = g.Save();
            try
            {
                g.SetClip(clipBounds);
                DrawStagePortrait(g, CharacterStageBounds);
            }
            finally
            {
                g.Restore(state);
            }
        }

        private Rectangle GetStageSourceRectangle(Size imageSize)
        {
            int left = Math.Max(0, (int)(imageSize.Width * 0.04));
            int top = 0;
            int right = Math.Min(imageSize.Width, (int)(imageSize.Width * 0.98));
            int bottom = imageSize.Height;
            return Rectangle.FromLTRB(left, top, right, bottom);
        }

        private void DrawStageGlow(Graphics g, Rectangle rect)
        {
            using (var cyan = new SolidBrush(Color.FromArgb(14, 70, 218, 222)))
            using (var white = new SolidBrush(Color.FromArgb(8, 255, 255, 255)))
            using (var outline = new Pen(Color.FromArgb(46, 70, 208, 218), 2F))
            {
                Rectangle glow = new Rectangle(rect.Width / 2 - rect.Width / 4, 52, rect.Width / 2, Math.Max(120, rect.Height - 116));
                g.FillEllipse(cyan, glow);
                g.DrawEllipse(outline, glow);
                Rectangle inner = new Rectangle(glow.X + glow.Width / 6, glow.Y + 44, glow.Width * 2 / 3, Math.Max(80, glow.Height - 108));
                g.FillEllipse(white, inner);
            }
        }

        private Rectangle FitImage(Size imageSize, Rectangle bounds)
        {
            double scale = Math.Min(bounds.Width / (double)imageSize.Width, bounds.Height / (double)imageSize.Height);
            int width = Math.Max(1, (int)(imageSize.Width * scale));
            int height = Math.Max(1, (int)(imageSize.Height * scale));
            int x = bounds.X + (bounds.Width - width) / 2;
            int y = bounds.Y + bounds.Height - height;
            return new Rectangle(x, y, width, height);
        }

        private void DrawHalo(Graphics g, float cx, float top)
        {
            using (var pen = new Pen(Color.FromArgb(120, 98, 154, 224), 2F))
            {
                g.DrawEllipse(pen, cx - 70, top - 26, 140, 36);
            }
            using (var brush = new SolidBrush(Color.FromArgb(42, 98, 154, 224)))
            {
                g.FillEllipse(brush, cx - 92, top + 5, 184, 184);
            }
        }

        private void DrawBody(Graphics g, float cx, float top)
        {
            using (GraphicsPath coat = new GraphicsPath())
            {
                coat.AddBezier(cx - 58, top + 180, cx - 92, top + 245, cx - 80, top + 320, cx - 46, top + 330);
                coat.AddLine(cx - 46, top + 330, cx + 46, top + 330);
                coat.AddBezier(cx + 46, top + 330, cx + 80, top + 320, cx + 92, top + 245, cx + 58, top + 180);
                coat.CloseFigure();
                using (var brush = new LinearGradientBrush(new RectangleF(cx - 90, top + 170, 180, 170), Color.FromArgb(72, 93, 138), Color.FromArgb(142, 96, 164), 90F))
                {
                    g.FillPath(brush, coat);
                }
            }

            using (var brush = new SolidBrush(Color.FromArgb(245, 245, 252)))
            {
                g.FillPolygon(brush, new PointF[]
                {
                    new PointF(cx - 30, top + 188),
                    new PointF(cx + 30, top + 188),
                    new PointF(cx + 14, top + 280),
                    new PointF(cx - 14, top + 280)
                });
            }

            float armLift = State == AvatarState.Happy ? -16 : State == AvatarState.Speaking ? (frame % 4 < 2 ? -10 : 0) : 0;
            using (var pen = new Pen(Color.FromArgb(72, 93, 138), 16F))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, cx - 58, top + 205, cx - 100, top + 262 + armLift);
                g.DrawLine(pen, cx + 58, top + 205, cx + 100, top + 262 - armLift);
            }
        }

        private void DrawHead(Graphics g, float cx, float top)
        {
            using (var skin = new SolidBrush(Color.FromArgb(255, 225, 213)))
            {
                g.FillEllipse(skin, cx - 62, top + 52, 124, 124);
                g.FillEllipse(skin, cx - 70, top + 108, 20, 28);
                g.FillEllipse(skin, cx + 50, top + 108, 20, 28);
            }
        }

        private void DrawHair(Graphics g, float cx, float top)
        {
            using (GraphicsPath hair = new GraphicsPath())
            {
                hair.AddBezier(cx - 70, top + 96, cx - 64, top + 30, cx + 52, top + 24, cx + 70, top + 94);
                hair.AddBezier(cx + 70, top + 94, cx + 86, top + 184, cx - 82, top + 188, cx - 70, top + 96);
                hair.CloseFigure();
                using (var brush = new LinearGradientBrush(new RectangleF(cx - 80, top + 28, 160, 170), Color.FromArgb(56, 50, 82), Color.FromArgb(117, 84, 145), 90F))
                {
                    g.FillPath(brush, hair);
                }
            }

            using (var bang = new SolidBrush(Color.FromArgb(83, 64, 120)))
            {
                g.FillPie(bang, cx - 58, top + 36, 80, 70, 15, 120);
                g.FillPie(bang, cx - 8, top + 32, 72, 76, 55, 115);
            }

            using (var ribbon = new SolidBrush(Color.FromArgb(235, 116, 143)))
            {
                g.FillPolygon(ribbon, new PointF[]
                {
                    new PointF(cx + 52, top + 62),
                    new PointF(cx + 92, top + 44),
                    new PointF(cx + 82, top + 86)
                });
                g.FillEllipse(ribbon, cx + 47, top + 58, 18, 18);
            }
        }

        private void DrawFace(Graphics g, float cx, float top)
        {
            bool blink = frame % 34 == 0 || frame % 34 == 1;
            float mouthOpen = State == AvatarState.Speaking ? 8 + (frame % 3) * 5 : 0;
            using (var eyePen = new Pen(Color.FromArgb(49, 48, 70), 3F))
            using (var eyeBrush = new SolidBrush(Color.FromArgb(61, 87, 154)))
            using (var shine = new SolidBrush(Color.White))
            {
                if (State == AvatarState.Error)
                {
                    g.DrawLine(eyePen, cx - 36, top + 104, cx - 18, top + 116);
                    g.DrawLine(eyePen, cx - 18, top + 104, cx - 36, top + 116);
                    g.DrawLine(eyePen, cx + 18, top + 104, cx + 36, top + 116);
                    g.DrawLine(eyePen, cx + 36, top + 104, cx + 18, top + 116);
                }
                else if (blink)
                {
                    g.DrawLine(eyePen, cx - 38, top + 112, cx - 18, top + 112);
                    g.DrawLine(eyePen, cx + 18, top + 112, cx + 38, top + 112);
                }
                else
                {
                    g.FillEllipse(eyeBrush, cx - 40, top + 100, 22, 28);
                    g.FillEllipse(eyeBrush, cx + 18, top + 100, 22, 28);
                    g.FillEllipse(shine, cx - 34, top + 104, 7, 7);
                    g.FillEllipse(shine, cx + 24, top + 104, 7, 7);
                }
            }

            using (var blush = new SolidBrush(Color.FromArgb(90, 237, 128, 145)))
            {
                if (State == AvatarState.Happy || State == AvatarState.Speaking)
                {
                    g.FillEllipse(blush, cx - 54, top + 130, 28, 12);
                    g.FillEllipse(blush, cx + 26, top + 130, 28, 12);
                }
            }

            using (var mouthPen = new Pen(Color.FromArgb(118, 52, 75), 3F))
            using (var mouthBrush = new SolidBrush(Color.FromArgb(154, 55, 89)))
            {
                if (State == AvatarState.Error)
                {
                    g.DrawArc(mouthPen, cx - 14, top + 142, 28, 20, 200, 140);
                }
                else if (State == AvatarState.Thinking)
                {
                    g.DrawLine(mouthPen, cx - 10, top + 148, cx + 10, top + 148);
                }
                else if (State == AvatarState.Speaking)
                {
                    g.FillEllipse(mouthBrush, cx - 10, top + 140, 20, 8 + mouthOpen);
                }
                else
                {
                    g.DrawArc(mouthPen, cx - 18, top + 132, 36, 24, 20, 140);
                }
            }
        }

        private void DrawStatusBadge(Graphics g, Rectangle rect)
        {
            string label = "空闲";
            Color color = Color.FromArgb(80, 120, 170);
            if (State == AvatarState.Thinking) { label = "思考中"; color = Color.FromArgb(85, 112, 190); }
            if (State == AvatarState.Speaking) { label = "说话中"; color = Color.FromArgb(190, 82, 130); }
            if (State == AvatarState.Happy) { label = "完成"; color = Color.FromArgb(68, 150, 108); }
            if (State == AvatarState.Error) { label = "错误"; color = Color.FromArgb(190, 76, 76); }
            if (State == AvatarState.Shy) { label = "害羞"; color = Color.FromArgb(202, 104, 146); }
            if (State == AvatarState.Surprised) { label = "惊讶"; color = Color.FromArgb(212, 148, 72); }
            if (State == AvatarState.Cheer) { label = "加油"; color = Color.FromArgb(54, 168, 154); }
            if (State == AvatarState.Focus) { label = "专注"; color = Color.FromArgb(74, 126, 190); }

            Rectangle badge = new Rectangle(rect.Width - 98, 14, 76, 28);
            using (var brush = new SolidBrush(color))
            {
                g.FillRoundedRectangle(brush, badge, 12);
            }
            TextRenderer.DrawText(g, label, Font, badge, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    internal static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
        {
            using (GraphicsPath path = RoundedPath(bounds, radius))
            {
                graphics.FillPath(brush, path);
            }
        }

        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int radius)
        {
            using (GraphicsPath path = RoundedPath(bounds, radius))
            {
                graphics.DrawPath(pen, path);
            }
        }

        private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return new GraphicsPath();
            }
            radius = Math.Max(1, Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2));
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
