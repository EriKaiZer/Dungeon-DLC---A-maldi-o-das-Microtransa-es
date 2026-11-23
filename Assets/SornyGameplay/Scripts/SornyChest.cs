using UnityEngine;
namespace DungeonDLC.Sorny
{
 public sealed class SornyChest:MonoBehaviour
 {
  public string id;public int moedas=40;public bool contemMachado;public GameObject fechado,aberto;public bool Aberto{get;private set;}
  public void SetOpen(bool value){Aberto=value;if(fechado)fechado.SetActive(!value);if(aberto)aberto.SetActive(value);}
  public bool Open(SornyGameLoop game){if(Aberto||game.Terminou)return false;SetOpen(true);game.Moedas+=moedas;if(contemMachado){game.espada.GetComponent<SornyHandEquipment>().ReceberMachado();game.Notificar("MACHADO ROMPE-TAXAS ENCONTRADO!  TECLA 6  |  +"+moedas+" MOEDAS",5);}else game.Notificar("BAÚ ABERTO  +"+moedas+" MOEDAS",2);if(SornySession.Instancia)SornySession.Instancia.Save();return true;}
 }
}
