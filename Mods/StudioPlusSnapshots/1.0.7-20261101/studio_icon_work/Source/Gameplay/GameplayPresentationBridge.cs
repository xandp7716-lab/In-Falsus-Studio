using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using InFalsusStudio.Core;
using InFalsusStudio.NativeEditorUI;
using InFalsusStudio.NativeVisual;
using UnityEngine;
namespace InFalsusStudio.Gameplay {
 internal sealed class GameplayPresentationBridge:IDisposable {
  internal struct Pulse {internal JudgeNote Note;internal double Time;}
  internal readonly List<Pulse> Pulses=new List<Pulse>();
  readonly StudioKeySounds sounds;readonly StudioTransport transport;readonly StudioKeySoundOutput filter;readonly AudioSource output;
  readonly PcmSample[] samples;
  readonly HashSet<int> skyStarts,playedShots=new HashSet<int>();
  // Reactive attacks and scheduled hold beds share the existing FX voice. The
  // audio thread starts each attack at its next writable block, preserving
  // sample onset without shifting music, chart time or project audio offset.
  internal sealed class ReactiveAttacks {
   struct Shot {internal PcmSample Sample;internal float Gain;internal double Position;}
   readonly Shot[] pending=new Shot[128],voices=new Shot[64];int read,write;internal int Frequency;
   internal ReactiveAttacks(int hz){Frequency=hz;}
   internal void Enqueue(PcmSample sample,float gain){if(sample==null||gain<=0)return;int at=write,next=(at+1)%pending.Length;if(next==Volatile.Read(ref read))return;pending[at]=new Shot{Sample=sample,Gain=gain};Volatile.Write(ref write,next);}
   internal void Mix(float[] data,int channels){
    if(channels<1)return;int hz=Volatile.Read(ref Frequency);if(hz<8000)return;
    while(read!=Volatile.Read(ref write)){var shot=pending[read];int slot=0;for(int i=0;i<voices.Length;i++){if(voices[i].Sample==null){slot=i;break;}if(voices[i].Position>voices[slot].Position)slot=i;}voices[slot]=shot;Volatile.Write(ref read,(read+1)%pending.Length);}
    int frames=data.Length/channels;
    for(int i=0;i<voices.Length;i++){var shot=voices[i];if(shot.Sample==null)continue;double step=(double)shot.Sample.Frequency/hz;for(int f=0;f<frames&&shot.Position<shot.Sample.Frames;f++,shot.Position+=step)for(int c=0;c<channels;c++)data[f*channels+c]+=shot.Sample.At(shot.Position,c,false)*shot.Gain;if(shot.Position>=shot.Sample.Frames)shot.Sample=null;voices[i]=shot;}
    for(int i=0;i<data.Length;i++){float x=data[i],v=Math.Abs(x);if(v>.95f)data[i]=Math.Sign(x)*(.95f+.04f*(1-1/(1+(v-.95f)*25)));}
   }
  }
  static ReactiveAttacks activeAttacks;static StudioKeySoundOutput activeFilter;
  ReactiveAttacks attacks;
  internal static void MixAttacks(StudioKeySoundOutput instance,float[] data,int channels){var bus=Volatile.Read(ref activeAttacks);if(bus!=null&&ReferenceEquals(instance,activeFilter))bus.Mix(data,channels);}
  internal struct Judgement {internal JudgeNote Note;internal int Grade;internal float Started;internal double Error;}
  internal readonly Judgement[] Judgements=new Judgement[7];
  double floorStart=-1,skyStart=-1,lastRate=-1;
  float lastMaster=-1,lastShot=-1,lastHold=-1;
  int lastHz=-1,lastRev=-1;
  GUIStyle text;bool stopped,wasFloor,wasSky;
  internal GameplayPresentationBridge(StudioBootstrap owner){
   sounds=NativeEditorUIPlugin.Val<StudioKeySounds>(owner,"keySounds");transport=NativeEditorUIPlugin.Val<StudioTransport>(owner,"transport");sounds.StopAll();
   sounds.GetType().GetMethod("DecodeReadySamples",NativeEditorUIPlugin.F).Invoke(sounds,null);
   filter=(StudioKeySoundOutput)Read(sounds,"filter");output=(AudioSource)Read(sounds,"output");samples=(PcmSample[])Read(sounds,"samples");
   attacks=new ReactiveAttacks(Math.Max(8000,AudioSettings.outputSampleRate));
   activeFilter=filter;Volatile.Write(ref activeAttacks,attacks);
   WarmSound();
   skyStarts=GameplaySession.Engine!=null?new SkyConnections(GameplaySession.Engine.Notes.Select(n=>n.Source)).Starts:new HashSet<int>();
  }
  void WarmSound(){if(sounds.Enabled&&AudioSettings.GetConfiguration().numRealVoices>=2&&!output.isPlaying){output.priority=1;output.pitch=1;output.volume=1;output.Play();}}
  void TriggerShot(HitSoundKind kind){
   if(!sounds.Enabled||AudioSettings.GetConfiguration().numRealVoices<2)return;
   int idx=(int)kind;if(idx>=0&&idx<samples.Length)attacks.Enqueue(samples[idx],Mathf.Clamp01(sounds.MasterVolume)*Mathf.Clamp01(sounds.ShotVolume));
  }
  static object Read(object owner,string field){return owner.GetType().GetField(field,NativeEditorUIPlugin.F).GetValue(owner);}
  internal static void ResetScoreAnimation(){var type=typeof(NativeVisualPlugin).Assembly.GetType("InFalsusStudio.NativeVisual.NativePresentation");type.GetField("scoreTime",NativeEditorUIPlugin.F).SetValue(null,-1d);}
  internal void Tick(){
   var engine=GameplaySession.Engine;double now=GameplaySession.Now;
   double originMs=transport.PlaybackStartMs,originDsp=transport.DspTimeAtChart(originMs);
   double audioNow=Math.Max(originMs,transport.ChartTimeAtDsp(AudioSettings.dspTime));
   bool dirty=false;
   bool holdAttack=NativeEditorUIPlugin.Val<bool>(GameplaySession.Owner,"groundHoldAttacks");
   var skyPolicy=NativeEditorUIPlugin.Val<SkyAttackPolicy>(GameplaySession.Owner,"skyAttackPolicy");
   foreach(var result in engine.Results){
    var note=result.Note.Source;int lane=note.Kind==EventKind.SkyArea||note.Kind==EventKind.Flick?6:note.Lane;
    Judgements[lane]=new Judgement{Note=result.Note,Grade=result.Grade,Started=Time.realtimeSinceStartup,Error=result.Error};
    if(result.Grade==1)continue;
    if(note.Kind!=EventKind.SkyArea)Pulses.Add(new Pulse{Note=result.Note,Time=now});
    var kind=note.Kind;
    if(kind==EventKind.Tap||kind==EventKind.Hold&&result.Tick.Head&&holdAttack){
     if(playedShots.Add(note.SourceId)){
      HitSoundKind sample=note.Lane==0||note.Lane==5?HitSoundKind.SideHit:HitSoundKind.FloorHit;
      TriggerShot(sample);
     }
    }else if(kind==EventKind.Flick){
     if(playedShots.Add(note.SourceId)){
      TriggerShot(HitSoundKind.Flick);
     }
    }
   }
   // Trigger SkyArea attack sound once at segment/chain capture start (not on every hold tick).
   foreach(var n in engine.Notes){
    if(n.Source.TimeMs>now+200)break;
    if(n.Source.Kind==EventKind.SkyArea&&now>=n.Source.TimeMs&&now<=n.Source.EndMs&&engine.SkyActive(n.Source.SourceId)){
     bool allowSky=skyPolicy==SkyAttackPolicy.EverySegment||(skyPolicy==SkyAttackPolicy.ContinuousRegionStart&&skyStarts.Contains(n.Source.SourceId));
     if(allowSky&&playedShots.Add(n.Source.SourceId)){
      TriggerShot(HitSoundKind.SkyHit);
     }
    }
   }
   Pulses.RemoveAll(p=>now-p.Time>1500);
   bool floor=false;foreach(var n in engine.Notes){if(n.Source.TimeMs>now)break;if(n.Source.Kind==EventKind.Hold&&n.Source.EndMs>=now&&engine.Holding(n,GameplaySession.InputState)){floor=true;break;}}
   bool sky=engine.ActiveSky!=null;
   if(floor!=wasFloor){wasFloor=floor;floorStart=floor?audioNow:-1;dirty=true;}
   if(sky!=wasSky){wasSky=sky;skyStart=sky?audioNow:-1;dirty=true;}
   if(!sounds.Enabled||AudioSettings.GetConfiguration().numRealVoices<2){StopSound();return;}
   int hz=Math.Max(8000,AudioSettings.outputSampleRate);Volatile.Write(ref attacks.Frequency,hz);Volatile.Write(ref activeAttacks,attacks);
   float master=Mathf.Clamp01(sounds.MasterVolume),shotVol=Mathf.Clamp01(sounds.ShotVolume),holdVol=Mathf.Clamp01(sounds.HoldVolume);
   if(hz!=lastHz||transport.Revision!=lastRev||transport.Rate!=lastRate||master!=lastMaster||shotVol!=lastShot||holdVol!=lastHold)dirty=true;
   if(dirty){
    lastHz=hz;lastRev=transport.Revision;lastRate=transport.Rate;lastMaster=master;lastShot=shotVol;lastHold=holdVol;
    double holdEnd=Math.Max(GameplaySession.EndTime+10000,audioNow+10000);
    var plan=new HitSoundPlan(new SpcEvent[0]);
    if(floor)plan.Holds.Add(new HitSoundCue{Kind=HitSoundKind.FloorHold,Start=floorStart,End=holdEnd});
    if(sky)plan.Holds.Add(new HitSoundCue{Kind=HitSoundKind.SkyHold,Start=skyStart,End=holdEnd});
    var pcm=new PcmSoundPlan(plan,samples);
    filter.Publish(new PcmMixState(pcm,originMs,originDsp,transport.Rate,originMs,double.PositiveInfinity,master,shotVol,holdVol,true),hz);
   }
   if(!output.isPlaying){output.priority=1;output.pitch=1f;output.volume=1f;output.Play();}
   stopped=false;
  }
  internal void StopSound(){if(stopped)return;Volatile.Write(ref activeAttacks,null);sounds.StopAll();attacks=new ReactiveAttacks(Math.Max(8000,AudioSettings.outputSampleRate));stopped=true;lastRev=-1;wasFloor=wasSky=false;floorStart=skyStart=-1;}
  public void Dispose(){StopSound();Volatile.Write(ref activeAttacks,null);activeFilter=null;attacks=null;Pulses.Clear();playedShots.Clear();Array.Clear(Judgements,0,Judgements.Length);}
  internal float[] Powers(){var p=new float[6];if(GameplaySession.Phase!=GameplayPhase.Playing)return p;for(int i=0;i<6;i++)p[i]=GameplaySession.InputState.Held[i]?1:0;return p;}
  internal void DrawHud(){
   // The preparation countdown is transient. Playing/results use only the
   // original score/combo HUD and native judgement artwork at the track.
   if(GameplaySession.Phase!=GameplayPhase.LeadIn)return;
   var ui=NativeEditorUIPlugin.I;float scale=Mathf.Max(.6f,Mathf.Min(Screen.width/1920f,Screen.height/1080f));
   if(text==null)text=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=Mathf.RoundToInt(20*scale),wordWrap=false};
   string value=(ui.ChineseGameplay?"准备 — ":"Ready — ")+Math.Max(0,GameplaySession.LeadDeadline-Time.realtimeSinceStartupAsDouble).ToString("0.0")+" s";
   var rect=new Rect(0,155*scale,Screen.width,32*scale);text.normal.textColor=Color.black;var shadow=rect;shadow.x++;shadow.y++;GUI.Label(shadow,value,text);text.normal.textColor=Color.white;GUI.Label(rect,value,text);
  }
 }
}
