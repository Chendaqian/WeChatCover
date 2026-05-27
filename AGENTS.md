# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

WeChatCover 是一个 Windows 隐私保护工具：当用户从微信窗口切换到其他应用时，自动在微信窗口上方覆盖一层马赛克遮罩，防止他人窥屏。运行时驻留在系统托盘。

## 技术栈

- 语言：C# 8.0
- 框架：.NET Framework 4.0 (WinForms)
- 项目格式：旧版 MSBuild (.csproj)，**不是** SDK-style
- IDE：Visual Studio 2022

## 构建命令

```bash
# 命令行构建（需要 MSBuild 或 dotnet）
msbuild WeChatCover.sln /p:Configuration=Release

# 调试构建
msbuild WeChatCover.sln /p:Configuration=Debug
```

没有测试项目。验证方式为运行程序后切换窗口观察遮罩效果。

## 架构

单项目结构，核心文件均在 `Src/WeChatCover/` 下：

| 文件 | 职责 |
|------|------|
| `Program.cs` | 入口点，启动 FormMain |
| `FormMain.cs` | 核心逻辑：定时轮询前台窗口（300ms Sleep），检测微信窗口（类名 `WeChatMainWndForPC`），截屏+马赛克后作为子窗口覆盖到微信上 |
| `FormMain.Designer.cs` | 窗体设计器：NotifyIcon 托盘图标 + 右键菜单（仅 Exit） |
| `NativeCodes.cs` | Win32 P/Invoke 声明（user32.dll）：窗口枚举、父子窗口设置、窗口显示/隐藏 |
| `Utilities.cs` | 图像工具：`Mosaic()` 方法实现逐像素块平均值马赛克算法 |

## 关键实现细节

- **窗口检测**：通过 `GetForegroundWindow()` + `GetClassName()` 轮询前台窗口，微信主窗口类名含 `WeChatMainWndForPC`
- **遮罩机制**：`SetParent(Handle, _weChatHandle)` 将遮罩窗体设为微信的子窗口，`SetForegroundWindow` 恢复焦点
- **订阅号窗口**：枚举类名 `CWebviewControlHostWnd` 的子窗口，遮罩时隐藏，解除时恢复（目前需手动打开）
- **窗口黑名单**：`ClassNameBlackList` 排除微信图片查看器（`ImagePreviewWnd`）等弹窗，避免遮挡
- **马赛克算法**：`Mosaic()` 将图片按 `radius` 分块，每块取平均颜色填充，纯 CPU 像素操作（`GetPixel`/`SetPixel`）
- **遮罩文字**：通过 `App.config` 的 `Notice` 用户设置项配置遮罩层显示文字，默认 `(^ω^)`

## 项目文件注意事项

`.csproj` 是旧版格式，新增 `.cs` 文件后必须手动将其添加到 `<Compile Include="...">` 中，否则不会被编译。
