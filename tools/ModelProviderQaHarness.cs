using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace IrohaAgentDesktop
{
    internal static class ModelProviderQaProgram
    {
        private static readonly List<string> Results = new List<string>();
        private static string reportPath;

        private static void Main(string[] args)
        {
            reportPath = GetArgument(args, "--output");
            if (string.IsNullOrWhiteSpace(reportPath)) throw new ArgumentException("--output is required");
            reportPath = Path.GetFullPath(reportPath);
            try
            {
                Run();
                Flush();
            }
            catch (Exception ex)
            {
                Results.Add("ERROR: " + ex.Message);
                Flush();
                Environment.ExitCode = 1;
            }
        }

        private static void Run()
        {
            Assert(ModelProviderCatalog.All.Count >= 20, "provider catalog covers mainstream vendors");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (ModelProviderDefinition provider in ModelProviderCatalog.All)
            {
                Assert(ids.Add(provider.Id), "provider id is unique: " + provider.Id);
                Assert(!string.IsNullOrWhiteSpace(provider.DisplayName), "provider has display name: " + provider.Id);
                if (provider.Id != "azure-openai")
                {
                    Uri uri;
                    Assert(Uri.TryCreate(provider.DefaultBaseUrl, UriKind.Absolute, out uri), "provider base URL is absolute: " + provider.Id);
                }
            }

            var legacy = new AppSettings
            {
                ProviderId = null,
                ApiKey = "legacy-deepseek-key",
                BaseUrl = "https://api.deepseek.com",
                Model = "deepseek-v4-pro",
                ProviderApiKeys = null,
                ProviderModels = null,
                ProviderBaseUrls = null
            };
            ModelProviderCatalog.NormalizeSettings(legacy);
            Assert(legacy.ProviderId == "deepseek", "legacy DeepSeek settings migrate to provider profile");
            Assert(legacy.ApiKey == "legacy-deepseek-key", "legacy API key survives migration");
            Assert(legacy.Model == "deepseek-v4-pro", "legacy model survives migration");

            var serializer = new JavaScriptSerializer();
            AppSettings legacyJson = serializer.Deserialize<AppSettings>(
                "{\"ApiKey\":\"legacy-json-key\",\"BaseUrl\":\"https://api.deepseek.com\",\"Model\":\"deepseek-v4-pro\"}");
            ModelProviderCatalog.NormalizeSettings(legacyJson);
            Assert(legacyJson.ApiKey == "legacy-json-key", "legacy JSON migration preserves API key");
            Assert(legacyJson.ProviderModels["deepseek"] == "deepseek-v4-pro", "legacy JSON migration creates model profile");

            legacy.ApiKey = "deepseek-profile-key";
            ModelProviderCatalog.SaveActiveProfile(legacy);
            ModelProviderCatalog.ActivateProvider(legacy, "openai");
            Assert(string.IsNullOrWhiteSpace(legacy.ApiKey), "new provider starts with isolated key");
            legacy.ApiKey = "openai-profile-key";
            ModelProviderCatalog.SaveActiveProfile(legacy);
            ModelProviderCatalog.ActivateProvider(legacy, "deepseek");
            Assert(legacy.ApiKey == "deepseek-profile-key", "switching provider restores its own key");

            string roundTripJson = serializer.Serialize(legacy);
            AppSettings roundTrip = serializer.Deserialize<AppSettings>(roundTripJson);
            ModelProviderCatalog.NormalizeSettings(roundTrip);
            Assert(roundTrip.ProviderApiKeys.ContainsKey("openai"), "provider profiles survive JSON round trip");
            RunCredentialStorageChecks();

            ModelProviderCatalog.ActivateProvider(legacy, "custom");
            legacy.ApiKey = "";
            Assert(ModelProviderCatalog.HasCredentials(legacy), "local custom endpoint may omit API key");

            var client = new ModelApiClient();
            var history = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string> { { "role", "user" }, { "content", "hello" } },
                new Dictionary<string, string> { { "role", "assistant" }, { "content", "hi" } }
            };

            AssertRequest(client, "deepseek", history, "/chat/completions", "Authorization");
            AssertRequest(client, "openai", history, "/responses", "Authorization");
            AssertRequest(client, "anthropic", history, "/messages", "x-api-key");
            AssertRequest(client, "gemini", history, ":generateContent", "x-goog-api-key");
            AssertRequest(client, "cohere", history, "/chat", "Authorization");
            AssertRequest(client, "azure-openai", history, "/openai/deployments/", "api-key");

            Assert(client.ExtractResponseText(
                ModelApiProtocol.OpenAiChat,
                "{\"choices\":[{\"message\":{\"content\":\"openai-ok\"}}]}") == "openai-ok",
                "OpenAI-compatible response parses");
            Assert(client.ExtractResponseText(
                ModelApiProtocol.OpenAiResponses,
                "{\"output\":[{\"content\":[{\"type\":\"output_text\",\"text\":\"responses-ok\"}]}]}") == "responses-ok",
                "OpenAI Responses output parses");
            Assert(client.ExtractResponseText(
                ModelApiProtocol.AnthropicMessages,
                "{\"content\":[{\"type\":\"text\",\"text\":\"anthropic-ok\"}]}") == "anthropic-ok",
                "Anthropic content blocks parse");
            Assert(client.ExtractResponseText(
                ModelApiProtocol.GeminiGenerateContent,
                "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"gemini-ok\"}]}}]}") == "gemini-ok",
                "Gemini candidate parts parse");
            Assert(client.ExtractResponseText(
                ModelApiProtocol.CohereChat,
                "{\"message\":{\"content\":[{\"type\":\"text\",\"text\":\"cohere-ok\"}]}}") == "cohere-ok",
                "Cohere content blocks parse");

            IList<string> openAiModels = client.ExtractModelIds(
                ModelApiProtocol.OpenAiChat,
                "{\"data\":[{\"id\":\"model-b\"},{\"id\":\"model-a\"}]}");
            Assert(openAiModels.Count == 2 && openAiModels[0] == "model-a", "OpenAI model list parses and sorts");
            IList<string> geminiModels = client.ExtractModelIds(
                ModelApiProtocol.GeminiGenerateContent,
                "{\"models\":[{\"name\":\"models/gemini-test\"}]}");
            Assert(geminiModels.Count == 1 && geminiModels[0] == "gemini-test", "Gemini model prefix normalizes");
            AppSettings cohereSettings = BuildSettings("cohere");
            Assert(
                ModelApiClient.BuildModelsUrl(cohereSettings, ModelProviderCatalog.Get("cohere")).EndsWith("/v1/models", StringComparison.OrdinalIgnoreCase),
                "Cohere model discovery uses its v1 models endpoint");

            var custom = BuildSettings("custom");
            custom.ApiKey = "";
            ModelApiRequest customRequest = client.BuildRequest(
                custom,
                ModelProviderCatalog.Get("custom"),
                "system",
                history);
            Assert(!customRequest.Headers.ContainsKey("Authorization"), "custom local request omits blank authorization header");

            var insecureRemote = BuildSettings("custom");
            insecureRemote.BaseUrl = "http://example.com/v1";
            insecureRemote.ApiKey = "qa-placeholder-key";
            AssertRequestRejected(client, insecureRemote, history, "HTTPS", "remote HTTP endpoint cannot receive an API key");

            var embeddedCredentials = BuildSettings("custom");
            embeddedCredentials.BaseUrl = "https://user:password@example.com/v1";
            embeddedCredentials.ApiKey = "";
            AssertRequestRejected(client, embeddedCredentials, history, "用户名或密码", "base URL rejects embedded credentials");

            const string leakedKey = "sk-qa0123456789abcdef";
            string redacted = ModelApiClient.RedactSensitiveText(
                "Authorization: Bearer " + leakedKey + ", api_key=" + leakedKey,
                leakedKey);
            Assert(redacted.IndexOf(leakedKey, StringComparison.Ordinal) < 0, "provider errors redact the active API key");
            Assert(redacted.IndexOf("[已隐藏]", StringComparison.Ordinal) >= 0, "redacted provider errors remain understandable");

            RunToolRoundFinalizationChecks();
        }

        private static void RunToolRoundFinalizationChecks()
        {
            var handler = new ToolLoopHandler();
            using (var httpClient = new HttpClient(handler))
            {
                var client = new ModelApiClient(httpClient);
                var settings = BuildSettings("custom");
                settings.ApiKey = "";
                settings.BaseUrl = "https://qa.local/v1";
                settings.Model = "qa-tool-model";
                settings.ToolsEnabled = true;
                settings.ToolBundleAEnabled = true;

                var statuses = new List<string>();
                var context = new AgentToolExecutionContext(settings) { StatusHandler = status => statuses.Add(status) };
                var session = new AgentToolSession(new AgentToolRegistry(), context);
                var history = new List<Dictionary<string, string>>
                {
                    new Dictionary<string, string> { { "role", "user" }, { "content", "请计算 1 + 1。" } }
                };

                string response = client.RequestWithToolsAsync(
                    settings,
                    "请以严格 JSON 返回 zh、ja、mood。",
                    history,
                    session,
                    TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();

                Assert(response.IndexOf("计算结果是 2", StringComparison.Ordinal) >= 0,
                    "tool loop returns a final answer after the fourth execution round");
                Assert(handler.RequestBodies.Count == AgentToolSession.MaxRounds + 1,
                    "tool loop performs exactly one no-tools finalization request");
                for (int i = 0; i < AgentToolSession.MaxRounds; i++)
                {
                    Assert(handler.RequestBodies[i].IndexOf("\"tools\"", StringComparison.Ordinal) >= 0,
                        "tool round " + (i + 1) + " keeps native tool definitions");
                }
                string finalRequest = handler.RequestBodies[handler.RequestBodies.Count - 1];
                Assert(finalRequest.IndexOf("\"tools\"", StringComparison.Ordinal) < 0,
                    "finalization request disables further tool calls");
                Assert(finalRequest.IndexOf("calculator", StringComparison.OrdinalIgnoreCase) >= 0 &&
                    finalRequest.IndexOf("计算完成", StringComparison.Ordinal) >= 0,
                    "finalization request carries the completed tool result");

                int calculatorExecutions = 0;
                bool reportedFinalization = false;
                foreach (string status in statuses)
                {
                    if (status == "正在计算") calculatorExecutions++;
                    if (status == "工具步骤已完成，正在整理最终回复") reportedFinalization = true;
                }
                Assert(calculatorExecutions == 1,
                    "identical repeated tool calls reuse the first result instead of executing again");
                Assert(reportedFinalization,
                    "tool loop reports the final response stage to the UI");

                var firstCall = new AgentToolCall
                {
                    Name = "CALCULATOR",
                    Arguments = new Dictionary<string, object>
                    {
                        { "mode", "expression" },
                        { "expression", "1+1" }
                    }
                };
                var reorderedCall = new AgentToolCall
                {
                    Name = "calculator",
                    Arguments = new Dictionary<string, object>
                    {
                        { "expression", "1+1" },
                        { "MODE", "expression" }
                    }
                };
                Assert(client.BuildToolCallFingerprint(firstCall) == client.BuildToolCallFingerprint(reorderedCall),
                    "tool-call deduplication ignores tool and argument key casing/order");
            }
        }

        private static void RunCredentialStorageChecks()
        {
            string previousRoot = Environment.GetEnvironmentVariable("IROHA_APP_DATA_ROOT");
            string parent = Path.GetDirectoryName(reportPath) ?? Path.GetTempPath();
            string root = Path.Combine(parent, "credential-work");
            try
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
                Directory.CreateDirectory(root);
                Environment.SetEnvironmentVariable("IROHA_APP_DATA_ROOT", root);

                var secure = new AppSettings();
                secure.ApiKey = "deepseek-storage-qa-key";
                ModelProviderCatalog.SaveActiveProfile(secure);
                ModelProviderCatalog.ActivateProvider(secure, "openai");
                secure.ApiKey = "openai-storage-qa-key";
                ModelProviderCatalog.SaveActiveProfile(secure);
                ModelProviderCatalog.ActivateProvider(secure, "deepseek");
                SettingsStore.Save(secure);

                string raw = File.ReadAllText(SettingsStore.FilePath, Encoding.UTF8);
                Assert(raw.IndexOf("deepseek-storage-qa-key", StringComparison.Ordinal) < 0,
                    "settings file never stores active API key as plaintext");
                Assert(raw.IndexOf("openai-storage-qa-key", StringComparison.Ordinal) < 0,
                    "settings file never stores provider API keys as plaintext");
                Assert(raw.IndexOf(ApiKeyProtector.ProtectedPrefix, StringComparison.Ordinal) >= 0,
                    "settings file stores DPAPI-protected credentials");

                AppSettings loaded = SettingsStore.Load();
                Assert(loaded.ApiKey == "deepseek-storage-qa-key", "DPAPI active API key decrypts for current Windows user");
                ModelProviderCatalog.ActivateProvider(loaded, "openai");
                Assert(loaded.ApiKey == "openai-storage-qa-key", "DPAPI provider API key decrypts after profile switch");

                string legacyRoot = Path.Combine(parent, "credential-legacy-work");
                if (Directory.Exists(legacyRoot)) Directory.Delete(legacyRoot, true);
                Directory.CreateDirectory(legacyRoot);
                Environment.SetEnvironmentVariable("IROHA_APP_DATA_ROOT", legacyRoot);
                File.WriteAllText(
                    SettingsStore.FilePath,
                    "{\"ApiKey\":\"legacy-storage-qa-key\",\"BaseUrl\":\"https://api.deepseek.com\",\"Model\":\"deepseek-v4-flash\"}",
                    new UTF8Encoding(false));
                string migratedCorrupt = SettingsStore.FilePath + ".corrupt";
                File.WriteAllText(migratedCorrupt, "{\"ApiKey\":\"legacy-corrupt-qa-key\"", new UTF8Encoding(false));
                AppSettings migrated = SettingsStore.Load();
                string migratedRaw = File.ReadAllText(SettingsStore.FilePath, Encoding.UTF8);
                Assert(migrated.ApiKey == "legacy-storage-qa-key", "legacy plaintext API key remains available after migration");
                Assert(migratedRaw.IndexOf("legacy-storage-qa-key", StringComparison.Ordinal) < 0,
                    "legacy plaintext API key is automatically replaced on disk");
                Assert(migratedRaw.IndexOf(ApiKeyProtector.ProtectedPrefix, StringComparison.Ordinal) >= 0,
                    "legacy API key migrates to DPAPI format");
                string migratedBackup = SettingsStore.FilePath + ".bak";
                if (File.Exists(migratedBackup))
                {
                    string backupRaw = File.ReadAllText(migratedBackup, Encoding.UTF8);
                    Assert(backupRaw.IndexOf("legacy-storage-qa-key", StringComparison.Ordinal) < 0,
                        "legacy settings backup never retains plaintext API key");
                    Assert(backupRaw.IndexOf(ApiKeyProtector.ProtectedPrefix, StringComparison.Ordinal) >= 0,
                        "legacy settings backup is replaced with protected content");
                }
                Assert(!File.Exists(migratedCorrupt), "legacy corrupt settings sidecar is removed during credential migration");

                string invalidRoot = Path.Combine(parent, "credential-invalid-work");
                if (Directory.Exists(invalidRoot)) Directory.Delete(invalidRoot, true);
                Directory.CreateDirectory(invalidRoot);
                Environment.SetEnvironmentVariable("IROHA_APP_DATA_ROOT", invalidRoot);
                File.WriteAllText(
                    SettingsStore.FilePath,
                    "{\"ApiKey\":\"" + ApiKeyProtector.ProtectedPrefix + "not-base64\",\"BaseUrl\":\"https://api.deepseek.com\",\"Model\":\"deepseek-v4-flash\"}",
                    new UTF8Encoding(false));
                AppSettings invalid = SettingsStore.Load();
                Assert(string.IsNullOrWhiteSpace(invalid.ApiKey), "unreadable protected API key fails closed");
                Assert(!string.IsNullOrWhiteSpace(invalid.CredentialStatusMessage),
                    "unreadable protected API key produces a user-facing recovery message");
            }
            finally
            {
                Environment.SetEnvironmentVariable("IROHA_APP_DATA_ROOT", previousRoot);
            }
        }

        private static void AssertRequest(
            ModelApiClient client,
            string providerId,
            IList<Dictionary<string, string>> history,
            string expectedUrlPart,
            string expectedHeader)
        {
            AppSettings settings = BuildSettings(providerId);
            ModelProviderDefinition provider = ModelProviderCatalog.Get(providerId);
            ModelApiRequest request = client.BuildRequest(settings, provider, "system prompt", history);
            Assert(request.Url.IndexOf(expectedUrlPart, StringComparison.OrdinalIgnoreCase) >= 0,
                providerId + " request URL matches protocol");
            Assert(request.Headers.ContainsKey(expectedHeader), providerId + " request uses correct authentication header");
            Assert(request.JsonBody.IndexOf(settings.Model, StringComparison.Ordinal) >= 0 || provider.Protocol == ModelApiProtocol.GeminiGenerateContent,
                providerId + " request carries selected model");
            Assert(request.JsonBody.IndexOf("hello", StringComparison.Ordinal) >= 0,
                providerId + " request carries conversation history");
        }

        private static void AssertRequestRejected(
            ModelApiClient client,
            AppSettings settings,
            IList<Dictionary<string, string>> history,
            string expectedMessage,
            string label)
        {
            try
            {
                client.RequestAsync(settings, "system", history, TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
                Assert(false, label);
            }
            catch (InvalidOperationException ex)
            {
                Assert(ex.Message.IndexOf(expectedMessage, StringComparison.OrdinalIgnoreCase) >= 0, label);
            }
        }

        private static AppSettings BuildSettings(string providerId)
        {
            var settings = new AppSettings();
            ModelProviderCatalog.ActivateProvider(settings, providerId);
            ModelProviderDefinition provider = ModelProviderCatalog.Get(providerId);
            settings.ApiKey = "qa-placeholder-key";
            settings.BaseUrl = provider.Id == "azure-openai"
                ? "https://qa-resource.openai.azure.com"
                : provider.DefaultBaseUrl;
            settings.Model = provider.Id == "azure-openai"
                ? "qa-deployment"
                : (provider.Models.Length > 0 ? provider.Models[0] : "qa-model");
            return settings;
        }

        private static void Assert(bool condition, string label)
        {
            Results.Add((condition ? "PASS: " : "FAIL: ") + label);
            if (!condition) throw new InvalidOperationException(label);
        }

        private static void Flush()
        {
            string directory = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            File.WriteAllLines(reportPath, Results.ToArray(), new UTF8Encoding(false));
        }

        private static string GetArgument(string[] args, string name)
        {
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }
            return null;
        }

        private sealed class ToolLoopHandler : HttpMessageHandler
        {
            private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

            public List<string> RequestBodies { get; private set; }

            public ToolLoopHandler()
            {
                RequestBodies = new List<string>();
            }

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                string body = request.Content == null ? "" : await request.Content.ReadAsStringAsync();
                RequestBodies.Add(body);
                string responseBody = RequestBodies.Count <= AgentToolSession.MaxRounds
                    ? BuildToolCallResponse(RequestBodies.Count)
                    : BuildFinalResponse();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                };
            }

            private string BuildToolCallResponse(int round)
            {
                string arguments = serializer.Serialize(new Dictionary<string, object>
                {
                    { "mode", "expression" },
                    { "expression", "1+1" }
                });
                return serializer.Serialize(new Dictionary<string, object>
                {
                    { "choices", new object[]
                        {
                            new Dictionary<string, object>
                            {
                                { "message", new Dictionary<string, object>
                                    {
                                        { "content", "" },
                                        { "tool_calls", new object[]
                                            {
                                                new Dictionary<string, object>
                                                {
                                                    { "id", "qa-call-" + round },
                                                    { "type", "function" },
                                                    { "function", new Dictionary<string, object>
                                                        {
                                                            { "name", "calculator" },
                                                            { "arguments", arguments }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
            }

            private string BuildFinalResponse()
            {
                string content = serializer.Serialize(new Dictionary<string, object>
                {
                    { "zh", "计算结果是 2。" },
                    { "ja", "計算結果は2です。" },
                    { "mood", "happy" }
                });
                return serializer.Serialize(new Dictionary<string, object>
                {
                    { "choices", new object[]
                        {
                            new Dictionary<string, object>
                            {
                                { "message", new Dictionary<string, object> { { "content", content } } }
                            }
                        }
                    }
                });
            }
        }
    }
}
