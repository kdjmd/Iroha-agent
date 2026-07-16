# 安全说明

## 凭据处理

- DeepSeek API Key 只应通过应用设置界面写入本机用户配置目录。
- 不要把真实密钥写入源码、README、Issue、截图或 Git 提交历史。
- 提交前运行 `git diff --cached`，确认没有 `settings.json`、`memory.json`、`.env` 或日志文件。
- 如果密钥曾进入提交历史，应立即在服务商后台撤销并重新生成，单纯删除文件不足以消除泄漏。

## 本地服务

GPT-SoVITS 默认只监听 `127.0.0.1:9880`。不要在没有访问控制的情况下把端口暴露到公网。

“重新部署语音”只允许清理 `%LOCALAPPDATA%\IrohaLocalAgent` 下的应用托管副本。任何修改路径边界、进程筛选或递归删除逻辑的变更都必须重新执行 Bootstrap QA。

## 发布前检查

```powershell
git status --short
git ls-files | Select-String -Pattern 'settings.json|memory.json|crash.log|\.env'
git ls-files | Select-String -Pattern '\.(ckpt|pth|wav|exe|zip|7z)$'
```

发布包应在源码目录之外解压并启动验证。FullVoice 含大型第三方运行时和角色语音资源，公开上传前必须单独完成来源、许可和再分发授权核查。
