using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using InFalsusStudio.Core;
namespace InFalsusStudio.NativeEditorUI {
 public sealed partial class NativeEditorUIPlugin {
  float quickToolsContentHeight=1040;bool quickSkySelection;int quickSkyCount;
  void InitQuickSkyTools(){harmony.Patch(Method("DrawAuthoringTools"),transpiler:new HarmonyMethod(typeof(NativeEditorUIPlugin).GetMethod("MoveLegacySkyTools",F)){priority=Priority.First});}
  static bool QuickSkyToolsAvailable(){return I!=null&&I.ready;}
  static IEnumerable<CodeInstruction> MoveLegacySkyTools(IEnumerable<CodeInstruction> input,ILGenerator generator){
   var code=input.ToList();int equal=code.FindIndex(c=>c.opcode==OpCodes.Ldstr&&(string)c.operand=="1. 等分切分（仅单条 SkyArea）"),repair=code.FindIndex(c=>c.opcode==OpCodes.Ldstr&&(string)c.operand=="3. 坐标修复");
   Func<CodeInstruction,string,string,bool> call=(c,type,name)=>{var m=c.operand as MethodInfo;return m!=null&&m.DeclaringType.FullName==type&&m.Name==name;};
   if(equal<0||repair<=equal)throw new MissingMethodException("Sky split UI anchors changed.");
   int start=code.FindLastIndex(equal,c=>call(c,"UnityEngine.GUILayout","Space"))-1,end=code.FindLastIndex(repair,c=>call(c,"UnityEngine.GUILayout","Space"))-1;
   int save=code.FindIndex(start,end-start,c=>call(c,"UnityEngine.GUI","get_enabled"));int restore=code.FindLastIndex(end-1,end-start,c=>call(c,"UnityEngine.GUI","set_enabled"));
   bool stored=save>=0&&code[save+1].opcode.Name.StartsWith("stloc"),stacked=save>=0&&code[save+1].opcode==OpCodes.Dup;
   if(start<0||end<=start||save<start||restore<start||!stored&&!stacked||!code[restore-1].opcode.Name.StartsWith("ldloc"))throw new MissingMethodException("Sky split GUI state anchors changed.");
   // Keep the original body for fallback UI. When the integrated quick panel
   // is active, only bypass the moved controls; preserve the later repair
   // controls' operationEnabled value and the outer GUI.enabled state. The
   // shipped optimized core keeps operationEnabled on the evaluation stack.
   var resume=generator.DefineLabel();code[end].labels.Add(resume);var original=generator.DefineLabel();
   var jump=new List<CodeInstruction>{new CodeInstruction(OpCodes.Call,typeof(NativeEditorUIPlugin).GetMethod("QuickSkyToolsAvailable",F)),new CodeInstruction(OpCodes.Brfalse,original),new CodeInstruction(code[save].opcode,code[save].operand)};
   if(stored)jump.Add(new CodeInstruction(code[save+1].opcode,code[save+1].operand));
   jump.Add(new CodeInstruction(code[restore-1].opcode,code[restore-1].operand));jump.Add(new CodeInstruction(code[restore].opcode,code[restore].operand));jump.Add(new CodeInstruction(OpCodes.Br,resume));
   jump[0].labels.AddRange(code[start].labels);code[start].labels.Clear();code[start].labels.Add(original);code.InsertRange(start,jump);return code;
  }
  void MatchedQuickSkyTools(){
   if(Event.current.type==EventType.Layout){var ids=Val<HashSet<int>>(boot,"selection");var selected=boot.Events.Where(e=>ids.Contains(e.SourceId)).ToArray();quickSkyCount=selected.Length;quickSkySelection=selected.Length>0&&selected.Length==ids.Count&&selected.All(e=>e.Kind==EventKind.SkyArea);}
   bool previous=GUI.enabled,available=previous&&boot.HasDocument&&!Transport.Playing&&!Val<bool>(boot,"guiWorkflowActive");
   try{
    MatchedSection(RainL("等分切分（单条 SkyArea）","Equal split (one SkyArea)"));
    GUI.enabled=available;MatchedField(RainL("分段数量 N","Piece count N"),"field_split_count",Val<int>(boot,"splitCount").ToString(),value=>{int n;if(!int.TryParse(value,out n)||n<2||n>SkySplitOperations.MaxPieces)throw new ArgumentException("N: 2..4096");Set(boot,"splitCount",n);});
    GUI.enabled=available&&quickSkySelection&&quickSkyCount==1;
    QuickSkyButtons(RainL("切成 N 条 SkyArea","Split into N SkyAreas"),false,RainL("转成 N 个 Flick","Convert to N Flicks"));
    GUI.enabled=previous;MatchedSection(RainL("按当前网格切分（多条 SkyArea）","Current grid split (SkyAreas)"));
    var info=ContentRow(57);UiParagraph(new Rect(0,info.y+8*scale,405*scale,49*scale),RainL("当前细分：1/","Subdivision: 1/")+Prop(boot,"Division")+RainL(" 拍；两端不足一格也保留。"," beat; partial end intervals are retained."),13,Css("#8caab8"));
    GUI.enabled=available&&quickSkySelection;QuickSkyButtons(RainL("按网格切 SkyArea","Grid → SkyAreas"),true,RainL("按网格转 Flick","Grid → Flicks"));
    GUI.enabled=available;var direction=ContentRow(58);if(FlatButton(new Rect(0,direction.y+8*scale,405*scale,42*scale),RainL("生成 Flick 方向：","Flick direction: ")+(Val<int>(boot,"splitFlickDirection")==16?RainL("向左","Left"):RainL("向右","Right")),15))Queue(delegate{Set(boot,"splitFlickDirection",Val<int>(boot,"splitFlickDirection")==16?4:16);});
    GUI.enabled=previous;var help=ContentRow(145);UiParagraph(new Rect(0,help.y+8*scale,405*scale,137*scale),RainL("分别采样原来的左右曲线，坐标最多 128 份；小 SkyArea 两侧为 Linear，不指定组。Flick 在每段起点采样，不额外生成尾点。","Both edges are sampled within 128 coordinate parts. Sky pieces use Linear edges and omit group assignment. Flicks sample interval starts; no extra tail Flick."),13,Css("#789eae"));
   }finally{GUI.enabled=previous;}
  }
  void QuickSkyButtons(string sky,bool grid,string flick){
   var row=ContentRow(58);if(FlatButton(new Rect(0,row.y+8*scale,197*scale,42*scale),sky,14))Command("SplitSelectedSky",grid,false);
   if(FlatButton(new Rect(208*scale,row.y+8*scale,197*scale,42*scale),flick,14))Command("SplitSelectedSky",grid,true);
  }
 }
}
