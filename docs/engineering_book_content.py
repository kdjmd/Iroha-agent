"""Canonical content for the Iroha Agent engineering acceptance handbook."""

DOCUMENT = {
    "title": "彩叶 Iroha Agent",
    "subtitle": "工程验收、核查、扩展与交接手册",
    "version": "V2.0",
    "baseline_date": "2026-07-16",
    "baseline": "Windows 视觉小说主版本 / Android 0.1.0 原型",
    "status": "Windows 主版本有条件通过；Android 原型单列验收",
    "owner": "项目所有者：待交接方填写",
    "compiler": "编制：项目工程整理",
    "evidence_image": "evidence/round-2026-07-16-v20-packaged-voice-speaking.png",
}


def p(text):
    return {"type": "p", "text": text}


def lead(label, text, tone="info"):
    return {"type": "lead", "label": label, "text": text, "tone": tone}


def bullets(items):
    return {"type": "bullets", "items": items}


def steps(items):
    return {"type": "steps", "items": items}


def checklist(items):
    return {"type": "checklist", "items": items}


def table(headers, rows, widths=None, compact=False, dense=False):
    return {
        "type": "table",
        "headers": headers,
        "rows": rows,
        "widths": widths,
        "compact": compact,
        "dense": dense,
    }


def code(text):
    return {"type": "code", "text": text}


def note(label, text, tone="note"):
    return {"type": "note", "label": label, "text": text, "tone": tone}


def page_break():
    return {"type": "page_break"}


