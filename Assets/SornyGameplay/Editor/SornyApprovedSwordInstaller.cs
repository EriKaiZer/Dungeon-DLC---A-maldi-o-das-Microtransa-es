using System;
using System.IO;
using System.Reflection;
using DungeonDLC.Sorny;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class SornyApprovedSwordInstaller
{
 static double next;
 static SornyApprovedSwordInstaller(){EditorApplication.update+=Poll;}
 static void Poll(){
  const string request="Assets/SornyGameplay/Reports/espada-referencia.txt";
  if(EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+2;
  if(!File.Exists(request)||File.ReadAllText(request).Trim()!="pending-approved-sword-v1"||SceneManager.GetActiveScene().path!="Assets/SornyGameplay/Scenes/Fortaleza_Azul_Jogavel.unity")return;
  if(EditorApplication.isPlayingOrWillChangePlaymode){if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();return;}
  try{Apply();File.WriteAllText(request,"done-approved-sword-v1");}catch(Exception e){File.WriteAllText(request,"error: "+e);Debug.LogException(e);}
 }
 public static void Apply(){
  const string path="Assets/SornyGameplay/Art/ReferenciaAprovada/Espada_Referencia_Recortada.png";
  var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
  if(SceneManager.GetActiveScene().path!="Assets/SornyGameplay/Scenes/Fortaleza_Azul_Jogavel.unity")EditorSceneManager.OpenScene("Assets/SornyGameplay/Scenes/Fortaleza_Azul_Jogavel.unity");
  var scene=SceneManager.GetActiveScene();string backup="Assets/SornyGameplay/Reports/BackupEspadaReferencia/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);if(!EditorSceneManager.SaveScene(scene,backup+"/Anterior.unity",true))throw new Exception("Falha no backup");
  var hands=UnityEngine.Object.FindFirstObjectByType<SornyHandEquipment>();hands.espadaEmpunhada=AssetDatabase.LoadAssetAtPath<Sprite>(path);hands.usarEspadaReferencia=true;
  typeof(SornyHandEquipment).GetMethod("AplicarEquipamentos",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hands,null);EditorUtility.SetDirty(hands);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Debug.Log("SORNY_APPROVED_SWORD_APPLIED_OK");
 }
}
