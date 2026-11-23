using System.Linq;
using UnityEngine;
namespace DungeonDLC.Sorny
{
 [DefaultExecutionOrder(200)]public sealed class SornyBossArena:MonoBehaviour
 {
  public SornyBoss boss;public SornyCrediton crediton;public Transform grade;public AudioClip batalha,entradaChefe;
  [Range(.5f,1.5f)]public float ganhoBatalha=1.35f;
  public bool Fechado{get;private set;}public float Mistura{get;private set;}
  SornyGameLoop game;SornyAudioDirector director;AudioSource normal,combat,entrada;Vector3 doorOrigin;
  bool alvoAnterior;float proximaEntrada;
  void Start()
  {
   game=FindFirstObjectByType<SornyGameLoop>();director=game.GetComponent<SornyAudioDirector>();doorOrigin=grade.localPosition;
   combat=gameObject.AddComponent<AudioSource>();combat.clip=batalha;combat.loop=true;combat.playOnAwake=false;combat.volume=0;combat.priority=110;combat.dopplerLevel=0;
   entrada=gameObject.AddComponent<AudioSource>();entrada.clip=entradaChefe;entrada.playOnAwake=false;entrada.priority=28;entrada.dopplerLevel=0;
  }
  void LateUpdate()
  {
   AtualizarArena(Time.unscaledDeltaTime);
  }
  void AtualizarArena(float deltaTime)
  {
   if(!game||!boss)return;float z=game.jogador.transform.position.z;bool alive=!boss.Corpo.Morto;
   bool fighting=boss.Ativo&&alive&&!game.Terminou;
   Fechado=fighting&&z>58.3f;
   bool open=boss.Corpo.Morto||(!Fechado&&Mathf.Abs(z-56.3f)<5);
   grade.localPosition=Vector3.MoveTowards(grade.localPosition,doorOrigin+(open?Vector3.up*4.2f:Vector3.zero),Time.deltaTime*3.5f);
   bool segundo=crediton&&z>122&&!crediton.Corpo.Morto;bool target=((z>57&&alive)||segundo)&&!game.Terminou;combat.pitch=segundo?.92f:1;
   Mistura=Mathf.MoveTowards(Mistura,target?1:0,deltaTime/(target?.7f:1.25f));
   if(!normal&&director)normal=director.FonteMusica;
   if(target&&!alvoAnterior&&Time.unscaledTime>=proximaEntrada&&entradaChefe){entrada.clip=entradaChefe;entrada.volume=(director?director.volumeEfeitos:SornySession.Efeitos)*.9f;entrada.Play();proximaEntrada=Time.unscaledTime+8;}
   alvoAnterior=target;
   if(target&&!combat.isPlaying)combat.Play();if(!target&&Mistura<=0&&combat.isPlaying)combat.Stop();
   float volume=director?director.VolumeMusicaAtual:SornySession.Musica;if(normal)normal.volume=Mathf.Clamp01(volume*(director?director.ganhoExploracao:1)*(1-Mistura)*(game.Terminou?.4f:1));combat.volume=Mathf.Clamp01(volume*Mistura*ganhoBatalha);
   if(entrada)entrada.volume=(director?director.volumeEfeitos:SornySession.Efeitos)*.9f;
  }
 }
}
