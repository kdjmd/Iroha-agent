# 彩叶 Agent Tools 与 Skills 实施说明

> 状态：方案 A、B、C 已在 Windows v2.3.0 实现。共交付 18 个 Tool、10 个 Skill 工作方式、统一权限确认和最多 4 轮原生工具调用。`email_send`、任意 Shell、任意文件写删和桌面自动点击仍明确排除。

## 0. v2.3.0 交付摘要

| 能力组 | 状态 | 已交付范围 |
|---|---|---|
| A · 陪伴核心 | 已完成 | 联网搜索、网页读取、长期记忆、计算、日期时间、本地提醒 |
| B · 知识与学习 | 已完成 | 授权目录搜索、TXT/Markdown/CSV/JSON/XML/HTML/DOCX/PDF 读取、本地知识库 |
| C · 生活助理 | 已完成 | Open-Meteo 天气、本地日历与 ICS、邮件草稿、单次剪贴板、图片理解、媒体键、应用白名单 |
| 高风险能力 | 未开放 | 不发送邮件，不运行任意命令，不自动登录、支付、发帖或删除任意文件 |

实现采用应用内注册表和协议适配器。OpenAI Responses、OpenAI Chat/DeepSeek 兼容、Anthropic Messages、Gemini GenerateContent 与 Cohere v2 均使用各自的原生 Tool Calling 结构；不接受模型用普通文本伪造写操作。

## 1. 概念与实现原则

- **Tool**：可执行、可读取外部数据或改变状态的能力，例如联网搜索、写入记忆、创建提醒。
- **Skill**：由提示词、步骤、工具组合和输出规范构成的工作流，例如“陪伴倾听”“研究核查”“旅行规划”。
- Tool 由应用执行，模型只能提出调用请求，不能直接运行本地代码。
- 读取型 Tool 可以按用户设置自动运行；写入、删除、发送和系统控制必须经过明确授权。
- 主界面只显示简短的操作反馈，不展示技术日志。详细权限放在“设置 -> 工具与隐私”。

## 2. 已实现的 A、B 组 Tools

| 编号 | Tool | 作用 | 权限 | 额外 Key | 状态 |
|---|---|---|---|---|---|
| T01 | `web_search` | 联网搜索新闻、知识、攻略并返回来源 | 只读网络 | Brave 可选 | 已实现 |
| T02 | `open_url` | 读取指定网页，提取正文和标题 | 只读网络 | 否 | 已实现 |
| T03 | `memory_search` | 从长期记忆中检索与当前对话相关的信息 | 只读本地 | 否 | 已实现 |
| T04 | `memory_remember` | 保存用户确认的偏好、关系、目标和重要事实 | 每次确认 | 否 | 已实现 |
| T05 | `memory_forget` | 按关键词删除或清空长期记忆 | 每次确认 | 否 | 已实现 |
| T06 | `calculator` | 精确算术、日期差、单位换算 | 本地计算 | 否 | 已实现 |
| T07 | `datetime` | 当前时间、时区转换和日期推算 | 本地读取 | 否 | 已实现 |
| T08 | `reminder_manage` | 创建、查看、修改、完成和取消本地提醒 | 写操作确认 | 否 | 已实现 |
| T09 | `weather` | 当前天气与 1-7 天短期预报 | 只读网络 | 否 | 已实现 |
| T10 | `local_file_search` | 在用户授权目录内按文件名和正文查找资料 | 只读本地 | 否 | 已实现 |
| T11 | `document_read` | 读取 PDF、DOCX、TXT、Markdown 等文档 | 只读本地 | 否 | 已实现 |
| T12 | `knowledge_base` | 将指定资料建立可检索的私人知识库 | 索引/移除确认 | 否 | 已实现 |

### 联网搜索实现选项

1. **自动，默认**：配置了 Brave Search Key 时使用 Brave；否则使用 Bing RSS 回退。
2. **Brave Search API**：跨模型一致、返回结构化结果，需要用户自己的搜索 Key，使用 DPAPI 加密保存。
3. **关闭联网**：完全禁用搜索 Tool；网页读取仍受独立的 URL 安全策略控制。

