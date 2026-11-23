using System;
using System.IO;
using System.Reflection;
using DungeonDLC.Sorny;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SornyRigidGripInstaller
{
    const string ScenePath="Assets/SornyGameplay/Scenes/Fortaleza_Azul_Jogavel.unity";
    const string Request="Assets/SornyGameplay/Reports/empunhadura-reta.txt";
    static double next;
    static SornyRigidGripInstaller(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+2;
        if(!File.Exists(Request)||File.ReadAllText(Request).Trim()!="pending-rigid-grip-v1"||SceneManager.GetActiveScene().path!=ScenePath)return;
        if(EditorApplication.isPlayingOrWillChangePlaymode){if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();return;}
        try{Apply();File.WriteAllText(Request,"done-rigid-grip-v1");}
        catch(Exception e){File.WriteAllText(Request,"error: "+e);Debug.LogException(e);}
    }
    [MenuItem("Dungeon & DLC/20 - Corrigir eixo e pegada das armas")]
    public static void Apply()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath)throw new Exception("Abra a Fortaleza Azul Jogavel.");
        var hands=UnityEngine.Object.FindFirstObjectByType<SornyHandEquipment>();
        if(!hands||!hands.maoEmCamada||hands.espada.Length==0||hands.adaga.Length<4)throw new Exception("As armas e a mao originais devem estar disponiveis.");
        string backup="Assets/SornyGameplay/Reports/BackupEmpunhaduraReta/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Directory.CreateDirectory(backup);
        if(!EditorSceneManager.SaveScene(scene,backup+"/Estado_do_Editor.unity",true))throw new Exception("Nao foi possivel preservar a cena aberta.");
        typeof(SornyHandEquipment).GetMethod("AplicarEquipamentos",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(hands,null);
        EditorUtility.SetDirty(hands);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Assets/SornyGameplay/Reports/EMPUNHADURA_RETA.txt","Espada e adaga: lamina, guarda, cabo e pomo em um unico sprite rigido.\nMao sobreposta com proporcao preservada e cabo ajustado ao punho fechado.\nAnimacao e trilha de corte acompanham o conjunto.\nBackup: "+backup+"\n");
        Debug.Log("SORNY_RIGID_GRIP_V1_APPLIED_OK");
    }
    public static void BatchApply(){EditorSceneManager.OpenScene(ScenePath);Apply();File.WriteAllText(Request,"done-rigid-grip-v1");}
}
