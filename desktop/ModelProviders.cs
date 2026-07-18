using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace IrohaAgentDesktop
{
    internal enum ModelApiProtocol
    {
        OpenAiChat,
        OpenAiResponses,
        AnthropicMessages,
        GeminiGenerateContent,
        CohereChat,
        AzureOpenAiChat
    }

    internal sealed class ModelProviderDefinition
    {
        public string Id { get; private set; }
        public string DisplayName { get; private set; }
        public string BadgeText { get; private set; }
        public ModelApiProtocol Protocol { get; private set; }
        public string DefaultBaseUrl { get; private set; }
        public bool RequiresApiKey { get; private set; }
        public bool SupportsModelDiscovery { get; private set; }
        public string[] Models { get; private set; }

        public ModelProviderDefinition(
            string id,
            string displayName,
            string badgeText,
            ModelApiProtocol protocol,
            string defaultBaseUrl,
            bool requiresApiKey,
            bool supportsModelDiscovery,
            params string[] models)
        {
            Id = id;
            DisplayName = displayName;
            BadgeText = badgeText;
            Protocol = protocol;
            DefaultBaseUrl = defaultBaseUrl;
            RequiresApiKey = requiresApiKey;
            SupportsModelDiscovery = supportsModelDiscovery;
            Models = models ?? new string[0];
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    internal static class ModelProviderCatalog
    {
        public const string DefaultProviderId = "deepseek";

        private static readonly List<ModelProviderDefinition> Providers = new List<ModelProviderDefinition>
        {
            new ModelProviderDefinition("deepseek", "DeepSeek", "DS", ModelApiProtocol.OpenAiChat,
                "https://api.deepseek.com", true, true,
                "deepseek-v4-flash", "deepseek-v4-pro"),
            new ModelProviderDefinition("openai", "OpenAI", "GPT", ModelApiProtocol.OpenAiResponses,
                "https://api.openai.com/v1", true, true,
                "gpt-5.6-sol", "gpt-5.6-terra", "gpt-5.6-luna"),
            new ModelProviderDefinition("anthropic", "Anthropic Claude", "Claude", ModelApiProtocol.AnthropicMessages,
                "https://api.anthropic.com/v1", true, true,
                "claude-sonnet-5", "claude-opus-4-8", "claude-sonnet-4-6", "claude-haiku-4-5"),
            new ModelProviderDefinition("gemini", "Google Gemini", "Gemini", ModelApiProtocol.GeminiGenerateContent,
                "https://generativelanguage.googleapis.com/v1beta", true, true,
                "gemini-3.5-flash", "gemini-3.5-pro", "gemini-2.5-flash"),
            new ModelProviderDefinition("xai", "xAI Grok", "Grok", ModelApiProtocol.OpenAiChat,
                "https://api.x.ai/v1", true, true,
                "grok-4.5", "grok-4", "grok-3-mini"),
            new ModelProviderDefinition("mistral", "Mistral AI", "Mistral", ModelApiProtocol.OpenAiChat,
                "https://api.mistral.ai/v1", true, true,
                "mistral-large-latest", "mistral-medium-latest", "mistral-small-latest"),
            new ModelProviderDefinition("cohere", "Cohere", "Cohere", ModelApiProtocol.CohereChat,
                "https://api.cohere.com/v2", true, true,
                "command-a-plus-05-2026", "command-a-03-2025"),
            new ModelProviderDefinition("openrouter", "OpenRouter", "Router", ModelApiProtocol.OpenAiChat,
                "https://openrouter.ai/api/v1", true, true,
                "openai/gpt-5.6", "anthropic/claude-sonnet-5", "google/gemini-3.5-flash", "deepseek/deepseek-v4-pro"),
            new ModelProviderDefinition("qwen", "阿里云通义千问", "Qwen", ModelApiProtocol.OpenAiChat,
                "https://dashscope.aliyuncs.com/compatible-mode/v1", true, true,
                "qwen3.7-max", "qwen3.7-plus", "qwen3.6-plus", "qwen3.6-flash"),
            new ModelProviderDefinition("zhipu", "智谱 GLM", "GLM", ModelApiProtocol.OpenAiChat,
                "https://open.bigmodel.cn/api/paas/v4", true, true,
                "glm-5.2", "glm-5.1", "glm-5-turbo", "glm-4.7-flash"),
            new ModelProviderDefinition("siliconflow", "硅基流动 SiliconFlow", "SF", ModelApiProtocol.OpenAiChat,
                "https://api.siliconflow.cn/v1", true, true,
                "Pro/zai-org/GLM-5", "deepseek-ai/DeepSeek-V3.2", "Qwen/Qwen3.5-397B-A17B"),
            new ModelProviderDefinition("minimax", "MiniMax", "MiniMax", ModelApiProtocol.OpenAiChat,
                "https://api.minimaxi.com/v1", true, true,
                "MiniMax-M2.7", "MiniMax-M2.7-highspeed", "MiniMax-M2.5"),
            new ModelProviderDefinition("groq", "Groq", "Groq", ModelApiProtocol.OpenAiChat,
                "https://api.groq.com/openai/v1", true, true,
                "openai/gpt-oss-120b", "openai/gpt-oss-20b"),
            new ModelProviderDefinition("together", "Together AI", "Together", ModelApiProtocol.OpenAiChat,
                "https://api.together.xyz/v1", true, true,
                "openai/gpt-oss-120b", "openai/gpt-oss-20b", "meta-llama/Llama-3.3-70B-Instruct-Turbo"),
            new ModelProviderDefinition("moonshot", "Moonshot Kimi", "Kimi", ModelApiProtocol.OpenAiChat,
                "https://api.moonshot.cn/v1", true, true,
                "kimi-k2.5", "moonshot-v1-128k", "moonshot-v1-32k"),
            new ModelProviderDefinition("perplexity", "Perplexity", "PPLX", ModelApiProtocol.OpenAiChat,
                "https://api.perplexity.ai", true, true,
                "sonar-pro", "sonar", "sonar-reasoning-pro"),
            new ModelProviderDefinition("nvidia", "NVIDIA NIM", "NVIDIA", ModelApiProtocol.OpenAiChat,
                "https://integrate.api.nvidia.com/v1", true, true,
                "meta/llama-3.3-70b-instruct", "deepseek-ai/deepseek-r1"),
            new ModelProviderDefinition("fireworks", "Fireworks AI", "FW", ModelApiProtocol.OpenAiChat,
                "https://api.fireworks.ai/inference/v1", true, true,
                "accounts/fireworks/models/llama-v3p3-70b-instruct", "accounts/fireworks/models/deepseek-v3p2"),
            new ModelProviderDefinition("qianfan", "百度智能云千帆", "ERNIE", ModelApiProtocol.OpenAiChat,
                "https://qianfan.baidubce.com/v2", true, true,
                "ernie-4.5-turbo-20260402", "deepseek-v4-flash", "deepseek-v4-pro", "kimi-k2.6"),
            new ModelProviderDefinition("hunyuan", "腾讯混元", "混元", ModelApiProtocol.OpenAiChat,
                "https://api.hunyuan.cloud.tencent.com/v1", true, true,
                "hunyuan-turbos-latest", "hunyuan-vision"),
            new ModelProviderDefinition("volcengine", "火山方舟 / 豆包", "豆包", ModelApiProtocol.OpenAiResponses,
                "https://ark.cn-beijing.volces.com/api/v3", true, true,
                "doubao-seed-2-0-lite-260215"),
            new ModelProviderDefinition("github-models", "GitHub Models", "GitHub", ModelApiProtocol.OpenAiChat,
                "https://models.github.ai/inference", true, false,
                "openai/gpt-4.1", "deepseek/DeepSeek-V3-0324", "meta/Llama-3.3-70B-Instruct"),
            new ModelProviderDefinition("huggingface", "Hugging Face", "HF", ModelApiProtocol.OpenAiChat,
                "https://router.huggingface.co/v1", true, true,
                "openai/gpt-oss-120b:fastest", "deepseek-ai/DeepSeek-V4-Pro:fastest", "zai-org/GLM-5.1:fastest"),
            new ModelProviderDefinition("ollama", "Ollama（本地）", "Local", ModelApiProtocol.OpenAiChat,
                "http://127.0.0.1:11434/v1", false, true,
                "qwen3:8b", "llama3.2"),
            new ModelProviderDefinition("lmstudio", "LM Studio（本地）", "Local", ModelApiProtocol.OpenAiChat,
                "http://127.0.0.1:1234/v1", false, true,
                "填写或刷新已加载模型"),
            new ModelProviderDefinition("azure-openai", "Microsoft Azure OpenAI", "Azure", ModelApiProtocol.AzureOpenAiChat,
                "", true, false,
                "填写部署名称"),
            new ModelProviderDefinition("custom", "自定义 OpenAI 兼容接口", "Custom", ModelApiProtocol.OpenAiChat,
                "http://127.0.0.1:1234/v1", false, true,
                "填写或刷新模型名称")
        };

        public static IList<ModelProviderDefinition> All
        {
            get { return Providers.AsReadOnly(); }
        }

        public static ModelProviderDefinition Get(string providerId)
        {
            string id = string.IsNullOrWhiteSpace(providerId) ? DefaultProviderId : providerId.Trim();
            foreach (ModelProviderDefinition provider in Providers)
            {
                if (string.Equals(provider.Id, id, StringComparison.OrdinalIgnoreCase)) return provider;
            }
            return Providers[0];
        }

        public static bool HasCredentials(AppSettings settings)
        {
            if (settings == null) return false;
            ModelProviderDefinition provider = Get(settings.ProviderId);
            return !provider.RequiresApiKey || !string.IsNullOrWhiteSpace(settings.ApiKey);
        }

        public static string GetActiveDisplayName(AppSettings settings)
        {
            return Get(settings == null ? null : settings.ProviderId).DisplayName;
        }

        public static string GetActiveBadge(AppSettings settings)
        {
            ModelProviderDefinition provider = Get(settings == null ? null : settings.ProviderId);
            string model = settings == null ? "" : (settings.Model ?? "");
            string normalized = model.Replace("_", "-").ToLowerInvariant();
            if (provider.Id == "deepseek")
            {
                if (normalized.Contains("flash")) return "Flash";
                if (normalized.Contains("pro")) return "Pro";
                if (normalized.Contains("reasoner")) return "R1";
            }
            return provider.BadgeText;
        }

        public static void NormalizeSettings(AppSettings settings)
        {
            if (settings == null) return;
            EnsureProfileMaps(settings);

            string inferred = InferProviderId(settings.ProviderId, settings.BaseUrl, settings.Model);
            settings.ProviderId = Get(inferred).Id;

            string saved;
            if (settings.ProviderApiKeys.TryGetValue(settings.ProviderId, out saved))
            {
                settings.ApiKey = saved ?? "";
            }
            else
            {
                settings.ProviderApiKeys[settings.ProviderId] = settings.ApiKey ?? "";
            }

            if (settings.ProviderModels.TryGetValue(settings.ProviderId, out saved) && !string.IsNullOrWhiteSpace(saved))
            {
                settings.Model = saved.Trim();
            }
            else
            {
                settings.Model = ChooseInitialModel(settings.ProviderId, settings.Model);
                settings.ProviderModels[settings.ProviderId] = settings.Model;
            }

            if (settings.ProviderBaseUrls.TryGetValue(settings.ProviderId, out saved) && !string.IsNullOrWhiteSpace(saved))
            {
                settings.BaseUrl = saved.Trim();
            }
            else
            {
                settings.BaseUrl = ChooseInitialBaseUrl(settings.ProviderId, settings.BaseUrl);
                settings.ProviderBaseUrls[settings.ProviderId] = settings.BaseUrl;
            }
        }

        public static void SaveActiveProfile(AppSettings settings)
        {
            if (settings == null) return;
            EnsureProfileMaps(settings);
            settings.ProviderId = Get(settings.ProviderId).Id;
            settings.ProviderApiKeys[settings.ProviderId] = settings.ApiKey ?? "";
            settings.ProviderModels[settings.ProviderId] = settings.Model ?? "";
            settings.ProviderBaseUrls[settings.ProviderId] = settings.BaseUrl ?? "";
        }

        public static void ActivateProvider(AppSettings settings, string providerId)
        {
            if (settings == null) return;
            SaveActiveProfile(settings);
            EnsureProfileMaps(settings);

            ModelProviderDefinition provider = Get(providerId);
            settings.ProviderId = provider.Id;

            string value;
            settings.ApiKey = settings.ProviderApiKeys.TryGetValue(provider.Id, out value) ? value ?? "" : "";
            settings.Model = settings.ProviderModels.TryGetValue(provider.Id, out value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : ChooseInitialModel(provider.Id, null);
            settings.BaseUrl = settings.ProviderBaseUrls.TryGetValue(provider.Id, out value) && !string.IsNullOrWhiteSpace(value)
                ? value.Trim()
                : provider.DefaultBaseUrl;
            SaveActiveProfile(settings);
        }

        public static Dictionary<string, string> CopyProfiles(Dictionary<string, string> source)
        {
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (source == null) return copy;
            foreach (KeyValuePair<string, string> item in source) copy[item.Key] = item.Value;
            return copy;
        }

        private static void EnsureProfileMaps(AppSettings settings)
        {
            if (settings.ProviderApiKeys == null) settings.ProviderApiKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (settings.ProviderModels == null) settings.ProviderModels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (settings.ProviderBaseUrls == null) settings.ProviderBaseUrls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        private static string InferProviderId(string configured, string baseUrl, string model)
        {
            if (!string.IsNullOrWhiteSpace(configured))
            {
                foreach (ModelProviderDefinition provider in Providers)
                {
                    if (string.Equals(provider.Id, configured.Trim(), StringComparison.OrdinalIgnoreCase)) return provider.Id;
                }
            }

            string url = (baseUrl ?? "").ToLowerInvariant();
            foreach (ModelProviderDefinition provider in Providers)
            {
                if (!string.IsNullOrWhiteSpace(provider.DefaultBaseUrl) &&
                    url.StartsWith(provider.DefaultBaseUrl.ToLowerInvariant(), StringComparison.Ordinal)) return provider.Id;
            }

            string modelName = (model ?? "").ToLowerInvariant();
            if (modelName.StartsWith("gpt-")) return "openai";
            if (modelName.StartsWith("claude-")) return "anthropic";
            if (modelName.StartsWith("gemini-")) return "gemini";
            if (modelName.StartsWith("deepseek-")) return "deepseek";
            return DefaultProviderId;
        }

        private static string ChooseInitialModel(string providerId, string current)
        {
            if (!string.IsNullOrWhiteSpace(current) && current != "填写部署名称" && current != "填写或刷新模型名称")
            {
                return current.Trim();
            }
            ModelProviderDefinition provider = Get(providerId);
            return provider.Models.Length > 0 ? provider.Models[0] : "";
        }

        private static string ChooseInitialBaseUrl(string providerId, string current)
        {
            ModelProviderDefinition provider = Get(providerId);
            if (!string.IsNullOrWhiteSpace(current)) return current.Trim();
            return provider.DefaultBaseUrl;
        }
    }

    internal sealed class ModelApiClient
    {
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public async Task<string> RequestAsync(
            AppSettings settings,
            string systemPrompt,
            IList<Dictionary<string, string>> history,
            TimeSpan timeout)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            ModelProviderDefinition provider = ModelProviderCatalog.Get(settings.ProviderId);
            Validate(settings, provider);

            ModelApiRequest request = BuildRequest(settings, provider, systemPrompt, history);
            using (var client = new HttpClient())
            {
                client.Timeout = timeout;
                client.MaxResponseContentBufferSize = 8L * 1024L * 1024L;
                using (var message = new HttpRequestMessage(HttpMethod.Post, request.Url))
                {
                    foreach (KeyValuePair<string, string> header in request.Headers)
                    {
                        message.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                    message.Content = new StringContent(request.JsonBody, Encoding.UTF8, "application/json");
                    using (HttpResponseMessage response = await client.SendAsync(message))
                    {
                        string responseText = await response.Content.ReadAsStringAsync();
                        if (!response.IsSuccessStatusCode)
                        {
                            throw CreateHttpException(provider, response, responseText, settings.ApiKey);
                        }
                        return ExtractResponseText(provider.Protocol, responseText);
                    }
                }
            }
        }

        public async Task<string> RequestWithToolsAsync(
            AppSettings settings,
            string systemPrompt,
            IList<Dictionary<string, string>> history,
            AgentToolSession toolSession,
            TimeSpan timeout)
        {
            if (toolSession == null || toolSession.EnabledDefinitions.Count == 0)
            {
                return await RequestAsync(settings, systemPrompt, history, timeout);
            }
            if (settings == null) throw new ArgumentNullException("settings");
            ModelProviderDefinition provider = ModelProviderCatalog.Get(settings.ProviderId);
            Validate(settings, provider);

            var conversation = new List<ModelConversationMessage>();
            if (history != null)
            {
                foreach (Dictionary<string, string> item in history)
                {
                    string role = ReadHistory(item, "role") == "assistant" ? "assistant" : "user";
                    conversation.Add(new ModelConversationMessage { Role = role, Text = ReadHistory(item, "content") });
                }
            }

            using (var client = new HttpClient())
            {
                client.Timeout = timeout;
                client.MaxResponseContentBufferSize = 8L * 1024L * 1024L;
                for (int round = 0; round < AgentToolSession.MaxRounds; round++)
                {
                    ModelApiRequest request = BuildToolRequest(settings, provider, systemPrompt, conversation, toolSession.EnabledDefinitions);
                    string responseText = null;
                    bool usePlainFallback = false;
                    try
                    {
                        responseText = await PostAsync(client, request, provider, settings.ApiKey);
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (round == 0 && IsToolSchemaRejected(ex.Message))
                        {
                            usePlainFallback = true;
                        }
                        else throw;
                    }
                    if (usePlainFallback) return await RequestAsync(settings, systemPrompt, history, timeout);
                    ModelToolTurn turn = ExtractToolTurn(provider.Protocol, responseText);
                    if (turn.ToolCalls.Count == 0)
                    {
                        if (string.IsNullOrWhiteSpace(turn.Text)) throw new InvalidOperationException("模型响应中没有可显示的文本。");
                        return turn.Text.Trim();
                    }
                    if (turn.ToolCalls.Count > AgentToolSession.MaxCallsPerRound)
                    {
                        throw new InvalidOperationException("模型一次请求了过多工具，已停止本轮操作。");
                    }

                    conversation.Add(new ModelConversationMessage { Role = "assistant", Text = turn.Text, ToolCalls = turn.ToolCalls });
                    var results = new List<ModelToolResultMessage>();
                    foreach (AgentToolCall call in turn.ToolCalls)
                    {
                        AgentToolResult result = await toolSession.ExecuteAsync(call);
                        results.Add(new ModelToolResultMessage
                        {
                            CallId = call.Id,
                            ToolName = call.Name,
                            ResultText = result.ToModelText()
                        });
                    }
                    conversation.Add(new ModelConversationMessage { Role = "tool", ToolResults = results });
                }
            }
            throw new InvalidOperationException("工具调用已达到 4 轮上限，请缩小任务范围后重试。");
        }

        private static bool IsToolSchemaRejected(string message)
        {
            string value = (message ?? "").ToLowerInvariant();
            bool clientError = value.Contains("http 400") || value.Contains("http 404") || value.Contains("http 422");
            if (!clientError) return false;
            return value.Contains("tool") || value.Contains("function") || value.Contains("schema") ||
                value.Contains("additionalproperties") || value.Contains("工具") || value.Contains("函数");
        }

        private async Task<string> PostAsync(HttpClient client, ModelApiRequest request, ModelProviderDefinition provider, string apiKey)
        {
            using (var message = new HttpRequestMessage(HttpMethod.Post, request.Url))
            {
                foreach (KeyValuePair<string, string> header in request.Headers) message.Headers.TryAddWithoutValidation(header.Key, header.Value);
                message.Content = new StringContent(request.JsonBody, Encoding.UTF8, "application/json");
                using (HttpResponseMessage response = await client.SendAsync(message))
                {
                    string responseText = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode) throw CreateHttpException(provider, response, responseText, apiKey);
                    return responseText;
                }
            }
        }

        public Task<string> TestAsync(AppSettings settings)
        {
            var messages = new List<Dictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    { "role", "user" },
                    { "content", "测试连接，只回复连接正常。" }
                }
            };
            return RequestAsync(
                settings,
                "你是连接测试助手。请只回复：连接正常。",
                messages,
                TimeSpan.FromSeconds(45));
        }

        public async Task<string> AnalyzeImageAsync(AppSettings settings, string imagePath, string question, TimeSpan timeout)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            ModelProviderDefinition provider = ModelProviderCatalog.Get(settings.ProviderId);
            Validate(settings, provider);
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath)) throw new FileNotFoundException("图片不存在", imagePath);
            var file = new FileInfo(imagePath);
            if (file.Length <= 0 || file.Length > 10L * 1024L * 1024L) throw new InvalidOperationException("图片大小必须在 10 MB 以内");
            string mimeType = GetImageMimeType(imagePath);
            if (mimeType.Length == 0) throw new InvalidOperationException("不支持该图片格式");
            string base64 = Convert.ToBase64String(File.ReadAllBytes(imagePath));
            ModelApiRequest request = BuildImageRequest(settings, provider, question, mimeType, base64);
            using (var client = new HttpClient { Timeout = timeout, MaxResponseContentBufferSize = 8L * 1024L * 1024L })
            {
                string responseText = await PostAsync(client, request, provider, settings.ApiKey);
                return ExtractResponseText(provider.Protocol, responseText);
            }
        }

        internal ModelApiRequest BuildImageRequest(AppSettings settings, ModelProviderDefinition provider, string question, string mimeType, string base64)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            AddAuthenticationHeaders(headers, settings, provider);
            string prompt = string.IsNullOrWhiteSpace(question) ? "请用中文客观描述这张图片。" : question.Trim();
            string dataUri = "data:" + mimeType + ";base64," + base64;
            string url;
            object payload;
            switch (provider.Protocol)
            {
                case ModelApiProtocol.OpenAiResponses:
                    url = AppendPath(settings.BaseUrl, "responses");
                    payload = new Dictionary<string, object>
                    {
                        { "model", settings.Model },
                        { "instructions", "你是图片理解助手。只根据图片中可见信息回答，不确定时明确说明。" },
                        { "input", new object[] { new Dictionary<string, object>
                            {
                                { "role", "user" }, { "content", new object[]
                                    {
                                        new Dictionary<string, object> { { "type", "input_text" }, { "text", prompt } },
                                        new Dictionary<string, object> { { "type", "input_image" }, { "image_url", dataUri } }
                                    }
                                }
                            }
                        } }
                    };
                    break;
                case ModelApiProtocol.AnthropicMessages:
                    url = AppendPath(settings.BaseUrl, "messages");
                    payload = new Dictionary<string, object>
                    {
                        { "model", settings.Model }, { "max_tokens", 2048 },
                        { "system", "你是图片理解助手。只根据图片中可见信息回答，不确定时明确说明。" },
                        { "messages", new object[] { new Dictionary<string, object>
                            {
                                { "role", "user" }, { "content", new object[]
                                    {
                                        new Dictionary<string, object> { { "type", "image" }, { "source", new Dictionary<string, object> { { "type", "base64" }, { "media_type", mimeType }, { "data", base64 } } } },
                                        new Dictionary<string, object> { { "type", "text" }, { "text", prompt } }
                                    }
                                }
                            }
                        } }
                    };
                    break;
                case ModelApiProtocol.GeminiGenerateContent:
                    url = AppendPath(settings.BaseUrl, "models/" + Uri.EscapeDataString(settings.Model) + ":generateContent");
                    payload = new Dictionary<string, object>
                    {
                        { "systemInstruction", new Dictionary<string, object> { { "parts", new object[] { new Dictionary<string, object> { { "text", "你是图片理解助手。只根据图片中可见信息回答，不确定时明确说明。" } } } } } },
                        { "contents", new object[] { new Dictionary<string, object>
                            {
                                { "role", "user" }, { "parts", new object[]
                                    {
                                        new Dictionary<string, object> { { "text", prompt } },
                                        new Dictionary<string, object> { { "inlineData", new Dictionary<string, object> { { "mimeType", mimeType }, { "data", base64 } } } }
                                    }
                                }
                            }
                        } }
                    };
                    break;
                case ModelApiProtocol.CohereChat:
                    url = AppendPath(settings.BaseUrl, "chat");
                    payload = BuildOpenAiCompatibleImagePayload(settings.Model, prompt, dataUri);
                    break;
                case ModelApiProtocol.AzureOpenAiChat:
                    url = BuildAzureChatUrl(settings.BaseUrl, settings.Model);
                    payload = BuildOpenAiCompatibleImagePayload(settings.Model, prompt, dataUri);
                    break;
                default:
                    url = AppendPath(settings.BaseUrl, "chat/completions");
                    payload = BuildOpenAiCompatibleImagePayload(settings.Model, prompt, dataUri);
                    break;
            }
            return new ModelApiRequest { Url = url, Headers = headers, JsonBody = serializer.Serialize(payload) };
        }

        private static object BuildOpenAiCompatibleImagePayload(string model, string prompt, string dataUri)
        {
            return new Dictionary<string, object>
            {
                { "model", model },
                { "messages", new object[]
                    {
                        new Dictionary<string, object> { { "role", "system" }, { "content", "你是图片理解助手。只根据图片中可见信息回答，不确定时明确说明。" } },
                        new Dictionary<string, object> { { "role", "user" }, { "content", new object[]
                            {
                                new Dictionary<string, object> { { "type", "text" }, { "text", prompt } },
                                new Dictionary<string, object> { { "type", "image_url" }, { "image_url", new Dictionary<string, object> { { "url", dataUri } } } }
                            }
                        } }
                    }
                }
            };
        }

        private static string GetImageMimeType(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".png") return "image/png";
            if (extension == ".jpg" || extension == ".jpeg") return "image/jpeg";
            if (extension == ".gif") return "image/gif";
            if (extension == ".webp") return "image/webp";
            if (extension == ".bmp") return "image/bmp";
            return "";
        }

        public async Task<IList<string>> DiscoverModelsAsync(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            ModelProviderDefinition provider = ModelProviderCatalog.Get(settings.ProviderId);
            ValidateBaseUrl(settings, provider);
            if (!provider.SupportsModelDiscovery)
            {
                throw new InvalidOperationException(provider.DisplayName + " 需要填写部署名称，不能自动读取模型列表。");
            }
            if (provider.RequiresApiKey && string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                throw new InvalidOperationException("请先填写 " + provider.DisplayName + " 的 API Key。");
            }

            string url = BuildModelsUrl(settings, provider);
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.MaxResponseContentBufferSize = 8L * 1024L * 1024L;
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    AddAuthenticationHeaders(request, settings, provider);
                    using (HttpResponseMessage response = await client.SendAsync(request))
                    {
                        string responseText = await response.Content.ReadAsStringAsync();
                        if (!response.IsSuccessStatusCode)
                        {
                            throw CreateHttpException(provider, response, responseText, settings.ApiKey);
                        }
                        IList<string> models = ExtractModelIds(provider.Protocol, responseText);
                        if (models.Count == 0) throw new InvalidOperationException("接口已连接，但没有返回可用模型。");
                        return models;
                    }
                }
            }
        }

        internal ModelApiRequest BuildRequest(
            AppSettings settings,
            ModelProviderDefinition provider,
            string systemPrompt,
            IList<Dictionary<string, string>> history)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            AddAuthenticationHeaders(headers, settings, provider);
            object payload;
            string url;

            switch (provider.Protocol)
            {
                case ModelApiProtocol.OpenAiResponses:
                    url = AppendPath(settings.BaseUrl, "responses");
                    payload = BuildOpenAiResponsesPayload(settings.Model, systemPrompt, history);
                    break;
                case ModelApiProtocol.AnthropicMessages:
                    url = AppendPath(settings.BaseUrl, "messages");
                    payload = BuildAnthropicPayload(settings.Model, systemPrompt, history);
                    break;
                case ModelApiProtocol.GeminiGenerateContent:
                    url = AppendPath(settings.BaseUrl, "models/" + Uri.EscapeDataString(settings.Model) + ":generateContent");
                    payload = BuildGeminiPayload(systemPrompt, history);
                    break;
                case ModelApiProtocol.CohereChat:
                    url = AppendPath(settings.BaseUrl, "chat");
                    payload = BuildCoherePayload(settings.Model, systemPrompt, history);
                    break;
                case ModelApiProtocol.AzureOpenAiChat:
                    url = BuildAzureChatUrl(settings.BaseUrl, settings.Model);
                    payload = BuildOpenAiChatPayload(settings.Model, systemPrompt, history);
                    break;
                default:
                    url = AppendPath(settings.BaseUrl, "chat/completions");
                    payload = BuildOpenAiChatPayload(settings.Model, systemPrompt, history);
                    break;
            }

            return new ModelApiRequest
            {
                Url = url,
                Headers = headers,
                JsonBody = serializer.Serialize(payload)
            };
        }

        internal ModelApiRequest BuildToolRequest(
            AppSettings settings,
            ModelProviderDefinition provider,
            string systemPrompt,
            IList<ModelConversationMessage> conversation,
            IList<AgentToolDefinition> tools)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            AddAuthenticationHeaders(headers, settings, provider);
            object payload;
            string url;
            switch (provider.Protocol)
            {
                case ModelApiProtocol.OpenAiResponses:
                    url = AppendPath(settings.BaseUrl, "responses");
                    payload = BuildOpenAiResponsesToolPayload(settings.Model, systemPrompt, conversation, tools);
                    break;
                case ModelApiProtocol.AnthropicMessages:
                    url = AppendPath(settings.BaseUrl, "messages");
                    payload = BuildAnthropicToolPayload(settings.Model, systemPrompt, conversation, tools);
                    break;
                case ModelApiProtocol.GeminiGenerateContent:
                    url = AppendPath(settings.BaseUrl, "models/" + Uri.EscapeDataString(settings.Model) + ":generateContent");
                    payload = BuildGeminiToolPayload(systemPrompt, conversation, tools);
                    break;
                case ModelApiProtocol.CohereChat:
                    url = AppendPath(settings.BaseUrl, "chat");
                    payload = BuildCohereToolPayload(settings.Model, systemPrompt, conversation, tools);
                    break;
                case ModelApiProtocol.AzureOpenAiChat:
                    url = BuildAzureChatUrl(settings.BaseUrl, settings.Model);
                    payload = BuildOpenAiChatToolPayload(settings.Model, systemPrompt, conversation, tools);
                    break;
                default:
                    url = AppendPath(settings.BaseUrl, "chat/completions");
                    payload = BuildOpenAiChatToolPayload(settings.Model, systemPrompt, conversation, tools);
                    break;
            }
            return new ModelApiRequest { Url = url, Headers = headers, JsonBody = serializer.Serialize(payload) };
        }

        internal ModelToolTurn ExtractToolTurn(ModelApiProtocol protocol, string responseText)
        {
            object rootObject = serializer.DeserializeObject(responseText);
            var root = rootObject as Dictionary<string, object>;
            if (root == null) throw new InvalidOperationException("模型响应不是有效 JSON。");
            if (protocol == ModelApiProtocol.OpenAiResponses) return ExtractOpenAiResponsesTurn(root);
            if (protocol == ModelApiProtocol.AnthropicMessages) return ExtractAnthropicTurn(root);
            if (protocol == ModelApiProtocol.GeminiGenerateContent) return ExtractGeminiTurn(root);
            if (protocol == ModelApiProtocol.CohereChat) return ExtractOpenAiCompatibleTurn(root, true);
            return ExtractOpenAiCompatibleTurn(root, false);
        }

        internal string ExtractResponseText(ModelApiProtocol protocol, string responseText)
        {
            object rootObject = serializer.DeserializeObject(responseText);
            var root = rootObject as Dictionary<string, object>;
            if (root == null) throw new InvalidOperationException("模型响应不是有效 JSON。");

            string text;
            switch (protocol)
            {
                case ModelApiProtocol.OpenAiResponses:
                    text = ExtractOpenAiResponsesText(root);
                    break;
                case ModelApiProtocol.AnthropicMessages:
                    text = ExtractTextBlocks(root, "content");
                    break;
                case ModelApiProtocol.GeminiGenerateContent:
                    text = ExtractGeminiText(root);
                    break;
                case ModelApiProtocol.CohereChat:
                    text = ExtractNestedTextBlocks(root, "message", "content");
                    break;
                default:
                    text = ExtractOpenAiChoiceText(root);
                    break;
            }
            if (string.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("模型响应中没有可显示的文本。");
            return text.Trim();
        }

        internal IList<string> ExtractModelIds(ModelApiProtocol protocol, string responseText)
        {
            var result = new List<string>();
            object rootObject = serializer.DeserializeObject(responseText);
            var root = rootObject as Dictionary<string, object>;
            if (root == null) return result;

            object dataObject;
            object[] entries = null;
            if (root.TryGetValue("data", out dataObject)) entries = dataObject as object[];
            if (entries == null && root.TryGetValue("models", out dataObject)) entries = dataObject as object[];
            if (entries == null) return result;

            foreach (object entryObject in entries)
            {
                var entry = entryObject as Dictionary<string, object>;
                if (entry == null) continue;
                string id = ReadString(entry, "id");
                if (string.IsNullOrWhiteSpace(id)) id = ReadString(entry, "name");
                if (id.StartsWith("models/", StringComparison.OrdinalIgnoreCase)) id = id.Substring(7);
                if (!string.IsNullOrWhiteSpace(id) && !result.Contains(id)) result.Add(id);
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }

        private static object BuildOpenAiChatPayload(string model, string systemPrompt, IList<Dictionary<string, string>> history)
        {
            var messages = new List<object>
            {
                new Dictionary<string, object> { { "role", "system" }, { "content", systemPrompt ?? "" } }
            };
            AppendHistory(messages, history, false);
            return new Dictionary<string, object>
            {
                { "model", model },
                { "messages", messages }
            };
        }

        private static object BuildOpenAiResponsesPayload(string model, string systemPrompt, IList<Dictionary<string, string>> history)
        {
            var input = new List<object>();
            if (history != null)
            {
                foreach (Dictionary<string, string> item in history)
                {
                    string role = ReadHistory(item, "role");
                    if (role != "assistant") role = "user";
                    input.Add(new Dictionary<string, object>
                    {
                        { "role", role },
                        { "content", ReadHistory(item, "content") }
                    });
                }
            }
            return new Dictionary<string, object>
            {
                { "model", model },
                { "instructions", systemPrompt ?? "" },
                { "input", input }
            };
        }

        private static object BuildAnthropicPayload(string model, string systemPrompt, IList<Dictionary<string, string>> history)
        {
            var messages = new List<object>();
            AppendHistory(messages, history, true);
            return new Dictionary<string, object>
            {
                { "model", model },
                { "max_tokens", 2048 },
                { "system", systemPrompt ?? "" },
                { "messages", messages }
            };
        }

        private static object BuildGeminiPayload(string systemPrompt, IList<Dictionary<string, string>> history)
        {
            var contents = new List<object>();
            if (history != null)
            {
                foreach (Dictionary<string, string> item in history)
                {
                    contents.Add(new Dictionary<string, object>
                    {
                        { "role", ReadHistory(item, "role") == "assistant" ? "model" : "user" },
                        { "parts", new object[] { new Dictionary<string, object> { { "text", ReadHistory(item, "content") } } } }
                    });
                }
            }
            return new Dictionary<string, object>
            {
                { "systemInstruction", new Dictionary<string, object>
                    {
                        { "parts", new object[] { new Dictionary<string, object> { { "text", systemPrompt ?? "" } } } }
                    }
                },
                { "contents", contents }
            };
        }

        private static object BuildCoherePayload(string model, string systemPrompt, IList<Dictionary<string, string>> history)
        {
            var messages = new List<object>
            {
                new Dictionary<string, object> { { "role", "system" }, { "content", systemPrompt ?? "" } }
            };
            AppendHistory(messages, history, false);
            return new Dictionary<string, object>
            {
                { "model", model },
                { "messages", messages }
            };
        }

        private object BuildOpenAiChatToolPayload(string model, string systemPrompt, IList<ModelConversationMessage> conversation, IList<AgentToolDefinition> tools)
        {
            return new Dictionary<string, object>
            {
                { "model", model },
                { "messages", BuildOpenAiToolMessages(systemPrompt, conversation, false) },
                { "tools", BuildOpenAiNestedToolDefinitions(tools) },
                { "tool_choice", "auto" }
            };
        }

        private object BuildCohereToolPayload(string model, string systemPrompt, IList<ModelConversationMessage> conversation, IList<AgentToolDefinition> tools)
        {
            return new Dictionary<string, object>
            {
                { "model", model },
                { "messages", BuildOpenAiToolMessages(systemPrompt, conversation, true) },
                { "tools", BuildOpenAiNestedToolDefinitions(tools) }
            };
        }

        private List<object> BuildOpenAiToolMessages(string systemPrompt, IList<ModelConversationMessage> conversation, bool cohere)
        {
            var messages = new List<object> { new Dictionary<string, object> { { "role", "system" }, { "content", systemPrompt ?? "" } } };
            foreach (ModelConversationMessage item in conversation ?? new List<ModelConversationMessage>())
            {
                if (item.Role == "assistant" && item.ToolCalls != null && item.ToolCalls.Count > 0)
                {
                    var calls = new List<object>();
                    foreach (AgentToolCall call in item.ToolCalls)
                    {
                        calls.Add(new Dictionary<string, object>
                        {
                            { "id", call.Id }, { "type", "function" },
                            { "function", new Dictionary<string, object> { { "name", call.Name }, { "arguments", NormalizeArgumentsJson(call) } } }
                        });
                    }
                    messages.Add(new Dictionary<string, object> { { "role", "assistant" }, { "content", item.Text ?? "" }, { "tool_calls", calls } });
                }
                else if (item.Role == "tool" && item.ToolResults != null)
                {
                    foreach (ModelToolResultMessage result in item.ToolResults)
                    {
                        object content = result.ResultText ?? "";
                        if (cohere)
                        {
                            content = new object[] { new Dictionary<string, object>
                            {
                                { "type", "document" }, { "document", new Dictionary<string, object> { { "data", new Dictionary<string, object> { { "result", result.ResultText ?? "" } } } } }
                            } };
                        }
                        messages.Add(new Dictionary<string, object> { { "role", "tool" }, { "tool_call_id", result.CallId }, { "content", content } });
                    }
                }
                else
                {
                    messages.Add(new Dictionary<string, object> { { "role", item.Role == "assistant" ? "assistant" : "user" }, { "content", item.Text ?? "" } });
                }
            }
            return messages;
        }

        private object BuildOpenAiResponsesToolPayload(string model, string systemPrompt, IList<ModelConversationMessage> conversation, IList<AgentToolDefinition> tools)
        {
            var input = new List<object>();
            foreach (ModelConversationMessage item in conversation ?? new List<ModelConversationMessage>())
            {
                if (item.Role == "assistant" && item.ToolCalls != null && item.ToolCalls.Count > 0)
                {
                    if (!string.IsNullOrWhiteSpace(item.Text)) input.Add(new Dictionary<string, object> { { "role", "assistant" }, { "content", item.Text } });
                    foreach (AgentToolCall call in item.ToolCalls)
                    {
                        input.Add(new Dictionary<string, object> { { "type", "function_call" }, { "call_id", call.Id }, { "name", call.Name }, { "arguments", NormalizeArgumentsJson(call) } });
                    }
                }
                else if (item.Role == "tool" && item.ToolResults != null)
                {
                    foreach (ModelToolResultMessage result in item.ToolResults)
                    {
                        input.Add(new Dictionary<string, object> { { "type", "function_call_output" }, { "call_id", result.CallId }, { "output", result.ResultText ?? "" } });
                    }
                }
                else
                {
                    input.Add(new Dictionary<string, object> { { "role", item.Role == "assistant" ? "assistant" : "user" }, { "content", item.Text ?? "" } });
                }
            }
            var definitions = new List<object>();
            foreach (AgentToolDefinition tool in tools)
            {
                definitions.Add(new Dictionary<string, object> { { "type", "function" }, { "name", tool.Name }, { "description", tool.Description }, { "parameters", tool.BuildParametersSchema() } });
            }
            return new Dictionary<string, object> { { "model", model }, { "instructions", systemPrompt ?? "" }, { "input", input }, { "tools", definitions }, { "tool_choice", "auto" } };
        }

        private object BuildAnthropicToolPayload(string model, string systemPrompt, IList<ModelConversationMessage> conversation, IList<AgentToolDefinition> tools)
        {
            var messages = new List<object>();
            foreach (ModelConversationMessage item in conversation ?? new List<ModelConversationMessage>())
            {
                if (item.Role == "assistant" && item.ToolCalls != null && item.ToolCalls.Count > 0)
                {
                    var blocks = new List<object>();
                    if (!string.IsNullOrWhiteSpace(item.Text)) blocks.Add(new Dictionary<string, object> { { "type", "text" }, { "text", item.Text } });
                    foreach (AgentToolCall call in item.ToolCalls)
                    {
                        blocks.Add(new Dictionary<string, object> { { "type", "tool_use" }, { "id", call.Id }, { "name", call.Name }, { "input", call.Arguments ?? new Dictionary<string, object>() } });
                    }
                    messages.Add(new Dictionary<string, object> { { "role", "assistant" }, { "content", blocks } });
                }
                else if (item.Role == "tool" && item.ToolResults != null)
                {
                    var blocks = new List<object>();
                    foreach (ModelToolResultMessage result in item.ToolResults)
                    {
                        blocks.Add(new Dictionary<string, object> { { "type", "tool_result" }, { "tool_use_id", result.CallId }, { "content", result.ResultText ?? "" } });
                    }
                    messages.Add(new Dictionary<string, object> { { "role", "user" }, { "content", blocks } });
                }
                else messages.Add(new Dictionary<string, object> { { "role", item.Role == "assistant" ? "assistant" : "user" }, { "content", item.Text ?? "" } });
            }
            var definitions = new List<object>();
            foreach (AgentToolDefinition tool in tools) definitions.Add(new Dictionary<string, object> { { "name", tool.Name }, { "description", tool.Description }, { "input_schema", tool.BuildParametersSchema() } });
            return new Dictionary<string, object> { { "model", model }, { "max_tokens", 2048 }, { "system", systemPrompt ?? "" }, { "messages", messages }, { "tools", definitions } };
        }

        private object BuildGeminiToolPayload(string systemPrompt, IList<ModelConversationMessage> conversation, IList<AgentToolDefinition> tools)
        {
            var contents = new List<object>();
            foreach (ModelConversationMessage item in conversation ?? new List<ModelConversationMessage>())
            {
                var parts = new List<object>();
                if (!string.IsNullOrWhiteSpace(item.Text)) parts.Add(new Dictionary<string, object> { { "text", item.Text } });
                if (item.Role == "assistant" && item.ToolCalls != null)
                {
                    foreach (AgentToolCall call in item.ToolCalls) parts.Add(new Dictionary<string, object> { { "functionCall", new Dictionary<string, object> { { "name", call.Name }, { "args", call.Arguments ?? new Dictionary<string, object>() } } } });
                }
                else if (item.Role == "tool" && item.ToolResults != null)
                {
                    foreach (ModelToolResultMessage result in item.ToolResults)
                    {
                        parts.Add(new Dictionary<string, object> { { "functionResponse", new Dictionary<string, object> { { "name", result.ToolName }, { "response", new Dictionary<string, object> { { "result", result.ResultText ?? "" } } } } } });
                    }
                }
                contents.Add(new Dictionary<string, object> { { "role", item.Role == "assistant" ? "model" : "user" }, { "parts", parts } });
            }
            var declarations = new List<object>();
            foreach (AgentToolDefinition tool in tools) declarations.Add(new Dictionary<string, object> { { "name", tool.Name }, { "description", tool.Description }, { "parameters", tool.BuildParametersSchema() } });
            return new Dictionary<string, object>
            {
                { "systemInstruction", new Dictionary<string, object> { { "parts", new object[] { new Dictionary<string, object> { { "text", systemPrompt ?? "" } } } } } },
                { "contents", contents }, { "tools", new object[] { new Dictionary<string, object> { { "functionDeclarations", declarations } } } }
            };
        }

        private static List<object> BuildOpenAiNestedToolDefinitions(IList<AgentToolDefinition> tools)
        {
            var result = new List<object>();
            foreach (AgentToolDefinition tool in tools)
            {
                result.Add(new Dictionary<string, object> { { "type", "function" }, { "function", new Dictionary<string, object> { { "name", tool.Name }, { "description", tool.Description }, { "parameters", tool.BuildParametersSchema() } } } });
            }
            return result;
        }

        private string NormalizeArgumentsJson(AgentToolCall call)
        {
            if (call == null) return "{}";
            if (!string.IsNullOrWhiteSpace(call.ArgumentsJson)) return call.ArgumentsJson;
            return serializer.Serialize(call.Arguments ?? new Dictionary<string, object>());
        }

        private ModelToolTurn ExtractOpenAiCompatibleTurn(Dictionary<string, object> root, bool cohere)
        {
            Dictionary<string, object> message = null;
            if (cohere)
            {
                object messageObject;
                message = root.TryGetValue("message", out messageObject) ? messageObject as Dictionary<string, object> : null;
            }
            else
            {
                object choicesObject;
                object[] choices = root.TryGetValue("choices", out choicesObject) ? choicesObject as object[] : null;
                if (choices != null && choices.Length > 0)
                {
                    var first = choices[0] as Dictionary<string, object>;
                    object messageObject;
                    message = first != null && first.TryGetValue("message", out messageObject) ? messageObject as Dictionary<string, object> : null;
                }
            }
            var turn = new ModelToolTurn();
            if (message == null) return turn;
            object contentObject;
            if (message.TryGetValue("content", out contentObject)) turn.Text = FlattenText(contentObject);
            object callsObject;
            object[] calls = message.TryGetValue("tool_calls", out callsObject) ? callsObject as object[] : null;
            if (calls == null) return turn;
            foreach (object raw in calls)
            {
                var item = raw as Dictionary<string, object>;
                if (item == null) continue;
                object functionObject;
                var function = item.TryGetValue("function", out functionObject) ? functionObject as Dictionary<string, object> : null;
                if (function == null) continue;
                string name = ReadString(function, "name");
                object argumentsObject;
                function.TryGetValue("arguments", out argumentsObject);
                turn.ToolCalls.Add(CreateToolCall(ReadString(item, "id"), name, argumentsObject));
            }
            return turn;
        }

        private ModelToolTurn ExtractOpenAiResponsesTurn(Dictionary<string, object> root)
        {
            var turn = new ModelToolTurn { Text = ReadString(root, "output_text") };
            object outputObject;
            object[] output = root.TryGetValue("output", out outputObject) ? outputObject as object[] : null;
            if (output == null) return turn;
            var text = new StringBuilder(turn.Text ?? "");
            foreach (object raw in output)
            {
                var item = raw as Dictionary<string, object>;
                if (item == null) continue;
                string type = ReadString(item, "type");
                if (type == "function_call")
                {
                    object argumentsObject;
                    item.TryGetValue("arguments", out argumentsObject);
                    string callId = ReadString(item, "call_id");
                    if (callId.Length == 0) callId = ReadString(item, "id");
                    turn.ToolCalls.Add(CreateToolCall(callId, ReadString(item, "name"), argumentsObject));
                }
                else
                {
                    object content;
                    if (item.TryGetValue("content", out content)) AppendText(text, FlattenText(content));
                }
            }
            turn.Text = text.ToString();
            return turn;
        }

        private ModelToolTurn ExtractAnthropicTurn(Dictionary<string, object> root)
        {
            var turn = new ModelToolTurn();
            object contentObject;
            object[] content = root.TryGetValue("content", out contentObject) ? contentObject as object[] : null;
            if (content == null) return turn;
            var text = new StringBuilder();
            foreach (object raw in content)
            {
                var block = raw as Dictionary<string, object>;
                if (block == null) continue;
                string type = ReadString(block, "type");
                if (type == "tool_use")
                {
                    object input;
                    block.TryGetValue("input", out input);
                    turn.ToolCalls.Add(CreateToolCall(ReadString(block, "id"), ReadString(block, "name"), input));
                }
                else if (type == "text") AppendText(text, ReadString(block, "text"));
            }
            turn.Text = text.ToString();
            return turn;
        }

        private ModelToolTurn ExtractGeminiTurn(Dictionary<string, object> root)
        {
            var turn = new ModelToolTurn();
            object candidatesObject;
            object[] candidates = root.TryGetValue("candidates", out candidatesObject) ? candidatesObject as object[] : null;
            if (candidates == null || candidates.Length == 0) return turn;
            var first = candidates[0] as Dictionary<string, object>;
            object contentObject;
            var content = first != null && first.TryGetValue("content", out contentObject) ? contentObject as Dictionary<string, object> : null;
            object partsObject;
            object[] parts = content != null && content.TryGetValue("parts", out partsObject) ? partsObject as object[] : null;
            if (parts == null) return turn;
            var text = new StringBuilder();
            foreach (object raw in parts)
            {
                var part = raw as Dictionary<string, object>;
                if (part == null) continue;
                object functionObject;
                var function = part.TryGetValue("functionCall", out functionObject) ? functionObject as Dictionary<string, object> : null;
                if (function != null)
                {
                    object args;
                    function.TryGetValue("args", out args);
                    turn.ToolCalls.Add(CreateToolCall("gemini-" + Guid.NewGuid().ToString("N"), ReadString(function, "name"), args));
                }
                else AppendText(text, ReadString(part, "text"));
            }
            turn.Text = text.ToString();
            return turn;
        }

        private AgentToolCall CreateToolCall(string id, string name, object argumentsObject)
        {
            string callId = string.IsNullOrWhiteSpace(id) ? "call-" + Guid.NewGuid().ToString("N") : id;
            var arguments = argumentsObject as Dictionary<string, object>;
            string argumentsJson = argumentsObject as string;
            if (arguments == null && !string.IsNullOrWhiteSpace(argumentsJson))
            {
                try { arguments = serializer.DeserializeObject(argumentsJson) as Dictionary<string, object>; }
                catch { arguments = null; }
            }
            if (arguments == null) arguments = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(argumentsJson)) argumentsJson = serializer.Serialize(arguments);
            return new AgentToolCall { Id = callId, Name = name ?? "", Arguments = arguments, ArgumentsJson = argumentsJson };
        }

        private static void AppendHistory(List<object> messages, IList<Dictionary<string, string>> history, bool anthropic)
        {
            if (history == null) return;
            foreach (Dictionary<string, string> item in history)
            {
                string role = ReadHistory(item, "role");
                if (role != "assistant") role = "user";
                messages.Add(new Dictionary<string, object>
                {
                    { "role", role },
                    { "content", ReadHistory(item, "content") }
                });
            }
        }

        private static string ReadHistory(Dictionary<string, string> item, string key)
        {
            string value;
            return item != null && item.TryGetValue(key, out value) ? value ?? "" : "";
        }

        private static string ExtractOpenAiChoiceText(Dictionary<string, object> root)
        {
            object choicesObject;
            object[] choices = root.TryGetValue("choices", out choicesObject) ? choicesObject as object[] : null;
            if (choices == null || choices.Length == 0) return "";
            var first = choices[0] as Dictionary<string, object>;
            if (first == null) return "";
            object messageObject;
            var message = first.TryGetValue("message", out messageObject) ? messageObject as Dictionary<string, object> : null;
            if (message == null) return "";
            object content;
            return message.TryGetValue("content", out content) ? FlattenText(content) : "";
        }

        private static string ExtractOpenAiResponsesText(Dictionary<string, object> root)
        {
            string direct = ReadString(root, "output_text");
            if (!string.IsNullOrWhiteSpace(direct)) return direct;
            object outputObject;
            object[] output = root.TryGetValue("output", out outputObject) ? outputObject as object[] : null;
            if (output == null) return "";
            var text = new StringBuilder();
            foreach (object itemObject in output)
            {
                var item = itemObject as Dictionary<string, object>;
                if (item == null) continue;
                object content;
                if (item.TryGetValue("content", out content)) AppendText(text, FlattenText(content));
            }
            return text.ToString();
        }

        private static string ExtractGeminiText(Dictionary<string, object> root)
        {
            object candidatesObject;
            object[] candidates = root.TryGetValue("candidates", out candidatesObject) ? candidatesObject as object[] : null;
            if (candidates == null || candidates.Length == 0) return "";
            var first = candidates[0] as Dictionary<string, object>;
            if (first == null) return "";
            object contentObject;
            var content = first.TryGetValue("content", out contentObject) ? contentObject as Dictionary<string, object> : null;
            if (content == null) return "";
            object parts;
            return content.TryGetValue("parts", out parts) ? FlattenText(parts) : "";
        }

        private static string ExtractTextBlocks(Dictionary<string, object> root, string key)
        {
            object value;
            return root.TryGetValue(key, out value) ? FlattenText(value) : "";
        }

        private static string ExtractNestedTextBlocks(Dictionary<string, object> root, string parentKey, string childKey)
        {
            object parentObject;
            var parent = root.TryGetValue(parentKey, out parentObject) ? parentObject as Dictionary<string, object> : null;
            if (parent == null) return "";
            object value;
            return parent.TryGetValue(childKey, out value) ? FlattenText(value) : "";
        }

        private static string FlattenText(object value)
        {
            if (value == null) return "";
            string direct = value as string;
            if (direct != null) return direct;

            var map = value as Dictionary<string, object>;
            if (map != null)
            {
                object textObject;
                if (map.TryGetValue("text", out textObject)) return FlattenText(textObject);
                if (map.TryGetValue("content", out textObject)) return FlattenText(textObject);
                return "";
            }

            var array = value as object[];
            if (array == null) return Convert.ToString(value);
            var text = new StringBuilder();
            foreach (object item in array) AppendText(text, FlattenText(item));
            return text.ToString();
        }

        private static void AppendText(StringBuilder target, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            if (target.Length > 0) target.AppendLine();
            target.Append(value.Trim());
        }

        private static string ReadString(Dictionary<string, object> map, string key)
        {
            object value;
            return map != null && map.TryGetValue(key, out value) ? Convert.ToString(value) ?? "" : "";
        }

        private static void Validate(AppSettings settings, ModelProviderDefinition provider)
        {
            ValidateBaseUrl(settings, provider);
            if (string.IsNullOrWhiteSpace(settings.Model)) throw new InvalidOperationException("请先选择或填写模型名称。");
            if (provider.RequiresApiKey && string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                throw new InvalidOperationException("请先填写 " + provider.DisplayName + " 的 API Key。");
            }
        }

        private static void ValidateBaseUrl(AppSettings settings, ModelProviderDefinition provider)
        {
            Uri uri;
            if (string.IsNullOrWhiteSpace(settings.BaseUrl) ||
                !Uri.TryCreate(settings.BaseUrl.Trim(), UriKind.Absolute, out uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException("请填写有效的 " + provider.DisplayName + " API 地址。");
            }
            if (!string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                throw new InvalidOperationException("API 地址中不能包含用户名或密码。");
            }
            if (!string.IsNullOrWhiteSpace(settings.ApiKey) &&
                uri.Scheme == Uri.UriSchemeHttp &&
                !uri.IsLoopback)
            {
                throw new InvalidOperationException("携带 API Key 的远程接口必须使用 HTTPS；本机 127.0.0.1 接口不受影响。");
            }
        }

        private static void AddAuthenticationHeaders(HttpRequestMessage request, AppSettings settings, ModelProviderDefinition provider)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            AddAuthenticationHeaders(headers, settings, provider);
            foreach (KeyValuePair<string, string> header in headers)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        private static void AddAuthenticationHeaders(Dictionary<string, string> headers, AppSettings settings, ModelProviderDefinition provider)
        {
            string key = (settings.ApiKey ?? "").Trim();
            switch (provider.Protocol)
            {
                case ModelApiProtocol.AnthropicMessages:
                    if (key.Length > 0) headers["x-api-key"] = key;
                    headers["anthropic-version"] = "2023-06-01";
                    break;
                case ModelApiProtocol.GeminiGenerateContent:
                    if (key.Length > 0) headers["x-goog-api-key"] = key;
                    break;
                case ModelApiProtocol.AzureOpenAiChat:
                    if (key.Length > 0) headers["api-key"] = key;
                    break;
                default:
                    if (key.Length > 0) headers["Authorization"] = "Bearer " + key;
                    break;
            }
        }

        internal static string BuildModelsUrl(AppSettings settings, ModelProviderDefinition provider)
        {
            if (provider.Id == "cohere")
            {
                string cohereRoot = (settings.BaseUrl ?? "").Trim().TrimEnd('/');
                if (cohereRoot.EndsWith("/v2", StringComparison.OrdinalIgnoreCase))
                {
                    cohereRoot = cohereRoot.Substring(0, cohereRoot.Length - 3);
                }
                return cohereRoot + "/v1/models";
            }
            return AppendPath(settings.BaseUrl, "models");
        }

        private static string AppendPath(string baseUrl, string path)
        {
            string root = (baseUrl ?? "").Trim().TrimEnd('/');
            string suffix = (path ?? "").TrimStart('/');
            if (root.EndsWith("/" + suffix, StringComparison.OrdinalIgnoreCase)) return root;
            return root + "/" + suffix;
        }

        private static string BuildAzureChatUrl(string baseUrl, string deployment)
        {
            string root = (baseUrl ?? "").Trim().TrimEnd('/');
            if (root.IndexOf("/openai/deployments/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (root.IndexOf("chat/completions", StringComparison.OrdinalIgnoreCase) < 0) root += "/chat/completions";
            }
            else
            {
                root += "/openai/deployments/" + Uri.EscapeDataString(deployment) + "/chat/completions";
            }
            if (root.IndexOf("api-version=", StringComparison.OrdinalIgnoreCase) < 0)
            {
                root += (root.IndexOf('?') >= 0 ? "&" : "?") + "api-version=2024-10-21";
            }
            return root;
        }

        private static InvalidOperationException CreateHttpException(
            ModelProviderDefinition provider,
            HttpResponseMessage response,
            string responseText,
            string apiKey)
        {
            string detail = ExtractErrorMessage(responseText);
            if (string.IsNullOrWhiteSpace(detail)) detail = response.ReasonPhrase;
            if (detail == null) detail = "未知错误";
            detail = RedactSensitiveText(detail, apiKey);
            if (detail.Length > 420) detail = detail.Substring(0, 420) + "…";
            return new InvalidOperationException(
                provider.DisplayName + " HTTP " + ((int)response.StatusCode) + "：" + detail);
        }

        private static string ExtractErrorMessage(string responseText)
        {
            if (string.IsNullOrWhiteSpace(responseText)) return "";
            try
            {
                var serializer = new JavaScriptSerializer();
                var root = serializer.DeserializeObject(responseText) as Dictionary<string, object>;
                if (root == null) return responseText.Trim();
                object errorObject;
                var error = root.TryGetValue("error", out errorObject) ? errorObject as Dictionary<string, object> : null;
                string message = ReadString(error, "message");
                if (string.IsNullOrWhiteSpace(message)) message = ReadString(root, "message");
                if (string.IsNullOrWhiteSpace(message)) message = ReadString(root, "detail");
                return string.IsNullOrWhiteSpace(message) ? responseText.Trim() : message;
            }
            catch
            {
                return responseText.Trim();
            }
        }

        internal static string RedactSensitiveText(string text, string apiKey)
        {
            string value = text ?? "";
            string key = (apiKey ?? "").Trim();
            if (key.Length > 0) value = value.Replace(key, "[已隐藏]");
            value = Regex.Replace(
                value,
                @"(?i)\b(api[-_ ]?key|authorization|access[-_ ]?token)\b(\s*[:=]\s*)(bearer\s+)?[^\s,""'}]+",
                "$1$2[已隐藏]");
            value = Regex.Replace(value, @"(?i)\b(sk|xai)-[a-z0-9_-]{12,}\b", "[已隐藏]");
            return value;
        }
    }

    internal sealed class ModelConversationMessage
    {
        public string Role { get; set; }
        public string Text { get; set; }
        public List<AgentToolCall> ToolCalls { get; set; }
        public List<ModelToolResultMessage> ToolResults { get; set; }

        public ModelConversationMessage()
        {
            ToolCalls = new List<AgentToolCall>();
            ToolResults = new List<ModelToolResultMessage>();
        }
    }

    internal sealed class ModelToolResultMessage
    {
        public string CallId { get; set; }
        public string ToolName { get; set; }
        public string ResultText { get; set; }
    }

    internal sealed class ModelToolTurn
    {
        public string Text { get; set; }
        public List<AgentToolCall> ToolCalls { get; private set; }

        public ModelToolTurn()
        {
            Text = "";
            ToolCalls = new List<AgentToolCall>();
        }
    }

    internal sealed class ModelApiRequest
    {
        public string Url { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public string JsonBody { get; set; }
    }
}