用户可在“工具与隐私 -> 权限与连接”选择自动、Brave、Bing 或关闭。搜索结果带来源链接和访问时间。

### 长效记忆建议结构

当前记忆是规则提取的文本列表。建议升级为本地结构化记忆：

| 字段 | 说明 |
|---|---|
| `type` | 身份、偏好、关系、目标、项目、承诺、事件 |
| `content` | 规范化后的记忆正文 |
| `confidence` | 自动提取可信度 |
| `sourceConversationId` | 来源会话，可追溯但不复制整段聊天 |
| `createdAt / updatedAt / lastUsedAt` | 创建、更新和最近使用时间 |
| `expiresAt` | 临时信息可自动过期 |
| `pinned` | 用户确认的重要记忆不被自动淘汰 |

当前 v2.3.0 沿用兼容旧版的原子 JSON 记忆库，并增加显式写入/遗忘工具、去重、数量上限与敏感信息拒绝。SQLite、逐条编辑和语义向量检索保留为后续增强，不影响本版功能。

## 3. 已实现的 C 组 Tools

| 编号 | Tool | 作用 | v2.3.0 状态 |
|---|---|---|---|
| T20 | `calendar_manage` | 查询、创建、修改、取消本地日程并导出 ICS | 已实现；写操作每次确认，不绑定云账号 |
| T21 | `email_draft` | 根据对话生成并保存在本机的邮件草稿 | 已实现；每次确认，绝不发送 |
| T22 | `email_send` | 发送邮件 | 未实现，明确排除 |
| T23 | `clipboard_read` | 读取用户主动提交的剪贴板文本 | 已实现；单次确认，不后台监听 |
| T24 | `image_analyze` | 理解截图、照片和表格 | 已实现；每次确认，优先当前多模态模型，OCR 可选 |
| T25 | `image_generate` | 生成头像、壁纸和角色主题素材 | 未实现，避免未预期费用 |
| T26 | `music_control` | 发送播放、暂停、上一首、下一首和停止媒体键 | 已实现；每次确认 |
| T27 | `app_launcher` | 打开白名单中的本地应用 | 已实现；无参数、每次确认 |

## 4. 暂不建议默认开放的高风险 Tools

| 编号 | Tool | 原因 |
|---|---|---|
| T30 | 任意 Shell / PowerShell | 可修改系统和文件，陪伴型 Agent 没有默认开放的必要 |
| T31 | 任意文件写入或删除 | 容易误删；应改成限定目录和明确动作 |
| T32 | 桌面视觉自动点击 | 容易错点、泄露屏幕信息，必须按任务临时授权 |
| T33 | 浏览器自动登录或支付 | 涉及账号、资金和隐私，不应交给模型自行执行 |
| T34 | 自动发送消息或发帖 | 代表用户对外发言，必须预览并二次确认 |

## 5. 已启用 Skills

| 编号 | Skill | 主要行为 | 依赖 Tools |
|---|---|---|---|
| S01 | 彩叶日常陪伴 | 延续角色语气，主动倾听但不过度说教 | T03-T05 |
| S02 | 记忆整理员 | 提取、合并、纠正记忆并解释为什么要记住 | T03-T05 |
| S03 | 联网研究与核查 | 多来源搜索、比较时间、给出引用和不确定性 | T01-T02 |
| S04 | 计划与复盘 | 将目标拆成可执行步骤，定期复盘进展 | T03、T08 |
| S05 | 每日陪伴 | 问候、情绪签到、轻量日记和晚间回顾 | T03-T05、T07-T08 |
| S06 | 学习辅导 | 讲解、出题、错题复盘和学习进度追踪 | T03-T06、T11 |
| S07 | 阅读助手 | 对网页和文档做摘要、问答和观点对照 | T02、T11 |
| S08 | 灵感共创 | 写作、角色设定、脑暴和方案比较 | 无必需 Tool |
| S09 | 旅行规划 | 查天气、地点、交通信息并形成行程 | T01-T02、T07、T09 |
| S10 | 隐私守护 | 在调用高风险 Tool 前解释数据范围并请求确认 | 所有写入型 Tool |
| S11 | 提示词优化 | 保留原意、压缩上下文、控制 Token 消耗 | 无必需 Tool |
| S12 | 语音表演导演 | 按情绪选择语速、停顿、表情和动作反应 | GPT-SoVITS、动画状态机 |

