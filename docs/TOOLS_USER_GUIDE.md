# 彩叶 Agent 工具与工作方式使用指南

适用版本：Windows v2.3.0

## 1. 开始使用

1. 打开右侧“设置”，完成模型厂商、模型和 API Key 配置。
2. 点击“工具与隐私”，确认“启用彩叶工具能力”已开启。
3. 按需开启 A、B、C 能力组；默认全部开启。
4. 直接用自然语言提出任务。模型会判断是否需要 Tool，无需输入命令名。

示例：

- “搜索今天关于 DeepSeek 的官方更新，给我来源。”
- “记住我更喜欢简洁的中文回答。”
- “明晚八点提醒我复盘本周计划。”
- “在我授权的文档目录里找包含‘毕业设计’的资料。”
- “读取这份 PDF，概括核心结论并指出页内证据。”
- “查上海未来三天天气，帮我安排出行。”
- “给老师写一封请假邮件草稿，只保存，不发送。”
- 点击输入框旁的回形针选择图片，再问“这张图有什么问题？”

## 2. 三组能力

| 组别 | 适合任务 | 主要 Tools |
|---|---|---|
| A · 陪伴核心 | 联网核查、记忆、计算、时间、提醒 | `web_search`、`open_url`、`memory_*`、`calculator`、`datetime`、`reminder_manage` |
| B · 知识与学习 | 找资料、读文档、建立私人知识库 | `local_file_search`、`document_read`、`knowledge_base` |
| C · 生活助理 | 天气、日程、草稿、图片、系统轻操作 | `weather`、`calendar_manage`、`email_draft`、`clipboard_read`、`image_analyze`、`music_control`、`app_launcher` |

## 3. 权限规则

- 搜索、网页读取、计算、时间、天气、记忆检索和已授权文件读取属于只读操作。
- 写入或删除记忆、创建/修改提醒和日程、建立知识库索引都必须先确认。
- 读取剪贴板、分析图片、控制媒体和打开应用每次都会确认。
- 邮件只保存本地草稿，软件没有发送邮件的能力。
- 软件没有任意 Shell、PowerShell、任意文件删除、自动登录、支付、发帖或桌面乱点能力。

确认窗会显示工具名称、用途和本次参数。选择“取消”只取消本次动作，不影响聊天。

## 4. 联网搜索

“权限与连接”中可选：

- `自动`：有 Brave Search Key 时优先 Brave，否则使用 Bing RSS。
- `Brave`：只使用用户自己的 Brave Search Key。
- `Bing`：使用公开 RSS 回退，不需要额外 Key。
- `关闭`：禁用联网搜索。

Brave Search Key 与模型 API Key 一样使用当前 Windows 用户级 DPAPI 加密。换电脑或换 Windows 账户后需要重新填写。

网页读取只允许公开 HTTP/HTTPS 地址，会拒绝本机、局域网、云元数据、`file://`、URL 内嵌账号密码和危险重定向。网页正文被视为不可信资料，不能改变应用权限。

## 5. 本地文件与知识库

默认授权当前用户的“文档”和“桌面”目录，可在“权限与连接”添加或移除目录。

支持读取：TXT、Markdown、CSV、JSON、XML、HTML、日志、常见源码、DOCX 和文字型 PDF。扫描版 PDF 若没有文本层，需要当前多模态模型或可选的本机 Tesseract OCR 才能理解图片文字。

知识库完全保存在本机，使用关键词片段检索。索引文档时会复制文本片段到本地知识库；删除原文不会自动删除已索引片段，可让彩叶“移除知识库中的某个来源”。

## 6. 图片分析

1. 点击输入框右侧的回形针。
2. 选择 PNG、JPG、GIF、WebP 或 BMP，最大 10 MB。
3. 输入问题并发送。
4. 确认本次图片分析。

图片会发送给当前选择的模型厂商，因此必须使用支持视觉输入的模型。若当前模型不支持多模态，软件仍返回尺寸、平均色彩和可用 OCR 文本，并在回答中说明限制。

## 7. 本地数据位置

```text
%LOCALAPPDATA%\IrohaLocalAgent\reminders.json
%LOCALAPPDATA%\IrohaLocalAgent\calendar.json
%LOCALAPPDATA%\IrohaLocalAgent\email-drafts.json
%LOCALAPPDATA%\IrohaLocalAgent\knowledge-base.json
%LOCALAPPDATA%\IrohaLocalAgent\exports\IrohaAgent-Calendar.ics
```

这些文件采用临时文件、原子替换和备份恢复。发行包不包含用户数据、API Key 或聊天记录。

## 8. 常见问题

- 模型不调用工具：确认总开关和对应能力组已开启；部分旧模型不支持 Tool Calling，应用会安全降级为普通聊天。
- 搜索不可用：切换到“自动”或“Bing”，或检查 Brave Search Key。
- 文件找不到：确认文件在授权目录内；符号链接和目录联接会被安全策略拒绝。
- PDF 没有正文：它可能是扫描件；改用支持视觉的模型并通过回形针提交页面截图。
- 图片只有基础信息：当前模型不支持视觉输入，或厂商拒绝了该图片格式。
- 提醒没有系统通知：v2.3.0 在应用运行时通过视觉小说对白区提醒，不注册 Windows 通知中心后台任务。
- 日程没有同步手机：当前是本地日历，可导出 ICS 后导入其他日历应用。
