using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class SornyPlayFromMenu
{
    const string Menu="Assets/SornyMenu/Scenes/Tela_Inicial.unity";
    const string Game="Assets/SornyGameplay/Scenes/Fortaleza_Azul_Jogavel.unity";
    [InitializeOnLoadMethod] static void Initialize(){EditorApplication.delayCall+=Configure;}
    [MenuItem("Dungeon & DLC/22 - Iniciar sempre pelo menu")]
    public static void Configure()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        var scene=AssetDatabase.LoadAssetAtPath<SceneAsset>(Menu);if(!scene)return;
        EditorSceneManager.playModeStartScene=scene;
        var rest=EditorBuildSettings.scenes.Where(s=>s.path!=Menu&&s.path!=Game).ToArray();
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Menu,true),new EditorBuildSettingsScene(Game,true)}.Concat(rest).ToArray();
    }
}
