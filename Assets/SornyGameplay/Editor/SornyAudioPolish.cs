using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace DungeonDLC.Sorny.Editor
{
 [InitializeOnLoad] public static class SornyAudioPolish
 {
  const string Root="Assets/SornyGameplay",Marker=Root+"/Audio/aplicar.txt";
  static double next;
  static SornyAudioPolish(){EditorApplication.update+=Poll;}
  static void Poll(){if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)return;if(!File.Exists(Marker)||File.ReadAllText(Marker).Trim()!="pending")return;if(SceneManager.GetActiveScene().name!="Fortaleza_Azul_Jogavel")return;try{Apply();File.WriteAllText(Marker,"done");}catch(Exception e){File.WriteAllText(Marker,"error: "+e);Debug.LogException(e);}}
  static AudioClip Clip(string n)=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/"+n);
  static string Hierarchy(Transform t){string p=t.name;while(t.parent){t=t.parent;p=t.name+"/"+p;}return p;}
  static void RestoreSigns(Scene scene)
  {
   string first=Directory.GetFiles(Root+"/Reports/BackupAudioVisual","Cena_*.unity").OrderBy(p=>p).FirstOrDefault();if(first==null)return;
   var backup=EditorSceneManager.OpenScene(first,OpenSceneMode.Additive);
   try{var originals=backup.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>Regex.IsMatch(t.name,"^P[0-9]{2}$")).GroupBy(Hierarchy).ToDictionary(g=>g.Key,g=>g.First());foreach(var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>Regex.IsMatch(t.name,"^P[0-9]{2}$"))){if(originals.TryGetValue(Hierarchy(t),out var o)){t.localPosition=o.localPosition;t.localRotation=o.localRotation;t.localScale=o.localScale;}}}
   finally{EditorSceneManager.CloseScene(backup,true);SceneManager.SetActiveScene(scene);}
  }
  [MenuItem("Dungeon & DLC/7 - Aplicar audio e acabamento visual")]
  public static void Apply()
  {
   if(EditorApplication.isPlaying)throw new Exception("Pare o Play antes de aplicar.");
   var scene=SceneManager.GetActiveScene();if(scene.name!="Fortaleza_Azul_Jogavel")throw new Exception("Abra Fortaleza_Azul_Jogavel.");
   Directory.CreateDirectory(Root+"/Reports/BackupAudioVisual");
   string stamp=DateTime.Now.ToString("yyyyMMdd_HHmmss");EditorSceneManager.SaveScene(scene,Root+"/Reports/BackupAudioVisual/Cena_"+stamp+".unity",true);
   RestoreSigns(scene);
   foreach(string path in Directory.GetFiles(Root+"/Audio").Where(p=>p.EndsWith(".mp3")||p.EndsWith(".wav")))
   {var importer=AssetImporter.GetAtPath(path) as AudioImporter;if(!importer)continue;var sample=importer.defaultSampleSettings;sample.loadType=path.EndsWith(".mp3")?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;sample.compressionFormat=path.EndsWith(".mp3")?AudioCompressionFormat.Vorbis:AudioCompressionFormat.PCM;sample.quality=.7f;importer.defaultSampleSettings=sample;importer.forceToMono=!path.EndsWith(".mp3");importer.SaveAndReimport();}
   var game=UnityEngine.Object.FindFirstObjectByType<SornyGameLoop>();if(!game)throw new Exception("Gerenciador não encontrado.");
   var audio=game.GetComponent<SornyAudioDirector>();if(!audio)audio=game.gameObject.AddComponent<SornyAudioDirector>();
   audio.trilha=Clip("Darkest_Child_Kevin_MacLeod.mp3");audio.ambiente=Clip("ambiente.wav");audio.espadaAr=Clip("espada_ar.wav");audio.impacto=Clip("impacto.wav");audio.alerta=Clip("crack_alerta.wav");audio.ataque=Clip("crack_ataque.wav");audio.morte=Clip("crack_morte.wav");audio.moeda=Clip("moeda.wav");audio.cura=Clip("cura.wav");audio.vitoria=Clip("vitoria.wav");audio.derrota=Clip("derrota.wav");audio.passos=Enumerable.Range(0,4).Select(i=>Clip("passo_"+i+".wav")).ToArray();
   if(!audio.trilha||audio.passos.Any(c=>!c))throw new Exception("Arquivos de áudio ausentes.");
   int mats=0;foreach(string name in new[]{"Stone","Damp","Floor","MossFloor","Ceiling","Wood","Iron","Gold","Blue"})
   {string path="Assets/DungeonDLC_MegaPack/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m)continue;string backup=Root+"/Reports/BackupAudioVisual/"+name+".mat";if(!File.Exists(backup))AssetDatabase.CopyAsset(path,backup);foreach(string f in new[]{"_ScreenSpaceReflections","_EnvironmentReflections","_Smoothness","_Glossiness","_Metallic"})if(m.HasProperty(f))m.SetFloat(f,0);m.EnableKeyword("_SCREENSPACEREFLECTIONS_OFF");m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");EditorUtility.SetDirty(m);mats++;}
   var signs=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.gameObject.scene==scene&&Regex.IsMatch(t.name,"^P[0-9]{2}$")&&(!t.parent||t.parent.name!=t.name)).ToArray();int moved=0;
   for(int i=0;i<signs.Length;i++)for(int j=i+1;j<signs.Length;j++)if(Mathf.Abs(signs[i].position.x-signs[j].position.x)<.4f&&Mathf.Abs(signs[i].position.y-signs[j].position.y)<.7f&&Mathf.Abs(signs[i].position.z-signs[j].position.z)<2.05f)
   {var p=signs[j].position;float room=Mathf.Round(p.z/16)*16;p.z=room+(p.z>=room?-1.5f:1.5f);signs[j].position=p;moved++;}
   // Keep signs readable at oblique angles; surface textures remain pixel filtered.
   foreach(string path in Directory.GetFiles("Assets/DungeonDLC_MegaPack/Art/Signs","*.png")){var ti=AssetImporter.GetAtPath(path) as TextureImporter;ti.filterMode=FilterMode.Bilinear;ti.mipmapEnabled=true;ti.anisoLevel=4;ti.SaveAndReimport();}
   var hint=game.GetComponentsInChildren<UnityEngine.UI.Text>(true).FirstOrDefault(t=>t.name=="Controles");if(hint&&!hint.text.Contains("F10"))hint.text+="    |    F10  ÁUDIO";
   var nav=game.GetComponent<SornyNavGrid>();if(nav)nav.Bake();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   string report="Audio instalado: 1 musica, 14 efeitos/ambiente.\nMateriais foscos: "+mats+"\nPlacas reposicionadas: "+moved+"\nF10: volumes e creditos.\nBackup: Reports/BackupAudioVisual\n";File.WriteAllText(Root+"/Reports/AUDIO_VISUAL.txt",report);File.WriteAllLines(Root+"/Reports/PLACAS.txt",signs.Select(t=>Hierarchy(t)+" "+t.position));Debug.Log("SORNY_AUDIO_VISUAL_OK "+report);
  }
  public static void BatchApply(){EditorSceneManager.OpenScene(SornyInstaller.Cena);Apply();}
 }
}