## 6. 已采用的组合

### 方案 A：陪伴核心

`T01 T02 T03 T04 T05 T06 T07 T08 + S01 S02 S03 S04 S05 S10 S12`

特点：联网、有来源、可记忆、可提醒，同时保持陪伴型产品的克制和安全。开发量适中，用户价值最高。

### 方案 B：知识与学习增强

在方案 A 上增加：`T10 T11 T12 + S06 S07`

特点：适合学习、资料整理和项目陪伴，需要增加文档解析和知识库管理页面。

### 方案 C：生活助理扩展

在方案 A 上增加：`T09 T20 T21 T23 T24 T26 T27 + S09`

特点：能力更完整。当前使用本地日历和 ICS，不要求 OAuth；联网天气使用 Open-Meteo。

不建议第一阶段采用 `T22` 或 `T30-T34`。

## 7. Tool 调用架构建议

1. 应用内部工具注册表、JSON Schema 参数校验、统一超时和结构化结果已经实现。
2. OpenAI/DeepSeek 风格、OpenAI Responses、Anthropic、Gemini、Cohere 已分别适配原生 Tool Calling。
3. 不支持工具 Schema 的接口会降级为普通聊天，绝不解析普通文本来执行写操作。
4. MCP Client 仍是后续扩展方向；MCP 不会替代应用自身的权限系统。
5. 每次调用最多循环 4 轮、每轮最多 8 个调用，单个工具结果最多约 48 KiB。

官方接口均强调：模型只负责生成工具调用，实际执行和参数校验仍由应用负责。MCP 可以统一工具与数据源连接方式，但执行权限仍应由本地应用控制。

## 8. 交互方案

- 对话框上方显示一条轻量玻璃状态条：`正在搜索`、`找到 6 个来源`、`等待确认`、`已完成`、`已取消`。
- 不在主界面显示请求 JSON、调试日志或行为流水。
- 搜索来源由模型整合进最终中文回答；工具状态只在主对白区显示简短反馈。
- 写入型 Tool 使用统一确认浮窗，明确显示“将做什么、使用哪些数据、能否撤销”。
- 设置新增“工具与隐私”能力中心，可分组关闭 A/B/C、配置搜索源、授权目录、应用白名单与 Skill。

## 9. 主要安全要求

- 网页工具阻止访问本机、局域网、云元数据地址和 `file://`，防止 SSRF。
- 网页内容视为不可信数据，不能覆盖系统提示或工具权限。
- 所有参数按 JSON Schema 校验；模型生成的路径、URL、收件人和时间不得直接执行。
- 搜索、文件和记忆结果设置长度上限、超时、取消和来源标签。
- API Key 使用 Windows DPAPI 保存，不进入提示词、记忆、日志或发行包。
- 删除、发送、付款、系统控制始终需要用户确认，不提供“永远允许”。

## 10. 参考资料

- [DeepSeek Tool Calls](https://api-docs.deepseek.com/guides/tool_calls)
- [OpenAI Function Calling](https://developers.openai.com/api/docs/guides/function-calling)
- [Claude Tool Use](https://platform.claude.com/docs/en/agents-and-tools/tool-use/overview)
- [Gemini Function Calling](https://ai.google.dev/gemini-api/docs/function-calling)
- [Model Context Protocol](https://modelcontextprotocol.io/docs/getting-started/intro)
- [Brave Search API](https://api-dashboard.search.brave.com/documentation)

## 11. 验收入口

- 自动化：`powershell -ExecutionPolicy Bypass -File tools/run-regression-qa.ps1`
- 架构与权限边界：`docs/TOOLS_ARCHITECTURE.md`
- 用户使用方法：`docs/TOOLS_USER_GUIDE.md`
- v2.3.0 回归证据：`docs/evidence/round-2026-07-18-v23-agent-tools-qa.txt`
