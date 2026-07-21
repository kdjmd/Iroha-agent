using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace IrohaAgentDesktop
{
    internal enum AgentToolRisk
    {
        ReadOnly,
        ConfirmEveryTime,
        Denied
    }

    internal sealed class AgentToolParameterSpec
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public string Description { get; private set; }
        public bool Required { get; private set; }
        public int MaxLength { get; private set; }
        public string[] AllowedValues { get; private set; }

        public AgentToolParameterSpec(
            string name,
            string type,
            string description,
            bool required,
            int maxLength,
            params string[] allowedValues)
        {
            Name = name;
            Type = type;
            Description = description;
            Required = required;
            MaxLength = maxLength;
            AllowedValues = allowedValues ?? new string[0];
        }

        public Dictionary<string, object> ToSchema()
        {
            var schema = new Dictionary<string, object>
            {
                { "type", Type },
                { "description", Description ?? "" }
            };
            if (MaxLength > 0 && Type == "string") schema["maxLength"] = MaxLength;
            if (AllowedValues.Length > 0) schema["enum"] = AllowedValues;
            return schema;
        }
    }

    internal sealed class AgentToolDefinition
    {
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public string Description { get; private set; }
        public string Bundle { get; private set; }
        public AgentToolRisk Risk { get; private set; }
        public AgentToolParameterSpec[] Parameters { get; private set; }
        public Func<IDictionary<string, object>, bool> ApprovalRule { get; private set; }
        public Func<AgentToolExecutionContext, IDictionary<string, object>, Task<AgentToolResult>> Execute { get; private set; }

        public AgentToolDefinition(
            string name,
            string displayName,
            string description,
            string bundle,
            AgentToolRisk risk,
            AgentToolParameterSpec[] parameters,
            Func<AgentToolExecutionContext, IDictionary<string, object>, Task<AgentToolResult>> execute,
            Func<IDictionary<string, object>, bool> approvalRule)
        {
            Name = name;
            DisplayName = displayName;
            Description = description;
            Bundle = bundle;
            Risk = risk;
            Parameters = parameters ?? new AgentToolParameterSpec[0];
            Execute = execute;
            ApprovalRule = approvalRule;
        }

        public Dictionary<string, object> BuildParametersSchema()
        {
            var properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var required = new List<string>();
            foreach (AgentToolParameterSpec parameter in Parameters)
            {
                properties[parameter.Name] = parameter.ToSchema();
                if (parameter.Required) required.Add(parameter.Name);
            }
            var schema = new Dictionary<string, object>
            {
                { "type", "object" },
                { "properties", properties },
                { "additionalProperties", false }
            };
            if (required.Count > 0) schema["required"] = required.ToArray();
            return schema;
        }

        public bool RequiresApproval(IDictionary<string, object> arguments)
        {
            if (Risk == AgentToolRisk.Denied) return true;
            if (ApprovalRule != null) return ApprovalRule(arguments);
            return Risk == AgentToolRisk.ConfirmEveryTime;
        }

        public string ValidateArguments(IDictionary<string, object> arguments)
        {
            if (arguments == null) return "参数必须是 JSON 对象";
            var specs = Parameters.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
            foreach (string key in arguments.Keys)
            {
                if (!specs.ContainsKey(key)) return "不支持参数：" + key;
            }
            foreach (AgentToolParameterSpec spec in Parameters)
            {
                object value;
                bool found = TryGetValue(arguments, spec.Name, out value);
                if (!found || value == null || (value is string && string.IsNullOrWhiteSpace((string)value)))
                {
                    if (spec.Required) return "缺少参数：" + spec.Name;
                    continue;
                }
                if (!MatchesType(value, spec.Type)) return spec.Name + " 参数类型应为 " + spec.Type;
                string text = value as string;
                if (text != null && spec.MaxLength > 0 && text.Length > spec.MaxLength)
                {
                    return spec.Name + " 参数过长";
                }
                if (text != null && spec.AllowedValues.Length > 0 &&
                    !spec.AllowedValues.Any(item => string.Equals(item, text, StringComparison.OrdinalIgnoreCase)))
                {
                    return spec.Name + " 参数不在允许范围内";
                }
            }
            return "";
        }

        internal static bool TryGetValue(IDictionary<string, object> map, string key, out object value)
        {
            if (map.TryGetValue(key, out value)) return true;
            foreach (KeyValuePair<string, object> item in map)
            {
                if (string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = item.Value;
                    return true;
                }
            }
            value = null;
            return false;
        }

        private static bool MatchesType(object value, string type)
        {
            if (type == "string") return value is string;
            if (type == "boolean") return value is bool;
            if (type == "integer")
            {
                if (value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong) return true;
                if (value is float)
                {
                    float number = (float)value;
                    return !float.IsNaN(number) && !float.IsInfinity(number) && number == Math.Truncate(number);
                }
                if (value is double)
                {
                    double number = (double)value;
                    return !double.IsNaN(number) && !double.IsInfinity(number) && number == Math.Truncate(number);
                }
                if (value is decimal)
                {
                    decimal number = (decimal)value;
                    return number == decimal.Truncate(number);
                }
                return false;
            }
            if (type == "number")
            {
                return value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong ||
                    value is float || value is double || value is decimal;
            }
            if (type == "array") return value is object[] || value is System.Collections.IEnumerable;
            if (type == "object") return value is IDictionary<string, object>;
            return true;
        }
    }

    internal sealed class AgentToolCall
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ArgumentsJson { get; set; }
        public Dictionary<string, object> Arguments { get; set; }
    }

    internal sealed class AgentToolResult
    {
        private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();
        public const int MaxModelCharacters = 49152;

        public string ToolName { get; set; }
        public bool Success { get; set; }
        public string Summary { get; set; }
        public object Data { get; set; }

        public static AgentToolResult Ok(string toolName, string summary, object data)
        {
            return new AgentToolResult { ToolName = toolName, Success = true, Summary = summary, Data = data };
        }

        public static AgentToolResult Fail(string toolName, string summary)
        {
            return new AgentToolResult { ToolName = toolName, Success = false, Summary = summary, Data = null };
        }

        public string ToModelText()
        {
            string json = Serializer.Serialize(new Dictionary<string, object>
            {
                { "ok", Success },
                { "tool", ToolName ?? "" },
                { "summary", Summary ?? "" },
                { "data", Data }
            });
            if (json.Length <= MaxModelCharacters) return json;
            string clipped = UnicodeText.TruncateUtf16(json, MaxModelCharacters - 256);
            return Serializer.Serialize(new Dictionary<string, object>
            {
                { "ok", Success },
                { "tool", ToolName ?? "" },
                { "summary", "结果过长，已安全截断" },
                { "data", clipped }
            });
        }
    }

    internal delegate Task<bool> AgentToolApprovalHandler(
        AgentToolDefinition definition,
        IDictionary<string, object> arguments);

    internal sealed class AgentToolExecutionContext
    {
        public AppSettings Settings { get; private set; }
        public AgentToolApprovalHandler ApprovalHandler { get; set; }
        public Action<string> StatusHandler { get; set; }
        public string PendingImagePath { get; set; }

        public AgentToolExecutionContext(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            Settings = settings;
        }

        public void ReportStatus(string status)
        {
            if (StatusHandler != null && !string.IsNullOrWhiteSpace(status)) StatusHandler(status);
        }
    }

    internal sealed class AgentToolSession
    {
        public const int MaxRounds = 4;
        public const int MaxCallsPerRound = 8;
        private readonly AgentToolRegistry registry;
        private readonly AgentToolExecutionContext context;

        public AgentToolSession(AgentToolRegistry registry, AgentToolExecutionContext context)
        {
            if (registry == null) throw new ArgumentNullException("registry");
            if (context == null) throw new ArgumentNullException("context");
            this.registry = registry;
            this.context = context;
        }

        public IList<AgentToolDefinition> EnabledDefinitions
        {
            get { return registry.GetEnabled(context.Settings); }
        }

        public void ReportStatus(string status)
        {
            context.ReportStatus(status);
        }

        public async Task<AgentToolResult> ExecuteAsync(AgentToolCall call)
        {
            if (call == null || string.IsNullOrWhiteSpace(call.Name)) return AgentToolResult.Fail("unknown", "工具调用无效");
            AgentToolDefinition definition = registry.Find(call.Name);
            if (definition == null || !registry.IsEnabled(definition, context.Settings))
            {
                return AgentToolResult.Fail(call.Name, "该工具未启用或不存在");
            }

            IDictionary<string, object> arguments = call.Arguments ?? new Dictionary<string, object>();
            string validationError = definition.ValidateArguments(arguments);
            if (validationError.Length > 0) return AgentToolResult.Fail(call.Name, validationError);
            if (definition.Risk == AgentToolRisk.Denied) return AgentToolResult.Fail(call.Name, "安全策略拒绝执行该工具");

            if (definition.RequiresApproval(arguments))
            {
                context.ReportStatus("等待确认：" + definition.DisplayName);
                if (context.ApprovalHandler == null || !await context.ApprovalHandler(definition, arguments))
                {
                    return AgentToolResult.Fail(call.Name, "用户取消了本次操作");
                }
            }

            context.ReportStatus("正在" + definition.DisplayName);
            try
            {
                Task<AgentToolResult> execution = definition.Execute(context, arguments);
                Task completed = await Task.WhenAny(execution, Task.Delay(TimeSpan.FromSeconds(35)));
                if (completed != execution) return AgentToolResult.Fail(call.Name, "工具执行超时");
                AgentToolResult result = await execution;
                context.ReportStatus(result.Success ? definition.DisplayName + "完成" : definition.DisplayName + "未完成");
                return result;
            }
            catch (Exception ex)
            {
                return AgentToolResult.Fail(call.Name, AgentToolText.SafeError(ex));
            }
        }
    }

    internal sealed class AgentToolRegistry
    {
        private readonly Dictionary<string, AgentToolDefinition> definitions =
            new Dictionary<string, AgentToolDefinition>(StringComparer.OrdinalIgnoreCase);

        public AgentToolRegistry()
        {
            foreach (AgentToolDefinition definition in AgentToolExecutors.CreateDefinitions())
            {
                if (definitions.ContainsKey(definition.Name)) throw new InvalidOperationException("工具名称重复：" + definition.Name);
                definitions.Add(definition.Name, definition);
            }
        }

        public AgentToolDefinition Find(string name)
        {
            AgentToolDefinition definition;
            return !string.IsNullOrWhiteSpace(name) && definitions.TryGetValue(name, out definition) ? definition : null;
        }

        public IList<AgentToolDefinition> GetEnabled(AppSettings settings)
        {
            return definitions.Values.Where(item => IsEnabled(item, settings)).OrderBy(item => item.Name).ToList();
        }

        public bool IsEnabled(AgentToolDefinition definition, AppSettings settings)
        {
            if (definition == null || settings == null || !settings.ToolsEnabled) return false;
            if (definition.Bundle == "A") return settings.ToolBundleAEnabled;
            if (definition.Bundle == "B") return settings.ToolBundleBEnabled;
            if (definition.Bundle == "C") return settings.ToolBundleCEnabled;
            return false;
        }
    }

    internal static class AgentToolSettings
    {
        public static readonly string[] DefaultSkills =
        {
            "S01", "S02", "S03", "S04", "S05", "S06", "S07", "S09", "S10", "S12"
        };

        public static void Normalize(AppSettings settings)
        {
            if (settings == null) return;
            if (string.IsNullOrWhiteSpace(settings.WebSearchProvider)) settings.WebSearchProvider = "auto";
            string provider = settings.WebSearchProvider.Trim().ToLowerInvariant();
            if (provider != "auto" && provider != "brave" && provider != "bing" && provider != "off") provider = "auto";
            settings.WebSearchProvider = provider;
            settings.BraveSearchApiKey = settings.BraveSearchApiKey ?? "";

            if (settings.ToolAllowedDirectories == null) settings.ToolAllowedDirectories = new List<string>();
            var directories = new List<string>();
            foreach (string raw in settings.ToolAllowedDirectories)
            {
                string normalized = AgentToolPathPolicy.TryNormalizeDirectory(raw);
                if (normalized.Length > 0 && !directories.Any(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase)))
                {
                    directories.Add(normalized);
                }
            }
            if (directories.Count == 0)
            {
                AddDefaultDirectory(directories, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                AddDefaultDirectory(directories, Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));
            }
            settings.ToolAllowedDirectories = directories;

            if (settings.ToolAllowedApplications == null)
            {
                settings.ToolAllowedApplications = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            var apps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> item in settings.ToolAllowedApplications)
            {
                string name = (item.Key ?? "").Trim();
                string value = (item.Value ?? "").Trim();
                if (name.Length > 0 && name.Length <= 80 && value.Length > 0 && value.Length <= 520) apps[name] = value;
            }
            if (apps.Count == 0)
            {
                apps["记事本"] = "notepad.exe";
                apps["计算器"] = "calc.exe";
            }
            settings.ToolAllowedApplications = apps;

            if (settings.EnabledSkills == null || settings.EnabledSkills.Count == 0)
            {
                settings.EnabledSkills = new List<string>(DefaultSkills);
            }
            else
            {
                settings.EnabledSkills = settings.EnabledSkills
                    .Where(id => DefaultSkills.Contains((id ?? "").Trim().ToUpperInvariant()))
                    .Select(id => id.Trim().ToUpperInvariant())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        private static void AddDefaultDirectory(List<string> target, string path)
        {
            string normalized = AgentToolPathPolicy.TryNormalizeDirectory(path);
            if (normalized.Length > 0) target.Add(normalized);
        }
    }

    internal static class AgentSkillCatalog
    {
        private static readonly Dictionary<string, string> Prompts = new Dictionary<string, string>
        {
            { "S01", "日常陪伴：先理解情绪与意图，再给温柔、具体且不过度说教的回应。" },
            { "S02", "记忆整理：仅保存长期有用且非敏感的信息；写入、纠正或遗忘前调用对应记忆工具。" },
            { "S03", "联网研究：对时效性事实先搜索，比较多个来源，并在中文回答中保留来源链接和不确定性。" },
            { "S04", "计划复盘：把目标拆成小步骤，明确优先级、下一步与复盘点；需要时创建本地提醒。" },
            { "S05", "每日陪伴：问候、情绪签到、轻量日记与晚间回顾保持自然，不强迫用户打卡。" },
            { "S06", "学习辅导：先判断掌握程度，再讲解、举例、出题和复盘；资料题优先读取授权文档。" },
            { "S07", "阅读助手：区分原文事实、作者观点与自己的推断，摘要时保留来源位置。" },
            { "S09", "旅行规划：核查日期、天气与开放信息，给可调整的行程并提醒信息时效。" },
            { "S10", "隐私守护：调用写入、删除、剪贴板或系统控制工具前，清楚说明范围并等待确认。" },
            { "S12", "语音表演：日语台词简短自然，mood 与中文语气一致，便于表情和语音同步。" }
        };

        public static string BuildPrompt(AppSettings settings)
        {
            if (settings == null || settings.EnabledSkills == null || settings.EnabledSkills.Count == 0) return "";
            var builder = new StringBuilder();
            builder.Append("已启用工作方式：");
            foreach (string id in settings.EnabledSkills)
            {
                string prompt;
                if (!Prompts.TryGetValue(id, out prompt)) continue;
                builder.Append("\n- ").Append(prompt);
            }
            return builder.ToString();
        }

        public static string GetDisplayName(string id)
        {
            var names = new Dictionary<string, string>
            {
                { "S01", "彩叶日常陪伴" }, { "S02", "记忆整理员" }, { "S03", "联网研究与核查" },
                { "S04", "计划与复盘" }, { "S05", "每日陪伴" }, { "S06", "学习辅导" },
                { "S07", "阅读助手" }, { "S09", "旅行规划" }, { "S10", "隐私守护" },
                { "S12", "语音表演导演" }
            };
            string value;
            return names.TryGetValue(id ?? "", out value) ? value : id;
        }
    }

    internal static class AgentToolPathPolicy
    {
        public static string TryNormalizeDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            try
            {
                string full = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim().Trim('"')));
                return Directory.Exists(full) ? full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : "";
            }
            catch { return ""; }
        }

        public static bool TryAuthorizeFile(AppSettings settings, string path, out string fullPath, out string error)
        {
            fullPath = "";
            error = "";
            if (settings == null || string.IsNullOrWhiteSpace(path))
            {
                error = "文件路径为空";
                return false;
            }
            try { fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'))); }
            catch
            {
                error = "文件路径无效";
                return false;
            }
            foreach (string rootValue in settings.ToolAllowedDirectories ?? new List<string>())
            {
                string root = TryNormalizeDirectory(rootValue);
                if (root.Length == 0) continue;
                if (string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase) ||
                    fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    if (ContainsReparsePointBelowRoot(fullPath, root))
                    {
                        error = "文件路径经过符号链接或目录联接，已拒绝访问";
                        return false;
                    }
                    return true;
                }
            }
            error = "文件不在已授权目录内";
            return false;
        }

        public static bool TryAuthorizeDirectory(AppSettings settings, string path, out string fullPath, out string error)
        {
            if (string.IsNullOrWhiteSpace(path) && settings != null && settings.ToolAllowedDirectories != null && settings.ToolAllowedDirectories.Count > 0)
            {
                path = settings.ToolAllowedDirectories[0];
            }
            bool allowed = TryAuthorizeFile(settings, path, out fullPath, out error);
            if (allowed && !Directory.Exists(fullPath))
            {
                error = "目录不存在";
                return false;
            }
            return allowed;
        }

        private static bool ContainsReparsePointBelowRoot(string fullPath, string root)
        {
            try
            {
                string current = fullPath;
                if (File.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return true;
                if (!Directory.Exists(current)) current = Path.GetDirectoryName(current);
                while (!string.IsNullOrWhiteSpace(current) && !string.Equals(current, root, StringComparison.OrdinalIgnoreCase))
                {
                    if (Directory.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) return true;
                    current = Path.GetDirectoryName(current);
                }
            }
            catch { return true; }
            return false;
        }
    }

    internal static class AgentToolUrlPolicy
    {
        public static bool TryValidate(string value, out Uri uri, out string error)
        {
            uri = null;
            error = "";
            if (!Uri.TryCreate((value ?? "").Trim(), UriKind.Absolute, out uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                error = "只允许有效的 HTTP/HTTPS 地址";
                return false;
            }
            if (!string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                error = "网址不能包含用户名或密码";
                return false;
            }
            string host = uri.DnsSafeHost.Trim().TrimEnd('.');
            if (host.Length == 0 || host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            {
                error = "不允许访问本机或局域网地址";
                return false;
            }
            IPAddress direct;
            if (IPAddress.TryParse(host, out direct) && IsPrivate(direct))
            {
                error = "不允许访问本机、私网或元数据地址";
                return false;
            }
            try
            {
                foreach (IPAddress address in Dns.GetHostAddresses(host))
                {
                    if (IsPrivate(address))
                    {
                        error = "域名解析到本机、私网或元数据地址";
                        return false;
                    }
                }
            }
            catch (SocketException)
            {
                error = "无法解析网址域名";
                return false;
            }
            return true;
        }

        internal static bool IsPrivate(IPAddress address)
        {
            if (IPAddress.IsLoopback(address)) return true;
            byte[] bytes = address.GetAddressBytes();
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                if (bytes[0] == 0 || bytes[0] == 10 || bytes[0] == 127) return true;
                if (bytes[0] == 169 && bytes[1] == 254) return true;
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                if (bytes[0] == 192 && bytes[1] == 168) return true;
                if (bytes[0] >= 224) return true;
                return false;
            }
            if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                if (address.IsIPv6LinkLocal || address.IsIPv6Multicast || address.IsIPv6SiteLocal) return true;
                if (bytes.Length > 0 && (bytes[0] & 0xFE) == 0xFC) return true;
            }
            return false;
        }
    }

    internal static class AgentToolText
    {
        public static string String(IDictionary<string, object> arguments, string key, string fallback)
        {
            object value;
            if (!AgentToolDefinition.TryGetValue(arguments, key, out value) || value == null) return fallback;
            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? fallback;
        }

        public static bool Boolean(IDictionary<string, object> arguments, string key, bool fallback)
        {
            object value;
            if (!AgentToolDefinition.TryGetValue(arguments, key, out value) || value == null) return fallback;
            if (value is bool) return (bool)value;
            bool parsed;
            return bool.TryParse(Convert.ToString(value), out parsed) ? parsed : fallback;
        }

        public static int Integer(IDictionary<string, object> arguments, string key, int fallback, int minimum, int maximum)
        {
            object value;
            int parsed;
            if (!AgentToolDefinition.TryGetValue(arguments, key, out value) || value == null ||
                !int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
            {
                parsed = fallback;
            }
            return Math.Max(minimum, Math.Min(maximum, parsed));
        }

        public static string SafeError(Exception exception)
        {
            string message = exception == null ? "未知错误" : (exception.Message ?? "未知错误");
            message = UnicodeText.NormalizeForDisplay(message).Replace("\r", " ").Replace("\n", " ").Trim();
            if (message.Length > 220) message = UnicodeText.TruncateUtf16(message, 220) + "…";
            return message.Length == 0 ? "操作未完成" : message;
        }
    }
}
