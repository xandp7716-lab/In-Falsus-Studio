using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.Mono;
using UnityEngine;
using InFalsusStudio.NativeVisual;
using InFalsusStudio.NativeEditorUI;
using InFalsusStudio.TimeGroups;
using HarmonyLib;
using InFalsusStudio.Performance;
[assembly:AssemblyVersion("1.0.70.0")]
[assembly:AssemblyFileVersion("1.0.70.0")]
[assembly:AssemblyProduct("infstudioplus")]
[assembly:AssemblyTitle("InFalsus Studio+")]
[assembly:AssemblyInformationalVersion("1.06")]
namespace InFalsusStudio.VisualUI {
 [BepInPlugin("local.infalsusstudio.visualui","InFalsus Studio+","1.0.70")]
 public sealed class VisualUIPlugin:BaseUnityPlugin {
  NativeVisualPlugin visual;NativeEditorUIPlugin ui;Harmony groupHarmony;
  void Awake(){
   try{
    if(EditBenchmarkProbe.Requested||EditBenchmarkProbe.WorkflowRequested)AudioListener.volume=0f;
    if(AppDomain.CurrentDomain.GetAssemblies().Any(a=>a.GetName().Name=="NativeVisualMod"||a.GetName().Name=="NativeEditorUI"))throw new InvalidOperationException("Separate NativeVisual/UI assemblies are present. Use the integrated installer to back up and disable the old plugin DLLs first.");
    visual=gameObject.AddComponent<NativeVisualPlugin>();visual.Initialize(Config,Logger);
    if(!NativeVisualPlugin.Ready)throw new InvalidOperationException("Native visual initialization failed; see preceding error.");
    ui=gameObject.AddComponent<NativeEditorUIPlugin>();ui.Initialize(Config,Logger);
    if(!ui.ready)throw new InvalidOperationException("UI initialization failed; see preceding error.");
    groupHarmony=new Harmony("local.infalsusstudio.nonofficial.timinggroups");GroupDataPatches.Install(groupHarmony);GroupRuntime.Install(groupHarmony);GroupProperties.Install(groupHarmony);GroupMeasureLines.Install(groupHarmony);FastScrollHooks.Install(groupHarmony);RenderIndex.Install(groupHarmony);HandleOptimization.Install(groupHarmony);WindowFocusRecovery.Install(groupHarmony);AltHandleLayout.Install(groupHarmony);EndpointDragMode.Install(groupHarmony);OverlayAnnotationLayout.Install(groupHarmony);EditingGridStrokes.Install(groupHarmony);P1Profiler.Install(groupHarmony);DocumentAcceleration.Install(groupHarmony);DragPreview.Install(groupHarmony);DragInput.Install(groupHarmony);PausedNoteMeshes.Install(groupHarmony);StaticBeatMeshes.Install(groupHarmony);NoteTokenEdits.Install(groupHarmony);PausedBatchUploads.Install(groupHarmony);BatchEditing.Install(groupHarmony);CommitValidation.Install(groupHarmony);CommitRefresh.Install(groupHarmony);PreviewFrustum.Install(groupHarmony);PreviewSkyBody.Install(groupHarmony);PreviewGuiSnapshots.Install(groupHarmony);PreviewBatchUploads.Install(groupHarmony);PreviewFlickBody.Install(groupHarmony);
    PlaybackGcBudget.Init(Config);
    if(P1PlayerProbe.Requested)StartCoroutine(P1PlayerProbe.Run());
    if(EditBenchmarkProbe.Requested)StartCoroutine(EditBenchmarkProbe.Run());
    if(EditBenchmarkProbe.WorkflowRequested)StartCoroutine(EditBenchmarkProbe.RunWorkflow());
    Logger.LogInfo("infstudioplus · InFalsus Studio+ 1.06 ready (build 1.0.70): M+ executable icon and bright-blue chibi Mei about artwork.");
   }catch(Exception e){if(groupHarmony!=null)groupHarmony.UnpatchSelf();Logger.LogError("Integrated mod disabled: "+e);if(ui)Destroy(ui);if(visual)Destroy(visual);}
  }
  void Update(){ProductBrand.Tick();PlaybackGcBudget.Tick();P1Profiler.Tick();}
  void OnDestroy(){PlaybackGcBudget.Dispose();if(P1Profiler.Active)P1Profiler.Finish();if(groupHarmony!=null)groupHarmony.UnpatchSelf();GroupRuntime.Dispose();if(ui)Destroy(ui);if(visual)Destroy(visual);}
 }
}

