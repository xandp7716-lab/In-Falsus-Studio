using System;
using System.Linq;
using System.Collections.Generic;
namespace InfStudio.Universal {
 public static class Generator {
  sealed class Head {internal int Time;internal Source Source;internal string[] A,B;}
  public static void Run(string request,string output){Context.Run(request,output,"SkyAreaBracketTunnel",Generate);}
  static void Generate(Context c){
   var sources=c.R.skySources??new Source[0];Context.Need(sources.Length>0&&sources.Length<=256,"Select 1..256 SkyAreas.");string mode=c.S("mode","grid"),shape=c.S("endShape","follow");bool end=c.B("includeEnd",true);int n=c.I("count",16),duration=c.I("durationMs",1);Context.Need(mode=="grid"||mode=="count","Invalid sampling mode.");Context.Need(shape=="follow"||shape=="flat","Invalid end shape.");Context.Need(duration>=1&&duration<=300000,"Bracket duration must be 1..300000 ms.");if(mode=="count")Context.Need(n>=1&&n<=16000&&(!end||n>=2),"Count must be 1..16000; including the end requires at least 2.");
   var heads=new List<Head>();int skipped=0;
   foreach(var s in sources){Context.Need(s.kind=="SkyArea"&&s.end-s.start>=1&&s.end<=86400000,"Source SkyArea must last at least 1ms, within the timeline limit.");Context.Need((s.tokens??new string[0]).Length>=10,"Missing source tokens.");
    Context.Need(MinimumWidth(s)>=-1e-10,"Source SkyArea edges cross.");
    IEnumerable<double> times;
    if(mode=="grid"){Context.Need(s.grid!=null&&s.grid.Length>0&&s.grid.Length<=4096,"No group-local grid supplied; run this tool from Studio 1.0.47 or later.");times=s.grid.Where(t=>t>=s.start-1e-8&&t<=s.end+1e-8).Concat(new[]{s.start});if(end)times=times.Concat(new[]{s.end});else times=times.Where(t=>t<s.end-1e-8);}
    else times=Enumerable.Range(0,n).Select(i=>s.start+(s.end-s.start)*i/(end?n-1d:n));
    foreach(int t in times.Select(Context.Round).Distinct().OrderBy(t=>t)){Context.Need((long)t+duration<=86400000,"Generated SkyArea exceeds the 86400000ms timeline limit.");var a=Context.Range(s,t);var b=shape=="flat"?a:Context.Range(s,t+duration);if(a[1]-a[0]<=1e-12||b[1]-b[0]<=1e-12){skipped++;continue;}Context.Need(heads.Count<16000,"More than 16000 brackets; reduce subdivision or count.");heads.Add(new Head{Time=t,Source=s,A=Context.Limited(a[0],a[1],s.split),B=Context.Limited(b[0],b[1],s.tokens.Length>5?double.Parse(s.tokens[5],System.Globalization.CultureInfo.InvariantCulture):s.split)});}
   }
   Context.Need(heads.Count>0,"No positive-width brackets remain.");Context.Need(c.R.groupStart>=0&&(long)c.R.groupStart+heads.Count-1<=int.MaxValue,"No available groupid range.");int gid=c.R.groupStart;
   c.Append(heads.OrderBy(h=>h.Time).ThenBy(h=>h.Source.sourceId).Select(h=>"skyarea("+h.Time+","+string.Join(",",h.A)+","+string.Join(",",h.B)+",0,0,"+duration+","+(gid++)+")"));
   if(c.B("deleteSource"))foreach(var s in sources)c.Delete(s);
   c.Message="Generated "+heads.Count+" "+duration+"ms brackets; skipped "+skipped+" zero-width samples. groupid "+c.R.groupStart+".."+(gid-1)+". End brackets may extend "+duration+"ms past the source.";
  }
  static double MinimumWidth(Source s){
   Func<double,double> width=u=>{var range=Context.Range(s,s.start+(s.end-s.start)*u);return range[1]-range[0];};double minimum=Math.Min(width(0),width(1));
   double right=s.right1-s.right0,left=s.left1-s.left0,k=Math.PI*.5;
   double a=(s.easeR==0?right:0)-(s.easeL==0?left:0),b=k*((s.easeR==2?right:0)-(s.easeL==2?left:0)),d=k*((s.easeR==1?right:0)-(s.easeL==1?left:0));
   double radius=Math.Sqrt(b*b+d*d);if(radius<1e-15)return minimum;double z=-a/radius;if(z< -1-1e-12||z>1+1e-12)return minimum;z=Math.Max(-1,Math.Min(1,z));double angle=Math.Atan2(d,b),root=Math.Asin(z);
   foreach(double solution in new[]{root-angle,Math.PI-root-angle})for(int turn=-1;turn<=1;turn++){double theta=solution+turn*Math.PI*2;if(theta>0&&theta<k)minimum=Math.Min(minimum,width(theta/k));}
   return minimum;
  }
 }
}
