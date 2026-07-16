# GitHub 上传指南

本地仓库已经整理并提交到 `main` 分支。建议在 GitHub 中创建私有仓库 `IrohaAgent`，不要勾选自动生成 README、`.gitignore` 或 License，以免首次推送产生冲突。

当前已连接的 GitHub 账号为 `eistinlandfrank`。

仓库包含角色和语音相关元数据，首次发布请保持私有；确认素材授权后再决定是否公开。

## 命令行上传

在 GitHub 网页创建空的私有仓库后，在本目录执行：

```powershell
git remote add origin https://github.com/eistinlandfrank/IrohaAgent.git
git push -u origin main
```

首次推送时，Git Credential Manager 可能打开浏览器要求登录或授权。完成后回到终端等待推送结束。

## GitHub Desktop 上传

1. 安装并登录 GitHub Desktop。
2. 选择 `File -> Add local repository`。
3. 指向桌面的 `IrohaAgent-GitHub` 文件夹。
4. 点击 `Publish repository`。
5. 仓库名填写 `IrohaAgent`，取消公开选项后发布。

## 上传 Release

源码推送成功后，在 GitHub 仓库中创建 `v2.0.0` Release，并从桌面的 `IrohaAgent-GitHub-Releases` 文件夹添加：

- `IrohaAgent-Windows-v2.0.zip`
- `IrohaAgent-Android-v0.1.0-debug.apk`（仅作为原型，并明确标注）
- `SHA256SUMS.txt`

不要把 EXE、APK、模型权重或发布 ZIP 直接提交进 Git 历史。
