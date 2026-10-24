# 构建与安装 InFalsus Studio+ 1.06

## 前置条件

- Windows、PowerShell、InFalsus Studio **Windows Mono 1.0.10**。
- 匹配的 `InFalsusStudio_Data/Managed` 及 `BepInEx/core`。目标核心程序集 SHA-256 见 [兼容性说明](COMPATIBILITY.md)。
- Unity 2022.3.8f1c1 的 Roslyn / NetCoreRuntime 编译工具。参数指定 Unity 安装根目录，脚本不依赖作者机器的盘符。
- 现有整合版的 Presentation 运行资源、所需字体与依赖；仓库不再次分发游戏提取资源。原生视觉和新界面不能仅凭一个 DLL 独立运行。

在仓库根目录执行：

```powershell
.\Mods\InFalsusStudioPlus\1.06\Build.ps1 `
  -Editor 'C:\Games\InFalsusStudio_1.0.10' `
  -UnityEditor 'C:\Program Files\Unity\Hub\Editor\2022.3.8f1c1'
```

脚本编译 127 个源码文件并嵌入 `Source/BrandAssets/about-mascot.png`，输出 `Source/InfStudioVisualUI.dll`，Git 忽略此产物。程序集身份仍为 `InfStudioVisualUI`、入口 ID 仍为 `local.infalsusstudio.visualui`，用于保留既有配置与插件兼容性；项目/界面名称为 InFalsus Studio+。

## 更新已安装的整合版

1. 保存并退出目标编辑器。
2. 备份原 `BepInEx/plugins/InfStudioVisualUI/InfStudioVisualUI.dll`，再放入本次构建的 DLL。保留配置、Presentation 资源、ExternalTools 和用户谱面；不同时加载独立旧 NativeVisualMod / NativeEditorUI DLL。
3. 如需更新 EXE 图标，在备份后执行：

```powershell
.\Mods\InFalsusStudioPlus\1.06\Tools\Set-AppIcon.ps1 `
  -Editor 'C:\Games\InFalsusStudio_1.0.10'
```

该脚本只更新目标 EXE 的图标资源，并检查非图标资源及非资源 PE 区段不变。它不会从源码构建完整编辑器，也不会更新你的安装器记录。

外置工具可将选定工具文件夹复制到编辑器根目录的 `ExternalTools` 下。保留原文件夹 ID；不要将 `Default` / `Optional` 本身作为工具目录。先备份已有同名工具，保留个人捕获数据。删除/复制 Timing 已有内置快速入口。

本节点没有自动下载依赖或发布完整整合包。底层 Unity 工程的构建仍按根目录 [上游构建说明](../../../docs/BUILDING.md)执行；仅构建底层工程不会自动安装此可选插件。
