using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace IrohaAgentDesktop
{
    internal sealed class ToolCenterForm : Form
    {
        private static readonly Color ContentSurfaceColor = Color.FromArgb(238, 249, 253);
        private readonly Panel navigation = new Panel();
        private readonly Panel content = new Panel();
        private readonly Label pageTitle = new Label();
        private readonly Label pageHint = new Label();
        private readonly Button[] tabs = new Button[3];
        private readonly Panel[] pages = new Panel[3];
        private CheckBox toolsEnabledBox;
        private CheckBox bundleABox;
        private CheckBox bundleBBox;
        private CheckBox bundleCBox;
        private ComboBox searchProviderBox;
        private TextBox braveKeyBox;
        private ListBox directoryList;
        private ListBox applicationList;
        private CheckedListBox skillList;

        public AppSettings Settings { get; private set; }

        public ToolCenterForm(AppSettings current)
        {
            var serializer = new JavaScriptSerializer();
            Settings = serializer.Deserialize<AppSettings>(serializer.Serialize(current ?? new AppSettings())) ?? new AppSettings();
            Settings.CredentialStatusMessage = current == null ? "" : current.CredentialStatusMessage;
            AgentToolSettings.Normalize(Settings);

            Text = "工具与隐私";
            Size = new Size(940, 650);
            MinimumSize = new Size(820, 580);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = ContentSurfaceColor;
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
            DoubleBuffered = true;
            BuildLayout();
            Resize += delegate { LayoutControls(); };
        }

        private void BuildLayout()
        {
            var top = new Panel { BackColor = Color.FromArgb(250, 254, 255) };
            Controls.Add(top);

            var logo = Label("能力中心", 16F, FontStyle.Bold, Theme.TextMain);
            logo.TextAlign = ContentAlignment.MiddleLeft;
            top.Controls.Add(logo);
            logo.SetBounds(28, 14, 280, 42);

            var subtitle = Label("Tools & Privacy", 8.5F, FontStyle.Regular, Theme.TextSub);
            subtitle.TextAlign = ContentAlignment.MiddleLeft;
            top.Controls.Add(subtitle);
            subtitle.SetBounds(31, 49, 220, 24);

            Button close = MakeButton("\uE8BB", false);
            close.Font = new Font("Segoe Fluent Icons", 10.5F, FontStyle.Regular);
            var closeGlass = close as GlassButton;
            if (closeGlass != null) closeGlass.MinimalChrome = true;
            close.Click += delegate { DialogResult = DialogResult.Cancel; };
            top.Controls.Add(close);
            close.Name = "closeButton";

            navigation.BackColor = Color.FromArgb(246, 252, 255);
            Controls.Add(navigation);
            content.BackColor = ContentSurfaceColor;
            Controls.Add(content);

            string[] tabNames = { "能力组合", "权限与连接", "工作方式" };
            for (int i = 0; i < tabs.Length; i++)
            {
                int index = i;
                tabs[i] = MakeButton(tabNames[i], i == 0);
                tabs[i].TextAlign = ContentAlignment.MiddleLeft;
                tabs[i].Padding = new Padding(18, 0, 0, 0);
                tabs[i].Click += delegate { ShowPage(index); };
                navigation.Controls.Add(tabs[i]);
                pages[i] = new Panel { BackColor = ContentSurfaceColor, Visible = i == 0 };
                content.Controls.Add(pages[i]);
            }

            pageTitle.AutoSize = false;
            pageTitle.Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold);
            pageTitle.ForeColor = Theme.TextMain;
            pageTitle.TextAlign = ContentAlignment.MiddleLeft;
            content.Controls.Add(pageTitle);
            pageHint.AutoSize = false;
            pageHint.Font = new Font("Microsoft YaHei UI", 8.8F, FontStyle.Regular);
            pageHint.ForeColor = Theme.TextSub;
            pageHint.TextAlign = ContentAlignment.MiddleLeft;
            content.Controls.Add(pageHint);

            BuildBundlePage();
            BuildPrivacyPage();
            BuildSkillsPage();

            Button save = MakeButton("保存设置", true);
            save.Name = "saveButton";
            save.Click += Save_Click;
            Controls.Add(save);
            Button cancel = MakeButton("取消", false);
            cancel.Name = "cancelButton";
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
            Controls.Add(cancel);

            ShowPage(0);
            LayoutControls();
        }

        private void BuildBundlePage()
        {
            toolsEnabledBox = MakeCheckBox("启用彩叶工具能力", "模型可在需要时请求已授权工具；关闭后保持纯聊天模式。", Settings.ToolsEnabled);
            bundleABox = MakeCheckBox("A · 陪伴核心", "联网、网页读取、长期记忆、计算、日期时间与本地提醒", Settings.ToolBundleAEnabled);
            bundleBBox = MakeCheckBox("B · 知识与学习", "授权目录搜索、文档读取与私人知识库", Settings.ToolBundleBEnabled);
            bundleCBox = MakeCheckBox("C · 生活助理", "天气、日程、邮件草稿、剪贴板、图片、媒体与应用白名单", Settings.ToolBundleCEnabled);
            pages[0].Controls.Add(toolsEnabledBox);
            pages[0].Controls.Add(bundleABox);
            pages[0].Controls.Add(bundleBBox);
            pages[0].Controls.Add(bundleCBox);
        }

        private void BuildPrivacyPage()
        {
            Label searchLabel = Label("联网搜索", 10F, FontStyle.Bold, Theme.TextMain);
            pages[1].Controls.Add(searchLabel);
            searchProviderBox = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat, BackColor = Color.White, ForeColor = Theme.TextMain, Font = new Font("Microsoft YaHei UI", 9F) };
            searchProviderBox.Items.AddRange(new object[] { "自动（有 Brave Key 时优先）", "Brave Search", "Bing RSS 备用", "关闭联网搜索" });
            searchProviderBox.SelectedIndex = Settings.WebSearchProvider == "brave" ? 1 : Settings.WebSearchProvider == "bing" ? 2 : Settings.WebSearchProvider == "off" ? 3 : 0;
            pages[1].Controls.Add(searchProviderBox);
            braveKeyBox = new TextBox { Text = Settings.BraveSearchApiKey ?? "", UseSystemPasswordChar = true, BorderStyle = BorderStyle.None, BackColor = Color.White, ForeColor = Theme.TextMain, Font = new Font("Microsoft YaHei UI", 9.3F) };
            pages[1].Controls.Add(braveKeyBox);
            Label keyHint = Label("Brave Search Key（可选，已加密保存）", 8.3F, FontStyle.Regular, Theme.TextSub);
            pages[1].Controls.Add(keyHint);

            Label directoryLabel = Label("授权目录", 10F, FontStyle.Bold, Theme.TextMain);
            pages[1].Controls.Add(directoryLabel);
            directoryList = MakeListBox();
            foreach (string directory in Settings.ToolAllowedDirectories) directoryList.Items.Add(directory);
            pages[1].Controls.Add(directoryList);
            Button addDirectory = MakeButton("添加目录", false);
            addDirectory.Name = "addDirectoryButton";
            addDirectory.Click += AddDirectory_Click;
            pages[1].Controls.Add(addDirectory);
            Button removeDirectory = MakeButton("移除", false);
            removeDirectory.Name = "removeDirectoryButton";
            removeDirectory.Click += delegate { if (directoryList.SelectedIndex >= 0) directoryList.Items.RemoveAt(directoryList.SelectedIndex); };
            pages[1].Controls.Add(removeDirectory);

            Label applicationLabel = Label("应用白名单", 10F, FontStyle.Bold, Theme.TextMain);
            pages[1].Controls.Add(applicationLabel);
            applicationList = MakeListBox();
            foreach (KeyValuePair<string, string> item in Settings.ToolAllowedApplications) applicationList.Items.Add(item.Key + "  ·  " + item.Value);
            pages[1].Controls.Add(applicationList);
            Button addApplication = MakeButton("添加应用", false);
            addApplication.Name = "addApplicationButton";
            addApplication.Click += AddApplication_Click;
            pages[1].Controls.Add(addApplication);
            Button removeApplication = MakeButton("移除", false);
            removeApplication.Name = "removeApplicationButton";
            removeApplication.Click += delegate { if (applicationList.SelectedIndex >= 0) applicationList.Items.RemoveAt(applicationList.SelectedIndex); };
            pages[1].Controls.Add(removeApplication);

            searchLabel.Name = "searchLabel";
            keyHint.Name = "keyHint";
            directoryLabel.Name = "directoryLabel";
            applicationLabel.Name = "applicationLabel";
        }

        private void BuildSkillsPage()
        {
            skillList = new CheckedListBox { BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(250, 254, 255), ForeColor = Theme.TextMain, Font = new Font("Microsoft YaHei UI", 9.5F), CheckOnClick = true, IntegralHeight = false };
            foreach (string id in AgentToolSettings.DefaultSkills)
            {
                int index = skillList.Items.Add(id + "  " + AgentSkillCatalog.GetDisplayName(id));
                skillList.SetItemChecked(index, Settings.EnabledSkills.Contains(id));
            }
            pages[2].Controls.Add(skillList);
            Label note = Label("Skill 只调整彩叶的工作方式，不会绕过工具权限。写入、删除、剪贴板和系统控制仍然每次询问。", 9F, FontStyle.Regular, Theme.TextSub);
            note.Name = "skillsNote";
            note.AutoEllipsis = true;
            pages[2].Controls.Add(note);
        }

        private void ShowPage(int index)
        {
            string[] titles = { "能力组合", "权限与连接", "工作方式" };
            string[] hints = { "三组能力可以同时启用；每个有副作用的动作都会先征得你的同意。", "本地文件只在授权目录内读取，应用只能从白名单启动。", "按你的使用习惯选择彩叶擅长的工作流。" };
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].Visible = i == index;
                var glass = tabs[i] as GlassButton;
                if (glass != null) { glass.Accent = i == index; glass.TextColor = i == index ? Color.White : Theme.TextMain; glass.Invalidate(); }
            }
            pageTitle.Text = titles[index];
            pageHint.Text = hints[index];
            LayoutControls();
        }

        private void LayoutControls()
        {
            Control top = Controls[0];
            top.SetBounds(0, 0, ClientSize.Width, 80);
            Control close = FindByName(top.Controls, "closeButton");
            if (close != null) close.SetBounds(top.Width - 58, 20, 38, 38);
            int footerHeight = 76;
            navigation.SetBounds(0, 80, 210, Math.Max(200, ClientSize.Height - 80 - footerHeight));
            for (int i = 0; i < tabs.Length; i++) tabs[i].SetBounds(18, 24 + i * 54, navigation.Width - 36, 44);
            content.SetBounds(210, 80, Math.Max(400, ClientSize.Width - 210), Math.Max(250, ClientSize.Height - 80 - footerHeight));
            pageTitle.SetBounds(34, 18, content.Width - 68, 38);
            pageHint.SetBounds(35, 54, content.Width - 70, 34);
            foreach (Panel page in pages) page.SetBounds(32, 98, Math.Max(300, content.Width - 64), Math.Max(180, content.Height - 110));

            int cardWidth = Math.Max(300, pages[0].Width - 12);
            toolsEnabledBox.SetBounds(8, 4, cardWidth, 66);
            bundleABox.SetBounds(8, 82, cardWidth, 72);
            bundleBBox.SetBounds(8, 164, cardWidth, 72);
            bundleCBox.SetBounds(8, 246, cardWidth, 72);

            if (pages[1].Controls.Count > 0)
            {
                int width = pages[1].Width;
                FindByName(pages[1].Controls, "searchLabel").SetBounds(8, 0, 150, 28);
                searchProviderBox.SetBounds(8, 34, Math.Min(300, width - 16), 30);
                braveKeyBox.SetBounds(Math.Min(322, width / 2), 39, Math.Max(180, width - Math.Min(322, width / 2) - 10), 24);
                FindByName(pages[1].Controls, "keyHint").SetBounds(Math.Min(322, width / 2), 65, Math.Max(180, width - Math.Min(322, width / 2) - 10), 24);
                int listTop = 110;
                int columnGap = 20;
                int columnWidth = Math.Max(230, (width - columnGap - 16) / 2);
                FindByName(pages[1].Controls, "directoryLabel").SetBounds(8, listTop, columnWidth, 28);
                directoryList.SetBounds(8, listTop + 34, columnWidth, Math.Max(120, pages[1].Height - listTop - 94));
                FindByName(pages[1].Controls, "addDirectoryButton").SetBounds(8, pages[1].Height - 50, 112, 38);
                FindByName(pages[1].Controls, "removeDirectoryButton").SetBounds(128, pages[1].Height - 50, 82, 38);
                int right = 8 + columnWidth + columnGap;
                FindByName(pages[1].Controls, "applicationLabel").SetBounds(right, listTop, columnWidth, 28);
                applicationList.SetBounds(right, listTop + 34, columnWidth, Math.Max(120, pages[1].Height - listTop - 94));
                FindByName(pages[1].Controls, "addApplicationButton").SetBounds(right, pages[1].Height - 50, 112, 38);
                FindByName(pages[1].Controls, "removeApplicationButton").SetBounds(right + 120, pages[1].Height - 50, 82, 38);
            }

            skillList.SetBounds(8, 2, Math.Max(300, pages[2].Width - 16), Math.Max(180, pages[2].Height - 62));
            FindByName(pages[2].Controls, "skillsNote").SetBounds(10, pages[2].Height - 52, Math.Max(300, pages[2].Width - 20), 42);

            Control save = FindByName(Controls, "saveButton");
            Control cancel = FindByName(Controls, "cancelButton");
            if (save != null) save.SetBounds(ClientSize.Width - 154, ClientSize.Height - 56, 122, 40);
            if (cancel != null) cancel.SetBounds(ClientSize.Width - 268, ClientSize.Height - 56, 100, 40);
        }

        private void Save_Click(object sender, EventArgs e)
        {
            Settings.ToolsEnabled = toolsEnabledBox.Checked;
            Settings.ToolBundleAEnabled = bundleABox.Checked;
            Settings.ToolBundleBEnabled = bundleBBox.Checked;
            Settings.ToolBundleCEnabled = bundleCBox.Checked;
            Settings.WebSearchProvider = searchProviderBox.SelectedIndex == 1 ? "brave" : searchProviderBox.SelectedIndex == 2 ? "bing" : searchProviderBox.SelectedIndex == 3 ? "off" : "auto";
            Settings.BraveSearchApiKey = braveKeyBox.Text.Trim();
            Settings.ToolAllowedDirectories = directoryList.Items.Cast<object>().Select(item => Convert.ToString(item)).Where(item => !string.IsNullOrWhiteSpace(item)).ToList();
            var apps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (object raw in applicationList.Items)
            {
                string value = Convert.ToString(raw) ?? "";
                int separator = value.IndexOf("  ·  ", StringComparison.Ordinal);
                if (separator > 0) apps[value.Substring(0, separator).Trim()] = value.Substring(separator + 5).Trim();
            }
            Settings.ToolAllowedApplications = apps;
            Settings.EnabledSkills = new List<string>();
            foreach (object item in skillList.CheckedItems)
            {
                string value = Convert.ToString(item) ?? "";
                if (value.Length >= 3) Settings.EnabledSkills.Add(value.Substring(0, 3));
            }
            AgentToolSettings.Normalize(Settings);
            DialogResult = DialogResult.OK;
        }

        private void AddDirectory_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog { Description = "选择允许彩叶读取的目录", ShowNewFolderButton = false })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                string path = AgentToolPathPolicy.TryNormalizeDirectory(dialog.SelectedPath);
                if (path.Length > 0 && !directoryList.Items.Cast<object>().Any(item => string.Equals(Convert.ToString(item), path, StringComparison.OrdinalIgnoreCase))) directoryList.Items.Add(path);
            }
        }

        private void AddApplication_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog { Title = "选择允许启动的应用", Filter = "Windows 应用 (*.exe)|*.exe", CheckFileExists = true, Multiselect = false })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                string name = Path.GetFileNameWithoutExtension(dialog.FileName);
                applicationList.Items.Add(name + "  ·  " + dialog.FileName);
            }
        }

        private static CheckBox MakeCheckBox(string title, string description, bool value)
        {
            return new ToolBundleToggle { Title = title, Description = description, Checked = value, BackColor = ContentSurfaceColor, ForeColor = Theme.TextMain, Cursor = Cursors.Hand };
        }

        private static ListBox MakeListBox()
        {
            return new ListBox { BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(250, 254, 255), ForeColor = Theme.TextMain, Font = new Font("Microsoft YaHei UI", 8.7F), IntegralHeight = false };
        }

        private static Label Label(string text, float size, FontStyle style, Color color)
        {
            return new Label { Text = text, AutoSize = false, BackColor = Color.Transparent, ForeColor = color, Font = new Font("Microsoft YaHei UI", size, style), TextAlign = ContentAlignment.MiddleLeft };
        }

        private static Button MakeButton(string text, bool accent)
        {
            var button = new GlassButton { Text = text, Accent = accent, OpaqueBackfill = false, BackColor = Color.Transparent, TextColor = accent ? Color.White : Theme.TextMain, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold), Cursor = Cursors.Hand };
            return button;
        }

        private static Control FindByName(Control.ControlCollection controls, string name)
        {
            foreach (Control control in controls) if (control.Name == name) return control;
            return null;
        }
    }

    internal sealed class ToolBundleToggle : CheckBox
    {
        private bool hover;
        public string Title { get; set; }
        public string Description { get; set; }

        public ToolBundleToggle()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Opaque, true);
            Appearance = Appearance.Normal;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            TabStop = true;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color parentColor = BackColor.A == 255 ? BackColor : Color.FromArgb(238, 249, 253);
            using (var clear = new SolidBrush(parentColor)) g.FillRectangle(clear, ClientRectangle);
            Rectangle card = new Rectangle(1, 1, Math.Max(1, Width - 3), Math.Max(1, Height - 3));
            Color top = Checked ? Color.FromArgb(238, 253, 255) : Color.FromArgb(252, 255, 255);
            Color bottom = Checked ? Color.FromArgb(218, 246, 250) : Color.FromArgb(241, 250, 253);
            if (hover) top = Color.FromArgb(246, 255, 255);
            using (var fill = new LinearGradientBrush(card, top, bottom, 90F))
            using (var border = new Pen(Checked ? Color.FromArgb(119, 205, 221) : Color.FromArgb(181, 221, 232), 1F))
            {
                g.FillRoundedRectangle(fill, card, 12);
                g.DrawRoundedRectangle(border, card, 12);
            }
            int switchWidth = 44;
            int switchHeight = 24;
            Rectangle track = new Rectangle(card.Right - switchWidth - 18, card.Y + (card.Height - switchHeight) / 2, switchWidth, switchHeight);
            using (var trackFill = new SolidBrush(Checked ? Theme.Primary : Color.FromArgb(202, 220, 228))) g.FillRoundedRectangle(trackFill, track, switchHeight / 2);
            int knobSize = switchHeight - 6;
            int knobX = Checked ? track.Right - knobSize - 3 : track.X + 3;
            using (var knob = new SolidBrush(Color.White)) g.FillEllipse(knob, knobX, track.Y + 3, knobSize, knobSize);
            int textWidth = Math.Max(80, track.X - card.X - 34);
            using (var titleFont = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold))
            using (var descriptionFont = new Font("Microsoft YaHei UI", 8F, FontStyle.Regular))
            {
                TextRenderer.DrawText(g, Title ?? "", titleFont, new Rectangle(card.X + 18, card.Y + 9, textWidth, 25), Theme.TextMain, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
                TextRenderer.DrawText(g, Description ?? "", descriptionFont, new Rectangle(card.X + 18, card.Y + 32, textWidth, Math.Max(18, card.Height - 36)), Theme.TextSub, TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            }
            if (Focused && Enabled)
            {
                using (var focusPen = new Pen(Color.FromArgb(150, 78, 194, 214), 1.4F))
                {
                    g.DrawRoundedRectangle(focusPen, Rectangle.Inflate(card, -3, -3), 10);
                }
            }
        }
    }

    internal sealed class ToolApprovalForm : Form
    {
        public ToolApprovalForm(AgentToolDefinition definition, IDictionary<string, object> arguments)
        {
            Text = "确认工具操作";
            Size = new Size(560, 390);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.FromArgb(243, 252, 255);
            Font = new Font("Microsoft YaHei UI", 9F);
            BuildLayout(definition, arguments);
        }

        private void BuildLayout(AgentToolDefinition definition, IDictionary<string, object> arguments)
        {
            var title = new Label { Text = "允许彩叶执行“" + definition.DisplayName + "”吗？", AutoSize = false, ForeColor = Theme.TextMain, Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            title.SetBounds(28, 22, 500, 42);
            Controls.Add(title);
            var hint = new Label { Text = "这项操作会读取或改变本机状态。权限仅用于本次调用，不会自动记住。", AutoSize = false, ForeColor = Theme.TextSub, Font = new Font("Microsoft YaHei UI", 8.8F), TextAlign = ContentAlignment.MiddleLeft };
            hint.SetBounds(30, 64, 500, 42);
            Controls.Add(hint);
            var details = new TextBox { Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(252, 255, 255), ForeColor = Theme.TextMain, Font = new Font("Microsoft YaHei UI", 9F), ScrollBars = ScrollBars.Vertical, Text = BuildDetails(arguments) };
            details.SetBounds(30, 116, 500, 178);
            Controls.Add(details);
            Button cancel = new GlassButton { Text = "取消", OpaqueBackfill = true, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold) };
            cancel.SetBounds(318, 322, 100, 40);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; };
            Controls.Add(cancel);
            Button approve = new GlassButton { Text = "仅本次允许", Accent = true, OpaqueBackfill = true, TextColor = Color.White, Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold) };
            approve.SetBounds(430, 322, 100, 40);
            approve.Click += delegate { DialogResult = DialogResult.OK; };
            Controls.Add(approve);
        }

        private static string BuildDetails(IDictionary<string, object> arguments)
        {
            if (arguments == null || arguments.Count == 0) return "本次操作没有额外参数。";
            var builder = new StringBuilder();
            foreach (KeyValuePair<string, object> item in arguments)
            {
                string value = Convert.ToString(item.Value) ?? "";
                if (value.Length > 800) value = value.Substring(0, 800) + "…";
                builder.Append(item.Key).Append("：").AppendLine(value);
            }
            return builder.ToString();
        }
    }
}
