using System;
using UnityEngine;

namespace DungeonDLC.Sorny
{
    // A segunda masmorra prolonga a cena existente: um jogador, HUD e salvamento.
    [DefaultExecutionOrder(150)]
    public sealed class SornyProgression : MonoBehaviour
    {
        [Serializable] public class State { public bool midia, creditonAtivo; }
        public SornyBoss boss;
        public SornyCrediton crediton;
        public SornyPickup armaDoChefe;
        public Transform portaAcervo, portaMasmorra2;
        public Vector3 origemAcervo, origemMasmorra2;
        public bool MidiaColetada { get; private set; }
        public bool ChaveRecuperada => boss && boss.Corpo && boss.Corpo.Morto;
        public int NivelAtual => game && game.jogador.transform.position.z < -6 ? 0 : game && game.jogador.transform.position.z >= 90 ? 2 : 1;
        public string Objetivo => NivelAtual == 0 && chegada ? chegada.Objetivo : NivelAtual == 2
            ? crediton && crediton.Corpo.Morto ? "CREDITON DERROTADO — RECUPERE A ÚLTIMA MÍDIA"
                : "ACERVO SUBTERRÂNEO  |  ADVERSÁRIOS " + game.Derrotados + "/" + game.Total
            : !ChaveRecuperada ? "FORTALEZA AZUL  |  COBRADORES " + game.Derrotados + "/" + game.Total
            : MidiaColetada ? "PASSAGEM LIBERADA — EXPLORE O ACERVO SUBTERRÂNEO"
            : "CHAVE RECUPERADA — ENTRE NA SALA DA MÍDIA FÍSICA";
        SornyGameLoop game;
        SornyArrival chegada;
        bool chaveAnunciada;
        int ultimoNivel = 1;

        void Awake() { game = GetComponent<SornyGameLoop>(); chegada = GetComponent<SornyArrival>(); ultimoNivel = NivelAtual; }
        void Update()
        {
            if (!game || game.Terminou || Time.timeScale <= 0) return;
            if (ChaveRecuperada && !chaveAnunciada)
            {
                chaveAnunciada = true;
                game.Notificar("CHAVE DO ACERVO! A PASSAGEM ATRÁS DO TRONO FOI ABERTA", 5);
                if (SornySession.Instancia) SornySession.Instancia.Save();
            }
            AtualizarPortas(Time.deltaTime);
            if (NivelAtual != ultimoNivel)
            {
                ultimoNivel = NivelAtual;
                game.Notificar(ultimoNivel == 2 ? "MASMORRA 2 — ACERVO SUBTERRÂNEO" : ultimoNivel == 0 ? "CAMINHO DA FORTALEZA" : "FORTALEZA AZUL", 2);
                if (SornySession.Instancia) SornySession.Instancia.Save();
            }
        }

        void AtualizarPortas(float delta)
        {
            if (portaAcervo) portaAcervo.localPosition = Vector3.MoveTowards(portaAcervo.localPosition, origemAcervo + (ChaveRecuperada ? Vector3.up * 4.2f : Vector3.zero), delta * 3.2f);
            if (portaMasmorra2) portaMasmorra2.localPosition = Vector3.MoveTowards(portaMasmorra2.localPosition, origemMasmorra2 + (MidiaColetada ? Vector3.up * 4.2f : Vector3.zero), delta * 3.2f);
        }

        public bool ColetarMidia()
        {
            if (!ChaveRecuperada || MidiaColetada || game.Terminou) return false;
            MidiaColetada = true;
            game.Notificar("UM FRAGMENTO DA MÍDIA! O RESTANTE ESTÁ NO ACERVO SUBTERRÂNEO", 6);
            return true;
        }

        public void LiberarArmaChefe(Vector3 origem)
        {
            if(chegada && chegada.exploracaoLivre && ChaveRecuperada){game.espada.GetComponent<SornyHandEquipment>().RestaurarMachado(true);return;}
            if (!armaDoChefe || !ChaveRecuperada || game.espada.GetComponent<SornyHandEquipment>().MachadoLiberado) return;
            var nav = game.GetComponent<SornyNavGrid>();
            Vector3 destino = origem;
            float distancia = float.MaxValue;
            if (nav && nav.livres != null)
                for(int i=0;i<nav.livres.Length;i++)
                    if(nav.livres[i]){Vector3 p=nav.Ponto(i);float d=(p-origem).sqrMagnitude;if(d<distancia){distancia=d;destino=p;}}
            armaDoChefe.transform.position = new Vector3(destino.x,.08f,destino.z);
            armaDoChefe.RestaurarDisponibilidade(true);
        }

        public State Capturar() => new State { midia = MidiaColetada, creditonAtivo = crediton && crediton.Ativo };
        public void Restaurar(State state)
        {
            MidiaColetada = ChaveRecuperada && state != null && state.midia;
            chaveAnunciada = ChaveRecuperada;
            ultimoNivel = NivelAtual;
            if (portaAcervo) portaAcervo.localPosition = origemAcervo + (ChaveRecuperada ? Vector3.up * 4.2f : Vector3.zero);
            if (portaMasmorra2) portaMasmorra2.localPosition = origemMasmorra2 + (MidiaColetada ? Vector3.up * 4.2f : Vector3.zero);
            if (armaDoChefe)
            {
                armaDoChefe.RestaurarDisponibilidade(false);
                if(ChaveRecuperada) LiberarArmaChefe(boss.transform.position);
            }
            if(crediton){if(state==null)crediton.ResetBoss();else crediton.LoadState(state.creditonAtivo);}
            Physics.SyncTransforms();
        }
    }
}

