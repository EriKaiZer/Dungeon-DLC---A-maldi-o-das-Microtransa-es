using System.Linq;
using UnityEngine;

namespace DungeonDLC.Sorny
{
    [DefaultExecutionOrder(170)]
    public sealed class SornyCrediton : MonoBehaviour
    {
        public enum Golpe { CobrancaDupla, ExecucaoFiscal, JurosRotativos, QuebraDeBanca }
        public CrackEnemy Corpo;
        public Transform grade;
        public Vector3 origemGrade;
        public LineRenderer aviso, corrente;
        public AudioClip grito,laminas,impactoChao;
        public bool Ativo { get; private set; }
        public bool Fase2 => Corpo && Corpo.Vida <= Corpo.vidaMaxima / 2;
        public bool PodeReceberDano => Ativo && Corpo && !Corpo.Morto;
        public string Estado { get; private set; } = "Dormindo";
        public float TempoEstado { get; private set; }
        public float TempoDesdeHit { get; private set; } = 999;
        public float Ritmo => Fase2 ? .86f : 1;
        public int DanoAtual => Fase2 ? 30 : 24;
        SornyGameLoop game;
        CharacterController body;
        Vector3 alvo, direcao;
        bool acertou, segundoAcerto, faseAnunciada, salvarMorte;
        int ataques, postura;
        float espera, caminhoAte;
        Vector3 proximoNo;
        AudioSource som;

