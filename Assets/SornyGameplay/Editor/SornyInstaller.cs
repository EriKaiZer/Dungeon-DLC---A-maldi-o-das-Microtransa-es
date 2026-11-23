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
    public static class SornyInstaller
    {
        public const string Root="Assets/SornyGameplay";
        public const string Cena=Root+"/Scenes/Fortaleza_Azul_Jogavel.unity";
        static readonly Color Gold=new Color(.76f,.63f,.35f),Cream=new Color(.88f,.85f,.73f),Dark=new Color(.035f,.045f,.065f,.92f);
        static Material spriteMaterial;
        static void Folders(){foreach(var dir in new[]{"Scenes","Sprites","Materials","Prefabs","Reports"})Directory.CreateDirectory(Root+"/"+dir);AssetDatabase.Refresh();}
        static T Add<T>(GameObject go)where T:Component=>go.GetComponent<T>()??go.AddComponent<T>();
        static Sprite[] Cortar(string file,string prefix,int cols,int rows,int count)
        {
            string path=Root+"/Art/Crack/"+file;var ti=(TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType=TextureImporterType.Default;ti.isReadable=true;ti.npotScale=TextureImporterNPOTScale.None;ti.maxTextureSize=2048;
            ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.wrapMode=TextureWrapMode.Clamp;ti.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();int cellW=texture.width/cols,cellH=texture.height/rows;
            var sprites=new Sprite[count];
            for(int i=0;i<count;i++)
            {
                int left=(i%cols)*cellW,bottom=(rows-1-i/cols)*cellH,frameH=cellH;
                // This death sheet has an empty separator at image Y=576, not 512.
                // Keep the original pixels-per-unit while excluding the neighbouring row.
                if(prefix=="Crack_Morte") { int split=texture.height*9/16;bottom=i<cols?texture.height-split:0;frameH=i<cols?split:texture.height-split; }
                int x0=cellW,y0=frameH,x1=-1,y1=-1;
                // Crop metadata only: preserve every source PNG. Ignore translucent halos.
                for(int y=0;y<frameH;y++)for(int x=0;x<cellW;x++)if(pixels[(bottom+y)*texture.width+left+x].a>160)
                {x0=Mathf.Min(x0,x);y0=Mathf.Min(y0,y);x1=Mathf.Max(x1,x);y1=Mathf.Max(y1,y);}
                if(x1<x0)throw new Exception("Quadro vazio: "+file+" #"+i);
                int pad=2;x0=Mathf.Max(0,x0-pad);y0=Mathf.Max(0,y0-pad);x1=Mathf.Min(cellW-1,x1+pad);y1=Mathf.Min(frameH-1,y1+pad);
                float pivotX=Mathf.Clamp01((cellW*.5f-x0)/(x1-x0+1));
                var sprite=Sprite.Create(texture,new Rect(left+x0,bottom+y0,x1-x0+1,y1-y0+1),new Vector2(pivotX,0),cellH/2.05f,0,SpriteMeshType.FullRect);
                sprite.name=prefix+"_"+(i+1).ToString("00");string sp=Root+"/Sprites/"+sprite.name+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Sprite>(sp);
                if(existing){EditorUtility.CopySerialized(sprite,existing);UnityEngine.Object.DestroyImmediate(sprite);sprites[i]=existing;}
                else{AssetDatabase.CreateAsset(sprite,sp);sprites[i]=sprite;}
            }
            return sprites;
        }
        [MenuItem("Dungeon & DLC/6 - Corrigir recorte de morte do Crack")]
        public static void CorrigirMorte()
        {
            var sprites=Cortar("03_Morte_6_quadros.png","Crack_Morte",3,2,6);
            for(int i=0;i<sprites.Length;i++)
            {
                var r=sprites[i].rect;
                if(i>=3&&r.yMax>448)throw new Exception("Recorte invadiu a linha vizinha.");
                Debug.Log("CRACK_MORTE_CORRIGIDA "+i+" "+r);
            }
            AssetDatabase.SaveAssets();
        }
        static Material SpriteMaterial()
        {
            string p=Root+"/Materials/Crack_Pixel.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(p);
            if(!mat){mat=new Material(Shader.Find("DungeonDLC/Crack Pixel"));AssetDatabase.CreateAsset(mat,p);}
            mat.shader=Shader.Find("DungeonDLC/Crack Pixel");
            if(mat.HasProperty("_AlphaClip"))mat.SetFloat("_AlphaClip",1);if(mat.HasProperty("_Cutoff"))mat.SetFloat("_Cutoff",.48f);
            if(mat.HasProperty("_Cull"))mat.SetFloat("_Cull",0);mat.EnableKeyword("_ALPHATEST_ON");mat.SetOverrideTag("RenderType","TransparentCutout");mat.renderQueue=2450;mat.color=Color.white;return mat;
        }
        static GameObject Image(string name,Transform parent,Vector2 pos,Vector2 size,Color color,Vector2 anchor)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);
            var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=anchor;rt.pivot=anchor;rt.anchoredPosition=pos;rt.sizeDelta=size;var im=go.GetComponent<Image>();im.color=color;im.raycastTarget=false;return go;
        }
        static Text Label(string name,Transform parent,string text,Vector2 pos,Vector2 size,int fontSize,TextAnchor align,Vector2 anchor)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));go.transform.SetParent(parent,false);
            var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=anchor;rt.pivot=anchor;rt.anchoredPosition=pos;rt.sizeDelta=size;
            var t=go.GetComponent<Text>();t.text=text;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=fontSize;t.color=Cream;t.alignment=align;t.raycastTarget=false;
            return t;
        }
        static Image Bar(Transform parent,string name,Vector2 p,Vector2 size,Color color,Vector2 anchor)
        {
            Image(name+"_Moldura",parent,p,size+Vector2.one*4,Gold,anchor);
            Image(name+"_Fundo",parent,p+new Vector2(2,2),size,new Color(.15f,.035f,.03f),anchor);
            var im=Image(name+"_Vida",parent,p+new Vector2(2,2),size,color,anchor).GetComponent<Image>();im.type=UnityEngine.UI.Image.Type.Filled;im.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;im.fillOrigin=0;return im;
        }
        static void HUD(SornyGameLoop game)
        {
            var hud=new GameObject("HUD_Vida_Sorny",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));hud.transform.SetParent(game.transform,false);
            var canvas=hud.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
            var scaler=hud.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var panel=Image("Caloteus_Vida",hud.transform,new Vector2(24,24),new Vector2(300,86),Dark,Vector2.zero).transform;
            Label("Nome",panel,"CALOTEUS",new Vector2(14,-8),new Vector2(150,24),17,TextAnchor.MiddleLeft,new Vector2(0,1)).color=Gold;
            game.vidaTexto=Label("PV",panel,"100 / 100",new Vector2(-14,-8),new Vector2(110,24),17,TextAnchor.MiddleRight,Vector2.one);
            game.vidaPreenchimento=Bar(panel,"Vida",new Vector2(14,15),new Vector2(268,23),new Color(.65f,.1f,.075f),Vector2.zero);
            game.objetivoTexto=Label("Objetivo",hud.transform,"COBRADORES 0 / 5",new Vector2(0,-18),new Vector2(800,28),16,TextAnchor.MiddleCenter,new Vector2(.5f,1));
            game.avisoTexto=Label("Aviso",hud.transform,"",new Vector2(0,-54),new Vector2(950,34),22,TextAnchor.MiddleCenter,new Vector2(.5f,1));game.avisoTexto.color=Gold;
            game.painelInimigo=Image("Alvo",hud.transform,new Vector2(0,-105),new Vector2(340,62),Dark,new Vector2(.5f,1));
            game.inimigoNome=Label("Nome",game.painelInimigo.transform,"Crack Bandido Culto",new Vector2(0,-3),new Vector2(330,25),17,TextAnchor.MiddleCenter,new Vector2(.5f,1));
            game.inimigoPreenchimento=Bar(game.painelInimigo.transform,"Vida",new Vector2(12,9),new Vector2(312,14),new Color(.62f,.15f,.10f),Vector2.zero);
            game.dicaTexto=Label("Interacao",hud.transform,"",new Vector2(0,122),new Vector2(900,32),19,TextAnchor.MiddleCenter,new Vector2(.5f,0));game.dicaTexto.color=Gold;
            Label("Controles",hud.transform,"WASD  MOVER    |    MOUSE  OLHAR    |    CLIQUE  ESPADA    |    E  COLETAR",new Vector2(0,12),new Vector2(800,22),13,TextAnchor.MiddleCenter,new Vector2(.5f,0));
            Label("Mira",hud.transform,"+",Vector2.zero,new Vector2(24,24),18,TextAnchor.MiddleCenter,new Vector2(.5f,.5f));
            game.danoTela=Image("Dano",hud.transform,Vector2.zero,Vector2.zero,Color.clear,Vector2.zero).GetComponent<Image>();var damageRt=game.danoTela.rectTransform;damageRt.anchorMax=Vector2.one;damageRt.offsetMin=damageRt.offsetMax=Vector2.zero;
            game.painelFim=Image("Fim",hud.transform,Vector2.zero,Vector2.zero,new Color(.015f,.02f,.035f,.95f),Vector2.zero);var endRt=(RectTransform)game.painelFim.transform;endRt.anchorMax=Vector2.one;endRt.offsetMin=endRt.offsetMax=Vector2.zero;
            game.fimTexto=Label("Mensagem",game.painelFim.transform,"",Vector2.zero,new Vector2(1000,330),30,TextAnchor.MiddleCenter,new Vector2(.5f,.5f));game.fimTexto.color=Gold;game.painelFim.SetActive(false);game.painelInimigo.SetActive(false);
        }
        static void Pickup(Transform parent,SornyPickup.Tipo tipo,Vector3 p,int valor,string name)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=p;var item=go.AddComponent<SornyPickup>();item.tipo=tipo;item.valor=valor;item.titulo=name;
            string model=tipo==SornyPickup.Tipo.Moeda?"C16_Token_Premium":"C17_Cristal_Licenca";
            var visual=DDLCPackBuilder.Spawn(model,go.transform,new Vector3(0,.35f,0));visual.transform.localScale=Vector3.one*(tipo==SornyPickup.Tipo.Moeda?1.2f:.5f);item.visual=visual.transform;
            foreach(var c in visual.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(c);
            if(tipo!=SornyPickup.Tipo.Moeda)
            {
                var mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));Color col=tipo==SornyPickup.Tipo.Cura?new Color(.15f,.85f,.4f):new Color(.15f,.65f,1);
                mat.color=col;mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",col*.8f);
                string mp=Root+"/Materials/"+tipo+".mat";var exists=AssetDatabase.LoadAssetAtPath<Material>(mp);if(exists){UnityEngine.Object.DestroyImmediate(mat);mat=exists;}else AssetDatabase.CreateAsset(mat,mp);
                foreach(var r in visual.GetComponentsInChildren<Renderer>())r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();
            }
            var sphere=go.AddComponent<SphereCollider>();sphere.isTrigger=true;sphere.radius=.5f;sphere.center=Vector3.up*.55f;
        }
        static void Decoracao(Transform parent)
        {
            for(int room=0;room<5;room++)
            {
                float z=room*16,hw=room==4?10:6;
                for(int side=-1;side<=1;side+=2)
                {
                    DDLCPackBuilder.Spawn("M_Pilar",parent,new Vector3(side*(hw-.6f),0,z-4.8f));
                    DDLCPackBuilder.Spawn("B03_Teia",parent,new Vector3(side*(hw-.18f),1.5f,z+4.4f),side<0?-90:90);
                    DDLCPackBuilder.Spawn("B01_Musgo",parent,new Vector3(side*(hw-.18f),.15f,z-3.8f),side<0?-90:90);
                }
                int[] signs={6,5,9,22,14};DDLCPackBuilder.Spawn("P"+signs[room].ToString("00"),parent,new Vector3(-hw+.2f,1.25f,z+1.7f),-90);
            }
            DDLCPackBuilder.Spawn("C12_Pergaminho",parent,new Vector3(-4,.91f,.15f),15);
            DDLCPackBuilder.Spawn("C13_Vela",parent,new Vector3(-3.35f,.91f,0));
            DDLCPackBuilder.Spawn("C11_Livro_Remaster",parent,new Vector3(-3.7f,.91f,-.22f),-15);
            DDLCPackBuilder.Spawn("P19",parent,new Vector3(5.78f,1.25f,18),90);
            DDLCPackBuilder.Spawn("C09_Mesa",parent,new Vector3(4,0,34));
            for(int i=0;i<3;i++)DDLCPackBuilder.Spawn("C11_Livro_Remaster",parent,new Vector3(3.5f+i*.38f,.90f,34),i*15);
            DDLCPackBuilder.Spawn("P11",parent,new Vector3(5.78f,1.25f,35.5f),90);
            DDLCPackBuilder.Spawn("C02_Barril_Conteudo_Cortado",parent,new Vector3(-4.5f,0,35));
            DDLCPackBuilder.Spawn("C04_Caixa_Cordas",parent,new Vector3(-3.4f,0,36));
            DDLCPackBuilder.Spawn("C18_Corrente",parent,new Vector3(-5.6f,.8f,50));
            DDLCPackBuilder.Spawn("C18_Corrente",parent,new Vector3(-5.6f,.8f,51));
            DDLCPackBuilder.Spawn("P10",parent,new Vector3(5.78f,1.25f,51),90);
            DDLCPackBuilder.Spawn("C06_Pedestal_Premium",parent,new Vector3(0,0,67.3f));
            DDLCPackBuilder.Spawn("P23",parent,new Vector3(-9.78f,1.25f,66),-90);
        }
        [MenuItem("Dungeon & DLC/5 - Instalar Crack, combate e vida")]
        public static void Instalar()
        {
            if(EditorApplication.isPlaying)throw new Exception("Saia do Play antes de instalar.");Folders();
            var source=SceneManager.GetActiveScene();if(!source.IsValid()||!source.isLoaded)throw new Exception("Abra a Fortaleza Azul primeiro.");
            string backup=Root+"/Scenes/Backup_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
            EditorSceneManager.SaveScene(source,backup,true);
            if(source.path!=Cena){EditorSceneManager.SaveScene(source,Cena,true);source=EditorSceneManager.OpenScene(Cena,OpenSceneMode.Single);}
            var movement=UnityEngine.Object.FindFirstObjectByType<MovimentoGrade>();if(!movement)throw new Exception("Jogador com MovimentoGrade não encontrado na cena.");
            var sword=UnityEngine.Object.FindFirstObjectByType<Caloteus.EspadaFluida>();if(!sword)throw new Exception("EspadaFluida não encontrada.");
            var old=GameObject.Find("Sorny_Gameplay");if(old)UnityEngine.Object.DestroyImmediate(old);
            var walk=Cortar("01_Caminhada_8_quadros.png","Crack_Caminhada",4,2,8);
            var attack=Cortar("02_Ataque_6_quadros.png","Crack_Ataque",3,2,6);
            var death=Cortar("03_Morte_6_quadros.png","Crack_Morte",3,2,6);
            var hurt=Cortar("04_Dano_4_quadros.png","Crack_Dano",2,2,4);spriteMaterial=SpriteMaterial();
            var root=new GameObject("Sorny_Gameplay");var game=root.AddComponent<SornyGameLoop>();
            var life=Add<SornyPlayerLife>(movement.gameObject);life.vidaMaxima=100;game.jogador=life;game.movimento=movement;game.espada=sword;
            Add<SornySwordDamage>(sword.gameObject);movement.bloquearControles=false;sword.bloquearAtaque=false;
            movement.transform.position=new Vector3(0,.06f,-4);movement.transform.rotation=Quaternion.identity;
            var playerCam=movement.GetComponentInChildren<Camera>(true);playerCam.gameObject.SetActive(true);playerCam.enabled=true;playerCam.tag="MainCamera";
            foreach(var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(cam!=playerCam){cam.enabled=false;var listener=cam.GetComponent<AudioListener>();if(listener)listener.enabled=false;}
            var activeListener=Add<AudioListener>(playerCam.gameObject);activeListener.enabled=true;
            var staticEnemies=GameObject.Find("Sprites_Visuais_Sem_Combate");if(staticEnemies)staticEnemies.SetActive(false);
            var decoration=new GameObject("Sorny_Decoracao_Extra").transform;decoration.SetParent(root.transform,false);Decoracao(decoration);
            var nav=root.AddComponent<SornyNavGrid>();nav.Bake();
            var enemies=new GameObject("Crack_Encontros").transform;enemies.SetParent(root.transform,false);
            var positions=new[]{new Vector3(-1.8f,.04f,15),new Vector3(2.7f,.04f,19),new Vector3(1.8f,.04f,35),new Vector3(1.9f,.04f,49),new Vector3(-2.7f,.04f,64)};
            for(int i=0;i<positions.Length;i++)
            {
                var go=new GameObject("Crack_Bandido_Culto_"+(i+1));go.transform.SetParent(enemies,false);go.transform.position=positions[i];
                var controller=go.AddComponent<CharacterController>();controller.height=1.75f;controller.center=Vector3.up*.91f;controller.radius=.28f;controller.skinWidth=.025f;controller.stepOffset=.2f;
                var visual=new GameObject("Sprite_Animado");visual.transform.SetParent(go.transform,false);var renderer=visual.AddComponent<SpriteRenderer>();renderer.sprite=walk[0];renderer.sharedMaterial=spriteMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                var enemy=go.AddComponent<CrackEnemy>();enemy.visual=renderer;enemy.caminhada=walk;enemy.ataque=attack;enemy.morte=death;enemy.ferido=hurt;enemy.jogador=life;enemy.navegacao=nav;
                if(i==0)PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/Crack_Bandido_Culto.prefab");
            }
            var pickups=new GameObject("Itens_Recuperacao_e_Taxas").transform;pickups.SetParent(root.transform,false);
            Pickup(pickups,SornyPickup.Tipo.Cura,new Vector3(-3,0,3.2f),30,"Estorno Vital");
            Pickup(pickups,SornyPickup.Tipo.Cura,new Vector3(3,0,29),30,"Estorno Vital");
            Pickup(pickups,SornyPickup.Tipo.Cura,new Vector3(-2.6f,0,51),30,"Estorno Vital");
            Pickup(pickups,SornyPickup.Tipo.Cura,new Vector3(4,0,62),30,"Estorno Vital");
            foreach(var p in new[]{new Vector3(2.4f,0,2),new Vector3(-3,0,18),new Vector3(2.6f,0,33),new Vector3(-2,0,49),new Vector3(4,0,65)})Pickup(pickups,SornyPickup.Tipo.Moeda,p,5,"Taxa Recuperada");
            Pickup(pickups,SornyPickup.Tipo.Fragmento,new Vector3(0,1.3f,67.3f),1,"Fragmento do Jogo Completo");
            HUD(game);EditorSceneManager.MarkSceneDirty(source);EditorSceneManager.SaveScene(source,Cena);AssetDatabase.SaveAssets();
            Selection.activeGameObject=root;File.WriteAllText(Root+"/Reports/INSTALACAO.txt","Cena: "+Cena+"\nBackup: "+backup+"\n24 quadros, 5 inimigos, 4 curas, 5 moedas, 1 objetivo.\nVida jogador 100. Vida inimigo 70. Espada 25. Dano inimigo 12.\nCélulas livres: "+nav.livres.Count(b=>b)+"\n");
            Debug.Log("SORNY_INSTALADO: Crack, combate, vida, decoração e objetivo prontos em "+Cena);
        }
        public static void BatchBuild()
        {
            EditorSceneManager.OpenScene("Assets/DungeonDLC_MegaPack/Scenes/Fortaleza_Azul_Demo.unity");Instalar();
            string[] args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-sornyPackage");
            if(i>=0)AssetDatabase.ExportPackage(Root,args[i+1],ExportPackageOptions.Recurse);
        }
    }
}
