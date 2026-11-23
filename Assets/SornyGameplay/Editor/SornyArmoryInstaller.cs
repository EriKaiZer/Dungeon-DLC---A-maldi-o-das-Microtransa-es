using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace DungeonDLC.Sorny.Editor
{
    [InitializeOnLoad]
    public static class SornyArmoryInstaller
    {
        const string Root="Assets/SornyGameplay";
        const string ScenePath=Root+"/Scenes/Fortaleza_Azul_Jogavel.unity";
        const string Request=Root+"/Reports/empunhadura-inventario.txt";
        const string RequestVersion="pending-armory-v2";
        static double next;
        static SornyArmoryInstaller(){EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+2;
            if(!File.Exists(Request)||File.ReadAllText(Request).Trim()!=RequestVersion||SceneManager.GetActiveScene().path!=ScenePath)return;
            if(EditorApplication.isPlayingOrWillChangePlaymode){if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();return;}
            try{Apply();File.WriteAllText(Request,"done-armory-v2");}catch(Exception e){File.WriteAllText(Request,"error: "+e);Debug.LogException(e);}
        }
        static Texture2D Texture(string name)
        {
            string path=Root+"/Art/Armory/"+name+".png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(!importer)throw new Exception("Arte nao encontrada: "+path);
            importer.textureType=TextureImporterType.Default;importer.isReadable=true;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Sprite SaveSprite(Texture2D texture,Rect rect,Vector2 pivot,float ppu,string name)
        {
            string path=Root+"/Sprites/Armory/"+name+".asset";
            var sprite=Sprite.Create(texture,rect,pivot,ppu,0,SpriteMeshType.FullRect);sprite.name=name;
            var old=AssetDatabase.LoadAssetAtPath<Sprite>(path);if(old){EditorUtility.CopySerialized(sprite,old);UnityEngine.Object.DestroyImmediate(sprite);return old;}
            AssetDatabase.CreateAsset(sprite,path);return sprite;
        }
        static Sprite Trim(Texture2D texture,Rect cell,string name)
        {
            var pixels=texture.GetPixels32();int x0=(int)cell.width,y0=(int)cell.height,x1=0,y1=0;
            for(int y=0;y<cell.height;y++)for(int x=0;x<cell.width;x++)if(pixels[((int)cell.y+y)*texture.width+(int)cell.x+x].a>180){x0=Mathf.Min(x0,x);x1=Mathf.Max(x1,x);y0=Mathf.Min(y0,y);y1=Mathf.Max(y1,y);}
            x0=Mathf.Max(0,x0-4);y0=Mathf.Max(0,y0-4);x1=Mathf.Min((int)cell.width-1,x1+4);y1=Mathf.Min((int)cell.height-1,y1+4);
            return SaveSprite(texture,new Rect(cell.x+x0,cell.y+y0,x1-x0+1,y1-y0+1),new Vector2(.5f,.5f),100,name);
        }
        static Sprite Icon(Sprite source,string name)=>source&&source.texture.isReadable?Trim(source.texture,source.rect,name):source;
        static Sprite[] MerchantFrames(Texture2D texture)
        {
            int width=texture.width/4,height=texture.height/2,min=height,max=0;var pixels=texture.GetPixels32();
            for(int i=0;i<8;i++)for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(pixels[((1-i/4)*height+y)*texture.width+(i%4)*width+x].a>200){min=Mathf.Min(min,y);max=Mathf.Max(max,y);}
            float ppu=Mathf.Max(1,(max-min+1)/2f);var all=new Sprite[8];
            for(int i=0;i<8;i++)all[i]=SaveSprite(texture,new Rect((i%4)*width,(1-i/4)*height,width,height),new Vector2(.5f,(float)min/height),ppu,"Mercador_"+i);
            return all;
        }
        static SornyMerchant Merchant(SornyGameLoop game,Sprite[] frames)
        {
            var owner=game.GetComponent<SornyEconomy>().loja;if(!owner)throw new Exception("Balcao original da loja nao encontrado.");
            var merchant=owner.GetComponent<SornyMerchant>();if(!merchant)merchant=owner.gameObject.AddComponent<SornyMerchant>();
            var child=owner.Find("Mercador_Animado");if(!child){child=new GameObject("Mercador_Animado").transform;child.SetParent(owner,false);}
            child.localPosition=new Vector3(0,0,.7f);child.localRotation=Quaternion.identity;child.localScale=Vector3.one;
            var visual=child.GetComponent<SpriteRenderer>();if(!visual)visual=child.gameObject.AddComponent<SpriteRenderer>();
            visual.sprite=frames[0];visual.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/Crack_Pixel.mat");visual.shadowCastingMode=ShadowCastingMode.Off;
            merchant.visual=visual;merchant.parado=frames.Take(4).ToArray();merchant.compra=frames.Skip(4).ToArray();merchant.somCompra=game.GetComponent<SornyAudioDirector>().moeda;
            var sign=owner.Find("Letreiro Loja");if(sign){sign.localPosition=new Vector3(0,2.4f,.7f);foreach(var label in sign.GetComponentsInChildren<Text>(true))label.text="MERCADOR DO ESTORNO";}
            EditorUtility.SetDirty(merchant);return merchant;
        }
        [MenuItem("Dungeon & DLC/19 - Empunhadura, mercador e inventario")]
        public static void Apply()
        {
            var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath)throw new Exception("Abra a Fortaleza Azul Jogavel.");
            Directory.CreateDirectory(Root+"/Sprites/Armory");AssetDatabase.Refresh();
            string backup=Root+"/Reports/BackupArmory/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);
            if(!EditorSceneManager.SaveScene(scene,backup+"/Estado_do_Editor.unity",true))throw new Exception("Falha ao preservar a cena atual.");
            var game=UnityEngine.Object.FindFirstObjectByType<SornyGameLoop>();var hands=game.espada.GetComponent<SornyHandEquipment>();
            hands.espadaEmpunhada=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CaloteusEspada/Espada_Acesso_Antecipado.png");
            if(!hands.espadaEmpunhada)throw new Exception("Sprite original completo da espada nao encontrado.");
            typeof(SornyHandEquipment).GetMethod("AplicarEquipamentos",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hands,null);
            var frames=MerchantFrames(Texture("Mercador_Estorno_8"));Merchant(game,frames);
            var icons=Texture("Icones_Equipamentos_4");float w=icons.width/2f,h=icons.height/2f;
            var inventory=game.GetComponent<SornyInventoryUI>();if(!inventory)inventory=game.gameObject.AddComponent<SornyInventoryUI>();
            inventory.iconeEspada=Icon(hands.espada[0],"Icone_Espada");inventory.iconeEscudo=Icon(hands.escudo[0],"Icone_Escudo");inventory.iconeAdaga=Icon(hands.adaga[0],"Icone_Adaga");inventory.iconeMaca=Icon(hands.machado[0],"Icone_Maca");
            inventory.iconeArmadura=Trim(icons,new Rect(0,h,w,h),"Icone_Armadura");inventory.iconeBotas=Trim(icons,new Rect(w,h,w,h),"Icone_Botas");inventory.iconePocao=Trim(icons,new Rect(0,0,w,h),"Icone_Pocao");inventory.iconeMoeda=Trim(icons,new Rect(w,0,w,h),"Icone_Moeda");
            EditorUtility.SetDirty(inventory);
            EditorUtility.SetDirty(hands);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Root+"/Reports/EMPUNHADURA_INVENTARIO.txt","Espada e mao usam o desenho original completo, com pose e corte do mesmo conjunto.\nMercador animado: quatro quadros de repouso e quatro de compra.\nTAB/I abre inventario. ESC fecha. E negocia no balcao.\nEspada 60, armadura 45, botas 35 e pocao 30 moedas. Pocao cura ate 25 PV, limite de tres. H usa na exploracao.\nEquipamentos, escolhas e consumiveis integrados ao salvamento.\nBackup: "+backup+"\n");
            Debug.Log("SORNY_ARMORY_V2_APPLIED_OK");
        }
        public static void BatchSwordOnly(){EditorSceneManager.OpenScene(ScenePath);Apply();}
        public static void BatchApply(){EditorSceneManager.OpenScene(ScenePath);Apply();File.WriteAllText(Request,"done-armory-v2");}
    }
}
