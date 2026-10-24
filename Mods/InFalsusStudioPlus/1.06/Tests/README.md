# 隔离 Player 检查

`FocusChecks.cs` 和 `BrandChecks.cs` 使用运行中的编辑器、反射入口和内存合成谱面验证焦点恢复/设置品牌绘制。测试会静音音乐及 PCM 打击音，结束后退出测试 Player；不保存用户谱面。

先构建本节点插件，准备一个与日常编辑器分开的完整测试副本（含原有运行资源），关闭测试副本后执行：

```powershell
.\Mods\InFalsusStudioPlus\1.06\Tests\Run-PlayerChecks.ps1 `
  -Editor 'C:\Games\InFalsusStudio_1.0.10' `
  -TestHost 'C:\Tests\InFalsusStudio_Isolated' -Suite focus
```

`-Suite brand` 检查设置图片、描边遮罩及标题像素。脚本只向指定隔离 TestHost 放入测试插件和候选程序集，不改日常 Editor 的文件或配置。测试结果在 `Tests/Results` 下，不纳入 Git。查看 `result.txt` 的 `error` 与 `exceptions`，不要仅凭启动成功判断通过。

焦点套件注入 Unity 焦点回调及残留输入状态，不是物理 Win 键桌面操作验收。测试中有明确的测试时限；不要对实际写谱窗口执行它。
