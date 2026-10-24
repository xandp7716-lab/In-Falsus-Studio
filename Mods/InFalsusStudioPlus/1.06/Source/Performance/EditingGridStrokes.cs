using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using InFalsusStudio.TimeGroups;
using UnityEngine;
namespace InFalsusStudio.Performance {
 internal static class EditingGridStrokes {
  internal static float Pixels(float original){return original*1.25f;}
  static void BeatStrip(MeshBatch batch,Camera camera,Vector3 a,Vector3 b,float pixels,Color color){batch.ScreenStrip(camera,a,b,Pixels(pixels),color);}
  static IEnumerable<CodeInstruction> Thicken(IEnumerable<CodeInstruction> input){
   int count=0;foreach(var code in input){var call=code.operand as MethodInfo;if(call!=null&&call.DeclaringType==typeof(MeshBatch)&&call.Name=="ScreenStrip"&&call.GetParameters().Select(p=>p.ParameterType).SequenceEqual(new[]{typeof(Camera),typeof(Vector3),typeof(Vector3),typeof(float),typeof(Color)})){code.opcode=OpCodes.Call;code.operand=typeof(EditingGridStrokes).GetMethod("BeatStrip",TimingGroups.F);count++;}yield return code;}
   if(count!=1)throw new MissingMethodException("Beat-grid stroke call changed");
  }
  internal static void Install(Harmony harmony){harmony.Patch(typeof(NoteRenderer).GetMethod("DrawBeats",TimingGroups.F),transpiler:new HarmonyMethod(typeof(EditingGridStrokes).GetMethod("Thicken",TimingGroups.F)));}
 }
}
