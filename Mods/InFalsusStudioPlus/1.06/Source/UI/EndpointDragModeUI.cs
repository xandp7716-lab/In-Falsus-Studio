using UnityEngine;
using InFalsusStudio.Performance;
namespace InFalsusStudio.NativeEditorUI {
 public sealed partial class NativeEditorUIPlugin {
  void DrawEndpointDragMode(){
   if(formal||!boot.HasDocument||!Val<bool>(boot,"showNotes")||(!EndpointDragMode.Notice(boot)&&!(bool)Prop(boot,"EditHeld")))return;
   string text=(EndpointDragMode.TimeOnly(boot)?"时间拖动 · ":"位置拖动 · ")+HotkeyLabel("endpoint")+" 切换";
   Rect song=Val<Rect>(boot,"hudSongRect");
   float width=UiFontRaster.Measure(T(text),CNFace,FontPixels(15))+16*scale;
   var area=new Rect(Mathf.Max(viewport.xMin,song.xMax-width),song.yMax+5*scale,width,26*scale);
   Fill(area,Css("#091c25ef"));
   UiText(area,text,15,Css("#d3edf0"),CNFace,1);
  }
 }
}
