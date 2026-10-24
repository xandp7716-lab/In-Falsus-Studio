using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.Mono;
using HarmonyLib;
using UnityEngine;
using InFalsusStudio;
using InFalsusStudio.Core;
[BepInPlugin("local.infstudioplus.focuschecks","Focus checks","1.0.0")]
[BepInDependency("local.infalsusstudio.visualui")]
public sealed class FocusChecks:BaseUnityPlugin {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 StudioBootstrap boot;object ui;Type recovery;string output,error;int exceptions;Action job;bool done;
 readonly List<string> checks=new List<string>();
 static object Get(object o,string n){return o.GetType().GetField(n,F).GetValue(o);}static void Set(object o,string n,object v){o.GetType().GetField(n,F).SetValue(o,v);}static object Call(object o,string n,params object[] a){return o.GetType().GetMethod(n,F).Invoke(o,a);}
 void Check(bool ok,string text){if(!ok)throw new Exception(text+"; status="+Get(boot,"status"));checks.Add("PASS "+text);File.WriteAllLines(Path.Combine(output,"checks.txt"),checks.ToArray());}
 static bool Silent(StudioKeySounds __instance){__instance.Enabled=false;__instance.MasterVolume=0;__instance.StopAll();return false;}
 void Awake(){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-focus-checks");if(at<0)return;output=args[at+1];Directory.CreateDirectory(output);AudioListener.volume=0;var mute=new Harmony("local.focuschecks.earlymute");mute.Patch(typeof(StudioKeySounds).GetMethod("Tick",F),new HarmonyMethod(GetType().GetMethod("Silent",F)){priority=5000});Application.runInBackground=true;Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Exception){exceptions++;File.AppendAllText(Path.Combine(output,"exceptions.txt"),m+"\n"+s+"\n");}};StartCoroutine(Run());}
 void OnGUI(){if(job==null||Event.current.type!=EventType.Repaint)return;var next=job;job=null;try{next();}catch(Exception e){error=e.ToString();}done=true;}
 IEnumerator Gui(Action action){if(error!=null)yield break;done=false;job=action;for(int i=0;i<600&&!done;i++)yield return null;if(!done)error="GUI job timeout";}
 IEnumerator Frames(int n=3){for(int i=0;i<n;i++)yield return null;}
 void Alt(bool held){var gate=Get(boot,"editModifier");gate.GetType().GetField("<Held>k__BackingField",F).SetValue(gate,held);}
 void Tool(string name){Call(boot,"UseTool",Enum.Parse(Get(boot,"tool").GetType(),name));Set(ui,"notePopupOpen",false);}
 void Prepare(){
  Call(boot,"LoadDocument",SpcDocument.Parse("chart(120,4)\r\nskyarea(1000,40,100,20,40,100,20,0,0,2000)\r\n"));
  ((StudioTransport)Get(boot,"transport")).Pause();((StudioTransport)Get(boot,"transport")).Seek(0);
  Set(boot,"showReleaseNotes",false);Set(boot,"guiShowReleaseNotes",false);Set(boot,"showUi",true);Set(boot,"showNotes",true);Set(ui,"formal",false);Set(ui,"panel","");
  int id=boot.Events.Single(e=>e.Kind==EventKind.SkyArea).SourceId;Call(boot,"SetSelection",new[]{id},id);Tool("Select");GUIUtility.hotControl=0;Alt(true);
 }
 void FocusCycle(){Call(boot,"OnApplicationFocus",false);Call(boot,"OnApplicationFocus",true);}
 void Repair(Event e){if(recovery!=null){var previous=Event.current;try{Event.current=e;recovery.GetMethod("BeforeGui",F).Invoke(null,new object[]{boot});}finally{Event.current=previous;}}}
 void Scene(Event e){Repair(e);Call(boot,"HandleSceneEvent",e);}
 Event Mouse(EventType type,Vector2 at,EventModifiers flags=0){return new Event{type=type,button=0,mousePosition=at,modifiers=flags};}
 Vector2 Handle(NoteHandle kind){Call(boot,"BuildHandles");foreach(var p in (IEnumerable)Get(boot,"handles"))if((NoteHandle)Get(p,"Kind")==kind)return (Vector2)Get(p,"Gui");throw new Exception("No projected "+kind+" handle");}
 Vector2 Project(Vector3 point){var p=boot.ViewCamera.WorldToScreenPoint(point);return new Vector2(p.x,Screen.height-p.y);}
 Vector2 Floor(double time){return Project(StageSpace.LanePoint((StudioProfile)Get(boot,"Profile"),2,.5f,(float)Call(boot,"Depth",time)));}
 Vector2 Air(double time,double x){var p=(StudioProfile)Get(boot,"Profile");return Project(new Vector3(StageSpace.AirX(p,x),p.skyHeight+.019f,(float)Call(boot,"Depth",time)));}
 void Drag(bool orphan,bool command){
  Prepare();var point=Handle(NoteHandle.Body);string before=boot.History.Document.ToText();FocusCycle();Alt(true);if(orphan)GUIUtility.hotControl=987654;
  Scene(Mouse(EventType.MouseDown,point,command?EventModifiers.Command:EventModifiers.Alt));
  if(recovery==null){Check(!boot.History.InTransaction,(orphan?"orphan capture":"stale Win modifier")+" reproduces the old blocked handle");GUIUtility.hotControl=0;Call(boot,"CancelGesture");return;}
  Check(boot.History.InTransaction,(orphan?"orphan capture":"stale Win modifier")+" no longer blocks the handle");
  Scene(Mouse(EventType.MouseDrag,point+new Vector2(22,0),EventModifiers.Command));Scene(Mouse(EventType.MouseUp,point+new Vector2(22,0),EventModifiers.Command));
  Check(!boot.History.InTransaction&&boot.History.UndoCount==1&&boot.History.Document.ToText()!=before,"recovered drag commits exactly one undo step");
  Call(boot,"Undo",false);Check(boot.History.Document.ToText()==before,"undo restores the recovered drag byte-for-byte");
 }
 void Tap(bool orphan,bool command){
  Prepare();Tool("Tap");FocusCycle();Alt(false);if(orphan)GUIUtility.hotControl=987655;var e=Mouse(EventType.MouseDown,Floor(1500),command?EventModifiers.Command:0);Scene(e);
  int taps=boot.Events.Count(n=>n.Kind==EventKind.Tap);
  Check(recovery==null?taps==0:taps==1,(orphan?"orphan capture":"stale Win modifier")+(recovery==null?" reproduces the old blocked point placement":" no longer blocks Tap point placement"));GUIUtility.hotControl=0;
 }
 void AirPlacement(string tool,EventKind kind){
  Prepare();Tool(tool);FocusCycle();Alt(false);GUIUtility.hotControl=987656;
  Scene(Mouse(EventType.MouseDown,Floor(1500),EventModifiers.Command));
  var draft=Get(boot,"pointPlacement");double start=(double)draft.GetType().GetProperty("StartMs",F).GetValue(draft,null);
  Scene(Mouse(EventType.MouseDown,Air(start,.5),EventModifiers.Command));
  if(kind==EventKind.SkyArea){Scene(Mouse(EventType.MouseDown,Floor(2500),EventModifiers.Command));double end=(double)draft.GetType().GetProperty("EndMs",F).GetValue(draft,null);Scene(Mouse(EventType.MouseDown,Air(end,.55),EventModifiers.Command));}
  Check(boot.Events.Count(n=>n.Kind==kind)==(kind==EventKind.SkyArea?2:1)&&boot.History.UndoCount==1,tool+" point placement completes after focus recovery in one undo step");
 }
 IEnumerator Run(){
  yield return new WaitForSeconds(2);
  try{
   boot=FindFirstObjectByType<StudioBootstrap>();var mod=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="InfStudioVisualUI");ui=mod.GetType("InFalsusStudio.NativeEditorUI.NativeEditorUIPlugin").GetField("I",F).GetValue(null);recovery=mod.GetType("InFalsusStudio.Performance.WindowFocusRecovery");Check(ui!=null&&(bool)Get(ui,"ready"),"integrated UI initializes");
   var h=new Harmony("local.focuschecks");h.Patch(typeof(StudioKeySounds).GetMethod("Tick",F),new HarmonyMethod(GetType().GetMethod("Silent",F)){priority=5000});Silent((StudioKeySounds)Get(boot,"keySounds"));((StudioTransport)Get(boot,"transport")).Volume=0;Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
  }catch(Exception e){error=e.ToString();}
  yield return Gui(()=>Prepare());yield return Frames(10);
  yield return Gui(()=>Drag(true,false));yield return Frames();
  yield return Gui(()=>Drag(false,true));yield return Frames();
  yield return Gui(()=>Tap(true,false));yield return Frames();
  yield return Gui(()=>Tap(false,true));yield return Frames();
  if(recovery!=null){
   yield return Gui(()=>AirPlacement("Sky",EventKind.SkyArea));yield return Frames();
   yield return Gui(()=>AirPlacement("FlickLeft",EventKind.Flick));yield return Frames();
   yield return Gui(()=>{
    Prepare();string before=boot.History.Document.ToText();var point=Handle(NoteHandle.Body);Scene(Mouse(EventType.MouseDown,point));Scene(Mouse(EventType.MouseDrag,point+new Vector2(25,0)));FocusCycle();
    Check(!boot.History.InTransaction&&boot.History.UndoCount==0&&boot.History.Document.ToText()==before,"focus loss cancels an unfinished drag without source changes or undo entries");
   });yield return Frames(5);
   yield return Gui(()=>Check(!boot.History.InTransaction&&boot.History.UndoCount==0,"deferred drag preview stays canceled after focus returns"));
   yield return Gui(()=>{
    var m=recovery.GetMethod("Reconcile",F);var e=new Event{type=EventType.MouseDown,modifiers=EventModifiers.Command|EventModifiers.CapsLock|EventModifiers.Numeric};
    var result=(EventModifiers)m.Invoke(null,new object[]{e,true,true,true,false});
    Check(result==(EventModifiers.Control|EventModifiers.Alt|EventModifiers.Shift|EventModifiers.CapsLock|EventModifiers.Numeric),"modifier recovery preserves real held keys and lock/numeric flags");
    e=new Event{type=EventType.KeyDown,keyCode=KeyCode.LeftAlt};result=(EventModifiers)m.Invoke(null,new object[]{e,false,false,false,false});Check((result&EventModifiers.Alt)!=0,"fresh Alt key-down survives asynchronous snapshot delay");
    e=new Event{type=EventType.KeyUp,keyCode=KeyCode.LeftWindows,modifiers=EventModifiers.Command};result=(EventModifiers)m.Invoke(null,new object[]{e,false,false,false,true});Check((result&EventModifiers.Command)==0,"Win key-up clears the command modifier");
    Prepare();FocusCycle();GUIUtility.hotControl=987657;
   });yield return Frames(3);
   yield return Gui(()=>Check(GUIUtility.hotControl==0,"the installed OnGUI hook releases an orphan capture automatically"));
   for(int i=0;i<4;i++){yield return Gui(()=>Tap(true,true));yield return Frames();}
  }
  File.WriteAllText(Path.Combine(output,"result.txt"),"candidate="+(recovery!=null)+" checks="+checks.Count+" exceptions="+exceptions+" error="+error);Application.Quit();
 }
}
