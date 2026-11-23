using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace DungeonDLC.Sorny.Editor
{
 [InitializeOnLoad]public static class SornyBossInstaller
 {
  const string Root="Assets/SornyGameplay",Marker=Root+"/Art/Boss/aplicar.txt";static double next;
  static SornyBossInstaller(){EditorApplication.update+=Poll;}
  static void Poll(){if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+2;if(EditorApplication.isCompiling||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Marker)||File.ReadAllText(Marker).Trim()!="pending")return;if(SceneManager.GetActiveScene().name!="Fortaleza_Azul_Jogavel")return;try{Install();File.WriteAllText(Marker,"done");}catch(Exception e){File.WriteAllText(Marker,"error: "+e);Debug.LogException(e);}}
  static GameObject Prefab(string name,Transform parent){var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DungeonDLC_MegaPack/Prefabs/Models/"+name+".prefab");if(!source)throw new Exception(name+" ausente");var go=(GameObject)PrefabUtility.InstantiatePrefab(source,parent);go.transform.localPosition=Vector3.zero;go.transform.localRotation=Quaternion.identity;return go;}
  static Sprite BossSprite()
  {
   string path=Root+"/Art/Boss/Lorde_So_Nao.png";var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Default;ti.isReadable=true;ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;ti.npotScale=TextureImporterNPOTScale.None;ti.maxTextureSize=2048;ti.alphaIsTransparency=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
   var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var px=tex.GetPixels32();int x0=tex.width,y0=tex.height,x1=0,y1=0;for(int y=0;y<tex.height;y++)for(int x=0;x<tex.width;x++)if(px[y*tex.width+x].a>180){x0=Mathf.Min(x0,x);x1=Mathf.Max(x1,x);y0=Mathf.Min(y0,y);y1=Mathf.Max(y1,y);}
   var sprite=Sprite.Create(tex,new Rect(x0,y0,x1-x0+1,y1-y0+1),new Vector2(.5f,0),(y1-y0+1)/3.25f,0,SpriteMeshType.FullRect);sprite.name="Lorde_So_Nao";string sp=Root+"/Sprites/Lorde_So_Nao.asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(sp);if(old){EditorUtility.CopySerialized(sprite,old);UnityEngine.Object.DestroyImmediate(sprite);return old;}AssetDatabase.CreateAsset(sprite,sp);return sprite;
  }
  [MenuItem("Dungeon & DLC/10 - Instalar chefao, baus e loja")]
  public static void Install()
  {
   if(EditorApplication.isPlaying)throw new Exception("Pare o Play antes de instalar.");
   var scene=SceneManager.GetActiveScene();if(scene.name!="Fortaleza_Azul_Jogavel")throw new Exception("Abra Fortaleza_Azul_Jogavel.");var game=UnityEngine.Object.FindFirstObjectByType<SornyGameLoop>();if(!game)throw new Exception("Dungeon ausente");
   Directory.CreateDirectory(Root+"/Reports/BackupBoss");EditorSceneManager.SaveScene(scene,Root+"/Reports/BackupBoss/Cena_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
   var old=GameObject.Find("Boss_Baus_Loja");if(old)UnityEngine.Object.DestroyImmediate(old);
   // The original altar occupies the centre of the throne room. Move it to the side so it cannot trap the boss or block his wind-up.
   foreach(var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))if(t.name=="D01_Altar_Exclusividade"&&t.position.z>58){var p=t.position;p.x=-6;t.position=p;}
   var root=new GameObject("Boss_Baus_Loja").transform;root.SetParent(game.transform,false);
   var economy=game.GetComponent<SornyEconomy>();if(!economy)economy=game.gameObject.AddComponent<SornyEconomy>();
   var positions=new[]{new Vector3(3.6f,.02f,-3),new Vector3(3.8f,.02f,17),new Vector3(-3.7f,.02f,33),new Vector3(3.8f,.02f,49)};int[] gold={70,40,50,40};economy.baus=new SornyChest[positions.Length];
   for(int i=0;i<positions.Length;i++)
   {var chest=new GameObject("Bau_Moedas_"+(i+1));chest.transform.SetParent(root,false);chest.transform.position=positions[i];var c=chest.AddComponent<SornyChest>();c.id="bau_"+(i+1);c.moedas=gold[i];c.fechado=Prefab("C07_Bau_Deluxe",chest.transform);c.aberto=Prefab("C07_Bau_Deluxe_Aberto",chest.transform);c.SetOpen(false);economy.baus[i]=c;}
   var shop=new GameObject("Loja_do_Estorno").transform;shop.SetParent(root,false);shop.position=new Vector3(-3.6f,.02f,-3);Prefab("C09_Mesa",shop);var sack=Prefab("C15_Saco_Moedas",shop);sack.transform.localPosition=new Vector3(0,.88f,0);economy.loja=shop;
   var sign=new GameObject("Letreiro Loja",typeof(RectTransform),typeof(Canvas)).transform;sign.SetParent(shop,false);sign.localPosition=new Vector3(0,1.7f,0);sign.localRotation=Quaternion.Euler(0,180,0);sign.localScale=Vector3.one*.003f;var rect=(RectTransform)sign;rect.sizeDelta=new Vector2(600,150);sign.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
   sign.gameObject.AddComponent<SornyWorldBillboard>();var label=new GameObject("Texto",typeof(RectTransform),typeof(Text));label.transform.SetParent(sign,false);var lr=label.GetComponent<RectTransform>();lr.anchorMin=Vector2.zero;lr.anchorMax=Vector2.one;lr.offsetMin=lr.offsetMax=Vector2.zero;var text=label.GetComponent<Text>();text.font=Resources.Load<SornyMenuTheme>("SornyMenuTheme").fonte;text.fontSize=24;text.alignment=TextAnchor.MiddleCenter;text.color=new Color(1,.75f,.25f);text.text="LOJA DO ESTORNO\nE / NEGOCIAR";text.raycastTarget=false;
   var bossGo=new GameObject("Lorde_So_Nao");bossGo.transform.SetParent(root,false);bossGo.transform.position=new Vector3(0,.04f,65.2f);var cc=bossGo.AddComponent<CharacterController>();cc.height=2.85f;cc.center=Vector3.up*1.45f;cc.radius=.55f;cc.stepOffset=.2f;cc.skinWidth=.025f;
   var visual=new GameObject("Visual").AddComponent<SpriteRenderer>();visual.transform.SetParent(bossGo.transform,false);var sprite=BossSprite();visual.sprite=sprite;visual.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Crack_Pixel.mat");visual.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   var health=bossGo.AddComponent<CrackEnemy>();health.enabled=false;health.nome="Lorde Só-Não";health.vidaMaxima=360;health.visual=visual;health.jogador=game.jogador;health.navegacao=game.GetComponent<SornyNavGrid>();health.caminhada=health.ataque=health.morte=health.ferido=new[]{sprite};
   var boss=bossGo.AddComponent<SornyBoss>();boss.Corpo=health;economy.boss=boss;
   var warning=new GameObject("Aviso_de_ataque").AddComponent<LineRenderer>();warning.transform.SetParent(bossGo.transform,false);warning.useWorldSpace=true;warning.widthMultiplier=.065f;warning.enabled=false;warning.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;string matPath=Root+"/Materials/Aviso_Boss.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!mat){mat=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(mat,matPath);}warning.sharedMaterial=mat;boss.aviso=warning;
   var sound=bossGo.AddComponent<AudioSource>();sound.clip=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/crack_ataque.wav");sound.playOnAwake=false;sound.spatialBlend=1;sound.minDistance=2;sound.maxDistance=15;sound.pitch=.65f;sound.volume=.55f;
   var hint=game.GetComponentsInChildren<Text>(true).FirstOrDefault(t=>t.name=="Controles");if(hint&&!hint.text.Contains("BAÚ"))hint.text+="   |   E BAÚ / LOJA";
   game.GetComponent<SornyNavGrid>().Bake();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
   File.WriteAllText(Root+"/Reports/BOSS_LOJA.txt","Lorde Só-Não: 360 PV; impacto e investida; fase 2 abaixo de 50%.\nDesperta após os cinco cobradores, na última sala.\n4 baús: 70, 40, 50 e 40 moedas.\nLoja na entrada: espada 60 moedas (40 dano), armadura 45 (-25% dano), botas 35 (+20% velocidade).\nCompras e baús integrados ao salvamento.\nBackup da cena: Reports/BackupBoss.\n");Debug.Log("SORNY_BOSS_COMMERCE_INSTALLED_OK");
  }
  public static void BatchInstall(){EditorSceneManager.OpenScene(SornyInstaller.Cena);Install();}
 }
}
