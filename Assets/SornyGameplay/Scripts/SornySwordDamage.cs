using UnityEngine;
namespace DungeonDLC.Sorny
{
    [RequireComponent(typeof(Caloteus.EspadaFluida))]
    public sealed class SornySwordDamage : MonoBehaviour
    {
        public int dano=25;public float alcance=2.6f;
        Caloteus.EspadaFluida espada;
        void OnEnable(){espada=GetComponent<Caloteus.EspadaFluida>();espada.aoGolpear.AddListener(AoGolpear);}
        void OnDisable(){if(espada)espada.aoGolpear.RemoveListener(AoGolpear);}
        void AoGolpear(){Golpear();}
        public CrackEnemy Golpear()
        {
            var cam=Camera.main;if(!cam)return null;CrackEnemy alvo=null;float melhor=alcance;
            foreach(var enemy in FindObjectsByType<CrackEnemy>(FindObjectsSortMode.None))
            {
                if(enemy.Morto)continue;var boss=enemy.GetComponent<SornyBoss>();if(boss&&!boss.PodeReceberDano)continue;var finalBoss=enemy.GetComponent<SornyCrediton>();if(finalBoss&&!finalBoss.PodeReceberDano)continue;Vector3 p=enemy.transform.position+Vector3.up*1.05f;Vector3 delta=p-cam.transform.position;
                float d=Vector3.Distance(Vector3.ProjectOnPlane(p,Vector3.up),Vector3.ProjectOnPlane(cam.transform.position,Vector3.up));
                if(d>melhor||Vector3.Dot(cam.transform.forward,delta.normalized)<.55f)continue;
                if(Physics.Raycast(cam.transform.position,delta.normalized,out var hit,delta.magnitude+.05f,~0,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponentInParent<CrackEnemy>()!=enemy)continue;
                alvo=enemy;melhor=d;
            }
            SornyTrainingDummy treino = null;
            foreach (var dummy in FindObjectsByType<SornyTrainingDummy>(FindObjectsSortMode.None))
            {
                Vector3 p = dummy.transform.position + Vector3.up * 1.1f;
                Vector3 delta = p - cam.transform.position;
                float d = Vector3.Distance(Vector3.ProjectOnPlane(p, Vector3.up), Vector3.ProjectOnPlane(cam.transform.position, Vector3.up));
                if (d > melhor || Vector3.Dot(cam.transform.forward, delta.normalized) < .55f) continue;
                if (Physics.Raycast(cam.transform.position, delta.normalized, out var hit, delta.magnitude + .05f, ~0, QueryTriggerInteraction.Ignore)
                    && hit.collider.GetComponentInParent<SornyTrainingDummy>() != dummy) continue;
                treino = dummy; melhor = d;
            }
            if (treino)
            {
                treino.ReceberGolpe(espada.GolpePesado); espada.ConfirmarImpacto();
                var jogo = FindFirstObjectByType<SornyGameLoop>();
                if (jogo) jogo.movimento.AplicarImpacto(espada.GolpePesado ? 1.2f : .7f, espada.espelhar);
                return null;
            }
            int danoAtual=Mathf.CeilToInt(dano*espada.MultiplicadorDano);
            if (!alvo || !alvo.ReceberDano(danoAtual)) return null;
            var crediton=alvo.GetComponent<SornyCrediton>();if(crediton)crediton.RegistrarImpacto(espada.GolpePesado);
            espada.ConfirmarImpacto();
            alvo.Empurrar(cam.transform.forward, espada.golpeCurto ? 1.25f : 2.2f);
            var game = FindFirstObjectByType<SornyGameLoop>();
            if (game)
            {
                var chegada = game.GetComponent<SornyArrival>(); if (chegada) chegada.RegistrarAcerto(espada.GolpePesado);
                game.movimento.AplicarImpacto((espada.golpeCurto ? .6f : 1)*(espada.GolpePesado?1.3f:1), espada.espelhar);
                var audio = game.GetComponent<SornyAudioDirector>();
                if (audio) audio.TocarAcerto(alvo, espada.golpeCurto, espada.GolpePesado);
                game.Notificar((espada.GolpePesado?"GOLPE PESADO  -":"GOLPE CONFIRMADO  -") + danoAtual, .65f);
            }
            return alvo;
        }
    }
}
