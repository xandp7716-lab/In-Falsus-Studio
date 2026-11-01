# 外置工具通用接口（协议版本 1 / 当前插件 1.06）

每个工具一个文件夹，文件夹名与 tool.json 的 id 一致。工具由配置描述参数界面，用 PowerShell 接收 -RequestPath 与 -OutputPath，可在脚本内调用任意本地语言。生成器不直接写当前谱面。编辑器读回结果后排入现有编辑事务，一次撤销，用户手动保存。

## 新工具使用 output: spc

```json
{
  "version": 1,
  "id": "MyTool",
  "name": "我的工具",
  "nameEn": "My Tool",
  "script": "Tool.ps1",
  "output": "spc",
  "fields": []
}
```

不用在插件中登记 MyTool。目录放入 ExternalTools 后按工具栏的重载按钮发现。原 blackline / timing / split / timingedit 协议继续可用，校验和语义保持兼容；新工具不必套用它们。

可选 manifest 字段：

| 字段 | 作用 |
| --- | --- |
| selection | any / skyarea / air；执行时要求非空选择且类型符合。省略则不要求选中 |
| gridField | 指向一个整数参数 key，为选中的每条 SkyArea 提供主界面当前时间组和细分对应的节拍网格 |
| groupMode | noinput；显示默认开启的新建 noinput 组选项。关闭时要求当前组已有 noinput / noclip。省略则不限制普通组 |
| capture | true 显示持久化捕获按钮 |
| captureKind | air 原样捕获所选 Flick / SkyArea；省略保留旧版中心线路径捕获 |

参数字段仍支持 time、integer、number、text、file、bool、choice；与已有工具的配置方式相同。

## 请求数据

version 仍为 1，兼容旧字段。spc 工具额外获得：

- document：整份谱面原文；hasBom：源 BOM 状态。
- rows：所有源行，含 sourceId、index、source 原文、editable、tokens、kind、scope。注释、未知语句、非官方语句同样提供，不因不参与预览而省略。sourceId 是本次文档会话中的稳定标识，不是行号。
- selection：所选源行；capture：本工具持久化捕获的完整物件；skySources：selection 中的 SkyArea。
- currentTime、currentGroup、targetGroup、createGroup、overflow。
- range：最近一次有效段落选中的 start/end；没有则为 null。
- values：key/value 参数数组；groupStart：当前全谱最大显式 SkyArea 组号加一；没有可分配的整数组号时为 -1，工具应明确报错，不能回绕到0。
- 有效 SkyArea 源行预计算 start/end、left0/right0/left1/right1、easeL/easeR、split。声明 gridField 时，skySources 的 grid 为主界面当前时间组及细分下 BPM 网格的精确时间，含原始起点；每条最多4096点。mode=count 时省略网格，避免不需要的密集采样。

完整原文与 tokens 用来处理通用语法；归一化数据只是便利字段，不限制可输出的语句类型。遇到尚未实现的语法可以无损保留，接口不会自动给它新增预览语义。

## 输出一：普通 SPC 文本

输出任意支持的语句，例如：

```text
tap(1000,2,1)
skyarea(1200,50,100,30,70,100,30,1,2,100,1000)
track(1300,-0.5)
```

没有时间组边界时追加到 targetGroup。有 timinggroup / groupend 边界时按原文整块追加到全谱，不强行移动进当前组。可包含注释、未知语句和已有非官方参数。非官方 timinggroup / noinput / noclip / hidegroup 的可用性仍取决于目标壳子，并非官方本体保证。

## 输出二：通用 JSON 修改补丁

```json
{
  "version": 1,
  "operations": [
    {"action":"append","scope":"$target","lines":["tap(1000,2,1)","track(1200,0.5)"]},
    {"action":"replace","sourceId":12,"expected":"track(100,1)","text":"track(100,2)"},
    {"action":"delete","sourceId":15}
  ]
}
```

- append：lines 为逐行原文，也可用 text 表示单行。scope 为组名、空字符串（基础组）、$target（请求中的目标组）、null 或省略（全局按原文追加）。整块时间组须用 null scope；可向指定组插入任意语句或注释。
- replace：替换一行，保留该行 SourceId 与换行。text 可为语句或注释。expected 可选，用于核对原文。
- delete：按请求 rows 中的 SourceId 删除一行。删除整组时需同时删除组边界和组内源行，不能留下半个时间组。
- 同一已有 SourceId 在一批中只能被 replace/delete 一次；新插入行无需也不能猜测 SourceId。
- 默认不删除源；删除是工具输出的明确操作，不由接口推断。

结果可另写 OutputPath + .info.json：{"segments":16,"message":"工具摘要"}。segments 为生成物件数，可让计算物件数显示物件数量；未提供时显示修改行数。摘要不参与结果有效性判断。

## 通用校验与提交

不限定物件宽度、坐标分母、藏头方式或配对模式。已有整数坐标和参数语义由编辑器当前模型校验，不为每个工具硬编码生成规则。已知物件要有有效参数；新生成 SkyArea 必须正时长，禁止 0ms；超界物件需开启天空超界。未知语句保留原文，不宣称可预览。

完整组边界、组名及现有模型能检查的结构须有效。结果最多 8,000,000 字节、64000 行操作；脚本超时120秒。运行期间文档、编辑修订或当前组变化则取消；应用前再检查一次。计算数量也在文档克隆上完整验证，不能建组、删源或产生撤销步骤。成功后才用一个事务提交，保留源 token、未知语句、BOM/EOL 与撤销恢复，不自动排序或保存。

新增同类生成工具只需配置与脚本。只有引入编辑器本身尚未实现的新语法/渲染行为，或全新的参数控件类型，才需要修改编辑器。
