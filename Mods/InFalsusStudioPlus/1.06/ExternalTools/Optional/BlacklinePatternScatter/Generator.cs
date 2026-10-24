using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
namespace InfStudio.Universal {
 public static class Generator {
  sealed class Box {internal double Time,X;}
  sealed class CopyNote {internal double Time;internal Source Source;internal Dictionary<int,string> Changes;}
  public static void Run(string request,string output){Context.Run(request,output,"BlacklinePatternScatter",Generate);}
  static long Gcd(long a,long b){while(b!=0){long n=a%b;a=b;b=n;}return Math.Abs(a);}
  static double Num(string token){return double.Parse(token,CultureInfo.InvariantCulture);}
  static void Translate(Source s,int index,long shift,Dictionary<int,string> changes){
   long x=(long)Num(s.tokens[index]),d=(long)Num(s.tokens[index+1]),w=(long)Num(s.tokens[index+2]);Context.Need(d>0&&Num(s.tokens[index])==x&&Num(s.tokens[index+1])==d&&Num(s.tokens[index+2])==w,"Captured coordinates must use integer X / split / width.");
   if(shift==0)return;long denominator=10000,divisor=Gcd(Math.Abs(shift),denominator);long sn=shift/divisor,sd=denominator/divisor;long common=checked(d/Gcd(d,sd)*sd),factor=common/d;long xx=checked(x*factor+sn*(common/sd)),ww=checked(w*factor);long g=Gcd(Gcd(xx,ww),common);xx/=g;ww/=g;common/=g;
   Context.Need(Math.Abs(xx)<=1000000000&&Math.Abs(ww)<=1000000000&&common<=1000000000,"Exact translated coordinates exceed supported magnitude; reduce X range.");changes[index]=xx.ToString(CultureInfo.InvariantCulture);changes[index+1]=common.ToString(CultureInfo.InvariantCulture);changes[index+2]=ww.ToString(CultureInfo.InvariantCulture);
  }
  static void Generate(Context c){
   var notes=c.R.capture??new Source[0];Context.Need(notes.Length>0&&notes.Length<=256,"Capture Flick / SkyArea notes first. Old sampled paths must be recaptured once.");Context.Need(notes.All(s=>s.kind=="SkyArea"||s.kind=="Flick"),"Captured pattern contains unsupported note types.");
   double start=c.D("start"),end=c.D("end",1000),left=c.D("xMin",-.5),right=c.D("xMax",1.5);int count=c.I("count",10),gap=c.I("minGapMs"),seed=c.I("seed");Context.Need(start>=0&&end<=86400000&&end>start&&right>left&&left>=-1000&&right<=1000&&count>=1&&count<=10000&&gap>=0,"Invalid time / X bounds, count or minimum gap.");Context.Need((long)count*notes.Length<=16000,"More than 16000 scattered notes.");
   double first=notes.Min(s=>s.start),duration=notes.Max(s=>s.end)-first,min=notes.Min(s=>Math.Min(s.left0,s.left1)),max=notes.Max(s=>Math.Max(s.right0,s.right1)),width=max-min;
   Context.Need(duration<=end-start&&width<=right-left,"Captured pattern does not fit the requested time / X range.");
   long dx0=(long)Math.Ceiling((left-min)*10000-1e-7),dx1=(long)Math.Floor((right-max)*10000+1e-7);int dt0=(int)Math.Ceiling(start-first),dt1=(int)Math.Floor(end-first-duration);Context.Need(dx1>=dx0&&dt1>=dt0,"No exact translated placement fits.");
   var random=seed==0?new Random():new Random(seed);var placed=new List<Box>();var output=new List<CopyNote>();int nextGroup=c.R.groupStart;
   for(int copy=0;copy<count;copy++)for(int attempt=0;attempt<200;attempt++){
    int dt=dt0+random.Next(dt1-dt0+1);long dx=dx0+(long)Math.Floor(random.NextDouble()*(dx1-dx0+1));double t=first+dt,x=min+dx/10000d;
    if(placed.Any(p=>gap>0?t<p.Time+duration+gap&&p.Time<t+duration+gap:t<p.Time+Math.Max(1,duration)&&p.Time<t+Math.Max(1,duration)&&Math.Abs(x-p.X)<width+.02))continue;
    var groups=new Dictionary<int,int>();foreach(var s in notes){var changes=new Dictionary<int,string>{{0,Context.N(s.start+dt)}};Translate(s,1,dx,changes);if(s.kind=="SkyArea"){
      Translate(s,4,dx,changes);if(s.tokens.Length>10&&!string.IsNullOrWhiteSpace(s.tokens[10])&&Num(s.tokens[10])>=0){int old=(int)Num(s.tokens[10]),fresh;if(!groups.TryGetValue(old,out fresh)){Context.Need(nextGroup>=0&&nextGroup<int.MaxValue,"No groupid range available.");fresh=nextGroup++;groups.Add(old,fresh);}changes[10]=fresh.ToString(CultureInfo.InvariantCulture);}
     }output.Add(new CopyNote{Time=s.start+dt,Source=s,Changes=changes});}
    placed.Add(new Box{Time=t,X=x});break;
   }
   Context.Need(placed.Count>0,"No placement fits; increase time / X range or reduce spacing.");
   var sorted=output.OrderBy(r=>r.Time).ToArray();var chronological=new Dictionary<string,int>();int gid=c.R.groupStart;
   foreach(var row in sorted){string key;if(row.Changes.TryGetValue(10,out key)){int mapped;if(!chronological.TryGetValue(key,out mapped)){mapped=gid++;chronological.Add(key,mapped);}row.Changes[10]=mapped.ToString(CultureInfo.InvariantCulture);}}
   c.Append(sorted.Select(row=>Context.Rewrite(row.Source,row.Changes)));c.Message="Placed "+placed.Count+" / "+count+" complete patterns. Only time and X are translated; widths, easing, durations, Flick directions and trailing parameters are retained. Explicit SkyArea group IDs are remapped per copy in chronological order to preserve grouping without colliding with the originals.";
  }
 }
}
