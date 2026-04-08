# XEmuera-R

`XEmuera-R` 是一个基于 `Xamarin.Forms` 的安卓版 Emuera 解释器分支。

项目地址：

- https://github.com/Future-R/XEmuera

## 简介

本项目源自 `XEmuera`，底层解释器内核来自 `Emuera1824+v15` 私家改造版，并在此基础上持续做 Android 可用性和 `EE+EM` 版新特性的兼容修复。

这个分支相较于之前的XEmuera，主要改动包括：

- 虚拟手柄映射
-  `HTML_PRINT` 兼容修正
- 音频相关支持
- 可从游戏目录下 `font` / `fonts` 加载外部字体
- 自动识别 `UTF-8 / UTF-8 BOM / Shift-JIS` 编码
- 解释器日志

## 使用说明

- 首次启动时间可能较长，请耐心等待。
- 游戏目录放在存储根目录下的 `emuera` 文件夹内。
- 每个游戏目录至少需要有 `ERB` 和 `CSV` 文件夹。
- 外部字体可放在游戏目录下的 `font` 或 `fonts` 文件夹中，支持 `*.ttf` / `*.otf`。
- 当前内置字体包含 `MS Gothic` 和 `Microsoft YaHei`。
- Android 10 及以上通常需要授予文件管理权限。
- 从屏幕左侧向右滑动可以打开侧边菜单。
- 解释器报错后，可以在侧边菜单里打开 `解释器日志` 页面并直接复制日志内容。

目录示例：

```text
/storage/emulated/0/emuera/
  era萝乐娜/
    CSV/
    ERB/
    font/
    resources/
    sound/
```

## 构建与打包

仓库内已经提供了可直接使用的脚本：

- `build-android.ps1`
  作用：调用 Visual Studio 自带的 `MSBuild.exe` 构建 Android 工程
- `package-android.ps1`
  作用：执行 `SignAndroidPackage`，并把最新签名 APK 复制到统一产物目录

常用命令：

```powershell
powershell -ExecutionPolicy Bypass -File .\build-android.ps1
```

```powershell
powershell -ExecutionPolicy Bypass -File .\package-android.ps1
```

默认打包产物位置：

- `artifacts/android/XEmuera-android-Release.apk`

## 已知问题

- 项目仍基于较旧的 Xamarin.Android / Xamarin.Forms 技术栈。
- 仍有部分 Emuera / EvilMask 文案没有完全中文化。
- `HTML_PRINT` 兼容尚未完全追平桌面版。
- 某些字体的加粗、斜体、字形回退和桌面版可能仍有差异。
- 构建时会看到 `SkiaSharp 2.88.0` 的已知漏洞警告；当前保留该版本是为了和旧 Xamarin.Android 打包链保持兼容。

## 关于

- 改版作者：未来
- 原作者：Fegelein21
