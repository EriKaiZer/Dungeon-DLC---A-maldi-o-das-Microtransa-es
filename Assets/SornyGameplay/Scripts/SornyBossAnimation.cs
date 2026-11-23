using UnityEngine;
namespace DungeonDLC.Sorny
{
 [DefaultExecutionOrder(100)]public sealed class SornyBossAnimation:MonoBehaviour
 {
  public Sprite[] caminhada,golpe,morte;public SornyBoss boss;
  string previous="";float since;Vector3 last;bool dead;
  void Awake(){last=transform.position;}
  void LateUpdate()
  {
   if(!boss||!boss.Corpo||!boss.Corpo.visual)return;var renderer=boss.Corpo.visual;string state=boss.Estado;bool isDead=boss.Corpo.Morto;
   if(state!=previous||isDead!=dead){since=Time.time;previous=state;dead=isDead;}
   Sprite[] frames=caminhada;int frame=0;float elapsed=Time.time-since;
   if(isDead){frames=morte;frame=Mathf.FloorToInt(elapsed*8);renderer.transform.localScale=Vector3.one;renderer.transform.localPosition=Vector3.zero;}
   else if(state=="Preparando impacto"||state=="Preparando investida"){frames=golpe;frame=Mathf.Clamp(Mathf.FloorToInt(elapsed/(boss.Fase2?.8f:1.15f)*4),0,3);}
   else if(state=="Recuperando"||state=="Investida"){frames=golpe;frame=Mathf.Clamp(4+Mathf.FloorToInt(elapsed*7),4,7);}
   else if((transform.position-last).sqrMagnitude>.000001f){frame=Mathf.FloorToInt(Time.time*(boss.Fase2?11:8));}
   else frame=Mathf.FloorToInt(Time.time*2)%2;
   if(frames!=null&&frames.Length>0)renderer.sprite=frames[isDead?Mathf.Min(frame,frames.Length-1):frame%frames.Length];last=transform.position;
  }
 }
}
