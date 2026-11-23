using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
namespace DungeonDLC.Sorny
{
    public sealed class SornyGameLoop : MonoBehaviour
    {
        public SornyPlayerLife jogador;
        public MovimentoGrade movimento;
        public Caloteus.EspadaFluida espada;
        public Image vidaPreenchimento,danoTela,inimigoPreenchimento;
        public Text vidaTexto,inimigoNome,objetivoTexto,dicaTexto,avisoTexto,fimTexto;
        public GameObject painelInimigo,painelFim;
        public int Nivel => progressao ? progressao.NivelAtual : 1;
        public int Derrotados => inimigos == null ? 0 : inimigos.Count(e => e && e.nivel == Nivel && e.Morto);
        public int Total => inimigos == null ? 0 : inimigos.Count(e => e && e.nivel == Nivel);
        SornyProgression progressao;
        public int Moedas;
        public bool Terminou{get;private set;}
        CrackEnemy[] inimigos;SornyPickup[] itens;
        float avisoAte,flash;Vector3 inicio;Quaternion giroInicio;
        void Start()
        {
            inimigos=FindObjectsByType<CrackEnemy>(FindObjectsSortMode.None);itens=FindObjectsByType<SornyPickup>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            progressao=GetComponent<SornyProgression>();inicio=jogador.transform.position;giroInicio=jogador.transform.rotation;Terminou=false;
            painelFim.SetActive(false);painelInimigo.SetActive(false);Notificar(GetComponent<SornyArrival>()?"A CHEGADA À FORTALEZA":"FORTALEZA AZUL DAS EXCLUSIVIDADES",4);
        }
        public void PiscarDano(){flash=.22f;}
        public void Notificar(string texto,float duracao){if(avisoTexto)avisoTexto.text=texto;avisoAte=Time.time+duracao;}
        public void InimigoDerrotado(){Moedas+=10;Notificar("+10 MOEDAS",1.2f);}
        public void Encerrar(bool venceu)
        {
            Terminou=true;movimento.bloquearControles=true;espada.bloquearAtaque=true;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            painelFim.SetActive(true);fimTexto.text=venceu?"ACERVO RECUPERADO\n\nVocê venceu as duas masmorras e recuperou a mídia física!\n\nR  /  JOGAR NOVAMENTE":"SUA LICENÇA EXPIROU\n\nAté a morte exige renovação.\n\nR  /  TENTAR NOVAMENTE";
        }
        public void Reiniciar()
        {
            var cc=jogador.GetComponent<CharacterController>();if(cc)cc.enabled=false;jogador.transform.SetPositionAndRotation(inicio,giroInicio);if(cc)cc.enabled=true;
            jogador.Restaurar();foreach(var e in inimigos)if(e)e.Reiniciar();foreach(var item in itens)if(item)item.RestaurarDisponibilidade(true);var economy=GetComponent<SornyEconomy>();if(economy)economy.ResetRun();
            var chegada=GetComponent<SornyArrival>();if(chegada)chegada.Reiniciar();
            if(progressao)progressao.Restaurar(null);Moedas=0;Terminou=false;flash=0;painelFim.SetActive(false);movimento.bloquearControles=false;espada.bloquearAtaque=false;
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;Notificar("NOVA TENTATIVA. MESMA MENSALIDADE.",2);
        }
        void Update()
        {
            if(!jogador)return;if(Keyboard.current!=null&&Keyboard.current.rKey.wasPressedThisFrame&&Terminou){Reiniciar();return;}
            vidaTexto.text=jogador.Vida+" / "+jogador.vidaMaxima;vidaPreenchimento.fillAmount=(float)jogador.Vida/jogador.vidaMaxima;
            vidaPreenchimento.rectTransform.localScale=new Vector3((float)jogador.Vida/jogador.vidaMaxima,1,1);
            objetivoTexto.text=(progressao?progressao.Objetivo:"COBRADORES  "+Derrotados+" / "+Total)+"   |   MOEDAS  "+Moedas;
            if(Time.time>avisoAte)avisoTexto.text="";flash=Mathf.MoveTowards(flash,0,Time.unscaledDeltaTime*.65f);danoTela.color=new Color(.7f,.03f,.01f,flash);
            if(Terminou||Time.timeScale<=0)return;var cam=Camera.main;if(!cam)return;
            CrackEnemy visto=null;float melhor=.86f;
            foreach(var enemy in inimigos)
            {
                if(!enemy||enemy.Morto)continue;Vector3 delta=enemy.transform.position+Vector3.up-cam.transform.position;
                float dot=Vector3.Dot(cam.transform.forward,delta.normalized);if(delta.magnitude>10||dot<melhor)continue;
                if(Physics.Raycast(cam.transform.position,delta.normalized,out var hit,delta.magnitude+.05f,~0,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<CrackEnemy>()==enemy){visto=enemy;melhor=dot;}
            }
            painelInimigo.SetActive(visto);if(visto){inimigoNome.text=visto.nome;inimigoPreenchimento.fillAmount=(float)visto.Vida/visto.vidaMaxima;inimigoPreenchimento.rectTransform.localScale=new Vector3((float)visto.Vida/visto.vidaMaxima,1,1);}
            SornyPickup perto=null;float distancia=2.35f;
            foreach(var item in itens)
            {
                if(!item||!item.gameObject.activeInHierarchy)continue;Vector3 delta=item.transform.position+Vector3.up*.4f-cam.transform.position;
                if(delta.magnitude>distancia||Vector3.Dot(cam.transform.forward,delta.normalized)<.25f)continue;
                // Ignore item triggers, but do not interact through walls.
                bool parede=false;foreach(var hit in Physics.RaycastAll(cam.transform.position,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))
                    if(!hit.collider.GetComponentInParent<SornyPlayerLife>()&&!hit.collider.GetComponentInParent<CrackEnemy>()){parede=true;break;}
                if(!parede){perto=item;distancia=delta.magnitude;}
            }
            var commerce=GetComponent<SornyEconomy>();if(commerce&&commerce.Interaction(perto,distancia))return;
            dicaTexto.text=perto?perto.Dica(this):"";
            if(perto&&Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame)perto.Usar(this);
        }
    }
}

