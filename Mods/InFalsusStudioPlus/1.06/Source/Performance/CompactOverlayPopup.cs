using System;
using UnityEngine;
namespace InFalsusStudio.Performance {
 internal static class CompactOverlayPopup {
  // Reduce the original area by 25%, preserving its aspect ratio.
  internal const float SizeFactor=.8660254f,Width=138f*SizeFactor,Height=39f*SizeFactor;
  static readonly Color ValueInk=new Color(224/255f,246/255f,1,1);
  static readonly Color AxisInk=new Color(108/255f,212/255f,1,1);
  static readonly Color EdgeInk=new Color(124/255f,224/255f,1,1);
  static GUIStyle axis,value,suffix,skyAxis,skyValue,skySuffix;static GUISkin lastSkin;static int lastScale;
  static GUIStyle TextStyle(StudioBootstrap owner,float size,float k,Color ink,bool bold){
   var style=new GUIStyle(OverlayAnnotationLayout.Style(owner,false,size,TextAnchor.MiddleLeft));
   style.fontSize=Mathf.Max(1,Mathf.RoundToInt(size*k));style.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;
   style.alignment=TextAnchor.MiddleLeft;style.wordWrap=false;style.clipping=TextClipping.Overflow;
   style.padding=new RectOffset();style.margin=new RectOffset();style.contentOffset=Vector2.zero;
   style.fixedWidth=style.fixedHeight=0;style.normal.textColor=ink;return style;
  }
  static void EnsureStyles(StudioBootstrap owner,float k){
   int key=Mathf.RoundToInt(k*1000);if(axis!=null&&lastSkin==GUI.skin&&lastScale==key)return;
   lastSkin=GUI.skin;lastScale=key;axis=TextStyle(owner,13,k,AxisInk,true);
   value=TextStyle(owner,17,k,ValueInk,true);suffix=TextStyle(owner,12,k,ValueInk,false);
   skyAxis=TextStyle(owner,14,k,AxisInk,true);skyValue=TextStyle(owner,18,k,ValueInk,true);skySuffix=TextStyle(owner,13,k,ValueInk,false);
  }
  static void Fill(Rect rect,Color ink){Color previous=GUI.color;GUI.color=previous*ink;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=previous;}
  static void Bracket(Rect rect,float k){
   float stroke=Mathf.Max(.75f,k),cap=4*k;
   Fill(rect,new Color(11/255f,27/255f,38/255f,.91f));
   Fill(new Rect(rect.x,rect.y,stroke,rect.height),EdgeInk);Fill(new Rect(rect.xMax-stroke,rect.y,stroke,rect.height),EdgeInk);
   Fill(new Rect(rect.x,rect.y,cap,stroke),EdgeInk);Fill(new Rect(rect.x,rect.yMax-stroke,cap,stroke),EdgeInk);
   Fill(new Rect(rect.xMax-cap,rect.y,cap,stroke),EdgeInk);Fill(new Rect(rect.xMax-cap,rect.yMax-stroke,cap,stroke),EdgeInk);
  }
  static void Label(Rect rect,string first,string number,string ending,float k,bool grid=false){
   GUIStyle axisStyle=grid?axis:skyAxis,valueStyle=grid?value:skyValue,suffixStyle=grid?suffix:skySuffix;
   var a=new GUIContent(first);var b=new GUIContent(number);var c=new GUIContent(ending);
   float aw=axisStyle.CalcSize(a).x,bw=valueStyle.CalcSize(b).x,cw=suffixStyle.CalcSize(c).x,gap=3*k;
   float firstGap=grid?axisStyle.CalcSize(new GUIContent(" ")).x:gap,total=aw+bw+cw+firstGap+gap;
   float available=rect.width-16*k,fit=grid?1:Mathf.Min(1,available/Mathf.Max(1,total));
   var matrix=GUI.matrix;var pivot=new Vector2(rect.center.x,rect.y);
   try{
    if(!grid)GUIUtility.ScaleAroundPivot(new Vector2(fit,1),pivot);
    float x=rect.center.x-total*.5f;
    GUI.Label(new Rect(x,rect.y,aw,rect.height),a,axisStyle);x+=aw+firstGap;
    GUI.Label(new Rect(x,rect.y,bw,rect.height),b,valueStyle);x+=bw+gap;
    GUI.Label(new Rect(x,rect.y,cw,rect.height),c,suffixStyle);
   }finally{GUI.matrix=matrix;}
  }
  static float GridWidth(string first,string number,float k){
   return Mathf.Ceil(axis.CalcSize(new GUIContent(first)).x+axis.CalcSize(new GUIContent(" ")).x+
    value.CalcSize(new GUIContent(number)).x+3*k+suffix.CalcSize(new GUIContent("ms")).x+10*k);
  }
  static Rect GridRect(StudioBootstrap owner,Vector2 pointer,float k,float width){
   Rect bounds=OverlayAnnotationLayout.Bounds(owner);float height=Height*k,gap=Mathf.Max(64,64*k);
   float x=pointer.x+gap;
   if(x+width>bounds.xMax)x=pointer.x-gap-width;
   x=Mathf.Clamp(x,bounds.xMin,Mathf.Max(bounds.xMin,bounds.xMax-width));
   float y=Mathf.Clamp(pointer.y,bounds.yMin,Mathf.Max(bounds.yMin,bounds.yMax-height));
   return new Rect(x,y,width,height);
  }
  internal static bool TryDraw(StudioBootstrap owner,Vector2 pos,string text,float designWidth,bool box){
   if(!box||string.IsNullOrEmpty(text))return false;
   bool coordinate=text.StartsWith("X ",StringComparison.Ordinal)&&text.EndsWith(" / 100",StringComparison.Ordinal);
   bool grid=(text.StartsWith("GRID ",StringComparison.Ordinal)||text.StartsWith("FREE ",StringComparison.Ordinal))&&text.EndsWith(" ms",StringComparison.Ordinal);
   if(!coordinate&&!grid)return false;
   float k=OverlayAnnotationLayout.Scale(owner);if(float.IsNaN(pos.x)||float.IsNaN(pos.y)||float.IsInfinity(pos.x)||float.IsInfinity(pos.y))return false;
   EnsureStyles(owner,k);Vector2 anchor;Rect preferred;string gridTitle=null,gridNumber=null;
   if(coordinate){
    anchor=new Vector2(pos.x+designWidth*.5f*k,pos.y+49*k);
    preferred=new Rect(anchor.x-Width*k*.5f,anchor.y-(Height+12)*k,Width*k,Height*k);
   }else{
    // Existing guide labels are offset 18/23 design pixels from the pointer.
    int start=text.IndexOf(' ')+1;gridTitle=text.Substring(0,start-1);gridNumber=text.Substring(start,text.Length-start-3).Trim();
    anchor=pos-new Vector2(18,23)*k;preferred=GridRect(owner,anchor,k,GridWidth(gridTitle,gridNumber,k));
   }
   // Keep grid labels on a clear side of the pointer even when other labels
   // would make the generic collision layout move them back across it.
   Rect rect=coordinate?OverlayAnnotationLayout.Place(owner,preferred):preferred;
   OverlayAnnotationLayout.State(owner).Labels.Add(new OverlayAnnotationLayout.Annotation{Text=text,Rect=rect,Box=true});
   if(Event.current.type!=EventType.Repaint)return true;
   Bracket(rect,k);
   if(coordinate){
    bool above=rect.center.y<anchor.y;float edge=above?rect.yMax:rect.y,tip=above?Mathf.Min(anchor.y,edge+7*k):Mathf.Max(anchor.y,edge-7*k);
    float stemX=Mathf.Clamp(anchor.x,rect.x+4*k,rect.xMax-4*k);
    Fill(new Rect(stemX-.5f*k,Mathf.Min(edge,tip),k,Mathf.Abs(edge-tip)),EdgeInk);
   }
   if(coordinate)Label(rect,"X",text.Substring(2,text.Length-8).Trim(),"/100",k);
   else Label(rect,gridTitle,gridNumber,"ms",k,true);
   return true;
  }
 }
}
