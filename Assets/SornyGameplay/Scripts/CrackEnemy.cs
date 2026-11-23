using System.Collections.Generic;
using UnityEngine;
namespace DungeonDLC.Sorny
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CrackEnemy : MonoBehaviour
    {
        public string nome="Crack Bandido Culto";
        public int vidaMaxima=70,dano=18,nivel=1;
        public Color corBase=Color.white;
        public float intervaloAtaque=.75f,quadrosCaminhada=9;
        public float velocidade=2.1f,percepcao=8.5f,alcance=1.45f;
        public Sprite[] caminhada,ataque,morte,ferido;
        public SpriteRenderer visual;
        public SornyPlayerLife jogador;
        public SornyNavGrid navegacao;
        public int Vida{get;private set;}
        public bool Morto=>Vida<=0;
        public string Estado{get;private set;}="Parado";
        CharacterController body;
        List<Vector3> caminho=new List<Vector3>();int waypoint;
        float timer,frameTimer,tempoEstado,proximoAtaque,proximoCaminho,flash;
        int frame;bool alertado,acertou;
        Vector3 origem;
        Vector3 recuo;
        float pausaReacao;
        void Awake(){body=GetComponent<CharacterController>();origem=transform.position;Reiniciar();}
        public void Reiniciar()
        {
            if(!body)body=GetComponent<CharacterController>();body.enabled=false;transform.position=origem;body.enabled=true;
            Vida=vidaMaxima;alertado=false;timer=frameTimer=0;proximoAtaque=0;proximoCaminho=0;flash=0;recuo=Vector3.zero;pausaReacao=0;caminho.Clear();Mudar("Parado");Mostrar(caminhada,0);
        }
        void Mudar(string estado){if(Estado==estado)return;Estado=estado;tempoEstado=Time.time;frameTimer=0;frame=0;acertou=false;}
        bool Visivel()
        {
            if(!jogador)return false;Vector3 a=transform.position+Vector3.up*1.05f,b=jogador.transform.position+Vector3.up*1.05f;
            if(!Physics.Linecast(a,b,out var hit,~0,QueryTriggerInteraction.Ignore))return true;
            return hit.collider.GetComponentInParent<SornyPlayerLife>()==jogador;
        }
        void Update()
        {
            if(!jogador)jogador=FindFirstObjectByType<SornyPlayerLife>();if(!navegacao)navegacao=FindFirstObjectByType<SornyNavGrid>();
            if(!body)body=GetComponent<CharacterController>();timer+=Time.deltaTime;frameTimer+=Time.deltaTime;
            if(Morto){Mostrar(morte,Mathf.Min(morte.Length-1,Mathf.FloorToInt((Time.time-tempoEstado)*8)));return;}
            if (Time.time < pausaReacao) return;
            AtualizarRecuo(Time.deltaTime);
            if(!jogador||jogador.Morto)return;
            float distancia=Vector3.Distance(transform.position,jogador.transform.position);
            bool ve=distancia<percepcao&&Visivel();if(ve)alertado=true;
            if(Estado=="Dano"&&Time.time-tempoEstado<.32f){Mostrar(ferido,Mathf.FloorToInt((Time.time-tempoEstado)*12));return;}
            if(Estado=="Ataque")
            {
                float t=Time.time-tempoEstado;Mostrar(ataque,Mathf.FloorToInt(t*10));
                if(!acertou&&t>=.28f){acertou=true;if(distancia<alcance+.25f&&Visivel())jogador.ReceberDano(dano, transform.position);}
                if(t>=.6f){Mudar("Parado");proximoAtaque=Time.time+intervaloAtaque;}return;
            }
            if(alertado&&distancia<=alcance&&Visivel())
            {if(Time.time>=proximoAtaque){Mudar("Ataque");Mostrar(ataque,0);}else{Mudar("Parado");Mostrar(caminhada,0);}return;}
            if(alertado&&distancia>alcance*.85f)
            {
                if(Time.time>=proximoCaminho&&navegacao){caminho=navegacao.Caminho(transform.position,jogador.transform.position);waypoint=0;proximoCaminho=Time.time+.7f;}
                Vector3 target=transform.position;
                if(caminho.Count>waypoint){target=caminho[waypoint];if(Vector3.ProjectOnPlane(target-transform.position,Vector3.up).magnitude<.2f)waypoint++;}
                Vector3 direction=target-transform.position;direction.y=0;
                if(direction.magnitude>.05f){Mudar("Caminhada");body.Move(direction.normalized*velocidade*Time.deltaTime+Vector3.down*2*Time.deltaTime);Mostrar(caminhada,Mathf.FloorToInt(timer*quadrosCaminhada));return;}
            }
            Mudar("Parado");Mostrar(caminhada,0);
        }
        void Mostrar(Sprite[] sprites,int i){if(visual&&sprites!=null&&sprites.Length>0)visual.sprite=sprites[Mathf.Clamp(i<0?0:i,0,sprites.Length-1)];}
        void LateUpdate()
        {
            if(!visual)return;var cam=Camera.main;if(cam){Vector3 d=visual.transform.position-cam.transform.position;d.y=0;if(d.sqrMagnitude>.001f)visual.transform.rotation=Quaternion.LookRotation(d);}
            visual.color=Time.time<flash?new Color(1,.45f,.35f):corBase;
            // Walking loops, other clips keep their final frame.
            if(Estado=="Caminhada"&&caminhada.Length>0)Mostrar(caminhada,Mathf.FloorToInt(timer*quadrosCaminhada)%caminhada.Length);
        }
        public bool ReceberDano(int valor)
        {
            if(Morto||valor<=0)return false;var boss=GetComponent<SornyBoss>();if(boss&&!boss.PodeReceberDano)return false;var finalBoss=GetComponent<SornyCrediton>();if(finalBoss&&!finalBoss.PodeReceberDano)return false;Vida=Mathf.Max(0,Vida-valor);flash=Time.time+.12f;alertado=true;if(boss)boss.Hit();var crediton=finalBoss;if(crediton)crediton.Hit();
            if(Morto){Mudar("Morte");body.enabled=false;Mostrar(morte,0);var game=FindFirstObjectByType<SornyGameLoop>();if(game)game.InimigoDerrotado();if(boss)boss.OnKilled();if(crediton)crediton.OnKilled();}
            else{Mudar("Dano");Mostrar(ferido,0);}return true;
        }
        public void Empurrar(Vector3 direcao, float forca)
        {
            if (Morto || GetComponent<SornyBoss>() || GetComponent<SornyCrediton>()) return;
            recuo = Vector3.ProjectOnPlane(direcao, Vector3.up).normalized * Mathf.Clamp(forca, 0, 3);
            pausaReacao = Time.time + .045f;
        }

        void AtualizarRecuo(float deltaTime)
        {
            if (!body || !body.enabled || Morto || recuo.sqrMagnitude < .0001f) return;
            body.Move((recuo + Vector3.down * 2) * deltaTime);
            recuo = Vector3.MoveTowards(recuo, Vector3.zero, deltaTime * 12);
        }
    }
}
