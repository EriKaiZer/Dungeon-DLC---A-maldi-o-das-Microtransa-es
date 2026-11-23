using UnityEngine;
using UnityEngine.UI;
namespace DungeonDLC.Sorny
{
    [DefaultExecutionOrder(900)]
    public sealed class SornyFreeExploration : MonoBehaviour
    {
        SornyGameLoop game; SornyProgression progress;
        bool Bloqueado(Vector3 p)
        {
            foreach(var collider in Physics.OverlapCapsule(p+Vector3.up*.38f,p+Vector3.up*1.4f,.27f,~0,QueryTriggerInteraction.Ignore))
                if(!collider.GetComponentInParent<SornyPlayerLife>()&&!collider.GetComponentInParent<CrackEnemy>())return true;
            return false;
        }
        public void AjustarPosicaoSegura()
        {
            if(!game)game=GetComponent<SornyGameLoop>();Physics.SyncTransforms();
            var player=game.jogador.transform;var position=player.position;if(!Bloqueado(position))return;
            var nav=game.GetComponent<SornyNavGrid>();if(!nav||nav.livres==null)return;
            float closest=36;Vector3 target=position;
            for(int i=0;i<nav.livres.Length;i++)if(nav.livres[i]){var candidate=nav.Ponto(i);float d=(candidate-position).sqrMagnitude;if(d<closest&&!Bloqueado(candidate)){closest=d;target=candidate;}}
            if(target==position)return;var body=player.GetComponent<CharacterController>();body.enabled=false;player.position=target;body.enabled=true;Physics.SyncTransforms();
        }
        void Start()
        {
            game=GetComponent<SornyGameLoop>();progress=GetComponent<SornyProgression>();
            if(!SornySession.LoadSaved)game.espada.GetComponent<SornyHandEquipment>().RestaurarEscolhas(0,0);
            foreach(var text in FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(text.name=="Controles"||text.name=="Soltar_para_atacar")text.gameObject.SetActive(false);
        }
        void LateUpdate()
        {
            if(!game||game.Terminou)return;
            if(game.objetivoTexto)game.objetivoTexto.text=(game.Nivel==0?"CAMINHO DA FORTALEZA":game.Nivel==1?"FORTALEZA AZUL":"ACERVO SUBTERRÂNEO")+"   |   MOEDAS "+game.Moedas;
            if(Time.timeScale<=0||game.jogador.Morto||!progress)return;
            if(progress.ChaveRecuperada&&!progress.MidiaColetada&&game.jogador.transform.position.z>=77)
            {progress.ColetarMidia();if(SornySession.Instancia)SornySession.Instancia.Save();}
            if(game.Nivel==2&&game.jogador.transform.position.z>=130&&game.Derrotados>=game.Total)
                game.Encerrar(true);
        }
    }
}