SECTIONS = [
    {
        "title": "文档控制与使用说明",
        "level": 1,
        "blocks": [
            lead(
                "文档用途",
                "本手册是当前项目的验收基线、运行核查依据、故障定位入口、扩展设计约束和工程交接清单。验收人员可直接使用第 11 章逐项执行；维护人员应先阅读第 3、6、7、13、15 章。",
            ),
            table(
                ["项目", "内容"],
                [
                    ["文档版本", "V2.0"],
                    ["软件基线日期", "2026-07-16"],
                    ["主验收对象", "Windows 桌面视觉小说聊天 Agent"],
                    ["次级对象", "Android 0.1.0 原型及其 Debug APK"],
                    ["不包含", "DeepSeek 账户、API Key、GPT-SoVITS 权重、角色素材授权、生产签名证书"],
                    ["变更原则", "代码、模型、视觉资源、API 合约或打包脚本变化后，必须更新基线哈希与回归记录"],
                ],
                widths=[1.6, 4.9],
            ),
            p("状态标识：通过表示已在本机完成可重复验证；有条件通过表示工程链路成立但仍需账户、听感或真机确认；未验收表示当前没有足够证据，不等同于失败。"),
            checklist(
                [
                    "验收前确认本手册版本与交付包日期一致。",
                    "任何截图只能作为视觉证据，不能替代按钮、接口、音频和数据持久化的实际操作。",
                    "现场验收不得在截图、日志、录屏或工单中暴露完整 API Key。",
                    "验收结论、偏差、责任人和复验日期应填写在第 19 章签字页。",
                ]
            ),
        ],
    },
    {
        "title": "1. 项目结论与验收边界",
        "level": 1,
        "blocks": [
            lead(
                "建议验收结论",
                "Windows 主版本建议有条件通过：构建、独立发布包启动、高清视觉小说界面、设置、记忆、会话、快捷操作和语音服务准备链路均已验证；DeepSeek 真实计费请求、最终音色听感和长时稳定性必须由验收人在自己的账户与设备上确认。Android 端仅按可构建原型验收，不与 Windows 端按同一产品完成度签字。",
                "success",
            ),
            table(
                ["子系统", "当前成熟度", "本轮证据", "建议结论"],
                [
                    ["Windows 桌面端", "主版本", "编译通过；1280 x 720 与 980 x 552 截图；独立目录启动通过", "有条件通过"],
                    ["视觉小说 UI", "主版本", "参考图并排比对；真实场景透明合成；高清立绘随窗口响应", "通过"],
                    ["DeepSeek 聊天", "已实现", "请求、解析、错误兜底和设置逻辑已核查", "现场联网复验"],
                    ["GPT-SoVITS", "已实现", "冷启动、TTS、WAV 校验、自动增益、同步播放和失败降级已核查", "技术通过；听感复验"],
                    ["长期记忆", "可用", "本地 JSON、40 条上限、最近 12 条注入、管理窗口已验证", "通过"],
                    ["Android", "0.1.0 原型", "Gradle 8.9 / AGP 8.7.3 / SDK 35 构建通过", "原型通过"],
                    ["发布打包", "可用", "高清立绘遗漏问题已修复；临时独立目录启动复验通过", "通过"],
                ],
                widths=[1.15, 1.05, 2.75, 1.55],
                compact=True,
            ),
            note(
                "重要边界",
                "程序名称中的“本地”表示客户端与记忆数据在本机运行，不表示大模型推理完全离线。聊天内容会发送到配置的 DeepSeek 兼容接口；语音可由本机或局域网 GPT-SoVITS 服务生成。",
                "warning",
            ),
            bullets(
                [
                    "核心目标：提供娱乐陪伴向中文聊天、日语角色语音和视觉小说式角色反馈。",
                    "主交付平台：Windows 桌面端；默认窗口 1280 x 720，最低支持 980 x 552。",
                    "非目标：生产级多用户服务、云端账户系统、端到端加密同步、官方角色应用声明、移动端与桌面端完全一致。",
                ]
            ),
        ],
    },
    {
        "title": "2. 产品范围与用户流程",
        "level": 1,
        "blocks": [
            p("目标用户是希望在本机获得角色陪伴、中文文字交流和日语语音反馈的个人用户。主要流程保持单窗口完成，复杂设置和记忆管理使用浮窗，避免主聊天画面堆叠。"),
            table(
                ["用户任务", "入口", "系统行为", "完成标志"],
                [
                    ["首次配置", "顶部设定或右侧设定", "保存 API Key、Flash/Pro 模型、语音和记忆开关", "设置提示为已准备；标题栏徽标同步"],
                    ["发送聊天", "底部文本框与发送按钮", "记录用户消息、请求模型、解析中日双语、同步文字与语音", "状态显示完成"],
                    ["快速陪伴", "陪我聊 / 做计划 / 找灵感 / 复盘", "把预设提示填入文本框，允许用户编辑后发送", "输入框出现对应提示"],
                    ["管理记忆", "长期记忆卡或右侧记忆", "新增、修改、删除、清空并保存记忆", "memory.json 更新"],
                    ["管理会话", "会话右键菜单", "重命名、置顶、删除当前界面会话项", "侧栏顺序或名称更新"],
                    ["试听语音", "左下角色卡、对白名字牌或语音播放按钮", "调用 TTS 并播放日语测试句", "可听见声音且状态恢复"],
                    ["保存聊天", "左侧保存对话", "导出当前聊天文本到桌面", "生成 IrohaAgent-Chat-Latest.txt"],
                ],
                widths=[1.1, 1.45, 2.8, 1.15],
            ),
            note("界面约束", "语音输入不在当前范围；用户只通过键盘输入。没有对应功能的图标不得加入界面，所有可见图标必须与实际操作一致。"),
        ],
    },
    {
        "title": "3. 总体架构",
        "level": 1,
        "blocks": [
            p("Windows 端采用 .NET Framework WinForms 单进程客户端。界面、业务编排、接口客户端、数据存储和绘制控件目前集中在 desktop/AgentDesktop.cs；语音模型与推理运行时作为外部依赖，不进入主发布包。"),
            table(
                ["层", "主要组件", "职责", "边界"],
                [
                    ["呈现层", "MainForm、AvatarControl、GlassPanel、TopBarControl、VnDialogueTextControl", "视觉小说舞台、透明玻璃、交互控件、响应式布局", "不直接保存业务数据"],
                    ["会话编排", "SendButton_Click、RunWithBusyState", "输入校验、忙碌状态、请求顺序、文字与语音同步", "当前没有取消令牌"],
                    ["模型接入", "RequestDeepSeekAsync、ParseAgentReply", "OpenAI 兼容聊天接口、严格 JSON 解析和兜底", "依赖用户 API Key"],
                    ["语音接入", "EnsureVoiceServiceReadyAsync、RequestVoiceAudioAsync", "健康检查、自动启动、TTS 请求、WAV 播放", "权重与运行时外置"],
                    ["本地数据", "SettingsStore、MemoryStore", "设置和长期记忆 JSON 持久化", "API Key 当前为明文"],
                    ["Android 原型", "MainActivity、AvatarView", "移动布局、聊天与远程 TTS、逐帧显示", "功能和视觉均未与桌面齐平"],
                ],
                widths=[0.9, 1.9, 2.55, 1.15],
                compact=True,
            ),
            steps(
                [
                    "用户输入中文文本；客户端在本地显示原文，并按开关执行提示词精简和记忆提取。",
                    "客户端把系统人设、最近会话和最多 12 条长期记忆组装为 /chat/completions 请求。",
                    "模型返回 zh、ja、mood 三个字段；zh 用于界面，ja 用于语音，mood 驱动角色状态。",
                    "若启用语音，客户端先准备音频文件，再同时启动日语播放和中文逐字显示。",
                    "完成后恢复按钮、角色状态和底部服务提示；失败时保留文字兜底并允许继续使用。",
                ]
            ),
            lead(
                "关键设计决定",
                "主舞台由 AvatarControl 统一绘制房间和高清立绘，信息卡和对白框作为其子控件进行真实透明合成；顶部栏作为窗体同级控件，另用 CharacterTopOverlayControl 补绘越过标题栏的头部片段，兼顾参考图层次与窗口按钮可用性。",
            ),
        ],
    },
    {
        "title": "4. 仓库与交付物结构",
        "level": 1,
        "blocks": [
            table(
                ["路径", "用途", "是否进入源码包", "是否进入运行包"],
                [
                    ["desktop/AgentDesktop.cs", "Windows 主程序源码", "是", "否"],
                    ["desktop/build.ps1", "调用 .NET Framework csc 并复制运行资源", "是", "否"],
                    ["desktop/dist", "Windows 构建产物", "否", "作为运行包来源"],
                    ["assets/character", "高清立绘、桌面帧、Android 帧", "是", "按构建脚本选择性复制"],
                    ["assets/ui", "房间背景、角色头像和历史 UI 素材", "是", "是"],
                    ["android", "Android Gradle 工程", "是", "APK 单独交付"],
                    ["voice-pack/manifest.json", "语音包来源与接入说明", "是", "否"],
                    ["tools/save-latest-desktop.ps1", "覆盖生成桌面运行包和源码包", "是", "否"],
                    ["docs", "优化记录、验收证据和本手册", "是", "工程书复制到运行包"],
                    [".toolchain / .gradle-user", "本机构建工具与缓存", "否", "否"],
                ],
                widths=[1.85, 2.65, 1.0, 1.0],
                compact=True,
            ),
            code(
                "ayaha-local-agent/\n"
                "  desktop/                 Windows 源码、构建脚本与 dist\n"
                "  android/                 Android 0.1.0 原型工程\n"
                "  assets/                  角色、逐帧、头像与房间资源\n"
                "  voice-pack/              语音模型接入清单，不含权重\n"
                "  tools/                   Android 引导与桌面打包脚本\n"
                "  docs/                    工程书、证据与优化记录\n"
                "  README.md                使用与仓库分享说明\n"
                "  design-qa.md             视觉对照与回归记录"
            ),
            note("版本库状态", "当前工作目录尚未初始化为 Git 仓库。交接时应在确认素材许可与 .gitignore 后执行 git init，并以本手册基线作为首个可追溯版本。", "warning"),
        ],
    },
    {
        "title": "5. Windows 桌面端实现",
        "level": 1,
        "blocks": [
            table(
                ["项目", "当前实现"],
                [
                    ["运行框架", ".NET Framework WinForms，csc.exe 直接编译为 winexe"],
                    ["默认窗口", "1280 x 720；最小 980 x 552；无系统边框，自绘最小化/最大化/关闭"],
                    ["响应断点", "宽度小于 1120 或高度小于 630 时进入紧凑布局"],
                    ["主字体", "Microsoft YaHei UI；图标使用 Segoe Fluent Icons 与真实 PNG 头像"],
                    ["图像质量", "HighQualityBicubic、HighQuality PixelOffset、独立高清透明立绘、1448 x 1086 陪伴卡插画与精确窗口尺寸的全分辨率背景缓存"],
                    ["界面日志", "不向用户显示行为日志；内部反馈仅 Debug.WriteLine；启动异常写 crash.log"],
                ],
                widths=[1.45, 5.05],
                compact=True,
            ),
            table(
                ["控件", "职责", "扩展注意"],
                [
                    ["AvatarControl", "统一绘制房间、高清立绘、约 20 FPS 渐进表情与按需空闲帧", "新增角色时优先替换透明立绘与状态渲染，不复制业务逻辑"],
                    ["CharacterTopOverlayControl", "在顶部栏内同步补绘同一动画帧的头部片段", "坐标必须与 CharacterStageBounds 共用"],
                    ["GlassPanel / GlassButton", "圆角、阴影、透明和交互反馈", "小按钮需 OpaqueBackfill，防止抗锯齿碎边"],
                    ["ToolRailPanel", "统一绘制记忆、设定、外观轨道及选中/悬停/按下状态", "可见轨道保持单控件；业务动作通过代理按钮复用"],
                    ["VnDialogueTextControl", "首行标题、正文多行和逐字更新", "保持 AddAssistantLine 两条路径同步写入"],
                    ["CompressionStatusControl", "省 token 状态、进度和说明", "百分比当前为展示值，不是精确 token 计量"],
                    ["ServiceStatusControl", "DeepSeek 配置与 GPT-SoVITS 状态胶囊", "“已配置”不能替代真实在线测试"],
                    ["ConversationItemControl", "头像、选择、悬停、置顶标记和右键菜单", "当前会话元数据未跨启动持久化"],
                ],
                widths=[1.55, 2.55, 2.4],
                compact=True,
            ),
            p("主界面保留会话历史、长期记忆、上下文压缩、服务状态、角色舞台、视觉小说对白、四个快捷操作、文本输入、语音试听和右侧工具栏。语音输入已按产品要求移除。"),
        ],
    },
    {
        "title": "6. DeepSeek 接口与回复协议",
        "level": 1,
        "blocks": [
            table(
                ["字段", "值或规则"],
                [
                    ["基础地址", "默认 https://api.deepseek.com，可在高级设置修改"],
                    ["端点", "POST {BaseUrl}/chat/completions"],
                    ["认证", "Authorization: Bearer {ApiKey}"],
                    ["默认模型", "deepseek-v4-flash；高级设置可切换 deepseek-v4-pro，标题栏分别显示 Flash / Pro"],
                    ["超时", "90 秒"],
                    ["温度", "0.7"],
                    ["上下文", "系统人设 + 最多 16 条进程内历史 + 最多 12 条长期记忆"],
                    ["期望回复", "严格 JSON：zh 中文、ja 日语、mood 情绪"],
                ],
                widths=[1.45, 5.05],
            ),
            code(
                "POST /chat/completions\n"
                "Authorization: Bearer <REDACTED>\n"
                "Content-Type: application/json\n\n"
                "{\n"
                "  \"model\": \"deepseek-v4-flash\",\n"
                "  \"temperature\": 0.7,\n"
                "  \"messages\": [\n"
                "    {\"role\": \"system\", \"content\": \"<角色与输出协议>\"},\n"
                "    {\"role\": \"user\", \"content\": \"<用户文本>\"}\n"
                "  ]\n"
                "}"
            ),
            code(
                "{\n"
                "  \"zh\": \"显示在聊天框中的中文回复\",\n"
                "  \"ja\": \"音声で再生する短い日本語\",\n"
                "  \"mood\": \"idle|thinking|speaking|happy|error|shy|surprised|cheer|focus\"\n"
                "}"
            ),
            note(
                "兼容性策略",
                "解析器会去除 Markdown 代码围栏并截取最外层 JSON。若模型仍返回非标准格式，中文原文会显示，日语使用兜底句，角色回到 happy；因此现场验收要同时测试标准 JSON 和异常格式。",
            ),
        ],
    },
    {
        "title": "7. GPT-SoVITS 语音链路",
        "level": 1,
        "blocks": [
            p("Windows 默认把语音服务视为本机外部进程。程序启动后访问 /docs 进行健康检查；服务不可达且检测到完整本地运行时后，以隐藏窗口启动 api_v2.py，并最多等待 150 秒。文字聊天不依赖语音成功。"),
            table(
                ["项目", "当前值"],
                [
                    ["默认服务地址", "http://127.0.0.1:9880"],
                    ["健康检查", "GET {BaseUrl}/docs，2 秒超时"],
                    ["生成端点", "优先 POST /tts，失败后回退 GET /tts"],
                    ["输出格式", "严格 RIFF/WAVE；拒绝错误正文、损坏文件和近静音音频"],
                    ["文本语种", "ja"],
                    ["切分方式", "cut2"],
                    ["播放实现", "PCM16 有界峰值归一化；SoundPlayer.Load + PlaySync；失败不阻断中文"],
                    ["临时文件", "%TEMP%/iroha-agent-voice-{GUID}.wav，播放后删除"],
                ],
                widths=[1.45, 5.05],
                compact=True,
            ),
            code(
                "{\n"
                "  \"text\": \"<日语台词>\", \"text_lang\": \"ja\",\n"
                "  \"prompt_text\": \"<参考文本>\", \"prompt_lang\": \"ja\",\n"
                "  \"ref_audio_path\": \"<服务端可访问的参考音频路径>\",\n"
                "  \"text_split_method\": \"cut2\", \"batch_size\": 1, \"speed_factor\": 1.0,\n"
                "  \"streaming_mode\": false, \"media_type\": \"wav\"\n"
                "}"
            ),
            steps(
                [
                    "模型回复到达后先生成完整音频临时文件。",
                    "校验 RIFF/WAVE、PCM 数据和非静音条件；偏小语音自动提升响度。",
                    "读取 WAV byteRate 与 dataSize 估算时长。",
                    "同时启动语音播放与中文逐字显示，使两者接近同步。",
                    "播放期间角色进入 Speaking，波形进入 Active；结束后删除临时文件。",
                    "语音失败时记录调试信息但继续显示中文，不阻断聊天。",
                ]
            ),
            note(
                "Android 局域网条件",
                "Android 的 127.0.0.1 指向手机自身。真机需填写电脑 LAN 地址与 9880 端口，确保服务监听局域网且 Windows 防火墙放行；参考音频路径由服务端解释。",
                "warning",
            ),
        ],
    },
    {
        "title": "8. 配置、数据与保留策略",
        "level": 1,
        "blocks": [
            table(
                ["数据", "Windows 位置", "内容", "清理方式"],
                [
                    ["设置", "%APPDATA%/IrohaLocalAgent/settings.json", "API、模型、语音、记忆与压缩开关", "退出后删除文件或在应用内覆盖"],
                    ["长期记忆", "%APPDATA%/IrohaLocalAgent/memory.json", "最多 40 条用户偏好记录", "记忆管理窗口清空或删除文件"],
                    ["启动异常", "%APPDATA%/IrohaLocalAgent/crash.log", "未处理异常堆栈，仅故障时写入", "关闭应用后删除"],
                    ["聊天导出", "桌面/IrohaAgent-Chat-Latest.txt", "用户主动导出的当前聊天", "用户手动删除"],
                    ["语音临时文件", "%TEMP%/iroha-agent-voice-*.wav", "单次生成的播放音频", "正常播放后自动删除"],
                    ["Android 设置", "应用私有 SharedPreferences", "API、模型、语音地址和开关", "清除应用数据或卸载"],
                ],
                widths=[1.0, 2.2, 2.25, 1.05],
                compact=True,
            ),
            table(
                ["设置字段", "默认值", "说明"],
                [
                    ["BaseUrl", "https://api.deepseek.com", "OpenAI 兼容接口根地址"],
                    ["ApiKey", "空", "首次使用由用户填写"],
                    ["Model", "deepseek-v4-flash", "高级设置可选 v4 Flash / v4 Pro；必须按供应商账户复验"],
                    ["VoiceServerUrl", "http://127.0.0.1:9880", "桌面本机语音服务"],
                    ["VoiceRefAudioPath", "本机 GPT-SoVITS voices/iroha/ref.wav", "由 TTS 服务端读取"],
                    ["VoicePromptText", "日语参考句", "与参考音频语义对应"],
                    ["VoicePromptLang", "ja", "参考音频语种"],
                    ["VoiceEnabled", "true（Windows）/ false（Android）", "平台默认不同"],
                    ["MemoryEnabled", "true", "仅 Windows 当前实现"],
                    ["AutoOptimizePrompt", "false", "仅 Windows 当前实现"],
                ],
                widths=[1.55, 2.25, 2.7],
                compact=True,
            ),
            note(
                "安全现状",
                "Windows settings.json 与 Android SharedPreferences 当前都不加密。交付包本身不含 API Key，但运行后本机账户可读取。生产化前应分别迁移到 Windows DPAPI/Credential Manager 与 Android EncryptedSharedPreferences/Keystore。",
                "risk",
            ),
        ],
    },
    {
        "title": "9. 人设、记忆与省 Token 逻辑",
        "level": 1,
        "blocks": [
            table(
                ["能力", "实现规则", "限制"],
                [
                    ["角色人设", "温柔可靠、轻吐槽、制作人式推进；中文短而有温度；日语台词更短", "高层风格参考，不应声称官方身份或复述原作长台词"],
                    ["情绪", "9 类 mood 映射角色位移、波形和轻量反馈", "高清主立绘目前以位移、缩放和装饰反馈为主"],
                    ["自动记忆", "检测“记住、以后、我喜欢、偏好”等标记后写入", "关键词启发式可能漏记或误记"],
                    ["记忆容量", "最多 40 条；请求时注入最近 12 条", "不是向量检索，也没有冲突合并"],
                    ["提示词精简", "删除部分礼貌短语、重复行和多余空白，并加直接处理前缀", "可能改变语气或细微语义，默认关闭"],
                    ["对话上下文", "进程内最多 16 条 role/content", "关闭应用后会话历史不持久化"],
                ],
                widths=[1.15, 3.45, 1.9],
                compact=True,
            ),
            note("验收原则", "省 Token 功能应以语义保持为先，不能只比较字符数。使用包含否定、条件、引用、代码和多段要求的样例进行 A/B 验收。", "warning"),
        ],
    },
    {
        "title": "10. 构建与发布复现",
        "level": 1,
        "blocks": [
            lead("发布基线", "任何正式验收都必须从源码重新构建，并在源码目录之外启动发布包。只在 desktop/dist 内运行不能证明资源打包完整。"),
            table(
                ["平台", "工具链", "当前验证结果"],
                [
                    ["Windows", ".NET Framework csc.exe；System.Drawing；WinForms；System.Net.Http；System.Web.Extensions", "desktop/build.ps1 通过"],
                    ["Android", "JDK 17；Gradle 8.9；Android Gradle Plugin 8.7.3；compile/target SDK 35；min SDK 23", ":app:assembleDebug 通过"],
                ],
                widths=[1.1, 3.8, 1.6],
            ),
            p("Windows 构建："),
            code("powershell -ExecutionPolicy Bypass -File .\\desktop\\build.ps1"),
            p("Windows 输出："),
            code("desktop\\dist\\IrohaAgent.exe\ndesktop\\dist\\Start-IrohaAgent.bat\ndesktop\\dist\\assets\\..."),
            p("覆盖生成桌面运行包与源码包："),
            code("powershell -ExecutionPolicy Bypass -File .\\tools\\save-latest-desktop.ps1"),
            p("Android 本地工具链引导与 Debug APK："),
            code("powershell -ExecutionPolicy Bypass -File .\\tools\\bootstrap-android.ps1"),
            table(
                ["基线文件", "大小", "SHA-256"],
                [
                    ["IrohaAgent.exe", "350,720 bytes", "F2C67DCF449D0F2BB36758649EF6D806147332EC2D16ED42EB883AD87868AD59"],
                    ["app-debug.apk", "8,830,814 bytes", "3DDFE85BE6DFE789F901A22EAEC73DFE2E490DE559AD8D4AC1ED2EA12D00CA68"],
                ],
                widths=[1.3, 1.2, 4.0],
                compact=True,
            ),
            note(
                "已修复的发布缺陷",
                "旧 build.ps1 只复制 character/frames 与 UI 图，开发目录会向上找到高清立绘，而独立发布包找不到。当前脚本已复制 assets/character 顶层高清立绘与 character/expressions 自然表情帧，并会在 csc.exe 返回非零退出码时立即失败。发布包已在 %TEMP% 的独立目录启动复验。",
                "success",
            ),
            note("哈希说明", "任何源码、编译器、图标或资源变化都可能改变哈希。重新发布后应更新本表，而不是继续沿用旧值。"),
        ],
    },
    {
        "title": "11. 验收用例与核查表",
        "level": 1,
        "blocks": [
            p("执行顺序建议：先验证不依赖账户的构建与界面，再验证设置和数据，最后执行会产生网络流量或声音的在线用例。每个失败项必须记录实际结果、截图/日志位置和复验人。"),
            table(
                ["编号", "验收项", "操作与期望", "当前状态"],
                [
                    ["W-01", "源码构建", "运行 desktop/build.ps1；无编译错误且生成 EXE 与 assets", "通过"],
                    ["W-02", "独立发布启动", "复制 dist 到源码外目录启动；背景、高清立绘、头像均存在", "通过"],
                    ["W-03", "主界面视觉", "1280 x 720 无黑边、重影、默认边框、文字遮挡和错误图标；VN 标题/正文/名字牌层级为 11.8/8.7/9 pt；人物与背景锐利", "通过"],
                    ["W-04", "紧凑窗口", "980 x 552 保留会话、对白、输入、语音和工具栏；校准后的 VN 文本无裁切", "通过"],
                    ["W-05", "窗口控制", "拖动、最小化、最大化、关闭均正确；角色头发不遮挡按钮", "待现场"],
                    ["W-06", "设置与模型", "API Key 掩码；Flash/Pro 可切换；徽标与 Model 同步；保存后重开仍存在", "通过"],
                    ["W-07", "DeepSeek 连接", "测试连接返回成功；401/404/模型错误给出可理解提示", "待现场联网"],
                    ["W-08", "中文聊天", "发送中文后收到中文 zh；输入为空不发请求", "待现场联网"],
                    ["W-09", "日语语音同步", "文字开始显示时语音同时开始；结束后恢复完成状态", "技术通过"],
                    ["W-10", "语音降级", "关闭或停止 TTS 后仍显示中文，不冻结、不崩溃", "通过"],
                    ["W-11", "记忆管理", "新增、修改、删除、清空和保存均生效；最多 40 条", "通过"],
                    ["W-12", "会话编辑", "右键可重命名、置顶/取消置顶和删除；命令后菜单关闭不写崩溃日志", "通过"],
                    ["W-13", "快捷操作", "四个快捷按钮把对应文本填入输入框且允许编辑", "通过"],
                    ["W-14", "省 Token", "开关可用；复杂提示 A/B 后主要语义保持", "待语义复验"],
                    ["W-15", "导出与清空", "保存聊天生成桌面文本；清空只影响当前对话", "待现场"],
                    ["W-16", "隐私界面", "主界面不显示行为日志、完整 Key、异常堆栈或模型内部信息", "通过"],
                    ["P-01", "运行包内容", "EXE、启动脚本、UI、陪伴卡插画、角色帧、自然表情帧和高清立绘齐全", "通过"],
                    ["P-02", "源码包排除", "不含 .toolchain、构建缓存、APK、ZIP、日志、音频和 Key", "通过"],
                    ["A-01", "Android 构建", "Gradle :app:assembleDebug 成功并生成 app-debug.apk", "通过"],
                    ["A-02", "Android 真机", "安装、启动、键盘、后台恢复和不同密度无崩溃", "未验收"],
                    ["A-03", "Android 聊天", "真机使用用户 Key 完成一轮中文聊天", "未验收"],
                    ["A-04", "Android 局域网语音", "填写电脑 LAN 地址后可播放日语语音", "未验收"],
                ],
                widths=[0.55, 1.25, 3.95, 0.75],
                compact=True,
            ),
            checklist(
                [
                    "测试使用专用 API Key，验收后立即轮换或撤销。",
                    "至少执行一次无网络、错误 Key、错误模型、TTS 关闭和 TTS 超时。",
                    "连续完成 20 轮聊天，观察内存增长、按钮恢复、临时 WAV 清理和 UI 卡顿。",
                    "使用 100%、125%、150% Windows 缩放复验文字和命中区域。",
                    "Android 至少覆盖一台 Android 6-8 与一台 Android 13+ 真机或等效模拟器。",
                ]
            ),
        ],
    },
    {
        "title": "12. 当前实测证据",
        "level": 1,
        "blocks": [
            table(
                ["证据", "结果", "位置"],
                [
                    ["Windows 编译与失败保护", "通过；csc.exe 非零退出码会中止脚本，不再误报 Built", "desktop/build.ps1"],
                    ["Windows 独立运行与布局", "正式 ZIP 启动通过；标准、紧凑及设置展开状态无主控件、对白或状态卡裁切", "docs/evidence/round-2026-07-16-v20-packaged-final.png；同目录 v20-rail-service-compact.png、v20-rail-settings.png"],
                    ["自然眨眼", "六阶段同位渐进；约 20 FPS；无圆形遮片或轮廓跳动", "docs/evidence/round-2026-07-15-v18-natural-blink-contact.png"],
                    ["自然口型", "十八阶段休止/小口/张口节奏；五官位置与全身轮廓稳定", "docs/evidence/round-2026-07-15-v18-natural-speech-contact.png"],
                    ["功能与菜单稳定性", "会话编辑、快捷操作、设置选中态、外观反馈、Flash/Pro、语音状态和紧凑布局全部通过", "docs/evidence/round-2026-07-16-v20-functional-qa.txt"],
                    ["语音输出链路", "健康检查、合成、响度、播放、清理、失败降级全部通过", "docs/evidence/round-2026-07-16-v20-voice-qa.txt"],
                    ["正式包真实发声", "独立 ZIP 中真实鼠标点击进入准备中和说话中；自然口型、波形及控件恢复通过", "docs/evidence/round-2026-07-16-v20-packaged-voice-speaking.png"],
                    ["右侧工具栏与服务卡对照", "owner-drawn 连续玻璃轨道无硬矩形接缝；两枚服务状态胶囊无裁切", "docs/evidence/round-2026-07-16-v20-rail-service-comparison.png"],
                    ["参考图同尺寸全景", "当前 1280 x 720 构建与同尺寸参考图并排核查", "docs/evidence/round-2026-07-16-v20-comparison-full.png"],
                    ["Android assembleDebug", "BUILD SUCCESSFUL，31 个任务 up-to-date", "android/app/build/outputs/apk/debug/app-debug.apk"],
                    ["现场联网与听感复验", "本轮未调用计费 DeepSeek 请求；最终音色仍需人工主观确认", "现场验收项 W-07/W-08/W-09"],
                ],
                widths=[1.75, 2.25, 2.5],
                compact=True,
                dense=True,
            ),
        ],
    },
    {
        "title": "13. 故障核查与恢复",
        "level": 1,
        "blocks": [
            table(
                ["现象", "优先检查", "恢复动作"],
                [
                    ["应用无法启动", "Windows 事件、crash.log、.NET Framework、资源目录", "重新解压到短路径；确认 EXE 与 assets 同级；重建发布包"],
                    ["界面退回低质量人物", "assets/character/iroha-portrait.png 是否进入运行包", "重新运行修复后的 build.ps1；执行独立目录回归"],
                    ["表情跳动或出现贴片感", "expressions 是否同尺寸；是否误用旧 frames；局部合成遮罩是否越界", "重跑 tools/build-expression-frames.py；对照 v18-natural-blink/speech-contact.png"],
                    ["会话菜单后程序报错", "crash.log 是否为 ContextMenuStrip ObjectDisposedException", "确认菜单 Closed 事件延迟 Dispose；运行 FunctionalQaHarness"],
                    ["背景或头像为空", "assets/ui 文件是否完整、是否被安全软件隔离", "恢复运行包 assets/ui；不要只复制 EXE"],
                    ["API 401/403", "Key、账户权限、BaseUrl", "重新保存 Key；撤销泄露 Key；使用连接测试"],
                    ["API 404/模型错误", "Model 与供应商可用列表", "在高级设置改为账户实际模型"],
                    ["回复显示原始 JSON/文本", "模型是否遵循严格 JSON", "检查系统提示和 ParseAgentReply 兜底；记录原始响应时必须脱敏"],
                    ["语音准备很久", "GET /docs、端口 9880、GPU/模型加载、运行时路径", "先手动启动 api_v2.py；检查配置和防火墙"],
                    ["有文本无声音", "VoiceEnabled、返回字节、默认播放设备、SoundPlayer", "点试听；用系统播放器打开临时 WAV；检查输出设备"],
                    ["文字和语音延迟", "TTS 生成时长、WAV 时长估算、CPU/GPU 负载", "缩短 ja 台词；预热模型；未来改为流式 TTS"],
                    ["记忆不生效", "MemoryEnabled、关键词、memory.json 权限", "在记忆管理中显式新增；检查最近 12 条注入"],
                    ["Android 无法连电脑语音", "是否仍使用 127.0.0.1、同网段、防火墙、监听地址", "填写电脑 LAN IP；允许端口；服务监听可达网卡"],
                    ["Android 构建离线失败", "Gradle 插件是否在缓存", "允许首次联网解析 AGP；之后再使用缓存构建"],
                ],
                widths=[1.5, 2.5, 2.5],
                compact=True,
            ),
            code(
                "# Windows 重新构建\n"
                "powershell -ExecutionPolicy Bypass -File .\\desktop\\build.ps1\n\n"
                "# GPT-SoVITS 健康检查\n"
                "Invoke-WebRequest http://127.0.0.1:9880/docs\n\n"
                "# Android 构建\n"
                "gradle -p android --no-daemon :app:assembleDebug"
            ),
        ],
    },
    {
        "title": "14. 安全、隐私与素材合规",
        "level": 1,
        "blocks": [
            table(
                ["风险", "现状", "优先级", "建议"],
                [
                    ["API Key 明文存储", "settings.json / SharedPreferences", "P1", "Windows 使用 DPAPI；Android 使用 Keystore/EncryptedSharedPreferences"],
                    ["聊天发送到第三方", "使用 DeepSeek 兼容接口", "P1", "首次使用明确告知；敏感内容本地过滤；提供隐私说明"],
                    ["Android 明文 HTTP", "usesCleartextTraffic=true 以支持局域网 TTS", "P1", "生产版使用受限 networkSecurityConfig 或 HTTPS/WSS"],
                    ["错误信息泄露", "UI 可能显示接口返回片段；crash.log 保存堆栈", "P2", "统一错误码；脱敏 Authorization、路径和用户文本"],
                    ["模型输出风险", "没有内容审核或年龄分级", "P2", "按发布地区增加安全策略、免责声明和可配置过滤"],
                    ["角色与语音版权", "视觉、头像、声音模型来自指定作品/训练包", "P0（公开发布）", "公开分享前取得角色、图像、声音、训练数据和商标授权"],
                    ["供应链", "Android 首次构建会下载 JDK/Gradle/SDK/插件", "P2", "锁定版本、记录校验值、使用可信镜像和 SBOM"],
                ],
                widths=[1.35, 2.25, 0.75, 2.15],
                compact=True,
            ),
            note(
                "公开分发红线",
                "工程可运行不等于拥有公开传播权。未获得授权前，不应把本项目描述为官方应用，不应公开分发角色立绘、原作头像、声音模型权重或可用于冒充真实角色/声优的材料。",
                "risk",
            ),
            checklist(
                [
                    "发布包和仓库中不存在 settings.json、memory.json、crash.log、聊天导出和临时音频。",
                    "提交前扫描 API Key、Bearer、私有路径、邮箱和访问令牌。",
                    "公开说明中标注非官方、素材来源、授权范围和删除联系渠道。",
                    "Android Release APK 使用正式签名、关闭调试、最小化明文网络范围。",
                ]
            ),
        ],
    },
    {
        "title": "15. 已知限制与技术债",
        "level": 1,
        "blocks": [
            table(
                ["优先级", "问题", "影响", "建议完成标准"],
                [
                    ["P0", "公开发布素材授权未确认", "法律与平台下架风险", "形成可审计授权清单或替换为原创资源"],
                    ["P1", "API Key 明文", "本机账户或恶意软件可读取", "迁移加密存储并提供清除凭据按钮"],
                    ["P1", "AgentDesktop.cs 约 7,376 行单文件", "维护、测试和并行开发困难", "按 UI/服务/数据/领域模型拆分并建立接口"],
                    ["P1", "Android 与桌面功能、视觉不一致", "用户预期和验收口径混乱", "单独制定 Android 产品设计与验收基线"],
                    ["P1", "没有自动化单元/集成测试工程", "回归依赖人工截图和反射脚本", "覆盖解析、记忆、压缩、API、打包清单和布局边界"],
                    ["P2", "会话列表不持久化", "重启后重命名、置顶、删除丢失", "引入 ConversationRepository 与 SQLite/JSON schema"],
                    ["P2", "本地语音路径硬编码到当前电脑", "换机后自动启动失效", "首次运行探测、可浏览选择、相对路径或环境变量"],
                    ["P2", "请求不可取消", "90 秒请求或 150 秒语音等待期间体验受限", "使用 CancellationToken 与取消按钮"],
                    ["P2", "压缩百分比为展示值", "可能被误解为真实 token 节省", "接入 tokenizer 或改为定性状态"],
                    ["P2", "记忆为关键词启发式", "漏记、误记和冲突", "结构化记忆、确认机制、去重和优先级"],
                    ["P3", "源码仍含未调用的旧布局辅助方法", "增加交接阅读成本", "完成调用图核查后删除死代码"],
                ],
                widths=[0.55, 2.25, 1.8, 1.9],
                compact=True,
            ),
        ],
    },
    {
        "title": "16. 后续扩展设计",
        "level": 1,
        "blocks": [
            lead("扩展原则", "先拆稳定边界，再增加功能。不得继续把新模型、新语音、新角色和新页面直接堆入 MainForm；所有外部能力都应通过可替换接口和可测试的数据契约接入。"),
            table(
                ["建议接口", "职责", "首个迁移对象"],
                [
                    ["ILLMClient", "聊天请求、模型发现、错误规范化、重试与取消", "RequestDeepSeekAsync"],
                    ["ITtsClient", "健康检查、启动、生成、流式播放、设备选择", "EnsureVoiceServiceReadyAsync / RequestVoiceAudioAsync"],
                    ["ISettingsStore", "加密设置、版本迁移、默认值", "SettingsStore"],
                    ["IMemoryStore", "结构化偏好、去重、确认、检索", "MemoryStore / RememberFromUserInput"],
                    ["IConversationRepository", "会话、消息、置顶、重命名和删除持久化", "sidebarConversationItems / history"],
                    ["ICharacterRenderer", "角色资源、状态、动画和命中区域", "AvatarControl"],
                    ["IClock / IFileSystem", "时间和文件副作用可测试化", "导出、记忆日期、临时音频"],
                ],
                widths=[1.65, 2.8, 2.05],
            ),
            p("推荐目录目标："),
            code(
                "src/\n"
                "  IrohaAgent.App/             启动与组合根\n"
                "  IrohaAgent.UI/              WinForms 页面和 owner-drawn 控件\n"
                "  IrohaAgent.Domain/          消息、记忆、角色状态、会话模型\n"
                "  IrohaAgent.Infrastructure/  DeepSeek、GPT-SoVITS、文件与加密存储\n"
                "tests/\n"
                "  IrohaAgent.UnitTests/\n"
                "  IrohaAgent.IntegrationTests/\n"
                "  IrohaAgent.PackagingTests/"
            ),
            table(
                ["扩展场景", "实施步骤", "验收重点"],
                [
                    ["新增大模型", "实现 ILLMClient；增加模型配置；复用 zh/ja/mood 契约", "错误码、超时、流式、成本和 JSON 兼容"],
                    ["新增语音角色", "建立 VoiceProfile；配置参考音频、文本、语言、模型路径", "音色授权、路径可移植、延迟和降级"],
                    ["新增角色", "独立 CharacterPack：portrait、头像、mood、persona、主题 token", "素材授权、命中区域、顶部叠层和紧凑布局"],
                    ["新增页面", "使用浮窗/页面容器；保持主聊天首屏；定义导航返回", "不遮挡主任务、键盘可达、状态保持"],
                    ["持久会话", "设计 schema、迁移、搜索和归档；关联长期记忆", "事务、损坏恢复、隐私删除和性能"],
                    ["流式回复", "SSE/流解析；增量 zh；分句 TTS；取消和背压", "文字语音同步、断线恢复和状态机"],
                    ["插件/Agent 工具", "白名单工具协议、权限确认、审计和沙箱", "最小权限、提示注入、防误操作和可撤销"],
                ],
                widths=[1.2, 3.35, 1.95],
                compact=True,
            ),
            steps(
                [
                    "第一阶段：拆分设置、模型、语音和本地数据接口，保持 UI 行为不变。",
                    "第二阶段：增加测试工程与独立发布包清单测试，建立 CI。",
                    "第三阶段：持久化会话、结构化记忆、取消与重试。",
                    "第四阶段：流式模型和流式 TTS；再评估 Android 共用领域层。",
                    "第五阶段：在授权与安全方案完成后考虑公开发布、自动更新和插件能力。",
                ]
            ),
        ],
    },
    {
        "title": "17. 运维、发布与变更管理",
        "level": 1,
        "blocks": [
            steps(
                [
                    "冻结需求与素材，记录软件基线日期和目标平台。",
                    "运行静态检查、Windows 构建、Android 构建和自动化测试。",
                    "生成 dist 后检查高清立绘、房间、头像、启动脚本和文档清单。",
                    "把 dist 复制到源码外目录，执行启动、设置、聊天、语音降级和 980 x 552 回归。",
                    "使用专用 Key 完成在线验收，删除测试聊天和临时凭据。",
                    "生成运行包、源码包、哈希清单和变更日志；在干净机器抽检。",
                    "由产品、工程、素材权利人和验收人分别签字。",
                ]
            ),
            table(
                ["变更类型", "最低回归范围"],
                [
                    ["UI 布局/控件", "1280、980、125% 缩放、设置浮窗、窗口控制、截图比对"],
                    ["角色/背景资源", "透明边缘、裁剪、顶部叠层、独立包、资源许可"],
                    ["LLM 提示或解析", "标准/非标准 JSON、中文/日语字段、mood、注入与成本"],
                    ["语音", "健康检查、冷启动、POST/GET、无服务降级、临时文件、听感"],
                    ["记忆/会话", "schema 迁移、去重、删除、备份恢复和隐私清除"],
                    ["构建脚本", "干净目录构建、独立目录启动、包清单与哈希"],
                    ["Android", "Debug/Release、低版本、目标版本、局域网、后台恢复和签名"],
                ],
                widths=[1.65, 4.85],
            ),
            checklist(
                [
                    "README、工程书、design-qa 和优化记录已更新。",
                    "源码包可在不依赖原开发目录的情况下重建。",
                    "运行包不含模型权重、工具链、缓存、用户数据和秘密。",
                    "新版本号、日期、哈希和 APK 签名信息已记录。",
                    "回滚包和数据备份路径已确认。",
                ]
            ),
        ],
    },
    {
        "title": "18. 工程交接清单",
        "level": 1,
        "blocks": [
            table(
                ["交接项", "应交付内容", "接收方核查"],
                [
                    ["源码", "完整项目目录、.gitignore、构建脚本、本手册源稿", "□"],
                    ["Windows 运行包", "IrohaAgent-Latest.zip 与 SHA-256", "□"],
                    ["源码包", "IrohaAgent-Source-Latest.zip 与 SHA-256", "□"],
                    ["Android", "源码、Debug APK；生产签名不在普通源码包中", "□"],
                    ["语音接入", "运行时版本、模型文件清单、配置 YAML、参考音频和许可说明", "□"],
                    ["外部账户", "DeepSeek 账户由接收方创建；仅通过安全渠道传递临时凭据", "□"],
                    ["视觉资源", "来源、原图、修改记录、授权范围和替换方案", "□"],
                    ["验收证据", "截图、构建输出、现场记录、偏差清单和签字页", "□"],
                    ["风险清单", "第 14、15 章未关闭项及责任人", "□"],
                ],
                widths=[1.35, 4.55, 0.6],
                compact=True,
            ),
            p("接收方第一天应完成："),
            steps(
                [
                    "在新目录解压源码，确认没有 settings.json、memory.json 或 API Key。",
                    "执行 Windows 构建与独立目录启动；核对主界面截图。",
                    "配置自己的测试 Key，执行 DeepSeek 连接测试。",
                    "确认 GPT-SoVITS 运行时路径、模型 YAML、参考音频和端口。",
                    "执行语音试听并人工确认音量、语言和音色。",
                    "执行 Android 构建；真机验收前配置局域网地址。",
                    "创建 Git 仓库、首个基线标签和受保护的秘密管理方式。",
                ]
            ),
            note("禁止交接方式", "不要把 API Key 写进聊天、截图、README、源码、ZIP 文件名或公开工单；不要把未授权语音权重和角色素材上传到公开仓库。", "risk"),
        ],
    },
    {
        "title": "19. 验收签字与偏差记录",
        "level": 1,
        "blocks": [
            table(
                ["角色", "姓名/单位", "结论", "日期", "签字"],
                [
                    ["产品/需求", "", "□通过  □有条件通过  □不通过", "", ""],
                    ["工程", "", "□通过  □有条件通过  □不通过", "", ""],
                    ["测试/验收", "", "□通过  □有条件通过  □不通过", "", ""],
                    ["素材/版权", "", "□通过  □有条件通过  □不通过", "", ""],
                    ["接收方", "", "□接收  □暂缓", "", ""],
                ],
                widths=[1.1, 1.45, 2.15, 0.85, 0.95],
            ),
            table(
                ["偏差编号", "对应用例", "现象与影响", "责任人", "计划日期", "复验结果"],
                [["", "", "", "", "", ""] for _ in range(6)],
                widths=[0.8, 0.8, 2.55, 0.8, 0.85, 0.7],
            ),
            lead(
                "默认签署建议",
                "在 W-07、W-08、W-09 主观听感、W-14 和 Android 真机项完成前，建议签署“Windows 主版本有条件通过；Android 原型接收但不作为正式移动版发布”。",
                "warning",
            ),
        ],
    },
    {
        "title": "附录 A：关键路径与命令速查",
        "level": 1,
        "blocks": [
            table(
                ["目的", "路径或命令"],
                [
                    ["Windows 源码", "desktop/AgentDesktop.cs"],
                    ["Windows 构建", "powershell -ExecutionPolicy Bypass -File desktop/build.ps1"],
                    ["桌面打包", "powershell -ExecutionPolicy Bypass -File tools/save-latest-desktop.ps1"],
                    ["Windows 输出", "desktop/dist/IrohaAgent.exe"],
                    ["Android 构建", "gradle -p android --no-daemon :app:assembleDebug"],
                    ["Android 输出", "android/app/build/outputs/apk/debug/app-debug.apk"],
                    ["用户设置", "%APPDATA%/IrohaLocalAgent/settings.json"],
                    ["长期记忆", "%APPDATA%/IrohaLocalAgent/memory.json"],
                    ["启动异常", "%APPDATA%/IrohaLocalAgent/crash.log"],
                    ["语音健康", "http://127.0.0.1:9880/docs"],
                    ["主验收图", "docs/evidence/round-2026-07-16-v20-packaged-voice-speaking.png"],
                    ["参考图全景对照", "docs/evidence/round-2026-07-16-v20-comparison-full.png"],
                    ["工具栏与服务卡对照", "docs/evidence/round-2026-07-16-v20-rail-service-comparison.png"],
                    ["左侧/底部/舞台对照", "docs/evidence/round-2026-07-15-v18-final-comparison-left.png；同目录 bottom.png、stage.png"],
                    ["运行时眨眼序列", "docs/evidence/round-2026-07-15-v18-natural-blink-contact.png"],
                    ["运行时口型序列", "docs/evidence/round-2026-07-15-v18-natural-speech-contact.png"],
                    ["功能回归记录", "docs/evidence/round-2026-07-16-v20-functional-qa.txt"],
                    ["语音回归记录", "docs/evidence/round-2026-07-16-v20-voice-qa.txt"],
                    ["语音界面回归", "docs/evidence/round-2026-07-16-v20-voice-ui-qa.txt"],
                    ["独立运行包验收图", "docs/evidence/round-2026-07-16-v20-packaged-final.png"],
                ],
                widths=[1.55, 4.95],
            ),
        ],
    },
    {
        "title": "附录 B：术语",
        "level": 1,
        "blocks": [
            table(
                ["术语", "定义"],
                [
                    ["视觉小说模式", "以场景、角色立绘、名字牌和大对白框为中心的聊天呈现方式"],
                    ["长期记忆", "跨会话保存在本机、用于补充系统提示的用户偏好记录"],
                    ["上下文压缩", "在请求前精简用户文本和重复内容的启发式功能"],
                    ["mood", "模型返回的角色状态枚举，用于驱动视觉和语音反馈"],
                    ["独立发布包", "脱离源码父目录仍能显示完整资源并运行的 dist 副本"],
                    ["有条件通过", "主体工程可接收，但列出的现场、账户、听感或真机条件尚未全部关闭"],
                    ["原型", "用于验证技术可行性，不代表达到正式产品的视觉、功能、安全和运维要求"],
                ],
                widths=[1.55, 4.95],
            ),
        ],
    },
]
