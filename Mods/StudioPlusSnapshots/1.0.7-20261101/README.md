# 当前制谱器源码备份

2026-11-01，插件版本 1.0.7。保存当前 studio_icon_work/Source、构建与安装脚本、外置工具及构建嵌入资源。保留原相对目录布局。

构建：在本快照目录执行 studio_icon_work/Build.ps1，Editor 指向已安装副本，UnityEditor 指向本机编译工具。外部 Unity、BepInEx 和本体运行资源依照 BUILDING.md 与 distribution.json 提供。

对应已安装 DLL SHA256：8590C37A3390B9686D0D8E9E2A1F65F1FC1065B56327F9AC15AF6058184DCFE0。
包含非游玩常驻 AP、动态游玩血条、HDR 视频合成、SPC2、noinput 裁切及 FFmpeg 路径布局。未重新编译或运行测试。

恢复源码时从本快照提取文件到新目录，按原相对布局构建；请先保存后续工作。
