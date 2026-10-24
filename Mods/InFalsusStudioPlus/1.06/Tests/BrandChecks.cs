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
[BepInPlugin("local.infstudioplus.brandchecks","Title raster checks","1.0.0")]
[BepInDependency("local.infalsusstudio.visualui")]
public sealed class BrandChecks:BaseUnityPlugin {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static BrandChecks I;StudioBootstrap boot;object ui;Assembly mod;string output,error;int exceptions;bool hideTitle;
 Vector2 windowOrigin,titleOrigin;readonly List<string> checks=new List<string>();
 static object Get(object o,string n){return o.GetType().GetField(n,F).GetValue(o);}static void Set(object o,string n,object v){o.GetType().GetField(n,F).SetValue(o,v);}static object Call(object o,string n,params object[] a){return o.GetType().GetMethod(n,F).Invoke(o,a);}
 void Check(bool ok,string text){if(!ok)throw new Exception(text);checks.Add("PASS "+text);File.WriteAllLines(Path.Combine(output,"checks.txt"),checks.ToArray());}
 static bool Silent(StudioKeySounds __instance){__instance.Enabled=false;__instance.MasterVolume=0;__instance.StopAll();return false;}
 static bool BeforeTitle(){return I==null||!I.hideTitle;}
 static void AfterTitle(Rect __0){
  if(I==null||I.hideTitle||Event.current.type!=EventType.Repaint)return;
  object r=Get(I.ui,"aboutTitleRaster");if(r==null)return;
  Vector2 local=(Vector2)Call(I.ui,"PixelOrigin",__0.center.x-(int)Get(r,"Advance")*.5f-(int)Get(r,"Padding"),__0.center.y-((int)Get(r,"Top")+(int)Get(r,"Bottom"))*.5f);
  I.titleOrigin=GUIUtility.GUIToScreenPoint(local)-I.windowOrigin;
 }
 void OnGUI(){windowOrigin=GUIUtility.GUIToScreenPoint(Vector2.zero);}
 void Awake(){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-brand-checks");if(at<0)return;output=args[at+1];Directory.CreateDirectory(output);I=this;AudioListener.volume=0;var mute=new Harmony("local.brandchecks.earlymute");mute.Patch(typeof(StudioKeySounds).GetMethod("Tick",F),new HarmonyMethod(GetType().GetMethod("Silent",F)){priority=5000});Application.runInBackground=true;Application.logMessageReceived+=(m,s,t)=>{if(t==LogType.Exception){exceptions++;File.AppendAllText(Path.Combine(output,"exceptions.txt"),m+"\n"+s+"\n");}};StartCoroutine(Run());}
 IEnumerator Frames(int n=8){for(int i=0;i<n;i++)yield return null;}
 IEnumerator Queue(Action action){bool done=false;Call(ui,"Queue",new Action(()=>{try{action();}catch(Exception e){error=e.ToString();}done=true;}));for(int i=0;i<600&&!done;i++)yield return null;if(!done)error="Queue timeout";}
 void CheckBottom(Color32[] shown,Color32[] hidden,int screenWidth,int screenHeight){
  var r=Get(ui,"aboutTitleRaster");int width=(int)Get(r,"Width"),height=(int)Get(r,"Height"),bottom=(int)Get(r,"Bottom");byte[] mask=(byte[])Get(r,"Pixels");
  int bestTotal=0,bestBottom=0,expected=0,expectedBottom=0,bestX=0,bestY=0;
  for(int row=0;row<bottom;row++)for(int x=0;x<width;x++)if(mask[((height-1-row)*width+x)*4+3]>=96){expected++;if(row>=bottom-2)expectedBottom++;}
  for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++){
   int total=0,low=0;
   for(int row=0;row<bottom;row++)for(int x=0;x<width;x++){
    if(mask[((height-1-row)*width+x)*4+3]<96)continue;
    int sx=Mathf.RoundToInt(titleOrigin.x)+x+dx,sy=Mathf.RoundToInt(titleOrigin.y)+row+dy;
    if(sx<0||sy<0||sx>=screenWidth||sy>=screenHeight)continue;int at=(screenHeight-1-sy)*screenWidth+sx;
    var a=shown[at];var b=hidden[at];if(Math.Max(Math.Abs(a.r-b.r),Math.Max(Math.Abs(a.g-b.g),Math.Abs(a.b-b.b)))>8){total++;if(row>=bottom-2)low++;}
   }
   if(total>bestTotal){bestTotal=total;bestBottom=low;bestX=dx;bestY=dy;}
  }
  File.WriteAllText(Path.Combine(output,"title-pixels.txt"),"origin="+titleOrigin+" size="+width+"x"+height+" bottom="+bottom+" expected="+expected+" shown="+bestTotal+" bottom_expected="+expectedBottom+" bottom_shown="+bestBottom+" offset="+bestX+","+bestY);
  Check(expected>0&&bestTotal>=expected*.98,"whole title strong-ink pixels are visible in actual settings panel");
  Check(expectedBottom>0&&bestBottom>=expectedBottom*.98,"bottom two ink rows are present in the actual screenshot");
 }
 IEnumerator Run(){
  yield return new WaitForSeconds(2);
  try{
   boot=FindFirstObjectByType<StudioBootstrap>();mod=AppDomain.CurrentDomain.GetAssemblies().Single(a=>a.GetName().Name=="InfStudioVisualUI");ui=mod.GetType("InFalsusStudio.NativeEditorUI.NativeEditorUIPlugin").GetField("I",F).GetValue(null);Check(ui!=null&&(bool)Get(ui,"ready"),"integrated UI initializes");
   var h=new Harmony("local.brandchecks");h.Patch(typeof(StudioKeySounds).GetMethod("Tick",F),new HarmonyMethod(GetType().GetMethod("Silent",F)){priority=5000});Silent((StudioKeySounds)Get(boot,"keySounds"));((StudioTransport)Get(boot,"transport")).Volume=0;
   h.Patch(ui.GetType().GetMethod("DrawAboutTitle",F),new HarmonyMethod(GetType().GetMethod("BeforeTitle",F)),new HarmonyMethod(GetType().GetMethod("AfterTitle",F)));
   Screen.SetResolution(1920,1080,FullScreenMode.Windowed);
  }catch(Exception e){error=e.ToString();}
  if(error==null){
   yield return Queue(()=>{Call(boot,"LoadDocument",SpcDocument.Parse("chart(120,4)\n"));Set(boot,"showReleaseNotes",false);Set(boot,"guiShowReleaseNotes",false);Set(ui,"formal",false);Set(ui,"panel","设置");Set(ui,"settingsTab","操作");Set(ui,"panelScroll",new Vector2(0,10000));});yield return Frames(15);
   try{
        var mascot=(Texture2D)Get(ui,"aboutMascot");Check(mascot!=null,"new chibi line-art resource loads in the Player");var strokes=mascot.GetPixels32();int ink=strokes.Count(p=>p.a>0);Check(ink>100&&ink<strokes.Length/3,"line-art interiors and surroundings remain transparent");Check(strokes.Where(p=>p.a>0).All(p=>p.r==255&&p.g==255&&p.b==255),"stroke mask supports the single bright-blue display tint");    var r=Get(ui,"aboutTitleRaster");Check(r!=null&&Get(ui,"aboutTitleTexture")!=null,"complete-line title raster and texture created");
    Check((int)Get(r,"Top")>=2&&(int)Get(r,"Height")-(int)Get(r,"Bottom")>=4,"title canvas retains padding below all ink");
    var rasterType=mod.GetType("InFalsusStudio.NativeEditorUI.UiFontRaster");
    foreach(int size in new[]{15,18,21,26}){var desc=rasterType.GetMethod("WholeLine",F).Invoke(null,new object[]{"Agjpqy_1.06","Bai Jamjuree Medium",size});Check((int)Get(desc,"Height")-(int)Get(desc,"Bottom")>=4,"descender canvas retains bottom margin at "+size+" px");}
   }catch(Exception e){error=e.ToString();}
   if(error==null){
    yield return new WaitForEndOfFrame();var shot=ScreenCapture.CaptureScreenshotAsTexture();var shown=shot.GetPixels32();int w=shot.width,h=shot.height;File.WriteAllBytes(Path.Combine(output,"settings-about.png"),shot.EncodeToPNG());Destroy(shot);
    hideTitle=true;yield return Frames(3);yield return new WaitForEndOfFrame();shot=ScreenCapture.CaptureScreenshotAsTexture();var hidden=shot.GetPixels32();Destroy(shot);hideTitle=false;
    try{CheckBottom(shown,hidden,w,h);}catch(Exception e){error=e.ToString();}
   }
  }
  File.WriteAllText(Path.Combine(output,"result.txt"),"checks="+checks.Count+" exceptions="+exceptions+" error="+error);Application.Quit();
 }
}
