# Iroha Agent v2.2.0

## 主要更新

- 新增统一模型服务适配层，聊天业务不再写死 DeepSeek。
- 设置改为“模型连接”和“语音与记忆”双页：先选厂商，再选模型，然后填写对应 Key。
- 内置 27 个常用云端、聚合与本地入口；支持自定义 OpenAI 兼容地址。
- 支持 OpenAI Responses、OpenAI Chat Completions、Anthropic Messages、Gemini GenerateContent、Cohere Chat 与 Azure OpenAI。
- 每个厂商独立保存 API Key、模型和 Base URL；旧 DeepSeek 配置自动迁移。
- 支持在线刷新模型列表，也允许手动输入厂商新发布的模型 ID。
- “重新部署 GPT-SoVITS 语音”移至独立语音设置页，保留实时部署进度和安全替换约束。
- 顶部模型徽标和服务状态卡随当前厂商更新。

## 兼容入口

包括 DeepSeek、OpenAI、Anthropic、Google Gemini、xAI、Mistral、Cohere、OpenRouter、通义千问、智谱 GLM、SiliconFlow、MiniMax、Groq、Together AI、Moonshot Kimi、Perplexity、NVIDIA NIM、Fireworks AI、百度千帆、腾讯混元、火山方舟、GitHub Models、Hugging Face、Azure OpenAI、Ollama、LM Studio 和自定义 OpenAI 兼容接口。

API Key 必须属于所选厂商并拥有所选模型权限。AWS Bedrock SigV4、Google Vertex OAuth 等非 API-Key 鉴权方式不在本版本的普通 Key 输入范围内。

## 验证

- Windows 主程序编译通过。
- 20+ 厂商目录、旧配置迁移与厂商 Key 隔离测试通过。
- 六类协议的 URL、鉴权头、请求体、响应解析和模型列表样本测试通过。
- 设置双页在 `1280 × 720` 下完成自动截图与边界检查。
- 记忆、语音 Bootstrap、会话菜单和主界面功能回归通过。

发布附件不包含 API Key、聊天记录、长期记忆、用户设置或崩溃日志。
