using InFalsusStudio.Core;
namespace InFalsusStudio.NativeEditorUI {
 public sealed partial class NativeEditorUIPlugin {
  static readonly ReleaseNotesEntry[] IntegratedReleaseNotes={
   new ReleaseNotesEntry("1.06",
    "• 程序图标换为 M＋，设置 → 操作改用统一亮蓝的 Q 版 Mei 描边图。\n• 修复按 Win 键切出再返回后把手拖动与点立得放置失效，恢复鼠标占用和实际修饰键状态。\n• 项目更名为 infstudioplus，界面名称为 InFalsus Studio+ 1.06；设置页名称采用分数字体并修正版本间距与基线，底栏不再显示名称。\n• 复制粘贴保留各物件原 SkyArea 组号和时间组，目标缺少该时间组时保留原组属性补建。\n• 设置 → 操作底部加入深蓝浅蓝角色图与 Fork 来源署名。",
    "• Uses the M+ executable icon and single bright-blue chibi Mei outline artwork in Settings > Controls.\n• Restores handle dragging and point placement after Windows focus changes by releasing stale capture and reconciling physical modifiers.\n• Renames the project to infstudioplus, displayed as InFalsus Studio+ 1.06; the score-font about title has explicit version spacing and baseline alignment. Footer branding is removed.\n• Pasting preserves each note's SkyArea group ID and timing group, creating missing groups with captured properties.\n• Adds the navy/light-blue mascot and fork attribution under Settings > Controls."),
   new ReleaseNotesEntry("1.0.64",
    "• 地面网格弹窗按时间戳文字长度自动调整宽度，不再横向压缩文字。\n• GRID / FREE 与时间戳之间改为一个空格，保留边框内边距和鼠标横向间距。",
    "• Ground-grid popup width follows its timestamp text without horizontal text compression.\n• Uses one space between GRID / FREE and the timestamp, retaining padding and cursor clearance."),
   new ReleaseNotesEntry("1.0.63",
    "• 地面网格时间戳取消纵向偏移，保留与鼠标的横向间距。\n• 天空坐标弹窗文字略微加大，左右保留足够内边距。",
    "• Removes the vertical offset from ground-grid timestamps while retaining cursor clearance.\n• Slightly enlarges sky-coordinate text with comfortable side padding."),
  };
 }
}
