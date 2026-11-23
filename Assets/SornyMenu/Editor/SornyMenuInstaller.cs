using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
namespace DungeonDLC.Sorny.Editor
{
 [InitializeOnLoad] public static class SornyMenuInstaller
 {
  const string Root="Assets/SornyMenu",MenuPath=Root+"/Scenes/Tela_Inicial.unity",GamePath="Assets/SornyGameplay/Scenes/Fortaleza_Azul_Jogavel.unity",Marker=Root+"/instalar.txt";
  static double next;
  static SornyMenuInstaller(){EditorApplication.update+=Poll;}
  static void Poll(){if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+2;if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Marker)||File.ReadAllText(Marker).Trim()!="pending")return;try{Install();File.WriteAllText(Marker,"done");}catch(Exception e){File.WriteAllText(Marker,"error: "+e);Debug.LogException(e);}}
  [MenuItem("Dungeon & DLC/8 - Instalar menu e abertura")]
  public static void Install()
  {
   if(EditorApplication.isPlaying)throw new Exception("Pare o Play para instalar o menu.");
   Directory.CreateDirectory(Root+"/Scenes");Directory.CreateDirectory(Root+"/Resources");Directory.CreateDirectory(Root+"/Reports");AssetDatabase.Refresh();
   foreach(var p in Directory.GetFiles(Root+"/Art","*.png"))
   {var t=AssetImporter.GetAtPath(p) as TextureImporter;t.textureType=p.EndsWith("Menu_Titulo.png")?TextureImporterType.Sprite:TextureImporterType.Default;t.spriteImportMode=SpriteImportMode.Single;t.alphaIsTransparency=true;t.filterMode=FilterMode.Point;t.mipmapEnabled=false;t.wrapMode=TextureWrapMode.Clamp;t.textureCompression=TextureImporterCompression.Uncompressed;t.maxTextureSize=2048;t.SaveAndReimport();}
   string themePath=Root+"/Resources/SornyMenuTheme.asset";var theme=AssetDatabase.LoadAssetAtPath<SornyMenuTheme>(themePath);if(!theme){theme=ScriptableObject.CreateInstance<SornyMenuTheme>();AssetDatabase.CreateAsset(theme,themePath);}
   theme.fundo=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Menu_Fundo.png");theme.titulo=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"/Art/Menu_Titulo.png");theme.fonte=AssetDatabase.LoadAssetAtPath<Font>(Root+"/Fonts/PressStart2P-Regular.ttf");theme.atmosfera=AssetDatabase.LoadAssetAtPath<Shader>(Root+"/Resources/SornyMenuAtmosphere.shader");
   theme.abertura=new[]{"01_Fortaleza_Azul","02_Portao_da_Exclusividade","03_Caloteus_na_Entrada","04_Primeiro_Corredor"}.Select(n=>AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/"+n+".png")).ToArray();
   theme.musica=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SornyGameplay/Audio/Darkest_Child_Kevin_MacLeod.mp3");theme.selecionar=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SornyGameplay/Audio/moeda.wav");theme.confirmar=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SornyGameplay/Audio/espada_ar.wav");
   if(!theme.fundo||!theme.fonte||!theme.titulo||theme.abertura.Any(t=>!t)||!theme.musica||!theme.atmosfera)throw new Exception("Assets do menu incompletos.");if(ShaderUtil.ShaderHasError(theme.atmosfera))throw new Exception("Erro no efeito de atmosfera.");EditorUtility.SetDirty(theme);
   // Preserve unsaved scene contents: generate the new scene additively, then restore the active scene.
   if(Application.isBatchMode&&string.IsNullOrEmpty(SceneManager.GetActiveScene().path))EditorSceneManager.OpenScene(GamePath);
   var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
   new GameObject("Menu e abertura").AddComponent<SornyFrontEnd>().tema=theme;
   var camera=new GameObject("Camera Menu",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;
   EditorSceneManager.SaveScene(scene,MenuPath);EditorSceneManager.CloseScene(scene,true);if(previous.IsValid())SceneManager.SetActiveScene(previous);
   string backup=Root+"/Reports/EditorBuildSettings_antes.asset";if(!File.Exists(backup))File.Copy("ProjectSettings/EditorBuildSettings.asset",backup);
   EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(MenuPath,true),new EditorBuildSettingsScene(GamePath,true)}.Concat(EditorBuildSettings.scenes.Where(s=>s.path!=MenuPath&&s.path!=GamePath)).ToArray();
   EditorSceneManager.playModeStartScene=AssetDatabase.LoadAssetAtPath<SceneAsset>(MenuPath);
   AssetDatabase.SaveAssets();File.WriteAllText(Root+"/Reports/INSTALADO.txt","Menu instalado. Play inicia Tela_Inicial.\nNovo Jogo: abertura de 4 telas e Fortaleza Azul.\nESC/Space pula abertura. ESC na dungeon abre pausa.\nSalvamento automático a cada 10 segundos enquanto vivo, ao pausar e ao sair.\nContinuar restaura vida, posição, moedas, inimigos e itens.\nF10: controles de áudio anteriores continuam disponíveis.\n");Debug.Log("SORNY_MENU_INSTALLED_OK");
  }
  [MenuItem("Dungeon & DLC/9 - Abrir tela inicial")]
  public static void Open(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(MenuPath);}
 }
}
