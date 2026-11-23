using UnityEngine;
namespace DungeonDLC.Sorny
{
    public sealed class SornyPlayerLife : MonoBehaviour
    {
        public int vidaMaxima=100;[Range(0,.8f)]public float reducaoDano;
        public int Vida {get;private set;}
        public bool Morto=>Vida<=0;
        float proximoDano;
        public SornyHandEquipment equipamentos;
        void Awake(){Restaurar();}
        public void Restaurar(){Vida=vidaMaxima;proximoDano=0;}
        public bool ReceberDano(int valor)
        {
            return AplicarDano(valor, null);
        }

        public bool ReceberDano(int valor, Vector3 origem)
        {
            return AplicarDano(valor, origem);
        }

        bool AplicarDano(int valor, Vector3? origem)
        {
            if(Morto || valor<=0 || Time.time<proximoDano)return false;
            bool bloqueado = origem.HasValue && equipamentos && equipamentos.TentarBloquear(origem.Value);
            if (bloqueado)
                valor = Mathf.Max(1, Mathf.CeilToInt(valor * .4f));
            valor=Mathf.Max(1,Mathf.CeilToInt(valor*(1-reducaoDano)));Vida=Mathf.Max(0,Vida-valor);proximoDano=Time.time+.65f;
            var game=FindFirstObjectByType<SornyGameLoop>();
            if(game){var audio=game.GetComponent<SornyAudioDirector>();if(audio)audio.TocarDanoJogador(bloqueado);game.PiscarDano();if(Morto)game.Encerrar(false);}
            return true;
        }
        public void CarregarVida(int valor){Vida=Mathf.Clamp(valor,1,vidaMaxima);proximoDano=0;}
        public bool Curar(int valor){if(Morto||Vida>=vidaMaxima)return false;Vida=Mathf.Min(vidaMaxima,Vida+valor);return true;}
    }
}
