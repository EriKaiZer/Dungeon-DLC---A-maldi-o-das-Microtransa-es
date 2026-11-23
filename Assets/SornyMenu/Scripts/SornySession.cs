using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
namespace DungeonDLC.Sorny
{
 public sealed class SornySession:MonoBehaviour
 {
  public static SornySession Instancia{get;private set;}
  public static bool LoadSaved;
  public static string SaveOverride;
  public static string SavePath=>string.IsNullOrEmpty(SaveOverride)?Path.Combine(Application.persistentDataPath,"fortaleza-save-v1.json"):SaveOverride;
  public static float Musica=>PlayerPrefs.GetFloat("Sorny.Music",.13f);
  public static float Efeitos=>PlayerPrefs.GetFloat("Sorny.Sfx",.6f);
  public static float Ambiente=>PlayerPrefs.GetFloat("Sorny.Amb",.18f);
  public static float Mouse=>PlayerPrefs.GetFloat("Sorny.Mouse",.09f);
  [Serializable] public class EnemySave{public string id;public int life;public Vector3 position;}
  [Serializable] public class PickupSave{public string id;public bool active;}
  [Serializable] public class SaveData{public int version=1,life,gold;public Vector3 position;public Quaternion rotation;public EnemySave[] enemies;public PickupSave[] pickups;public SornyEconomy.State economy;public SornyProgression.State progressao;public SornyArrival.State chegada;}
  SornyGameLoop game;SornyAudioDirector audio;SornyFrontEnd overlay;float nextSave;bool ready;
  public bool Pausado=>overlay;
  public bool Pronta=>ready;
  static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
  static bool Valid(Vector3 p)=>Finite(p.x)&&Finite(p.y)&&Finite(p.z)&&p.sqrMagnitude<100000000;
  public static SaveData ReadSave()
  {try{if(!File.Exists(SavePath))return null;string json=File.ReadAllText(SavePath);var s=JsonUtility.FromJson<SaveData>(json);if(s==null||s.version!=1||s.life<1||s.life>100||s.gold<0||s.enemies==null||s.pickups==null||!Valid(s.position)||!Finite(s.rotation.x)||!Finite(s.rotation.y)||!Finite(s.rotation.z)||!Finite(s.rotation.w))return null;if(s.enemies.Any(e=>e==null||e.life<0||e.life>10000||!Valid(e.position)))return null;if(json.IndexOf("\"chegada\"",StringComparison.Ordinal)<0)s.chegada=null;return s;}catch{return null;}}
  public static bool HasSave=>ReadSave()!=null;
  public static void NewRun(){if(File.Exists(SavePath))File.Delete(SavePath);LoadSaved=false;}
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
  static void ResetStatics(){Instancia=null;LoadSaved=false;SaveOverride=null;SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
  static void Loaded(Scene scene,LoadSceneMode mode)
  {
   if(scene.name==SornyFrontEnd.GameScene&&Resources.Load<SornyMenuTheme>("SornyMenuTheme")&&!FindFirstObjectByType<SornySession>())new GameObject("Sessao e salvamento").AddComponent<SornySession>();
  }
  static string Id(Transform t){string id=t.name+"["+t.GetSiblingIndex()+"]";while(t.parent){t=t.parent;id=t.name+"["+t.GetSiblingIndex()+"]/"+id;}return id;}
  IEnumerator Start()
  {
   Instancia=this;game=FindFirstObjectByType<SornyGameLoop>();audio=FindFirstObjectByType<SornyAudioDirector>();
   yield return null;if(!game){enabled=false;yield break;}
   if(LoadSaved)Restore();LoadSaved=false;ApplySettings();ready=true;nextSave=Time.unscaledTime+10;
   if(PlayerPrefs.HasKey("Sorny.Fullscreen"))Screen.fullScreen=PlayerPrefs.GetInt("Sorny.Fullscreen")==1;
   if(!HasSave)Save();
  }
  public static void ApplySettings()
  {foreach(var a in FindObjectsByType<SornyAudioDirector>(FindObjectsSortMode.None)){a.volumeMusica=Musica;a.volumeEfeitos=Efeitos;a.volumeAmbiente=Ambiente;foreach(var source in a.GetComponents<AudioSource>()){if(source.clip==a.trilha)source.volume=Musica;if(source.clip==a.ambiente)source.volume=Ambiente;}}foreach(var m in FindObjectsByType<MovimentoGrade>(FindObjectsSortMode.None))m.sensibilidadeMouse=Mouse;}
  public bool Save()
  {
   if(!ready||!game||game.Terminou||game.jogador.Morto)return false;
   try
   {
    var s=new SaveData{life=game.jogador.Vida,gold=game.Moedas,position=game.jogador.transform.position,rotation=game.jogador.transform.rotation,
     enemies=FindObjectsByType<CrackEnemy>(FindObjectsSortMode.None).Select(e=>new EnemySave{id=Id(e.transform),life=e.Vida,position=e.transform.position}).ToArray(),
     pickups=FindObjectsByType<SornyPickup>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(p=>new PickupSave{id=Id(p.transform),active=p.gameObject.activeSelf}).ToArray(),economy=game.GetComponent<SornyEconomy>()?game.GetComponent<SornyEconomy>().Capture():null,progressao=game.GetComponent<SornyProgression>()?game.GetComponent<SornyProgression>().Capturar():null,chegada=game.GetComponent<SornyArrival>()?game.GetComponent<SornyArrival>().Capturar():null};
    string temp=SavePath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(s,true));if(File.Exists(SavePath))File.Replace(temp,SavePath,null);else File.Move(temp,SavePath);return true;
   }
   catch(Exception e){Debug.LogWarning("Não foi possível salvar a partida: "+e.Message);return false;}
  }
  public bool Restore()
  {
   var s=ReadSave();if(s==null)return false;
   var player=game.jogador;var body=player.GetComponent<CharacterController>();body.enabled=false;player.transform.SetPositionAndRotation(s.position,s.rotation.normalized);body.enabled=true;
   player.CarregarVida(s.life);
   var chegada=game.GetComponent<SornyArrival>();if(chegada)chegada.Restaurar(s.chegada);
   foreach(var enemy in FindObjectsByType<CrackEnemy>(FindObjectsSortMode.None))
   {var saved=s.enemies.FirstOrDefault(e=>e.id==Id(enemy.transform));if(saved==null)continue;enemy.Reiniciar();var cc=enemy.GetComponent<CharacterController>();cc.enabled=false;enemy.transform.position=saved.position;cc.enabled=true;var boss=enemy.GetComponent<SornyBoss>();if(boss)boss.LoadState(true);var crediton=enemy.GetComponent<SornyCrediton>();if(crediton)crediton.LoadState(true);if(saved.life<enemy.vidaMaxima)enemy.ReceberDano(enemy.vidaMaxima-saved.life);}
   foreach(var pickup in FindObjectsByType<SornyPickup>(FindObjectsInactive.Include,FindObjectsSortMode.None))
   {
    var saved=s.pickups.FirstOrDefault(p=>p.id==Id(pickup.transform));
    if(saved!=null)pickup.RestaurarDisponibilidade(saved.active);
    else if(!pickup.disponivelNoNivel)pickup.RestaurarDisponibilidade(false);
   }
   var economy=game.GetComponent<SornyEconomy>();if(economy)economy.Restore(s.economy);game.Moedas=s.gold;var progressao=game.GetComponent<SornyProgression>();if(progressao)progressao.Restaurar(s.progressao);var livre=game.GetComponent<SornyFreeExploration>();if(livre)livre.AjustarPosicaoSegura();if(audio)audio.SincronizarEstado();game.Notificar("PARTIDA RETOMADA",3);return true;
  }
  public void Pausar()
  {
   if(overlay||!ready||Time.timeScale==0)return;
   Save();if(audio)audio.enabled=false;Time.timeScale=0;game.movimento.bloquearControles=true;game.espada.bloquearAtaque=true;
   overlay=new GameObject("Menu de pausa").AddComponent<SornyFrontEnd>();overlay.pausa=true;
  }
  public void Retomar()
  {
   if(overlay){Destroy(overlay.gameObject);overlay=null;}Time.timeScale=1;if(audio)audio.enabled=true;
   if(game&&!game.Terminou){game.movimento.bloquearControles=false;game.espada.bloquearAtaque=false;Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}nextSave=Time.unscaledTime+10;
  }
  public void VoltarMenu(){if(!game.Terminou&&!Save()){game.Notificar("FALHA AO SALVAR. TENTE NOVAMENTE.",4);Retomar();return;}Time.timeScale=1;SceneManager.LoadScene(SornyFrontEnd.MenuScene);}
  void Update()
  {
   if(!ready)return;var key=Keyboard.current;
   if(!overlay&&key!=null&&key.escapeKey.wasPressedThisFrame)Pausar();
   if(!overlay&&Time.timeScale>0&&Time.unscaledTime>=nextSave){Save();nextSave=Time.unscaledTime+10;}
  }
  void OnApplicationPause(bool paused){if(paused&&ready)Save();}
  void OnApplicationQuit(){if(ready)Save();}
  void OnDestroy(){if(Instancia==this)Instancia=null;if(overlay)Destroy(overlay.gameObject);Time.timeScale=1;}
 }
}

