# GitHub 上传指南

本仓库是 Windows-only 源码仓库。建议先创建私有仓库 `IrohaAgent`，确认角色、图片和语音素材授权后再决定是否公开。

当前仓库：`https://github.com/kdjmd/Iroha-agent`。

## 1. 创建空仓库

在 GitHub 网页创建空的私有仓库，不要自动生成 README、`.gitignore` 或 License，避免首次推送冲突。

## 2. 推送源码

在本项目根目录执行：

```powershell
git remote add origin https://github.com/kdjmd/Iroha-agent.git
git push -u origin main
```

如果已经存在 `origin`：

```powershell
git remote set-url origin https://github.com/kdjmd/Iroha-agent.git
git push -u origin main
```

也可以在 GitHub Desktop 中选择 `File -> Add local repository`，指向本目录后点击 `Publish repository`。

## 3. 生成 Windows Release

轻量 Portable：

```powershell
.\tools\build-windows-release.ps1 -Version 2.1.1
```

完整 FullVoice：

```powershell
.\tools\build-windows-release.ps1 `
  -Version 2.1.1 `
  -FullVoice `
  -RuntimeArchive "C:\path\to\GPT-SoVITS-runtime.7z" `
  -VoicePackage "C:\path\to\iroha-model.zip"
```

默认输出到桌面：

```text
IrohaAgent-GitHub-Releases\v2.1.1\
```

## 4. 创建 Release

在 GitHub 中创建标签与 Release：

```text
Tag: v2.1.1
Title: Iroha Agent v2.1.1 - Windows
```

上传以下附件：

- `IrohaAgent-Windows-v2.1.1-Portable.zip`
- `IrohaAgent-Windows-v2.1.1-FullVoice.7z.001`
- `IrohaAgent-Windows-v2.1.1-FullVoice.7z.002`
- 其余所有 FullVoice 分卷
- `RELEASE_NOTES.txt`
- `SHA256SUMS.txt`

用户必须下载全部 FullVoice 分卷，并使用 7-Zip 从 `.001` 解压。

## 5. 发布前核查

- `git status` 只包含预期源码与文档变更。
- 仓库中没有 `settings.json`、`memory.json`、`.env`、API Key 或聊天记录。
- 仓库中没有 `.ckpt`、`.pth`、`.wav`、完整运行时或 Release 分卷。
- `desktop\build.ps1` 编译通过。
- 功能、部署和语音 QA 报告全部通过。
- `SHA256SUMS.txt` 与本地附件一致。
- 已确认 FullVoice 中角色与语音资源的再分发授权。

不要把 EXE、模型权重、完整运行时、发布 ZIP/7Z 或分卷直接提交进 Git 历史，也不要用 Git LFS 绕过授权和 Release 边界。

## 6. 使用 GitHub CLI 替换旧发行版

首次在当前电脑授权：

```powershell
gh auth login --hostname github.com --git-protocol https --web
gh auth status
```

提交并推送源码前，先核对 `git diff`，再显式添加本次文件：

```powershell
git add README.md CHANGELOG.md desktop tools docs
git commit -m "fix portable voice deployment and memory persistence"
git push origin main
```

先创建并完整上传 v2.1.1，确认附件和哈希后，再删除旧的首个发行版，避免下载中断：

```powershell
gh release create v2.1.1 `
  --repo kdjmd/Iroha-agent `
  --title "Iroha Agent v2.1.1 - Windows" `
  --notes-file ".\docs\RELEASE_NOTES_v2.1.1.md" `
  ".\IrohaAgent-GitHub-Releases\v2.1.1\*"

gh release view v2.1.1 --repo kdjmd/Iroha-agent
gh release delete v2.1.0 --repo kdjmd/Iroha-agent --cleanup-tag --yes
```

若只需要替换同一标签的附件，可先用 `gh release delete-asset` 删除同名旧附件，再执行 `gh release upload v2.1.1 <文件> --clobber`。不要在新附件上传完成前删除唯一可下载版本。
