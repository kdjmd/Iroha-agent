# Iroha Agent 工程归档索引

更新时间：2026-07-21
当前稳定版：v2.3.1 Windows

## 从哪里开始

1. 项目介绍与用户安装方法：根目录 `README.md`
2. 工程验收和后续交接：`docs/彩叶_Iroha_Agent_工程验收与交接手册.md`
3. 最新发行说明：`docs/RELEASE_NOTES_v2.3.1.md`
4. UI 验收标准：`docs/design-qa.md`
5. Tools 使用与工程边界：`docs/TOOLS_USER_GUIDE.md`、`docs/TOOLS_ARCHITECTURE.md`

## 目录说明

- `desktop/`：Windows WinForms 主程序、构建依赖与产物脚本。
- `assets/`：角色立绘、逐帧动画、视觉小说背景和 UI 资源。
- `voice-pack/`：项目内的语音包配置与说明。
- `tools/`：构建、回归测试、截图验收和 Windows 发行脚本。
- `docs/`：工程书、设计验收、架构、安全、发布和维护文档。
- `docs/evidence/`：各轮 UI 与功能验收证据。
- `release/v2.3.1/`：D 盘归档中额外保存的最终可下载发行资产。
- `voice-source/`：D 盘归档中额外保存的原始 GPT-SoVITS 模型包。

## 常用命令

在项目根目录打开 PowerShell：

```powershell
powershell -ExecutionPolicy Bypass -File .\desktop\build.ps1
powershell -ExecutionPolicy Bypass -File .\tools\run-regression-qa.ps1
powershell -ExecutionPolicy Bypass -File .\tools\build-windows-release.ps1 -Version 2.3.1
```

## 发行文件

- `IrohaAgent-Windows-v2.3.1-Portable.zip`：便携版；文字聊天可直接使用，语音需要已有 GPT-SoVITS 或手动配置。
- `IrohaAgent-Windows-v2.3.1-FullVoice.7z.001` 至 `.005`：完整语音版；五个分卷必须放在同一目录，只解压 `.001`。
- `SHA256SUMS.txt`：全部发行资产的 SHA-256 校验值。
- `RELEASE_NOTES.txt`：面向安装用户的简要说明。

## 隐私与凭据

工程归档和发行包均不包含 API Key、聊天记录、长期记忆、个人设置或崩溃日志。API Key 由每位用户在自己的 Windows 账户中填写，并使用 CurrentUser DPAPI 保护。

GitHub 仓库：`https://github.com/kdjmd/Iroha-agent`
