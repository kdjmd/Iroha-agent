# Iroha Agent Windows 视觉验收记录

验收基线：2.3.0，截图覆盖完整窗口、`1280 × 720` 紧凑窗口与 `980 × 552` 最小窗口；自动布局矩阵覆盖至 `1920 × 1080`。2.3.0 保持视觉小说主界面，并增加同一浅蓝白玻璃体系的“工具与隐私”独立浮窗。

## 证据

- 标准主界面：`docs/evidence/round-2026-07-16-v21-standard.png`
- 设置浮窗：`docs/evidence/round-2026-07-16-v21-settings.png`
- 多厂商模型设置：`docs/evidence/round-2026-07-18-v221-model-settings.png`
- 语音与记忆设置：`docs/evidence/round-2026-07-18-v221-voice-settings.png`
- 设置页布局断言：`docs/evidence/round-2026-07-18-v221-settings-ui-qa.txt`
- 能力组合页：`docs/evidence/round-2026-07-18-v23-tools-center.png`
- 权限与连接页：`docs/evidence/round-2026-07-18-v23-tools-privacy.png`
- 工作方式页：`docs/evidence/round-2026-07-18-v23-tools-skills.png`
- v2.3 设置与能力中心布局断言：`docs/evidence/round-2026-07-18-v23-settings-ui-qa.txt`
- v2.3 紧凑布局语音区：`docs/evidence/round-2026-07-19-v23-compact-voice-dock.png`
- v2.3 紧凑布局与设置 UI 断言：`docs/evidence/round-2026-07-19-v23-settings-ui-qa.txt`
- v2.3 UI 稳定性完整主界面：`docs/evidence/round-2026-07-19-v23-ui-stability-main.png`
- v2.3 UI 稳定性紧凑主界面：`docs/evidence/round-2026-07-19-v23-ui-stability-compact.png`
- v2.3 UI 稳定性最小主界面：`docs/evidence/round-2026-07-19-v23-ui-stability-minimum.png`
- v2.3 UI 稳定性能力中心：`docs/evidence/round-2026-07-19-v23-ui-stability-tools.png`
- v2.3 UI 稳定性 261 项断言：`docs/evidence/round-2026-07-19-v23-ui-stability-qa.txt`
- 首次语音部署：`docs/evidence/round-2026-07-16-v21-deployment-progress.png`
- 参考图对比：`docs/evidence/round-2026-07-16-v20-comparison-full.png`
- 真实语音播放状态：`docs/evidence/round-2026-07-16-v20-packaged-voice-speaking.png`
- 功能与响应式检查：`docs/evidence/round-2026-07-16-v21-functional-qa.txt`
- 必需视觉资源保护：`docs/evidence/round-2026-07-16-v21-visual-asset-guard-qa.txt`

## 区域验收

| 区域 | 验收要求 | 结果 |
|---|---|---|
| 顶栏 | 品牌、Flash/Pro、在线状态、设置与窗口控制清晰 | 通过 |
| 左栏 | 搜索、新建、角色头像、会话编辑、保存与清空可交互 | 通过 |
| 信息卡 | 长期记忆、上下文压缩、服务状态层级清楚 | 通过 |
| 角色舞台 | 右侧大尺寸立绘，背景为高清书房/卧室，不使用小人物卡片 | 通过 |
| VN 对白 | 大型半透明蓝色对白框、名字牌和中文回复 | 通过 |
| 快捷动作 | 陪我聊、做计划、找灵感、复盘图标与功能一致；按钮只保留主标题，无矩形底色或残留文字 | 通过 |
| 输入区 | 只保留文字输入、透明回形针与水蓝发送按钮；两项操作不相交，不提供语音输入 | 通过 |
| 语音区 | 试听、日语女声、分隔、引擎状态和波形不重叠；空间不足时整组遥测安全隐藏 | 通过 |
| 工具栏 | 记忆、设置、外观均有对应功能与选中反馈 | 通过 |
| 设置浮窗 | 模型连接与语音/记忆分为两页，无旧控件残留或重叠 | 通过 |
| 能力中心 | A/B/C 开关、搜索连接、目录、应用白名单和 Skill 分页清晰；卡片无黑边或系统虚线框 | 通过 |

## 交互验收

- 会话支持重命名、置顶和删除，菜单关闭后正确释放。
- 厂商切换后模型列表、Key、Base URL、顶部徽标与服务状态同步更新。
- 不同厂商分别保存 Key；模型列表可刷新，模型 ID 可手动输入。
- 角色状态支持自然眨眼、思考、说话和多情绪动画。
- 名字牌、快捷动作、设置和右侧工具栏均有真实点击反馈。
- 标准窗口与紧凑窗口保持输入、发送、试听和 VN 对白主工作流。
- 1280×720 逻辑尺寸下，语音 Dock 的播放按钮、说明、分隔线、波形和引擎状态均在卡片内部，且不与输入区或底栏相交。
- 8 组尺寸矩阵逐项检查快捷按钮、输入文字、附件、发送、试听说明、分隔、波形和引擎状态的包含关系与间距。
- 首次语音部署使用独立 owner-drawn 浮窗，进度、阶段和细节分层显示。
- “重新部署 GPT-SoVITS 语音”位于独立语音页，与开关边界不相交。
- “工具与隐私”打开独立能力中心；三页切换、开关、目录和白名单操作均有真实反馈。
- 输入框回形针只用于选择图片，不恢复语音输入或挤压发送按钮。
- 高清背景或立绘缺失、损坏时，程序在创建主界面前终止，不再显示开发占位人物。

## 视觉约束

- 不使用紫色矩形按钮、系统默认硬边框或后台管理式表格主布局。
- 不把角色缩进小卡片；舞台立绘保持右侧主视觉。
- 背景和角色采用高清原图，不用低分辨率截图充当整页 UI。
- 玻璃面板保持浅蓝白、低对比边缘和足够文字可读性。
- UI 图标必须对应已实现功能，不放置装饰性假按钮。
- 语音输入已移除，底部布局不保留空麦克风占位。

## 已知差异

当前实现使用 WinForms/GDI+ 的真实交互控件，而参考图是静态概念稿，因此字体栅格化、玻璃折射和局部装饰细节不会逐像素相同。验收重点是第一视觉印象、区域比例、角色舞台、对白层级、高清素材和完整交互；这些项目均已达到本基线。

任何后续 UI 修改都必须重新生成完整、设置、紧凑和最小窗口截图，并执行 `FunctionalQaHarness` 与 `SettingsUiQaHarness`；语音 Dock、输入区和快捷操作必须通过多尺寸矩阵的子控件可见性、边界、间距与透明底色断言，不得只在单一分辨率下目测。
