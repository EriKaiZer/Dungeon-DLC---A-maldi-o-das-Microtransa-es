using UnityEngine;
namespace DungeonDLC.Sorny
{
    public sealed class SornyPickup : MonoBehaviour
    {
        public enum Tipo{Cura,Moeda,Fragmento,MidiaFinal,ArmaChefe,EspadaInicial,EscudoInicial,AdagaTesouro}
        public Tipo tipo;public int valor=30;public string titulo="Estorno Vital";
        public bool disponivelNoNivel = true;
        public bool flutuar = true, girar = true;
        public Transform visual;Vector3 inicio;
        void Awake(){if(visual)inicio=visual.localPosition;if(!disponivelNoNivel)gameObject.SetActive(false);}
        public void RestaurarDisponibilidade(bool ativo){gameObject.SetActive(disponivelNoNivel && ativo);}
        void Update(){if(visual){if(flutuar)visual.localPosition=inicio+Vector3.up*(.07f*Mathf.Sin(Time.time*2));if(girar)visual.Rotate(0,45*Time.deltaTime,0,Space.Self);}}
        public string Dica(SornyGameLoop game)
        {
            if(tipo==Tipo.EspadaInicial)return "E  /  PEGAR ESPADA";
            if(tipo==Tipo.EscudoInicial)return "E  /  PEGAR ESCUDO";
            if(tipo==Tipo.AdagaTesouro)return "E  /  PEGAR ADAGA  [2]";
            if(tipo==Tipo.Fragmento){var progress=game.GetComponent<SornyProgression>();if(progress&&!progress.ChaveRecuperada)return "DERROTE LORDE SÓ-NÃO";}
            if(tipo==Tipo.ArmaChefe)return "E  /  RECOLHER MAÇA-CHAVE DO SÓ-NÃO  [6]";
            if(tipo==Tipo.MidiaFinal&&game.Derrotados<game.Total)return "DERROTE OS GUARDIÕES: "+game.Derrotados+"/"+game.Total;
            if(tipo==Tipo.Cura&&game.jogador.Vida>=game.jogador.vidaMaxima)return "VIDA COMPLETA";
            return "E  /  "+titulo.ToUpperInvariant();
        }
        public bool Usar(SornyGameLoop game)
        {
            if (!disponivelNoNivel) return false;
            if(tipo==Tipo.EspadaInicial||tipo==Tipo.EscudoInicial||tipo==Tipo.AdagaTesouro)
            {var chegada=game.GetComponent<SornyArrival>();if(!chegada||!chegada.Coletar(tipo))return false;}
            if(tipo==Tipo.Cura){if(!game.jogador.Curar(valor))return false;game.Notificar("ESTORNO VITAL  +"+valor+" PV",2);}
            if(tipo==Tipo.Moeda){game.Moedas+=valor;game.Notificar("TAXA RECUPERADA  +"+valor,1.5f);}
            if(tipo==Tipo.Fragmento){var progress=game.GetComponent<SornyProgression>();if(progress){if(!progress.ColetarMidia())return false;}else{if(game.Derrotados<game.Total)return false;game.Encerrar(true);}}
            if(tipo==Tipo.ArmaChefe){var progress=game.GetComponent<SornyProgression>();if(!progress||!progress.ChaveRecuperada||game.espada.GetComponent<SornyHandEquipment>().MachadoLiberado)return false;game.espada.GetComponent<SornyHandEquipment>().ReceberMachado();game.Notificar("MAÇA-CHAVE DO SÓ-NÃO RECUPERADA!  TECLA 6",5);}
            if(tipo==Tipo.MidiaFinal){if(game.Nivel!=2||game.Derrotados<game.Total)return false;game.Encerrar(true);}
            gameObject.SetActive(false);if(SornySession.Instancia&&!game.Terminou)SornySession.Instancia.Save();return true;
        }
    }
}
