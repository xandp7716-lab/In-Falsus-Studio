# InFalsus Studio+

<img src="Branding/app-icon.png" width="96" alt="M+ icon" align="right">

**面向 In Falsus 的社区增强制谱器，项目名 `infstudioplus`。**

Forked from [Arthas-0/In-Falsus-Studio](https://github.com/Arthas-0/In-Falsus-Studio)。保留上游 Unity 工程，在 Windows Mono 1.0.10 编辑器上通过一个整合插件提供当前增强功能。

当前界面版本 **1.06**，内部插件版本 **1.0.70**。[最新源码](Mods/InFalsusStudioPlus/1.06/README.md) · [构建说明](Mods/InFalsusStudioPlus/1.06/BUILDING.md) · [更新记录](docs/releases/1.06.md) · [兼容性](Mods/InFalsusStudioPlus/1.06/COMPATIBILITY.md)

## 当前功能

- 原生轨道/HUD/背景视觉与现代编辑界面；M＋ 程序图标和亮蓝 Q 版 Mei 设置描边图。
- 多选、复制、镜像、批量编辑、SkyArea 切分与端点编辑；粘贴保留各物件的原组号和时间组。
- 底栏快速工具：删除/复制 Timing 直接用段落和目标时间选择；SkyArea 等分、按网格切分及 Flick 转换。
- 时间/位置端点拖移模式，可修改热键，支持鼠标侧键与中键；切窗后恢复把手与点立得。
- 预览、编辑提交、时间组和时序列表的索引/缓存/虚拟化优化。
- 通用外置工具：可输出 SPC 语句或追加/替换/删除补丁，保留旧工具接口；提供括号头隧道与随机散布可选示例。
- 无损源码与手动保存流程，保留原 token、未知语句、BOM/EOL、稳定 SourceId、撤销和失败回滚。

## 使用与构建

这是源码仓库，**不是可直接运行的整合包**。仓库不分发编译 EXE/DLL、BepInEx、游戏提取资源、字体、个人设置和谱面。当前源码需要匹配的 Windows Mono 编辑器及既有运行资源，按 [1.06 构建说明](Mods/InFalsusStudioPlus/1.06/BUILDING.md) 编译/更新。

底层 Unity 工程仍为上游 1.0.10；直接构建根目录工程不会自动装载增强插件。原工程功能、Unity 环境与构建入口见 [上游说明](README.upstream.md)。

## 非官方扩展与贡献

时间组 `timinggroup`、`noinput` / `noclip`、`hidegroup` 是插件支持的非官方壳子扩展，不能视为官方游戏格式。安装插件后识别扩展，打开谱面不自动迁移；保存保留扩展文本，未提供静默转成官方格式的流程。编辑器支持不等于游戏判定/播放兼容，见 [兼容性与验证边界](Mods/InFalsusStudioPlus/1.06/COMPATIBILITY.md)。

提交或 PR 包含非官方扩展时，标题仍须带 `[非官方壳子]` 并说明目标编辑器、壳子版本和验证范围。开发入口见 [AGENTS.md](AGENTS.md)、[贡献规范](CONTRIBUTING.md) 和 [架构](docs/ARCHITECTURE.md)。

素材、角色和第三方依赖分别遵循各自权利范围；社区派生图形不表示官方背书。见 [来源记录](THIRD_PARTY_NOTICES.md)。
