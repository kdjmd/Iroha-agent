using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace IrohaAgentDesktop
{
    internal static class AgentToolsQaProgram
    {
        private static readonly List<string> Checks = new List<string>();

        public static int Main(string[] args)
        {
            try
            {
                string output = ReadArgument(args, "--output");
                string workRoot = ReadArgument(args, "--work-root");
                Directory.CreateDirectory(workRoot);
                Environment.SetEnvironmentVariable("IROHA_APP_DATA_ROOT", Path.Combine(workRoot, "app-data"));
                RunAsync(workRoot).GetAwaiter().GetResult();
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllLines(output, Checks, new UTF8Encoding(false));
                Console.WriteLine("Agent tools QA passed: " + Checks.Count + " checks");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static async Task RunAsync(string workRoot)
        {
            string documents = Path.Combine(workRoot, "documents");
            Directory.CreateDirectory(documents);
            var settings = new AppSettings
            {
                ToolsEnabled = true,
                ToolBundleAEnabled = true,
                ToolBundleBEnabled = true,
                ToolBundleCEnabled = true,
                ToolAllowedDirectories = new List<string> { documents },
                ToolAllowedApplications = new Dictionary<string, string> { { "记事本", "notepad.exe" } },
                EnabledSkills = new List<string>(AgentToolSettings.DefaultSkills),
                WebSearchProvider = "off"
            };
            AgentToolSettings.Normalize(settings);
            Assert(settings.ToolAllowedDirectories.Count == 1, "settings.directory.normalize");
            Assert(settings.EnabledSkills.Count == AgentToolSettings.DefaultSkills.Length, "settings.skills.normalize");
            settings.BraveSearchApiKey = "brave-search-secret-qa";
            SettingsStore.Save(settings);
            string storedSettings = File.ReadAllText(SettingsStore.FilePath, Encoding.UTF8);
            Assert(!storedSettings.Contains("brave-search-secret-qa") && storedSettings.Contains(ApiKeyProtector.ProtectedPrefix), "security.brave-key.dpapi-at-rest");
            Assert(SettingsStore.Load().BraveSearchApiKey == "brave-search-secret-qa", "security.brave-key.dpapi-roundtrip");

            var registry = new AgentToolRegistry();
            IList<AgentToolDefinition> definitions = registry.GetEnabled(settings);
            Assert(definitions.Count == 18, "registry.all-tools.count");
            Assert(definitions.Select(item => item.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() == definitions.Count, "registry.names.unique");
            Assert(registry.Find("memory_remember").Risk == AgentToolRisk.ConfirmEveryTime, "registry.memory-write.risk");
            Assert(registry.Find("open_url").ValidateArguments(new Dictionary<string, object>()).Length > 0, "schema.required.validation");
            Assert(registry.Find("calculator").BuildParametersSchema().ContainsKey("properties"), "schema.export");
            Assert(registry.Find("web_search").ValidateArguments(new Dictionary<string, object> { { "query", "test" }, { "count", 1.5 } }).Length > 0, "schema.integer.reject-fraction");

            int approvals = 0;
            var context = new AgentToolExecutionContext(settings);
            context.ApprovalHandler = delegate
            {
                approvals++;
                return Task.FromResult(true);
            };
            var session = new AgentToolSession(registry, context);

            AgentToolResult calculation = await Execute(session, "calculator", new Dictionary<string, object> { { "mode", "expression" }, { "expression", "2 + 3 * (4 ^ 2)" } });
            Assert(calculation.Success && calculation.ToModelText().Contains("50"), "tool.calculator.expression");
            AgentToolResult conversion = await Execute(session, "calculator", new Dictionary<string, object> { { "mode", "unit_conversion" }, { "value", 1 }, { "from_unit", "km" }, { "to_unit", "m" } });
            Assert(conversion.Success && conversion.ToModelText().Contains("1000"), "tool.calculator.units");
            AgentToolResult clock = await Execute(session, "datetime", new Dictionary<string, object> { { "action", "now" } });
            Assert(clock.Success, "tool.datetime.now");

            AgentToolResult remembered = await Execute(session, "memory_remember", new Dictionary<string, object> { { "content", "我喜欢简洁的中文回答" } });
            Assert(remembered.Success && approvals == 1, "tool.memory.remember.confirmed");
            AgentToolResult memorySearch = await Execute(session, "memory_search", new Dictionary<string, object> { { "query", "简洁" } });
            Assert(memorySearch.Success && memorySearch.ToModelText().Contains("简洁"), "tool.memory.search");
            AgentToolResult sensitive = await Execute(session, "memory_remember", new Dictionary<string, object> { { "content", "API Key 是 sk-secret-value" } });
            Assert(!sensitive.Success, "tool.memory.reject-sensitive");

            string textPath = Path.Combine(documents, "study-notes.txt");
            File.WriteAllText(textPath, "彩叶知识库测试\n第二段包含学习计划和复盘。", new UTF8Encoding(false));
            AgentToolResult fileSearch = await Execute(session, "local_file_search", new Dictionary<string, object> { { "query", "复盘" }, { "directory", documents }, { "include_contents", true } });
            Assert(fileSearch.Success && fileSearch.ToModelText().Contains("study-notes.txt"), "tool.files.search-content");
            AgentToolResult document = await Execute(session, "document_read", new Dictionary<string, object> { { "path", textPath }, { "max_characters", 5000 } });
            Assert(document.Success && document.ToModelText().Contains("学习计划"), "tool.document.txt");

            string docxPath = Path.Combine(documents, "sample.docx");
            CreateDocx(docxPath, "DOCX 文档读取正常");
            AgentToolResult docx = await Execute(session, "document_read", new Dictionary<string, object> { { "path", docxPath } });
            Assert(docx.Success && docx.ToModelText().Contains("DOCX 文档读取正常"), "tool.document.docx");

            string pdfPath = Path.Combine(documents, "sample.pdf");
            CreatePdf(pdfPath, "IROHA PDF QA CONTENT");
            AgentToolResult pdf = await Execute(session, "document_read", new Dictionary<string, object> { { "path", pdfPath } });
            Assert(pdf.Success && pdf.ToModelText().Contains("IROHA PDF QA CONTENT"), "tool.document.pdfpig-runtime");

            AgentToolResult indexed = await Execute(session, "knowledge_base", new Dictionary<string, object> { { "action", "index" }, { "path", textPath } });
            Assert(indexed.Success && approvals == 3, "tool.knowledge.index.confirmed");
            AgentToolResult knowledge = await Execute(session, "knowledge_base", new Dictionary<string, object> { { "action", "search" }, { "query", "学习复盘" } });
            Assert(knowledge.Success && knowledge.ToModelText().Contains("study-notes"), "tool.knowledge.search");

            DateTimeOffset due = DateTimeOffset.Now.AddHours(1);
            AgentToolResult reminder = await Execute(session, "reminder_manage", new Dictionary<string, object> { { "action", "create" }, { "title", "测试提醒" }, { "due_at", due.ToString("o") } });
            Assert(reminder.Success && approvals == 4, "tool.reminder.create.confirmed");
            AgentToolResult reminderList = await Execute(session, "reminder_manage", new Dictionary<string, object> { { "action", "list" } });
            Assert(reminderList.Success && reminderList.ToModelText().Contains("测试提醒"), "tool.reminder.list");

            AgentToolResult calendar = await Execute(session, "calendar_manage", new Dictionary<string, object> { { "action", "create" }, { "title", "测试日程" }, { "start_at", due.ToString("o") }, { "end_at", due.AddHours(1).ToString("o") } });
            Assert(calendar.Success && approvals == 5, "tool.calendar.create.confirmed");
            AgentToolResult calendarList = await Execute(session, "calendar_manage", new Dictionary<string, object> { { "action", "list" } });
            Assert(calendarList.Success && calendarList.ToModelText().Contains("测试日程"), "tool.calendar.list");

            string outside = Path.Combine(workRoot, "outside.txt");
            File.WriteAllText(outside, "outside");
            AgentToolResult blockedFile = await Execute(session, "document_read", new Dictionary<string, object> { { "path", outside } });
            Assert(!blockedFile.Success && blockedFile.Summary.Contains("授权目录"), "security.path.escape.blocked");
            Uri uri;
            string urlError;
            Assert(!AgentToolUrlPolicy.TryValidate("http://127.0.0.1:9880/tts", out uri, out urlError), "security.ssrf.loopback");
            Assert(!AgentToolUrlPolicy.TryValidate("http://169.254.169.254/latest/meta-data", out uri, out urlError), "security.ssrf.metadata");
            Assert(!AgentToolUrlPolicy.TryValidate("file:///c:/windows/win.ini", out uri, out urlError), "security.ssrf.scheme");

            var deniedContext = new AgentToolExecutionContext(settings);
            deniedContext.ApprovalHandler = delegate { return Task.FromResult(false); };
            AgentToolResult denied = await new AgentToolSession(registry, deniedContext).ExecuteAsync(new AgentToolCall { Id = "deny", Name = "memory_forget", Arguments = new Dictionary<string, object> { { "action", "clear" } }, ArgumentsJson = "{\"action\":\"clear\"}" });
            Assert(!denied.Success && denied.Summary.Contains("取消"), "security.approval.denied");

            AgentToolResult oversized = AgentToolResult.Ok("test", "large", new string('x', AgentToolResult.MaxModelCharacters + 2000));
            Assert(oversized.ToModelText().Length <= AgentToolResult.MaxModelCharacters, "security.result.truncated");

            TestProtocolContracts(settings, definitions);
            TestProtocolParsing();
            TestImagePayloadContracts(settings);
            Assert(AgentSkillCatalog.BuildPrompt(settings).Contains("隐私守护"), "skills.prompt.enabled");
        }

        private static async Task<AgentToolResult> Execute(AgentToolSession session, string name, Dictionary<string, object> arguments)
        {
            return await session.ExecuteAsync(new AgentToolCall { Id = "qa-" + Guid.NewGuid().ToString("N"), Name = name, Arguments = arguments, ArgumentsJson = "" });
        }

        private static void TestProtocolContracts(AppSettings settings, IList<AgentToolDefinition> tools)
        {
            var client = new ModelApiClient();
            var conversation = new List<ModelConversationMessage> { new ModelConversationMessage { Role = "user", Text = "现在几点" } };
            string[] providers = { "deepseek", "openai", "anthropic", "gemini", "cohere" };
            foreach (string providerId in providers)
            {
                ModelProviderDefinition provider = ModelProviderCatalog.Get(providerId);
                settings.ProviderId = providerId;
                settings.Model = provider.Models[0];
                settings.BaseUrl = provider.DefaultBaseUrl;
                ModelApiRequest request = client.BuildToolRequest(settings, provider, "system", conversation, tools);
                Assert(request.JsonBody.Contains("calculator"), "protocol." + providerId + ".tool-definition");
                Assert(request.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase), "protocol." + providerId + ".https");
            }
        }

        private static void TestProtocolParsing()
        {
            var client = new ModelApiClient();
            ModelToolTurn openAi = client.ExtractToolTurn(ModelApiProtocol.OpenAiChat, "{\"choices\":[{\"message\":{\"content\":null,\"tool_calls\":[{\"id\":\"c1\",\"type\":\"function\",\"function\":{\"name\":\"calculator\",\"arguments\":\"{\\\"expression\\\":\\\"2+2\\\"}\"}}]}}]}");
            Assert(openAi.ToolCalls.Count == 1 && openAi.ToolCalls[0].Arguments.ContainsKey("expression"), "protocol.openai-chat.parse");
            ModelToolTurn responses = client.ExtractToolTurn(ModelApiProtocol.OpenAiResponses, "{\"output\":[{\"type\":\"function_call\",\"call_id\":\"c2\",\"name\":\"datetime\",\"arguments\":\"{\\\"action\\\":\\\"now\\\"}\"}]}");
            Assert(responses.ToolCalls.Count == 1 && responses.ToolCalls[0].Name == "datetime", "protocol.openai-responses.parse");
            ModelToolTurn anthropic = client.ExtractToolTurn(ModelApiProtocol.AnthropicMessages, "{\"content\":[{\"type\":\"tool_use\",\"id\":\"c3\",\"name\":\"weather\",\"input\":{\"location\":\"上海\"}}]}");
            Assert(anthropic.ToolCalls.Count == 1 && anthropic.ToolCalls[0].Name == "weather", "protocol.anthropic.parse");
            ModelToolTurn gemini = client.ExtractToolTurn(ModelApiProtocol.GeminiGenerateContent, "{\"candidates\":[{\"content\":{\"parts\":[{\"functionCall\":{\"name\":\"memory_search\",\"args\":{\"query\":\"偏好\"}}}]}}]}");
            Assert(gemini.ToolCalls.Count == 1 && gemini.ToolCalls[0].Name == "memory_search", "protocol.gemini.parse");
            ModelToolTurn cohere = client.ExtractToolTurn(ModelApiProtocol.CohereChat, "{\"message\":{\"tool_calls\":[{\"id\":\"c4\",\"function\":{\"name\":\"datetime\",\"arguments\":\"{\\\"action\\\":\\\"now\\\"}\"}}]}}");
            Assert(cohere.ToolCalls.Count == 1, "protocol.cohere.parse");
        }

        private static void TestImagePayloadContracts(AppSettings settings)
        {
            var client = new ModelApiClient();
            const string imageData = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAAB";
            string[] providers = { "deepseek", "openai", "anthropic", "gemini", "cohere" };
            foreach (string providerId in providers)
            {
                ModelProviderDefinition provider = ModelProviderCatalog.Get(providerId);
                settings.ProviderId = providerId;
                settings.Model = provider.Models[0];
                settings.BaseUrl = provider.DefaultBaseUrl;
                ModelApiRequest request = client.BuildImageRequest(settings, provider, "描述图片", "image/png", imageData);
                Assert(request.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase), "image." + providerId + ".https");
                Assert(request.JsonBody.Contains(imageData), "image." + providerId + ".base64");
            }
            settings.ProviderId = "openai";
            ModelProviderDefinition openAi = ModelProviderCatalog.Get("openai");
            settings.Model = openAi.Models[0];
            settings.BaseUrl = openAi.DefaultBaseUrl;
            Assert(client.BuildImageRequest(settings, openAi, "描述图片", "image/png", imageData).JsonBody.Contains("input_image"), "image.openai.responses-contract");

            ModelProviderDefinition anthropic = ModelProviderCatalog.Get("anthropic");
            settings.ProviderId = "anthropic";
            settings.Model = anthropic.Models[0];
            settings.BaseUrl = anthropic.DefaultBaseUrl;
            Assert(client.BuildImageRequest(settings, anthropic, "描述图片", "image/png", imageData).JsonBody.Contains("media_type"), "image.anthropic.base64-contract");

            ModelProviderDefinition gemini = ModelProviderCatalog.Get("gemini");
            settings.ProviderId = "gemini";
            settings.Model = gemini.Models[0];
            settings.BaseUrl = gemini.DefaultBaseUrl;
            Assert(client.BuildImageRequest(settings, gemini, "描述图片", "image/png", imageData).JsonBody.Contains("inlineData"), "image.gemini.inline-data-contract");
        }

        private static void CreateDocx(string path, string text)
        {
            using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry("word/document.xml");
                using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                {
                    writer.Write("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:p><w:r><w:t>");
                    writer.Write(System.Security.SecurityElement.Escape(text));
                    writer.Write("</w:t></w:r></w:p></w:body></w:document>");
                }
            }
        }

        private static void CreatePdf(string path, string text)
        {
            var builder = new PdfDocumentBuilder();
            PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);
            PdfPageBuilder page = builder.AddPage(PageSize.A4);
            page.AddText(text, 12, new PdfPoint(40, 760), font);
            File.WriteAllBytes(path, builder.Build());
        }

        private static string ReadArgument(string[] args, string name)
        {
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name) return Path.GetFullPath(args[i + 1]);
            throw new ArgumentException("Missing argument " + name);
        }

        private static void Assert(bool condition, string name)
        {
            if (!condition) throw new InvalidOperationException("QA failed: " + name);
            Checks.Add("PASS " + name);
        }
    }
}
