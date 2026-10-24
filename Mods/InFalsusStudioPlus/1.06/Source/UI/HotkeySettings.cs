using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
namespace InFalsusStudio.NativeEditorUI {
 public sealed partial class NativeEditorUIPlugin {
  [Serializable] sealed class HotkeyFile {public int version=1;public string bindings;}
  readonly StudioHotkeys hotkeys=new StudioHotkeys();ConfigEntry<string> hotkeyConfig;string capturingHotkey,hotkeyMessage="点击按键录入新组合，修改后自动保存。";bool hotkeyDispatching;float hotkeyPageHeight=2000;
  void InitHotkeys(){
   hotkeyConfig=Config.Bind("Hotkeys","Bindings","","Custom editor shortcuts. Edited from Settings > Operations > Hotkeys; no chart data.");
   try{LoadHotkeys(hotkeyConfig.Value);}catch(Exception e){hotkeys.Reset();hotkeyMessage="热键配置无效，已使用默认按键。";Logger.LogWarning("Hotkeys ignored: "+e.Message);}
   harmony.Patch(Method("OnGUI"),new HarmonyMethod(typeof(HotkeyHooks).GetMethod("Capture",F)){priority=Priority.First+1000});
  }
  void LoadHotkeys(string json){
   if(string.IsNullOrEmpty(json))return;if(json.Length>32768)throw new InvalidDataException("Hotkey settings too large");var file=JsonUtility.FromJson<HotkeyFile>(json);
   if(file==null||file.version!=1||string.IsNullOrEmpty(file.bindings))throw new InvalidDataException("Invalid hotkey settings");var entries=file.bindings.Split('|');if(entries.Length>64)throw new InvalidDataException("Too many hotkeys");var next=hotkeys.Snapshot();var seen=new HashSet<string>();
   foreach(var row in entries){var e=row.Split(':');int key,flags;if(e.Length!=3||!seen.Add(e[0])||!int.TryParse(e[1],out key)||!int.TryParse(e[2],out flags)||flags<0||flags>7)throw new InvalidDataException("Invalid hotkey entry");if(!next.ContainsKey(e[0]))continue;next[e[0]]=new HotkeyChord((KeyCode)key,(flags&1)!=0,(flags&2)!=0,(flags&4)!=0);}
   var previous=hotkeys.Snapshot();hotkeys.Restore(next);string error;if(hotkeys.DefaultsConflict(out error)){hotkeys.Restore(previous);throw new InvalidDataException(error);}
  }
  string SerializeHotkeys(){var payload=string.Join("|",StudioHotkeys.Actions.Select(a=>{var k=hotkeys[a.Id];return a.Id+":"+(int)k.Key+":"+((k.Ctrl?1:0)|(k.Alt?2:0)|(k.Shift?4:0));}).ToArray());var json=JsonUtility.ToJson(new HotkeyFile{bindings=payload});if(JsonUtility.FromJson<HotkeyFile>(json).bindings!=payload)throw new InvalidDataException("Hotkey serialization failed");return json;}
  static string PutHotkeys(string text,string value){
   string nl=text.Contains("\r\n")?"\r\n":"\n",line="Bindings = "+value;
   var section=Regex.Match(text,@"(?ms)^\[Hotkeys\][^\r\n]*(?:\r?\n|\z).*?(?=^\[|\z)");
   if(!section.Success)return text+(text.Length>0&&!text.EndsWith("\n",StringComparison.Ordinal)?nl:"")+nl+"[Hotkeys]"+nl+line+nl;
   string body=section.Value;var key=Regex.Match(body,@"(?m)^[ \t]*Bindings[ \t]*=[^\r\n]*");body=key.Success?body.Substring(0,key.Index)+line+body.Substring(key.Index+key.Length):body+(body.EndsWith("\n",StringComparison.Ordinal)?"":nl)+line+nl;
   return text.Substring(0,section.Index)+body+text.Substring(section.Index+section.Length);
  }
  bool PersistHotkeys(Dictionary<string,HotkeyChord> previous){
   string temp=null;try{
    string value=SerializeHotkeys(),path=Config.ConfigFilePath,serialized;bool auto=Config.SaveOnConfigSet;string old=hotkeyConfig.Value;Config.SaveOnConfigSet=false;
    try{hotkeyConfig.Value=value;serialized=hotkeyConfig.GetSerializedValue();}finally{hotkeyConfig.Value=old;Config.SaveOnConfigSet=auto;}
    byte[] bytes=File.Exists(path)?File.ReadAllBytes(path):new byte[0];bool bom=bytes.Length>=3&&bytes[0]==239&&bytes[1]==187&&bytes[2]==191;
    string text=bytes.Length==0?"":File.ReadAllText(path);temp=path+".hotkeys-"+Guid.NewGuid().ToString("N")+".tmp";File.WriteAllText(temp,PutHotkeys(text,serialized),new UTF8Encoding(bom));
    if(File.Exists(path))File.Replace(temp,path,path+".hotkeys.bak",true);else File.Move(temp,path);temp=null;
    Config.SaveOnConfigSet=false;try{hotkeyConfig.Value=value;}finally{Config.SaveOnConfigSet=auto;}
    hotkeyMessage="热键已保存。";return true;
   }catch(Exception e){hotkeys.Restore(previous);hotkeyMessage="保存失败，保留原热键："+e.Message;Logger.LogWarning(hotkeyMessage);return false;}
   finally{if(temp!=null&&File.Exists(temp))try{File.Delete(temp);}catch(Exception e){Logger.LogWarning(e.Message);}}
  }
  void AssignHotkey(string id,HotkeyChord chord){var old=hotkeys.Snapshot();string error;if(!hotkeys.Assign(id,chord,out error)){hotkeyMessage=error;return;}PersistHotkeys(old);}
  void ResetHotkeys(){var old=hotkeys.Snapshot();hotkeys.Reset();PersistHotkeys(old);capturingHotkey=null;}
  string HotkeyLabel(string id){var a=StudioHotkeys.Actions.First(x=>x.Id==id);var k=hotkeys[id];return k.ToString()+(k.Equals(a.Default)&&a.Aliases.Length>0?" / "+string.Join(" / ",a.Aliases.Select(x=>x.ToString()).ToArray()):"");}
  void StartHotkeyCapture(string id){Queue(delegate{if(!(bool)Call(boot,"CommitAllInputFieldsNow"))return;ReleasePopupFocus();GUI.FocusControl(null);capturingHotkey=id;hotkeyMessage="按下新组合键；Esc 取消录入。";});}
  bool CaptureHotkey(Event e){
   if(capturingHotkey==null)return false;if(panel!="设置"||settingsTab!="热键"){capturingHotkey=null;return false;}
   bool mouse=HotkeyChord.MouseEvent(e);if(!mouse&&e.type!=EventType.KeyDown&&e.type!=EventType.KeyUp)return false;
   if(e.type==EventType.KeyDown||e.type==EventType.MouseDown){if(!mouse&&e.keyCode==KeyCode.Escape){capturingHotkey=null;hotkeyMessage="已取消录入。";}
    else if(mouse||!HotkeyChord.Modifier(e.keyCode)){string id=capturingHotkey;var chord=HotkeyChord.From(e);capturingHotkey=null;Queue(delegate{AssignHotkey(id,chord);});}}
   e.Use();return true;
  }
  static bool ManagedNativeKey(HotkeyChord k){return StudioHotkeys.Native(k)||k.Key==KeyCode.Space||k.Key==KeyCode.F1||k.Key==KeyCode.M||k.Key==KeyCode.Q||k.Key==KeyCode.Delete||k.Key==KeyCode.Backspace||(k.Key>=KeyCode.Alpha1&&k.Key<=KeyCode.Alpha5)||(k.Ctrl&&new[]{KeyCode.S,KeyCode.O,KeyCode.Z,KeyCode.Y,KeyCode.A,KeyCode.C,KeyCode.X,KeyCode.V,KeyCode.D}.Contains(k.Key));}
  bool RouteHotkey(Event e,bool textFocused){
   if(hotkeyDispatching)return false;if(CaptureHotkey(e))return true;bool mouse=HotkeyChord.MouseEvent(e);if(!mouse&&e.type!=EventType.KeyDown&&e.type!=EventType.KeyUp)return false;bool release=e.type==EventType.KeyUp||e.type==EventType.MouseUp;
   var chord=HotkeyChord.From(e);HotkeyAction action=null;
   // A modifier can be released before the preview key. The main key release
   // must still stop held preview and restore the starting position.
   if(release){if(spacePreview.Active&&chord.Key==hotkeys["preview"].Key)action=StudioHotkeys.Actions.First(x=>x.Id=="preview");else{if(spacePreview.Active&&chord.Key==KeyCode.Space){e.Use();return true;}return false;}}
   else action=hotkeys.Find(chord);
   if(textFocused&&!release&&(action==null||(action.Id!="save"&&action.Id!="saveAs"))){if(chord.Ctrl&&chord.Key==KeyCode.S){e.Use();return true;}return false;}
   if(action!=null){
    var canonical=action.Default.Event(release?EventType.KeyUp:EventType.KeyDown);hotkeyDispatching=true;
    try{Call(boot,"HandleKeyboard",canonical,textFocused);}finally{hotkeyDispatching=false;e.Use();}return true;
   }
   // Once rebound, the old physical binding must not fall through to the
   // native handler and trigger its original hard-coded action as well.
   if(e.type==EventType.KeyDown&&ManagedNativeKey(chord)){e.Use();return true;}return false;
  }
  void DrawHotkeySettings(){
   MatchedLink("‹ 返回操作设置",delegate{Queue(delegate{settingsTab="操作";capturingHotkey=null;panelScroll=Vector2.zero;});});
   Rect info=ContentRow(96);UiParagraph(new Rect(0,info.y+8*scale,405*scale,88*scale),hotkeyMessage,14,Css("#a6c3d1"));
   string section=null;foreach(var a in StudioHotkeys.Actions){if(section!=a.Section){section=a.Section;MatchedSection(section);}Rect row=ContentRow(76);RowLine(row);
    UiText(new Rect(0,row.y,405*scale,32*scale),a.Name,16,Css("#a6c3d1"));bool capturing=capturingHotkey==a.Id;
    if(FlatButton(new Rect(0,row.y+34*scale,315*scale,34*scale),capturing?"请按键…":HotkeyLabel(a.Id),15,capturing)){string id=a.Id;StartHotkeyCapture(id);}
    if(FlatButton(new Rect(326*scale,row.y+34*scale,79*scale,34*scale),"默认",14)){string id=a.Id;var key=a.Default;Queue(delegate{capturingHotkey=null;AssignHotkey(id,key);});}
   }
   MatchedSection("固定交互");Rect fixedRow=ContentRow(126);UiParagraph(new Rect(0,fixedRow.y+8*scale,405*scale,118*scale),"Ctrl 多选 · Alt 把手 · 方向键微调\nTab 切换重叠候选 · Esc 取消\nEnter 确认输入 · F11 恢复窗口\n文字输入框保留常规文字编辑按键。",13,Css("#7c9dad"));
   MatchedLink("恢复全部默认热键",delegate{Queue(ResetHotkeys);});hotkeyPageHeight=contentY/scale+12;
  }
  static class HotkeyHooks {
   static bool Capture(){if(I==null||!I.ready)return true;var e=Event.current;if(I.CaptureHotkey(e))return false;
    if(I.boot!=null&&HotkeyChord.MouseEvent(e)){
     bool focused=I.PopupOwnsTextFocus||Get(I.boot,"hudField").ToString()!="None"||GUI.GetNameOfFocusedControl().StartsWith("field",StringComparison.Ordinal);
     if(I.RouteHotkey(e,focused))return false;
    }return true;}
  }
 }
}
