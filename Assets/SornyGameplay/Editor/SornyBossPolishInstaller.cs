using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace DungeonDLC.Sorny.Editor
{
 [InitializeOnLoad]public static class SornyBossPolishInstaller
 {
  const string Root="Assets/SornyGameplay",Marker=Root+"/Art/Boss/Animations/aplicar.txt";static double next;
  static SornyBossPolishInstaller(){EditorApplication.update+=Poll;}
  static void Poll(){if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+2;if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||!File.Exists(Marker)||File.ReadAllText(Marker).Trim()!="pending"||SceneManager.GetActiveScene().name!="Fortaleza_Azul_Jogavel")return;try{Install();File.WriteAllText(Marker,"done");}catch(Exception e){File.WriteAllText(Marker,"error: "+e);Debug.LogException(e);}}
  static Sprite[] Frames(string name)
  {
   string path=Root+"/Art/Boss/Animations/"+name+".png";var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Default;ti.isReadable=true;ti.npotScale=TextureImporterNPOTScale.None;ti.maxTextureSize=2048;ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.wrapMode=TextureWrapMode.Clamp;ti.SaveAndReimport();
   var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var px=tex.GetPixels32();int w=tex.width/4,h=tex.height/2;var result=new Sprite[8];float ppu=1;
   for(int i=0;i<8;i++)
   {int left=i%4*w,bottom=(1-i/4)*h,x0=w,y0=h,x1=-1,y1=-1;for(int y=0;y<h;y++)for(int x=0;x<w;x++)if(px[(bottom+y)*tex.width+left+x].a>180){x0=Mathf.Min(x0,x);y0=Mathf.Min(y0,y);x1=Mathf.Max(x1,x);y1=Mathf.Max(y1,y);}
    if(x1<x0)throw new Exception("Quadro vazio: "+name+"/"+i);if(i==0)ppu=(y1-y0+1)/3.25f;
    var rect=new Rect(left+x0,bottom+y0,x1-x0+1,y1-y0+1);var pivot=new Vector2(Mathf.Clamp01((w*.5f-x0)/rect.width),0);var sprite=Sprite.Create(tex,rect,pivot,ppu,0,SpriteMeshType.FullRect);sprite.name="Boss_"+name+"_"+i;string sp=Root+"/Sprites/"+sprite.name+".asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(sp);if(old){EditorUtility.CopySerialized(sprite,old);UnityEngine.Object.DestroyImmediate(sprite);result[i]=old;}else{AssetDatabase.CreateAsset(sprite,sp);result[i]=sprite;}
   }return result;
  }
  static string Key(Renderer r){var f=r.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)return null;var m=r.localToWorldMatrix;return (AssetDatabase.GetAssetPath(f.sharedMesh)+f.sharedMesh.name)+"/"+string.Join(",",r.sharedMaterials.Select(v=>v?AssetDatabase.GetAssetPath(v):"null"))+"/"+string.Join(",",Enumerable.Range(0,16).Select(i=>Mathf.RoundToInt(m[i]*10000)));}
  [MenuItem("Dungeon & DLC/11 - Animacoes, trilha e porta do chefao")]
  public static void Install()
  {
   var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.name!="Fortaleza_Azul_Jogavel")throw new Exception("Pare o Play e abra Fortaleza_Azul_Jogavel.");
   string backup=Root+"/Reports/BackupBossPolish/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(scene,backup+"/Cena.unity",true);
   var boss=UnityEngine.Object.FindFirstObjectByType<SornyBoss>();if(!boss)throw new Exception("Chefao ausente.");var animation=boss.GetComponent<SornyBossAnimation>();if(!animation)animation=boss.gameObject.AddComponent<SornyBossAnimation>();animation.boss=boss;animation.caminhada=Frames("Walk");animation.golpe=Frames("Attack");animation.morte=Frames("Death");boss.Corpo.visual.sprite=animation.caminhada[0];
   var old=GameObject.Find("Arena_Porta_Trilha");if(old)UnityEngine.Object.DestroyImmediate(old);var arenaGo=new GameObject("Arena_Porta_Trilha");arenaGo.transform.SetParent(boss.transform.parent,false);var arena=arenaGo.AddComponent<SornyBossArena>();arena.boss=boss;
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonDLC_MegaPack/Prefabs/Models/D04_Portao_Definitivo.prefab");var gate=(GameObject)PrefabUtility.InstantiatePrefab(prefab,arenaGo.transform);gate.name="Portao_da_Arena";gate.transform.position=new Vector3(0,0,56.3f);
   // Fit the existing gate mesh to the four-metre passage and keep all physical parts together.
   var renderers=gate.GetComponentsInChildren<Renderer>();Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);gate.transform.localScale=new Vector3(4.0f/bounds.size.x,3.7f/bounds.size.y,1);bounds=gate.GetComponentsInChildren<Renderer>()[0].bounds;foreach(var r in gate.GetComponentsInChildren<Renderer>())bounds.Encapsulate(r.bounds);gate.transform.position+=new Vector3(-bounds.center.x,-bounds.min.y,56.3f-bounds.center.z);arena.grade=gate.transform;
   if(gate.GetComponentsInChildren<Collider>().Length==0){var collider=gate.AddComponent<BoxCollider>();collider.size=new Vector3(4,3.7f,.3f);collider.center=Vector3.up*1.85f;}
   string musicPath=Root+"/Audio/Lorde_So_Nao_Batalha.wav";var ai=(AudioImporter)AssetImporter.GetAtPath(musicPath);var sample=ai.defaultSampleSettings;sample.loadType=AudioClipLoadType.Streaming;sample.compressionFormat=AudioCompressionFormat.Vorbis;sample.quality=.75f;ai.defaultSampleSettings=sample;ai.forceToMono=false;ai.SaveAndReimport();arena.batalha=AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath);
   int mats=0,textures=0,duplicates=0;var audit=new List<string>();
   foreach(var path in AssetDatabase.FindAssets("t:Material",new[]{"Assets/DungeonDLC_MegaPack/Materials"}).Select(AssetDatabase.GUIDToAssetPath))
   {var mat=AssetDatabase.LoadAssetAtPath<Material>(path);AssetDatabase.CopyAsset(path,backup+"/"+Path.GetFileName(path));
    if(mat.shader&&mat.shader.name=="Standard"){var tex=mat.mainTexture;var color=mat.color;mat.shader=Shader.Find("Universal Render Pipeline/Lit");mat.mainTexture=tex;mat.color=color;}
    foreach(var prop in new[]{"_ScreenSpaceReflections","_EnvironmentReflections","_Smoothness","_Glossiness"})if(mat.HasProperty(prop))mat.SetFloat(prop,0);mat.EnableKeyword("_SCREENSPACEREFLECTIONS_OFF");mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");EditorUtility.SetDirty(mat);mats++;
    if(!mat.shader||mat.shader.name.Contains("InternalError"))audit.Add("MATERIAL COM ERRO: "+path);
   }
   foreach(var path in Directory.GetFiles("Assets/DungeonDLC_MegaPack/Art/Textures","*.png")){var importer=(TextureImporter)AssetImporter.GetAtPath(path);File.Copy(path+".meta",backup+"/"+Path.GetFileName(path)+".meta.txt",true);importer.mipmapEnabled=true;importer.filterMode=FilterMode.Point;importer.anisoLevel=4;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();textures++;}
   var seen=new Dictionary<string,Renderer>();foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(r=>r.gameObject.scene==scene&&r.enabled)){string key=Key(renderer);if(key==null)continue;if(seen.ContainsKey(key)){renderer.enabled=false;duplicates++;audit.Add("RENDERER DUPLICADO OCULTADO: "+renderer.name+" "+renderer.transform.position);}else seen[key]=renderer;if(renderer.sharedMaterials.Any(m=>!m))audit.Add("MATERIAL AUSENTE: "+renderer.name+" "+renderer.transform.position);}
   var grid=UnityEngine.Object.FindFirstObjectByType<SornyNavGrid>();if(grid){var active=gate.activeSelf;gate.SetActive(false);grid.Bake();gate.SetActive(active);}
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   string report="24 quadros do boss: caminhada, ataque e morte.\nTrilha original de batalha em loop: "+arena.batalha.length+"s.\nPortao automatico na entrada da arena; fecha na luta e abre na vitoria.\nMateriais revisados: "+mats+". Texturas com mipmaps: "+textures+". Duplicatas: "+duplicates+".\nBackup: "+backup+"\n"+string.Join("\n",audit);File.WriteAllText(Root+"/Reports/BOSS_POLISH.txt",report);Debug.Log("BOSS_POLISH_INSTALLED_OK "+report);
  }
  public static void BatchInstall(){EditorSceneManager.OpenScene(SornyInstaller.Cena);Install();}
 }
}
