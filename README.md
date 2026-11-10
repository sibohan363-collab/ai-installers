# AI 一键安装程序

两个 Windows 小程序，双击就能自动装好 AI 工具，最后只需要登录自己的账号。

| 文件 | 装什么 | 需要梯子吗 |
|---|---|---|
| [Codex一键安装.exe](Codex一键安装.exe) | ChatGPT 电脑版（含 Codex 和 Work 模式），来自微软商店 | 需要 |
| [DeepSeek一键安装.exe](DeepSeek一键安装.exe) | DeepSeek Harness，来自 DeepSeek 官网 | 不需要（建议关掉梯子） |

## 用法

1. 下载对应的 `.exe`，双击运行。
2. 如果 Windows 弹出「Windows 已保护你的电脑」，点「更多信息」→「仍要运行」（程序没有付费签名，所以会提示）。
3. 程序会自动下载、安装、放好桌面图标并打开应用。
4. 登录自己的账号就能用。

## 用 AI 做 PPT

选一个文件夹，把资料放进去，然后告诉它：

> 根据这个文件夹里的资料，做一份 15 页的 PPT，风格简洁专业，保存成 .pptx

不满意就直接说哪里要改，它会接着修改。

## 说明

- 程序只从官方地址下载：微软商店（ChatGPT，商店编号 `9PLM9XGG6VKS`）和 `download.deepseek.com`。
- 不会收集、上传任何信息，也不会修改网络设置。
- 源码是 `Installer.cs`（Codex）和 `DshInstaller.cs`（DeepSeek），可以用 Windows 自带的 `csc.exe` 编译。
