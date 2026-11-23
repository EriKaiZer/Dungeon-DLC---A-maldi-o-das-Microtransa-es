using System.Linq;
using UnityEngine;
namespace DungeonDLC.Sorny
{
 public sealed class SornyBoss:MonoBehaviour
 {
  public CrackEnemy Corpo;public bool Ativo{get;private set;}public bool Fase2=>Corpo.Vida<=Corpo.vidaMaxima/2;
  public string Estado{get;private set;}="Dormindo";public LineRenderer aviso;
  [Min(1)]public int danoNormal=28,danoFuria=34;
  public int DanoAtual=>Fase2?danoFuria:danoNormal;
  SornyGameLoop game;CharacterController body;Vector3 origem,scale,alvo,direcao;float until,nextAttack,flash,deathTime;bool acertou,faseAnunciada;int attacks;
  void Awake(){if(!Corpo)Corpo=GetComponent<CrackEnemy>();Corpo.enabled=false;body=GetComponent<CharacterController>();origem=transform.position;scale=Corpo.visual.transform.localScale;}
  void Start(){game=FindFirstObjectByType<SornyGameLoop>();ResetBoss();}
  public bool PodeReceberDano=>Ativo&&!Corpo.Morto;
  public void Hit(){flash=Time.time+.12f;}
  public void LoadState(bool active){Ativo=active;Estado=Corpo.Morto?"Morto":active?"Perseguindo":"Dormindo";nextAttack=Time.time+1.8f;if(Corpo.Morto)deathTime=Time.time-2;if(aviso)aviso.enabled=false;}
  public void ResetBoss(){Ativo=false;Estado="Dormindo";attacks=0;faseAnunciada=false;nextAttack=0;Corpo.visual.transform.localScale=scale;Corpo.visual.transform.localRotation=Quaternion.identity;Corpo.visual.transform.localPosition=Vector3.zero;if(aviso)aviso.enabled=false;}
  bool Visible(){Vector3 start=transform.position+Vector3.up*1.3f,end=game.jogador.transform.position+Vector3.up;if(!Physics.Linecast(start,end,out var hit,~0,QueryTriggerInteraction.Ignore))return true;return hit.collider.GetComponentInParent<SornyPlayerLife>();}
  void BeginAttack(bool charge)
  {
   Estado=charge?"Preparando investida":"Preparando impacto";until=Time.time+(Fase2?.8f:1.15f);alvo=game.jogador.transform.position;direcao=Vector3.ProjectOnPlane(alvo-transform.position,Vector3.up).normalized;acertou=false;Corpo.visual.color=new Color(1,.75f,.25f);
   if(aviso){aviso.enabled=true;aviso.startColor=aviso.endColor=charge?new Color(1,.35f,.08f):new Color(1,.8f,.1f);aviso.positionCount=charge?2:49;
    if(charge){aviso.SetPosition(0,transform.position+Vector3.up*.07f);aviso.SetPosition(1,transform.position+direcao*5+Vector3.up*.07f);}else for(int i=0;i<49;i++){float a=i*Mathf.PI*2/48;aviso.SetPosition(i,transform.position+new Vector3(Mathf.Cos(a)*2.8f,.07f,Mathf.Sin(a)*2.8f));}}
   game.Notificar(charge?"INVESTIDA":"IMPACTO",1.2f);
  }
  public void OnKilled(){Ativo=true;Estado="Morto";deathTime=Time.time;if(aviso)aviso.enabled=false;if(game){game.Moedas+=90;var progress=game.GetComponent<SornyProgression>();if(progress)progress.LiberarArmaChefe(transform.position);game.Notificar("LORDE SÓ-NÃO DERROTADO!  CHAVE DO ACERVO RECUPERADA",4);}}
  void Update()
  {
   if(!game||game.Terminou||game.jogador.Morto||Time.timeScale<=0)return;
   if(Corpo.Morto){Estado="Morto";float t=Mathf.Clamp01((Time.time-deathTime)/1.1f);Corpo.visual.transform.localScale=new Vector3(scale.x,scale.y*Mathf.Lerp(1,.22f,t),scale.z);Corpo.visual.color=new Color(.42f,.42f,.48f);return;}
   if(!Ativo)
   {if(game.jogador.transform.position.z>58.3f){Ativo=true;Estado="Perseguindo";nextAttack=Time.time+1.5f;game.Notificar("LORDE SÓ-NÃO — SUA LICENÇA TERMINA AQUI",4);}else return;}
   if(Fase2&&!faseAnunciada){faseAnunciada=true;game.Notificar("FASE 2 — COBRANÇA EM DOBRO!",2.5f);}
   float distance=Vector3.ProjectOnPlane(game.jogador.transform.position-transform.position,Vector3.up).magnitude;
   if(Estado.StartsWith("Preparando"))
   {if(Time.time>=until){bool charge=Estado.Contains("investida");Estado=charge?"Investida":"Recuperando";until=Time.time+(charge?.65f:Fase2?1.2f:1.65f);if(aviso)aviso.enabled=false;
     if(!charge&&distance<2.8f&&Visible())game.jogador.ReceberDano(DanoAtual);var audio=GetComponent<AudioSource>();if(audio){audio.volume=SornySession.Efeitos*.75f;audio.Play();}}return;}
   if(Estado=="Investida")
   {Move(direcao*7);if(!acertou&&distance<1.75f&&Visible()){game.jogador.ReceberDano(DanoAtual, transform.position);acertou=true;}if(Time.time>=until){Estado="Recuperando";until=Time.time+1.3f;}return;}
   if(Estado=="Recuperando"){if(Time.time>=until){Estado="Perseguindo";nextAttack=Time.time+.7f;}return;}
   if(game.jogador.transform.position.z<57){Estado="Perseguindo";return;}
   if(Time.time>=nextAttack&&Visible()&&distance<8){BeginAttack(attacks++%2==1&&distance>2);return;}
   if(distance>1.9f){Vector3 direction=game.jogador.transform.position-transform.position;direction.y=0;if(Corpo.navegacao){var path=Corpo.navegacao.Caminho(transform.position,game.jogador.transform.position);var node=path.FirstOrDefault(p=>Vector3.ProjectOnPlane(p-transform.position,Vector3.up).magnitude>.35f);if(node!=Vector3.zero)direction=Vector3.ProjectOnPlane(node-transform.position,Vector3.up);}Move(direction.normalized*(Fase2?2.5f:1.8f));}
  }
  void Move(Vector3 velocity){if(body.enabled){body.Move((velocity+Vector3.down*2)*Time.deltaTime);Vector3 p=transform.position;p.x=Mathf.Clamp(p.x,-4.6f,4.6f);p.z=Mathf.Clamp(p.z,58.5f,68);if(p!=transform.position){body.enabled=false;transform.position=p;body.enabled=true;}}}
  void LateUpdate()
  {
   if(!Corpo||!Corpo.visual)return;var cam=Camera.main;if(cam){Vector3 d=Corpo.visual.transform.position-cam.transform.position;d.y=0;if(d.sqrMagnitude>.01f)Corpo.visual.transform.rotation=Quaternion.LookRotation(d);}
   if(!Corpo.Morto){bool warning=Estado.StartsWith("Preparando");Corpo.visual.color=Time.time<flash?new Color(1,.4f,.3f):warning?new Color(1,.75f,.25f):Color.white;Corpo.visual.transform.localPosition=Vector3.up*(Ativo?.025f*Mathf.Sin(Time.time*5):0);}
  }
 }
}

