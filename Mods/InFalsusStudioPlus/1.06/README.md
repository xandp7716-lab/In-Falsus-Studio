# InFalsus Studio+ 1.06

`infstudioplus` 的当前整合插件源码节点，内部程序集版本 **1.0.70.0**，面向 **InFalsus Studio Windows Mono 1.0.10**。127 个 C# 源文件与当前安装版本的源码逐一校验一致；源码哈希见 [SOURCE_MANIFEST.json](SOURCE_MANIFEST.json)。

该节点包含原生视觉、现代界面、编辑与预览优化、时间组、快速工具、可修改热键及通用外置工具接口。复制粘贴保留各物件的 SkyArea 组号和时间组；起止端点支持时间/位置拖移模式；切窗后恢复把手与点立得输入。M＋ EXE 图标和亮蓝 Q 版 Mei 设置描边图随源码保留。

这是一份源码交付，不包含编译 DLL、修改后的 EXE、BepInEx、游戏提取资源、字体、整合 ZIP、个人设置或谱面。完整运行仍需要匹配的编辑器及原有运行资源。[构建与安装说明](BUILDING.md) · [兼容性](COMPATIBILITY.md) · [外置工具接口](EXTERNAL_TOOLS.md) · [验证边界](VALIDATION.md)。

## 工具分发

- `ExternalTools/Default` 保留 GradientTiming、DeleteTiming、CopyTiming 三个原有工具源码。删除/复制 Timing 现在通过底栏快速工具直接选择段落和目标时间；它们在外置工具列表中隐藏，旧脚本仍兼容。
- `ExternalTools/Optional` 收录括号头隧道和随机散布工具，需自行安装。随机散布保留历史目录 ID `BlacklinePatternScatter`；不包含用户的 `pattern.json` 捕获缓存。
- 加入新工具时，使用 `output: spc` 通用接口输出语句或批量补丁，无须在编辑器内增加工具 ID 分支。未知语句的保留不等于预览器已经实现它的游戏语义。

## 版本边界

公开界面版本为 1.06，程序集版本为 1.0.70.0，底层编辑器仍为上游 1.0.10。这些版本号分别记录，避免把插件源码快照误认为重建过的 Unity 编辑器核心。

Forked from [Arthas-0/In-Falsus-Studio](https://github.com/Arthas-0/In-Falsus-Studio)。时间组为非官方壳子扩展；目标游戏内判定和播放没有在本次源码提交中验证。
