using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using DungeonDLC.MegaPack.Editor;

namespace DungeonDLC.Sorny.Editor
{
    [InitializeOnLoad]
    public static class SornyCombatBossInstaller
    {
        const string Root="Assets/SornyGameplay";
        const string ScenePath=Root+"/Scenes/Fortaleza_Azul_Jogavel.unity";
        const string Request=Root+"/Reports/armas-pesado-crediton.txt";
        static double next;
        static SornyCombatBossInstaller(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.timeSinceStartup<next||EditorApplication.isCompiling)return;next=EditorApplication.timeSinceStartup+2;
            if(!File.Exists(Request)||File.ReadAllText(Request).Trim()!="pending"||SceneManager.GetActiveScene().path!=ScenePath)return;
            if(EditorApplication.isPlayingOrWillChangePlaymode){if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();return;}
            try{Apply();File.WriteAllText(Request,"done");}catch(Exception e){File.WriteAllText(Request,"error: "+e);Debug.LogException(e);}
        }
        [Serializable]class Pivot{public float x,y;}
        [Serializable]class Cell{public float x,y,width,height;}
        [Serializable]class Frame{public Cell rect_unity_bottom_left;}
        [Serializable]class Sheet{public string id,file;public int width,height;public float pixels_per_unit_starting_point;public Pivot pivot_starting_point;public Frame[] frames;}
        [Serializable]class Map{public Sheet[] sheets;}
        static Texture2D Texture(string path)
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(path);if(!ti)throw new Exception("Textura ausente: "+path);
            ti.textureType=TextureImporterType.Default;ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;ti.isReadable=true;ti.alphaIsTransparency=true;ti.npotScale=TextureImporterNPOTScale.None;ti.maxTextureSize=2048;ti.wrapMode=TextureWrapMode.Clamp;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Sprite Sprite(Texture2D texture,Rect rect,Vector2 pivot,float ppu,string folder,string name)
        {
            string root=Root+"/Sprites/"+folder;Directory.CreateDirectory(root);
            string path=root+"/"+name+".asset";var sprite=UnityEngine.Sprite.Create(texture,rect,pivot,ppu,0,SpriteMeshType.FullRect);sprite.name=name;
            var old=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(old){EditorUtility.CopySerialized(sprite,old);UnityEngine.Object.DestroyImmediate(sprite);return old;}
            AssetDatabase.CreateAsset(sprite,path);return sprite;
        }
        static Sprite Weapon(string name)
        {
            var tex=Texture(Root+"/Art/ArmasFrontalV2/"+name+".png");
            return Sprite(tex,new Rect(0,0,tex.width,tex.height),new Vector2(1,0),100,"ArmasFrontalV2",name);
        }
        static Dictionary<string,Sprite[]> Frames()
        {
            const string folder=Root+"/Art/CreditonFinal/";
            var map=JsonUtility.FromJson<Map>(File.ReadAllText(folder+"animacoes_e_recortes.json"));
            var all=new Dictionary<string,Sprite[]>();
            foreach(var sheet in map.sheets)
            {
                var texture=Texture(folder+Path.GetFileName(sheet.file));
                if(texture.width!=sheet.width||texture.height!=sheet.height)throw new Exception("Dimensões alteradas: "+sheet.id);
                var frames=new Sprite[sheet.frames.Length];
                for(int i=0;i<frames.Length;i++)
                {
                    var r=sheet.frames[i].rect_unity_bottom_left;
                    frames[i]=Sprite(texture,new Rect(r.x,r.y,r.width,r.height),new Vector2(sheet.pivot_starting_point.x,sheet.pivot_starting_point.y),sheet.pixels_per_unit_starting_point,"CreditonFinal",sheet.id+"_"+i);
                }
                all[sheet.id]=frames;
            }
            return all;
        }
        static GameObject Group(string name,Transform parent){var go=new GameObject(name);go.transform.SetParent(parent,false);return go;}
        static Transform Gate(Transform parent)
        {
            var go=DDLCPackBuilder.Spawn("D04_Portao_Definitivo",parent,new Vector3(0,0,122));go.name="Portao_Arena_Crediton";
            var renderers=go.GetComponentsInChildren<Renderer>();Bounds b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            go.transform.localScale=new Vector3(3.8f/b.size.x,3.55f/b.size.y,1);
            b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);go.transform.position+=new Vector3(-b.center.x,-b.min.y,122-b.center.z);
            var barrier=go.AddComponent<BoxCollider>();barrier.center=go.transform.InverseTransformPoint(new Vector3(0,1.8f,122));barrier.size=new Vector3(3.85f/go.transform.lossyScale.x,3.6f/go.transform.lossyScale.y,.4f/go.transform.lossyScale.z);
            return go.transform;
        }
        static LineRenderer Line(Transform parent,string name,float width)
        {
            var line=Group(name,parent).AddComponent<LineRenderer>();line.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Aviso_Boss.mat");line.widthMultiplier=width;line.useWorldSpace=true;line.enabled=false;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;
        }
        static SornyCrediton Crediton(SornyGameLoop game,Dictionary<string,Sprite[]> frames)
        {
            var boss=UnityEngine.Object.FindFirstObjectByType<SornyCrediton>();
            if(!boss)
            {
                var parent=game.transform.Find("Guardioes_do_Acervo");if(!parent)throw new Exception("Grupo dos guardiões ausente.");
                var go=Group("Crediton_Deus_dos_Juros",parent);go.transform.position=new Vector3(0,.04f,129);
                var body=go.AddComponent<CharacterController>();body.height=2.5f;body.center=Vector3.up*1.28f;body.radius=.55f;body.skinWidth=.025f;body.stepOffset=.2f;
                var visual=Group("Sprite_Animado",go.transform).AddComponent<SpriteRenderer>();visual.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Crack_Pixel.mat");visual.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                var health=go.AddComponent<CrackEnemy>();health.enabled=false;health.nome="Crediton — o Deus dos Juros";health.vidaMaxima=620;health.nivel=2;health.dano=24;health.jogador=game.jogador;health.navegacao=game.GetComponent<SornyNavGrid>();health.visual=visual;
                boss=go.AddComponent<SornyCrediton>();boss.Corpo=health;boss.aviso=Line(go.transform,"Aviso_de_Cobranca",.07f);boss.corrente=Line(go.transform,"Corrente_Execucao_Fiscal",.055f);boss.corrente.startColor=boss.corrente.endColor=new Color(.85f,.75f,.45f);
                boss.grade=Gate(GameObject.Find("Acervo_Subterraneo_Expansao").transform);boss.origemGrade=boss.grade.localPosition;boss.grade.localPosition+=Vector3.up*4.2f;
                go.AddComponent<SornyCreditonAnimation>();
            }
            var anim=boss.GetComponent<SornyCreditonAnimation>();anim.boss=boss;
            anim.parado=frames["01_idle_e_caminhada"].Take(4).ToArray();anim.caminhada=frames["01_idle_e_caminhada"].Skip(4).ToArray();anim.combo=frames["02_combo_cobranca_dupla"];
            anim.hit=frames["03_hit_e_atordoamento"].Take(4).ToArray();anim.atordoado=frames["03_hit_e_atordoamento"].Skip(4).ToArray();anim.correntes=frames["04_especial_execucao_fiscal"];anim.giro=frames["05_especial_juros_rotativos"];anim.salto=frames["06_especial_quebra_de_banca"];anim.fase2=frames["07_transicao_fase2_calote_divino"];anim.morte=frames["08_morte_falencia"];
            boss.Corpo.caminhada=anim.parado;boss.Corpo.ataque=anim.combo;boss.Corpo.ferido=anim.hit;boss.Corpo.morte=anim.morte;boss.Corpo.visual.sprite=anim.parado[0];
            boss.grito=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/crack_ataque.wav");boss.laminas=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/CombateV2/Espada_Ar_0.wav");boss.impactoChao=AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/CombateV2/Espada_Acerto_3.wav");
            EditorUtility.SetDirty(anim);EditorUtility.SetDirty(boss.Corpo);EditorUtility.SetDirty(boss);
            return boss;
        }
        static void Shape(Transform parent,string name,PrimitiveType type,Vector3 p,Vector3 scale,Material material)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        static SornyPickup Drop(SornyGameLoop game)
        {
            var existing=game.GetComponent<SornyProgression>().armaDoChefe;if(existing)return existing;
            var root=Group("Arma_Chefe_Maca_Chave",game.GetComponent<SornyEconomy>().boss.transform.parent);root.transform.position=new Vector3(0,.08f,65);
            var pickup=root.AddComponent<SornyPickup>();pickup.tipo=SornyPickup.Tipo.ArmaChefe;pickup.titulo="Maça-Chave do Só-Não";
            var visual=Group("Maca_Chave_Visual",root.transform).transform;visual.localPosition=Vector3.up*.25f;
            var gold=AssetDatabase.LoadAssetAtPath<Material>("Assets/DungeonDLC_MegaPack/Materials/Gold.mat");
            Shape(visual,"Cabo",PrimitiveType.Cylinder,new Vector3(0,.3f,0),new Vector3(.065f,.3f,.065f),gold);
            Shape(visual,"Pomo",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.12f,gold);
            Shape(visual,"Cabeca_Chave",PrimitiveType.Cube,new Vector3(.04f,.73f,0),new Vector3(.13f,.36f,.14f),gold);
            for(int i=0;i<3;i++)Shape(visual,"Dente_"+i,PrimitiveType.Cube,new Vector3(-.09f,.59f+i*.14f,0),new Vector3(.25f,.075f,.14f),gold);
            pickup.visual=visual;var collider=root.AddComponent<SphereCollider>();collider.isTrigger=true;collider.radius=.45f;collider.center=Vector3.up*.5f;
            var lamp=Group("Brilho_Recompensa",root.transform).AddComponent<Light>();lamp.transform.localPosition=Vector3.up*.55f;lamp.color=new Color(1,.7f,.2f);lamp.intensity=1.2f;lamp.range=2.8f;
            root.SetActive(false);return pickup;
        }
        [MenuItem("Dungeon & DLC/17 - Armas retas, ataque pesado e Crediton")]
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlaying||scene.path!=ScenePath)throw new Exception("Abra Fortaleza_Azul_Jogavel fora do Play.");
            string backup=Root+"/Reports/BackupPesadoCrediton/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(scene,backup+"/Fortaleza_Azul_Jogavel.unity",true);
            var game=UnityEngine.Object.FindFirstObjectByType<SornyGameLoop>();var hands=game.espada.GetComponent<SornyHandEquipment>();
            Undo.RecordObject(hands,"Corrigir perspectiva e eixo das armas");
            var layers=Texture(Root+"/Art/ArmasFrontalV2/Armas_Retas_Camadas.png");int cell=layers.width/2;
            hands.espada=new[]{Sprite(layers,new Rect(0,0,cell,layers.height),new Vector2(1,0),100,"ArmasFrontalV2","Espada_Reta_Camada")};
            var dagger=Sprite(layers,new Rect(cell,0,layers.width-cell,layers.height),new Vector2(1,0),100,"ArmasFrontalV2","Adaga_Reta_Camada");hands.adaga=new[]{dagger,dagger,dagger,dagger};
            hands.machado=new[]{Weapon("Maca_Chave_SoNao")};hands.maoEmCamada=Weapon("Mao_Direita_Camada");
            string materialPath=Root+"/Materials/ArmasPixelUI.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);if(!material){material=new Material(Shader.Find("DungeonDLC/Armas Pixel UI"));AssetDatabase.CreateAsset(material,materialPath);}hands.materialArmasUI=material;
            hands.maoDireita=SornyHandEquipment.ArmaDireita.Espada;hands.AtualizarMelhoria(false);EditorUtility.SetDirty(hands);
            foreach(var chest in game.GetComponent<SornyEconomy>().baus){chest.contemMachado=false;EditorUtility.SetDirty(chest);}
            foreach(var text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                if(text.name=="Controles"){text.text="WASD MOVER | CLIQUE: LEVE / SEGURE E SOLTE: PESADO | DIR DEFENDE | 1 ESPADA / 2 ADAGA / 6 MAÇA | E USAR";EditorUtility.SetDirty(text);}
                if(text.transform.parent&&text.transform.parent.name=="Arma_do_Tesouro"){text.text="TESOUROS DO ACERVO\nMOEDAS PARA EQUIPAMENTOS";EditorUtility.SetDirty(text);}
            }
            var boss=Crediton(game,Frames());var progress=game.GetComponent<SornyProgression>();progress.crediton=boss;progress.armaDoChefe=Drop(game);EditorUtility.SetDirty(progress);
            var arena=UnityEngine.Object.FindFirstObjectByType<SornyBossArena>();arena.crediton=boss;EditorUtility.SetDirty(arena);
            var audio=game.GetComponent<SornyAudioDirector>();audio.acertosMachado=Enumerable.Range(0,3).Select(i=>AssetDatabase.LoadAssetAtPath<AudioClip>(Root+"/Audio/CombateV2/Espada_Acerto_"+i+".wav")).ToArray();EditorUtility.SetDirty(audio);
            var gates=new[]{arena.grade.gameObject,progress.portaAcervo.gameObject,progress.portaMasmorra2.gameObject,boss.grade.gameObject};var active=gates.Select(g=>g.activeSelf).ToArray();foreach(var gate in gates)gate.SetActive(false);try{game.GetComponent<SornyNavGrid>().Bake();}finally{for(int i=0;i<gates.Length;i++)gates[i].SetActive(active[i]);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(Root+"/Reports/PESADO_CREDITON.txt","Espada com eixo reto, adaga e maça em primeira pessoa. A maça-chave é a arma de Só-Não e cai ao derrotá-lo; recolher com E e equipar em 6. Baús preservam moedas.\nClique e solte rapidamente: golpe leve. Segure e solte: pesado; até 1,9x dano, pausa de impacto maior, recuperação mais lenta e barra de carga.\nCrediton: 620 PV, segundo chefe, 64 quadros originais e 10 animações. Cobrança dupla, corrente em linha, giro em área, salto/impacto, fase 2 e atordoamento. Porta e trilha integradas; salvamento compatível.\nPNGs originais de Crediton_BossFinal_Sprites.zip preservados. Recortes e pivôs importados do JSON.\nBackup: "+backup);
            Debug.Log("SORNY_HEAVY_CREDITON_APPLIED_OK");
        }
        public static void BatchApply(){EditorSceneManager.OpenScene(ScenePath);Apply();}
    }
}
