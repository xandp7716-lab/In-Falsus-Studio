using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
namespace InfStudio.Universal {
 public class Value {public string key,value;}
 public class Source {public int sourceId,index;public string kind,scope,source,editable;public string[] tokens;public double start,end,left0,right0,left1,right1,easeL,easeR,split;public double[] grid;}
 public class Request {public int version,groupStart;public string tool,targetGroup,currentGroup,document;public bool overflow,createGroup;public Value[] values;public Source[] selection,capture,skySources;}
 public class Operation {public string action,scope,text,expected;public int sourceId;public string[] lines;}
 public class Context {
  public readonly Request R;public readonly Dictionary<string,string> Values;public readonly List<Operation> Operations=new List<Operation>();public int Count;public string Message="";
  public Context(Request r){R=r;Need(r!=null&&r.version==1,"Unsupported request.");Values=(r.values??new Value[0]).ToDictionary(v=>v.key,v=>v.value);}
  public static void Need(bool ok,string message){if(!ok)throw new ArgumentException(message);}
  public string S(string key,string fallback=""){string value;return Values.TryGetValue(key,out value)?value:fallback;}
  public double D(string key,double fallback=0){double n;Need(double.TryParse(S(key,fallback.ToString(CultureInfo.InvariantCulture)),NumberStyles.Float,CultureInfo.InvariantCulture,out n)&&!double.IsNaN(n)&&!double.IsInfinity(n),"Invalid number: "+key);return n;}
  public int I(string key,int fallback=0){double n=D(key,fallback);Need(n==Math.Truncate(n)&&n>=int.MinValue&&n<=int.MaxValue,"Invalid integer: "+key);return (int)n;}
  public bool B(string key,bool fallback=false){bool b;Need(bool.TryParse(S(key,fallback?"true":"false"),out b),"Invalid boolean: "+key);return b;}
  public static string N(double n){Need(!double.IsNaN(n)&&!double.IsInfinity(n),"Nonfinite generated value.");return n.ToString("0.############",CultureInfo.InvariantCulture);}
  public static int Round(double n){Need(n>=0&&n<=86400000,"Time outside 0..86400000 ms.");return (int)Math.Round(n,MidpointRounding.AwayFromZero);}
  public void Append(IEnumerable<string> rows){var lines=rows.ToArray();if(lines.Length>0)Operations.Add(new Operation{action="append",scope="$target",lines=lines});Count+=lines.Length;}
  public void Delete(Source source){Operations.Add(new Operation{action="delete",sourceId=source.sourceId,expected=source.source});}
  public static void Run(string request,string output,string tool,Action<Context> generate){
   var json=new JavaScriptSerializer{MaxJsonLength=8000000,RecursionLimit=64};var r=json.Deserialize<Request>(File.ReadAllText(request,Encoding.UTF8));Need(r!=null&&r.tool==tool,"Tool/request mismatch.");var c=new Context(r);generate(c);Need(c.Count>0&&c.Count<=16000,"No objects generated, or more than 16000 objects.");
   File.WriteAllText(output,json.Serialize(new {version=1,operations=c.Operations}),new UTF8Encoding(false));File.WriteAllText(output+".info.json",json.Serialize(new {segments=c.Count,message=c.Message}),new UTF8Encoding(false));
  }
  public static double Ease(double kind,double u){u=Math.Max(0,Math.Min(1,u));Need(kind==0||kind==1||kind==2,"Unsupported SkyArea easing.");return kind==1?Math.Sin(Math.PI*u/2):kind==2?1-Math.Cos(Math.PI*u/2):u;}
  public static double[] Range(Source s,double t){double u=s.end>s.start?(t-s.start)/(s.end-s.start):0;return new[]{s.left0+(s.left1-s.left0)*Ease(s.easeL,u),s.right0+(s.right1-s.right0)*Ease(s.easeR,u)};}
  public static string[] Limited(double left,double right,double preferred){
   double center=(left+right)*.5,width=right-left;Need(width>0,"Zero-width sample.");
   if(preferred>=1&&preferred<=128&&preferred==Math.Truncate(preferred)&&Math.Abs(center*preferred-Math.Round(center*preferred))<1e-10&&Math.Abs(width*preferred-Math.Round(width*preferred))<1e-10&&Math.Round(width*preferred)>0)return new[]{N(Math.Round(center*preferred)),N(preferred),N(Math.Round(width*preferred))};
   double best=double.PositiveInfinity,sumBest=double.PositiveInfinity;int bx=0,bd=1,bw=1;bool inside=left>=-1e-10&&right<=1+1e-10,la=inside&&Math.Abs(left)<1e-12,ra=inside&&Math.Abs(right-1)<1e-12;
   for(int d=1;d<=128;d++){int x0=(int)Math.Floor(center*d)-1,x1=(int)Math.Ceiling(center*d)+1,w0=Math.Max(1,(int)Math.Floor(width*d)-1),w1=Math.Max(1,(int)Math.Ceiling(width*d)+1);
    for(int x=x0;x<=x1;x++)for(int w=w0;w<=w1;w++){double l=(x-w*.5)/d,r=(x+w*.5)/d;if(inside&&(l< -1e-10||r>1+1e-10)||la&&Math.Abs(l)>1e-12||ra&&Math.Abs(r-1)>1e-12)continue;double a=Math.Abs(l-left),b=Math.Abs(r-right),error=Math.Max(a,b),sum=a+b;
     if(error<best-1e-12||Math.Abs(error-best)<=1e-12&&sum<sumBest-1e-12){best=error;sumBest=sum;bx=x;bd=d;bw=w;}}
    if(best<=1e-12)break;
   }Need(!double.IsInfinity(best),"Cannot encode sampled width within 128 coordinate parts.");return new[]{bx.ToString(),bd.ToString(),bw.ToString()};
  }
  public static string Rewrite(Source source,Dictionary<int,string> replacements){
   var m=Regex.Match(source.editable??source.source,@"^(\s*[A-Za-z_][A-Za-z_0-9]*\s*\()(.*)(\).*)$");Need(m.Success,"Captured note is not a numeric SPC statement.");var tokens=m.Groups[2].Value.Split(',');
   foreach(var p in replacements){Need(p.Key>=0&&p.Key<tokens.Length,"Missing captured token.");string old=tokens[p.Key];int front=old.Length-old.TrimStart().Length,back=old.Length-old.TrimEnd().Length;tokens[p.Key]=old.Substring(0,front)+p.Value+(back>0?old.Substring(old.Length-back):"");}
   return m.Groups[1].Value+string.Join(",",tokens)+m.Groups[3].Value;
  }
 }
}