        void Awake()
        {
            if (!Corpo) Corpo = GetComponent<CrackEnemy>();
            Corpo.enabled = false;
            body = GetComponent<CharacterController>();
            game = FindFirstObjectByType<SornyGameLoop>();
        }
        void Start() { som=gameObject.AddComponent<AudioSource>();som.playOnAwake=false;som.spatialBlend=1;som.dopplerLevel=0;som.priority=50;som.minDistance=2;som.maxDistance=18;ResetBoss(); }
        void Tocar(AudioClip clip,float pitch,float gain){if(!som||!clip)return;som.pitch=pitch;som.volume=game.GetComponent<SornyAudioDirector>().volumeEfeitos;som.PlayOneShot(clip,gain);}
        void Mudar(string state) { Estado = state; TempoEstado = 0; acertou = segundoAcerto = false; }
        public void ResetBoss()
        {
            Ativo = false; faseAnunciada = salvarMorte = false; ataques = postura = 0; espera = .9f; TempoDesdeHit = 999;
            Mudar("Dormindo"); OcultarAvisos(); AtualizarGrade(true);
        }
        public void LoadState(bool active)
        {
            Ativo = active || Corpo.Morto; faseAnunciada = Fase2 && active; salvarMorte = false; postura = 0;
            Mudar(Corpo.Morto ? "Morto" : active ? "Perseguindo" : "Dormindo");
            if (Corpo.Morto) TempoEstado = 2;
            espera = 1.5f; TempoDesdeHit = 999; OcultarAvisos(); AtualizarGrade(true);
        }
        public void Hit() { TempoDesdeHit = 0; }
        public void RegistrarImpacto(bool pesado)
        {
            if (!PodeReceberDano || Estado == "CaloteDivino" || Estado == "Atordoado") return;
            postura += pesado ? 38 : 7;
            if (postura < 100) return;
            postura = 0; Mudar("Atordoado"); OcultarAvisos();
            game.Notificar("CREDITON ATORDOADO!", 1);
        }
        public void OnKilled()
        {
            Ativo = true; Mudar("Morto"); salvarMorte = true; OcultarAvisos(); AtualizarGrade(true);
            game.Moedas += 140; game.Notificar("FALÊNCIA DIVINA! RECUPERE A ÚLTIMA MÍDIA FÍSICA", 6);
        }
        void OcultarAvisos() { if (aviso) aviso.enabled = false; if (corrente) corrente.enabled = false; }
        void AtualizarGrade(bool instant)
        {
            if (!grade) return;
            Vector3 destination = origemGrade + (!Ativo || Corpo.Morto ? Vector3.up * 4.2f : Vector3.zero);
            grade.localPosition = instant ? destination : Vector3.MoveTowards(grade.localPosition, destination, Time.deltaTime * 4);
            if (instant) Physics.SyncTransforms();
        }
        void Update()
        {
            if (salvarMorte) { salvarMorte = false; if (SornySession.Instancia) SornySession.Instancia.Save(); }
            Avancar(Time.deltaTime);
            AtualizarGrade(false);
        }
        bool Visivel()
        {
            Vector3 from = transform.position + Vector3.up * 1.35f;
            Vector3 to = game.jogador.transform.position + Vector3.up;
            return !Physics.Linecast(from, to, out var hit, ~0, QueryTriggerInteraction.Ignore) || hit.collider.GetComponentInParent<SornyPlayerLife>();
        }
        bool Atingir(float range, int damage, bool area)
        {
            if (Vector3.ProjectOnPlane(game.jogador.transform.position - transform.position, Vector3.up).magnitude > range || !Visivel()) return false;
            if (area) return game.jogador.ReceberDano(damage);
            return game.jogador.ReceberDano(damage, transform.position);
        }
        public void ComecarAtaque(Golpe attack)
        {
            if (!PodeReceberDano) return;
            Mudar(attack.ToString());
            Tocar(grito,.72f,.35f);
            alvo = game.jogador.transform.position;
            alvo.x = Mathf.Clamp(alvo.x, -8.3f, 8.3f); alvo.z = Mathf.Clamp(alvo.z, 123.5f, 132.3f);
            direcao = Vector3.ProjectOnPlane(alvo - transform.position, Vector3.up).normalized;
            if (direcao.sqrMagnitude < .01f) direcao = Vector3.back;
            if (!aviso) return;
            aviso.enabled = true; aviso.startColor = aviso.endColor = attack == Golpe.CobrancaDupla || attack == Golpe.ExecucaoFiscal ? new Color(1,.22f,.06f) : new Color(1,.72f,.06f);
            if (attack == Golpe.CobrancaDupla || attack == Golpe.ExecucaoFiscal)
            {
                aviso.positionCount = 2; aviso.SetPosition(0, transform.position + Vector3.up * .06f);
                aviso.SetPosition(1, transform.position + direcao * (attack == Golpe.ExecucaoFiscal ? 5.8f : 2.8f) + Vector3.up * .06f);
            }
            else
            {
                aviso.positionCount = 49; Vector3 center = attack == Golpe.QuebraDeBanca ? alvo : transform.position; float radius = attack == Golpe.QuebraDeBanca ? 2.5f : 3.1f;
                for (int i=0;i<49;i++) { float a=i*Mathf.PI*2/48; aviso.SetPosition(i,center+new Vector3(Mathf.Cos(a)*radius,.06f,Mathf.Sin(a)*radius)); }
            }
        }
        void Avancar(float delta)
        {
            if (!game || Time.timeScale <= 0) return;
            TempoEstado += delta; TempoDesdeHit += delta;
            if (Corpo.Morto) return;
            if (game.Terminou || game.jogador.Morto) return;
            if (!Ativo)
            {
                if (game.jogador.transform.position.z > 123 && FindObjectsByType<CrackEnemy>(FindObjectsSortMode.None).All(e=>e.nivel!=2||e==Corpo||e.Morto))
                { Ativo=true; Mudar("Perseguindo"); espera=1.4f; game.Notificar("CREDITON — O DEUS DOS JUROS",5); }
                return;
            }
            if (Fase2 && !faseAnunciada) { faseAnunciada=true; Mudar("CaloteDivino"); OcultarAvisos(); Tocar(grito,.6f,.7f); game.Notificar("CALOTE DIVINO — OS JUROS DOBRARAM!",3); }
            if (Estado=="CaloteDivino") { if(TempoEstado>=8f/7) { Mudar("Perseguindo"); espera=.6f; } return; }
            if (Estado=="Atordoado") { if(TempoEstado>=.85f) { Mudar("Perseguindo"); espera=.65f; } return; }
            if (Estado=="Recuperando") { if(TempoEstado>=espera) { Mudar("Perseguindo"); espera=.35f; } return; }
            float t=TempoEstado/Ritmo;
            if (Estado=="CobrancaDupla")
            {
                if(t>=.50f&&!acertou){acertou=true;if(aviso)aviso.enabled=false;Tocar(laminas,.85f,.8f);Atingir(2.8f,DanoAtual,false);}
                if(t>=1.28f&&!segundoAcerto){segundoAcerto=true;Tocar(laminas,.9f,.8f);Atingir(2.8f,DanoAtual,false);}
                if(t>=1.73f) Recuperar(.95f); return;
            }
            if (Estado=="ExecucaoFiscal")
            {
                if(t>=.50f&&!acertou)
                {
                    acertou=true;if(aviso)aviso.enabled=false;Tocar(laminas,.75f,.9f);
                    Vector3 relative=Vector3.ProjectOnPlane(game.jogador.transform.position-transform.position,Vector3.up);
                    float along=Vector3.Dot(relative,direcao),side=(relative-direcao*along).magnitude;
                    if(along>0&&along<5.8f&&side<.85f&&Visivel())game.jogador.ReceberDano(Fase2?33:27,transform.position);
                }
                if(corrente){corrente.enabled=t>=.48f&&t<.8f;corrente.positionCount=9;float reach=Mathf.Sin(Mathf.Clamp01((t-.45f)/.35f)*Mathf.PI)*5.8f;for(int i=0;i<9;i++){float u=i/8f;corrente.SetPosition(i,transform.position+Vector3.up*1.3f+direcao*(reach*u)+Vector3.up*Mathf.Sin(u*Mathf.PI)*.25f);}}
                if(t>=1)Recuperar(1.05f);return;
            }
            if (Estado=="JurosRotativos")
            {
                if(t>=.506f&&!acertou){acertou=true;if(aviso)aviso.enabled=false;Tocar(laminas,.7f,1);Atingir(3.1f,Fase2?29:22,true);}
                if(t>=1.35f)Recuperar(1.15f);return;
            }
            if (Estado=="QuebraDeBanca")
            {
                if(t>.40f&&t<.94f)Mover(Vector3.ProjectOnPlane(alvo-transform.position,Vector3.up).normalized*7.5f,delta);
                if(t>=.94f&&!acertou){acertou=true;if(aviso)aviso.enabled=false;Tocar(impactoChao,.72f,1);Atingir(2.5f,Fase2?42:34,true);}
                if(t>=1.5f)Recuperar(1.35f);return;
            }
            espera-=delta;
            float distance=Vector3.ProjectOnPlane(game.jogador.transform.position-transform.position,Vector3.up).magnitude;
            if(espera<=0&&Visivel()&&distance<9)
            {
                Golpe attack=(Golpe)(ataques++%4);
                if(attack==Golpe.CobrancaDupla&&distance>3)attack=Golpe.ExecucaoFiscal;
                ComecarAtaque(attack);return;
            }
            if(distance>2.1f)
            {
                if(Time.time>=caminhoAte){caminhoAte=Time.time+.7f;var path=Corpo.navegacao.Caminho(transform.position,game.jogador.transform.position);proximoNo=path.FirstOrDefault(p=>Vector3.ProjectOnPlane(p-transform.position,Vector3.up).magnitude>.35f);}
                Vector3 direction=Vector3.ProjectOnPlane((proximoNo==Vector3.zero?game.jogador.transform.position:proximoNo)-transform.position,Vector3.up).normalized;
                Mover(direction*(Fase2?2.4f:1.8f),delta);
            }
        }
        void Recuperar(float duration){Mudar("Recuperando");espera=duration*Ritmo;OcultarAvisos();}
        void Mover(Vector3 velocity,float delta)
        {
            if(!body.enabled)return;
            body.Move((velocity+Vector3.down*2)*delta);
            Vector3 p=transform.position;p.x=Mathf.Clamp(p.x,-8.5f,8.5f);p.z=Mathf.Clamp(p.z,123.3f,132.5f);
            if(p!=transform.position){body.enabled=false;transform.position=p;body.enabled=true;}
        }
        void LateUpdate()
        {
            if(!Corpo||!Corpo.visual)return;
            if(som)som.volume=game.GetComponent<SornyAudioDirector>().volumeEfeitos;
            var cam=Camera.main;if(cam){Vector3 dir=Corpo.visual.transform.position-cam.transform.position;dir.y=0;if(dir.sqrMagnitude>.001f)Corpo.visual.transform.rotation=Quaternion.LookRotation(dir);}
            Corpo.visual.color=TempoDesdeHit<.12f?new Color(1,.48f,.38f):Estado=="CaloteDivino"?new Color(.7f,1,.55f):Color.white;
            if(Ativo&&!Corpo.Morto&&!game.Terminou){game.painelInimigo.SetActive(true);game.inimigoNome.text="CREDITON — DEUS DOS JUROS"+(Fase2?" / CALOTE DIVINO":"");float life=(float)Corpo.Vida/Corpo.vidaMaxima;game.inimigoPreenchimento.fillAmount=life;game.inimigoPreenchimento.rectTransform.localScale=new Vector3(life,1,1);}
        }
    }
}
