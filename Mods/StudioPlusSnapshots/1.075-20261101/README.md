# InFalsus Studio+ 源码快照 1.075

2026-11-01。内部源码版号 **1.075**；公开界面版本 1.0.7，程序集绑定版本 1.0.7.0，AssemblyInformationalVersion 为 1.075。适用于 InFalsus Studio Windows Mono 1.0.10；非官方语义目标为 InFalsusModded1.07b。不是重新发布 Unity 核心，也没有生成整合安装包。

保存当前149个C#源文件、构建脚本、默认工具、HUD及其他自有Shader源码、必要嵌入资源和开发交接。包含SPC2天空判定、内置游玩与动态血条、离线视频渲染、分辨率采样修复、小字SDF范围修正、原生血条过渡与NO JACKET显示适配。

## 构建

在本快照目录执行：

```powershell
.\studio_icon_work\Build.ps1 -Editor 'C:\Games\InFalsusStudio_1.0.10' -UnityEditor 'D:\Unity\2022.3.8f1c1'
```

需要匹配编辑器的Managed程序集与BepInEx/core引用。输出为studio_icon_work/Source/InfStudioVisualUI.dll，Git忽略此产物。HUD Shader可用Unity 2022.3.8f1c1打开feature_work/native-dots-text/ShaderProject，执行BuildHud.Build。

运行需要已安装匹配版本的完整Assets/Presentation依赖，精确指纹见studio_icon_work/distribution.json。本快照不上传本体DLL、原版字体、提取的NO JACKET图片及其他完整运行资源，也不上传配置、谱面、日志、缓存或可执行工具。仅保留与上一源码快照一致的两项构建嵌入资源，确保Build.ps1相对路径可用。自有Shader源及依赖说明完整保留。

## 版本与验证边界

此次只更新内部源码标记，不改公开公告、插件入口ID或现有副本二进制。基础功能对应已安装DLL FCB80D91CE9E58C9185DD54706D02EABE8FD7260C9C8C38FC2B6EE6749DDCDED；加上内部标记后的源码另做编译检查。历史25项HUD/血条检查分别在1080p与2K通过，见feature_work/hud-native-refine/RESULTS.md；该历史检查不冒充本轮重新执行的界面验收。

源码校验见studio_icon_work/SOURCE_MANIFEST.json；本快照文件校验见SNAPSHOT_MANIFEST.json。相较前一1.0.7快照，保留源码目录布局，不覆盖历史节点。

## 非官方语义

延续timinggroup(name[,noinput|noclip]): ... groupend()、组内hidegroup(ms,0/1)及BPM/track扩展，以现有COMPATIBILITY.md为准；普通语句时间单位为毫秒，不在本轮改变语法或保存规则。官方本体不保证支持这些扩展，不能将本快照当作官方格式兼容认证。本轮不新增语法；官方模式界面与运行未重新验证。
