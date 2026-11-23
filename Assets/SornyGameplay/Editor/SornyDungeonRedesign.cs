using System;
using System.IO;
using System.Linq;
using DungeonDLC.Sorny;
using DungeonDLC.MegaPack.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class SornyDungeonRedesign
{
    const string Scene="Assets/SornyGameplay/Scenes/Fortaleza_Azul_Jogavel.unity";
    const string Request="Assets/SornyGameplay/Reports/exploracao-livre.txt";
    static double next;
    static SornyDungeonRedesign(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+2;
        if(!File.Exists(Request)||File.ReadAllText(Request).Trim()!="pending-exploration-v1"||SceneManager.GetActiveScene().path!=Scene)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();return;}
        try{Apply();File.WriteAllText(Request,"done-exploration-v1");}catch(Exception e){File.WriteAllText(Request,"error: "+e);Debug.LogException(e);}
    }
    static Transform Group(Transform parent,string name)
    {var t=parent.Find(name);if(!t){t=new GameObject(name).transform;t.SetParent(parent,false);}return t;}
    static GameObject Prop(Transform parent,string name,string prefab,Vector3 position,float yaw=0)
    {
        var old=parent.Find(name);var go=old?old.gameObject:DDLCPackBuilder.Spawn(prefab,parent,Vector3.zero,yaw);
        go.name=name;go.transform.position=position;go.transform.rotation=Quaternion.Euler(0,yaw,0);go.SetActive(true);return go;
    }
    static void LightAt(Transform parent,string name,Vector3 position,Color color,float intensity,float range)
    {var t=Group(parent,name);t.position=position;var light=t.GetComponent<Light>();if(!light)light=t.gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=range;light.shadows=LightShadows.None;}
    static void Torch(Transform parent,string name,Vector3 position,float yaw,Color color)
    {Prop(parent,name,"C01_Tocha_Suporte",position,yaw);LightAt(parent,name+"_Luz",position+Vector3.up*.8f,color,1.2f,6);}
    static void MoveEnemy(CrackEnemy enemy,Vector3 position)
    {var body=enemy.GetComponent<CharacterController>();bool was=body&&body.enabled;if(body)body.enabled=false;enemy.transform.position=position;if(body)body.enabled=was;EditorUtility.SetDirty(enemy);}
    [MenuItem("Dungeon & DLC/23 - Revisar primeira masmorra e remover tutoriais")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Saia do Play.");
        var scene=SceneManager.GetActiveScene();if(scene.path!=Scene)throw new Exception("Abra a Fortaleza Azul Jogavel.");
        string backup="Assets/SornyGameplay/Reports/BackupExploracaoLivre/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);
        if(!EditorSceneManager.SaveScene(scene,backup+"/Estado_do_Editor.unity",true))throw new Exception("Falha no backup da cena aberta.");
        var game=UnityEngine.Object.FindFirstObjectByType<SornyGameLoop>();var arrival=game.GetComponent<SornyArrival>();var economy=game.GetComponent<SornyEconomy>();
        arrival.exploracaoLivre=true;EditorUtility.SetDirty(arrival);
        if(!game.GetComponent<SornyFreeExploration>())game.gameObject.AddComponent<SornyFreeExploration>();
        foreach(var p in UnityEngine.Object.FindObjectsByType<SornyPickup>(FindObjectsInactive.Include,FindObjectsSortMode.None)){p.disponivelNoNivel=false;p.gameObject.SetActive(false);EditorUtility.SetDirty(p);}
        foreach(var chest in UnityEngine.Object.FindObjectsByType<SornyChest>(FindObjectsInactive.Include,FindObjectsSortMode.None))chest.gameObject.SetActive(false);
        economy.baus=Array.Empty<SornyChest>();EditorUtility.SetDirty(economy);
        foreach(var dummy in UnityEngine.Object.FindObjectsByType<SornyTrainingDummy>(FindObjectsInactive.Include,FindObjectsSortMode.None))dummy.gameObject.SetActive(false);
        var all=UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        foreach(var t in all)
        {
            if(t.name.StartsWith("C07_")||t.name.StartsWith("C17_"))t.gameObject.SetActive(false);
            if(t.name=="Controles")t.gameObject.SetActive(false);
            if(t.name=="Posto_da_Primeira_Espada"||t.name=="Abrigo_Antes_da_Ponte"||t.name=="Adaga_do_Tesouro")t.gameObject.SetActive(false);
        }
        var exterior=all.FirstOrDefault(t=>t.name=="Fase_00_Chegada_a_Fortaleza");
        if(exterior)foreach(var text in exterior.GetComponentsInChildren<Text>(true))text.gameObject.SetActive(false);
        var extra=game.transform.Find("Sorny_Decoracao_Extra");if(extra)foreach(Transform t in extra)if(!t.name.StartsWith("M_Pilar"))t.gameObject.SetActive(false);
        Transform[] rooms=all.Where(t=>t.parent&&t.name.StartsWith("Sala_0")&&t.parent.name=="Fortaleza_Azul_Demonstracao").OrderBy(t=>t.position.z).ToArray();
        if(rooms.Length!=5)throw new Exception("As cinco salas originais nao foram encontradas.");
        var decor=new Transform[5];
        for(int i=0;i<rooms.Length;i++)
        {
            foreach(Transform t in rooms[i])if(t.name.StartsWith("Decoracao_Seed")||t.name=="Tesouro_Organizado")t.gameObject.SetActive(false);
            decor[i]=Group(rooms[i],"Revisao_Exploracao");
            float z=rooms[i].position.z;float side=i==4?9.4f:5.65f;
            Torch(decor[i],"Tocha_Entrada_E",new Vector3(-side,1.05f,z-3.6f),-90,new Color(1,.52f,.19f));
            Torch(decor[i],"Tocha_Saida_D",new Vector3(side,1.05f,z+3.6f),90,new Color(1,.52f,.19f));
            if(i!=4){Prop(decor[i],"Bandeira_E","B04_Bandeira_Sorny",new Vector3(-5.8f,.45f,z),-90);Prop(decor[i],"Bandeira_D","B04_Bandeira_Sorny",new Vector3(5.8f,.45f,z),90);}
        }
        // Entry: clear central route and a small abandoned reception at the side.
        Prop(decor[0],"Mesa_Recepcao","C09_Mesa",new Vector3(-4.3f,0,.6f));
        Prop(decor[0],"Livro_Recepcao","C11_Livro_Dividas",new Vector3(-4.3f,.9f,.6f),15);
        Prop(decor[0],"Barril_Canto","C02_Barril",new Vector3(4.6f,0,-3.8f));
        Prop(decor[0],"Caixa_Canto","C04_Caixa_Cordas",new Vector3(4.5f,0,3.8f));
        // First encounter: side cover and two staggered opponents, open middle.
        Prop(decor[1],"Caixa_Cobertura_E","C04_Caixa_Cordas",new Vector3(-2.9f,0,15.5f),12);
        Prop(decor[1],"Barril_Apoio_E","C02_Barril",new Vector3(-3.8f,0,15.4f));
        Prop(decor[1],"Caixa_Fundo_D","C03_Caixa",new Vector3(4.4f,0,20));
        // Gallery: a pair of islands creates two flanking routes, with all exits clear.
        Prop(decor[2],"Pilar_Ilha_E","M_Pilar",new Vector3(-1.9f,0,30.8f));
        Prop(decor[2],"Pilar_Ilha_D","M_Pilar",new Vector3(1.9f,0,34.6f));
        Prop(decor[2],"Caixa_Ilha_E","C04_Caixa_Cordas",new Vector3(-2.7f,0,30.9f));
        Prop(decor[2],"Caixa_Ilha_D","C04_Caixa_Cordas",new Vector3(2.7f,0,34.7f));
        // Last room: a warm refuge and merchant before the boss gate.
        economy.loja.position=new Vector3(-3.25f,.02f,49.1f);economy.loja.rotation=Quaternion.identity;
        var sign=economy.loja.Find("Letreiro Loja");if(sign)sign.gameObject.SetActive(false);
        var bag=economy.loja.Find("C15_Saco_Moedas");if(bag){bag.localScale=Vector3.one*.3f;bag.localPosition=new Vector3(.6f,.9f,-.1f);}
        LightAt(decor[3],"Luz_Mercador",new Vector3(-3.2f,2.4f,48.5f),new Color(1,.68f,.32f),1.9f,6);
        Prop(decor[3],"Barril_Mercador","C02_Barril",new Vector3(-4.9f,0,50.5f));
        Prop(decor[3],"Banco_Descanso","C10_Cadeira",new Vector3(4.2f,0,47),-90);
        Prop(decor[3],"Velas_Descanso","C14_Candelabro",new Vector3(4.4f,0,50.5f));
        // Boss: keep a broad, empty arena; statues frame the final passage.
        Prop(decor[4],"Estatua_Trono_E","C08_Estatua_Executivo",new Vector3(-7.4f,0,67.8f),145);
        Prop(decor[4],"Estatua_Trono_D","C08_Estatua_Executivo",new Vector3(7.4f,0,67.8f),215);
        LightAt(decor[4],"Luz_Trono",new Vector3(0,2.8f,68),new Color(.42f,.55f,1),1.35f,8);
        var enemies=game.transform.Find("Crack_Encontros").GetComponentsInChildren<CrackEnemy>(true).OrderBy(e=>e.name).ToArray();
        var points=new[]{new Vector3(1.4f,.04f,13),new Vector3(3,.04f,19.7f),new Vector3(-3.4f,.04f,28.5f),new Vector3(3.5f,.04f,33),new Vector3(-2.5f,.04f,36.7f)};
        if(enemies.Length!=points.Length)throw new Exception("Numero de encontros diferente do esperado.");for(int i=0;i<enemies.Length;i++)MoveEnemy(enemies[i],points[i]);
        if(arrival.sentinelaPonte)arrival.sentinelaPonte.nome="Cobrador da Ponte";
        var gates=new[]{game.GetComponent<SornyProgression>().portaAcervo,game.GetComponent<SornyProgression>().portaMasmorra2,UnityEngine.Object.FindFirstObjectByType<SornyBossArena>().grade,game.GetComponent<SornyProgression>().crediton.grade};
        bool[] enabled=gates.Select(g=>g.gameObject.activeSelf).ToArray();foreach(var g in gates)g.gameObject.SetActive(false);
        try{game.GetComponent<SornyNavGrid>().Bake();}finally{for(int i=0;i<gates.Length;i++)gates[i].gameObject.SetActive(enabled[i]);}
        SornyPlayFromMenu.Configure();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("SORNY_EXPLORATION_V1_APPLIED_OK");
    }
    public static void BatchApply(){EditorSceneManager.OpenScene(Scene);Apply();}
}
