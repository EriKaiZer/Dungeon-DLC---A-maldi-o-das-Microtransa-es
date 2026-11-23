using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace DungeonDLC.Sorny
{
 public sealed class SornyEconomy:MonoBehaviour
 {
  [Serializable]public class State{public bool espada,armadura,botas,bossActive,machado;public string[] baus;public int armaDireita,armaEsquerda;public bool armaduraGuardada,botasGuardadas;public int pocoes;}
  public Transform loja;public SornyBoss boss;public SornyChest[] baus;
  public bool Espada{get;private set;}public bool Armadura{get;private set;}public bool Botas{get;private set;}
  public static readonly int[] Precos={60,45,35,30};
  public const int MaxPocoes=3;
  public int Pocoes{get;private set;}
  public bool ArmaduraEquipada=>Armadura&&!armaduraGuardada;
  public bool BotasEquipadas=>Botas&&!botasGuardadas;
  bool armaduraGuardada,botasGuardadas;
  public string Mensagem{get;private set;}="";
  public bool LojaAberta=>inventory&&inventory.Aberto&&inventory.ModoLoja;
  SornyInventoryUI inventory;
  SornyGameLoop game;Text gearText;
  void Start()
  {
   game = GetComponent<SornyGameLoop>();
   inventory = GetComponent<SornyInventoryUI>();
   Apply();
   var canvas = game.vidaTexto.GetComponentInParent<Canvas>();
   var go = new GameObject("Equipamentos", typeof(RectTransform), typeof(Text));
   go.transform.SetParent(canvas.transform, false);
   var rect = go.GetComponent<RectTransform>();
   rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
   rect.anchoredPosition = new Vector2(24, -102);
   rect.sizeDelta = new Vector2(440, 110);
   gearText = go.GetComponent<Text>();
   gearText.font = Resources.Load<SornyMenuTheme>("SornyMenuTheme").fonte;
   gearText.fontSize = 16;
   gearText.alignment = TextAnchor.UpperLeft;
   gearText.color = new Color(1, .8f, .4f);
   gearText.raycastTarget = false;
   go.SetActive(false);
   var shadow = go.AddComponent<Shadow>();
   shadow.effectColor = new Color(0, 0, 0, .85f);
   shadow.effectDistance = new Vector2(1, -1);
  }
  public bool Owned(int item)=>item==0?Espada:item==1?Armadura:item==2&&Botas;
  public void LimparMensagem(){Mensagem="";}
  public bool Buy(int item)
  {
   if(item<0||item>=Precos.Length||Owned(item)||game.Terminou)return false;
   var hands=game.espada.GetComponent<SornyHandEquipment>();
   if(item==0&&!hands.EspadaLiberada){Mensagem="ENCONTRE PRIMEIRO SUA ESPADA.";return false;}
   if(item==3&&Pocoes>=MaxPocoes){Mensagem="VOCÊ JÁ CARREGA TRÊS POÇÕES.";return false;}
   if(game.Moedas<Precos[item]){Mensagem="FALTAM "+(Precos[item]-game.Moedas)+" MOEDAS.";return false;}
   game.Moedas-=Precos[item];
   if(item==0)Espada=true;else if(item==1){Armadura=true;armaduraGuardada=false;}else if(item==2){Botas=true;botasGuardadas=false;}else Pocoes++;
   Apply();Mensagem=item==3?"POÇÃO GUARDADA NO INVENTÁRIO.":"COMPRADO E EQUIPADO.";
   var merchant=loja?loja.GetComponent<SornyMerchant>():null;if(merchant)merchant.AoComprar();
   if(SornySession.Instancia)SornySession.Instancia.Save();return true;
  }
  public bool AlternarArmadura(){if(!Armadura)return false;armaduraGuardada=!armaduraGuardada;Apply();Mensagem=ArmaduraEquipada?"ARMADURA EQUIPADA.":"ARMADURA GUARDADA.";if(SornySession.Instancia)SornySession.Instancia.Save();return true;}
  public bool AlternarBotas(){if(!Botas)return false;botasGuardadas=!botasGuardadas;Apply();Mensagem=BotasEquipadas?"BOTAS EQUIPADAS.":"BOTAS GUARDADAS.";if(SornySession.Instancia)SornySession.Instancia.Save();return true;}
  public bool UsarPocao()
  {
   if(!game||game.Terminou||game.jogador.Morto||Pocoes<=0){Mensagem="SEM POÇÕES.";return false;}
   int cura=Mathf.Min(25,game.jogador.vidaMaxima-game.jogador.Vida);
   if(!game.jogador.Curar(25)){Mensagem="SUA VIDA JÁ ESTÁ COMPLETA.";return false;}
   Pocoes--;Mensagem="POÇÃO UTILIZADA: +"+cura+" DE VIDA.";if(inventory)inventory.TocarCura();
   if(SornySession.Instancia)SornySession.Instancia.Save();return true;
  }
  public void Apply()
  {
   if (!game) game = GetComponent<SornyGameLoop>();
   if (!game) return;
   var hands = game.espada.GetComponent<SornyHandEquipment>();
   if (hands) hands.AtualizarMelhoria(Espada);
   else
   {
    game.espada.GetComponent<SornySwordDamage>().dano = Espada ? 40 : 25;
    var image = game.espada.GetComponent<Image>();
    if (image) image.color = Espada ? new Color(.65f, .83f, 1) : Color.white;
   }
   game.jogador.reducaoDano = ArmaduraEquipada ? .25f : 0;
   game.movimento.velocidade = BotasEquipadas ? 4.2f : 3.5f;
  }

  public State Capture()
  {
   var hands = game.espada.GetComponent<SornyHandEquipment>();
   return new State
   {
    espada = Espada, armadura = Armadura, botas = Botas,
    bossActive = boss && boss.Ativo,
    machado = hands && hands.MachadoLiberado,
    baus = baus.Where(b => b && b.gameObject.activeInHierarchy && b.Aberto).Select(b => b.id).ToArray(),
    armaDireita = hands ? (int)hands.maoDireita : 0,
    armaEsquerda = hands ? (int)hands.maoEsquerda : 0,
    armaduraGuardada=armaduraGuardada,botasGuardadas=botasGuardadas,pocoes=Pocoes
   };
  }

  public void Restore(State state)
  {
   Espada = state != null && state.espada;
   Armadura = state != null && state.armadura;
   Botas = state != null && state.botas;
   armaduraGuardada=state!=null&&state.armaduraGuardada;botasGuardadas=state!=null&&state.botasGuardadas;Pocoes=state!=null?Mathf.Clamp(state.pocoes,0,MaxPocoes):0;
   foreach (var chest in baus) if(chest) chest.SetOpen(state != null && state.baus != null && state.baus.Contains(chest.id));
   Apply();
   var hands = game.espada.GetComponent<SornyHandEquipment>();
   if (hands) { hands.RestaurarMachado(state != null && state.machado && boss && boss.Corpo.Morto); hands.RestaurarEscolhas(state != null ? state.armaDireita : 0, state != null ? state.armaEsquerda : 0); }
   if (boss) boss.LoadState(state != null && state.bossActive);
  }
  public void ResetRun(){if(LojaAberta)CloseShop();Restore(null);if(boss)boss.ResetBoss();}
  public bool Interaction(SornyPickup pickup,float pickupDistance)
  {
   if(!game||game.Terminou||Time.timeScale<=0)return false;var camera=Camera.main;if(!camera)return false;
   float distance=pickup?pickupDistance:2.65f;SornyChest chosen=null;bool merchant=false;
   foreach(var b in baus)if(b && b.gameObject.activeInHierarchy && CanReach(b.transform.position+Vector3.up*.55f,b.transform,distance,out float d)){distance=d;chosen=b;}
   if(loja&&CanReach(loja.position+Vector3.up*.65f,loja,distance,out float m)){merchant=true;chosen=null;}
   if(!merchant&&!chosen)return false;
   game.dicaTexto.text=merchant?"E  /  NEGOCIAR COM O MERCADOR":chosen.Aberto?"BAÚ VAZIO":chosen.contemMachado?"E  /  ABRIR TESOURO — ARMA":"E  /  ABRIR BAÚ";
   if(Keyboard.current!=null&&Keyboard.current.eKey.wasPressedThisFrame){if(merchant)OpenShop();else chosen.Open(game);}return true;
  }
  bool CanReach(Vector3 p,Transform owner,float limit,out float distance)
  {
   var camera=Camera.main;Vector3 delta=p-camera.transform.position;distance=delta.magnitude;if(distance>limit||Vector3.Dot(camera.transform.forward,delta.normalized)<.4f)return false;
   foreach(var hit in Physics.RaycastAll(camera.transform.position,delta.normalized,distance,~0,QueryTriggerInteraction.Ignore))if(!hit.transform.IsChildOf(owner)&&!hit.collider.GetComponentInParent<SornyPlayerLife>())return false;return true;
  }
  public void OpenShop()
  {if(game.Terminou||Time.timeScale==0)return;if(!inventory)inventory=GetComponent<SornyInventoryUI>();if(inventory){Mensagem="";inventory.AbrirLoja(loja?loja.GetComponent<SornyMerchant>():null);}}
  public void CloseShop(){if(inventory)inventory.Fechar();}
  void LateUpdate()
  {
   if (gearText)
   {
    gearText.gameObject.SetActive(false);
    var hands = game.espada.GetComponent<SornyHandEquipment>();
    gearText.text = hands
     ? "D  " + hands.NomeDireita + "\nE  " + hands.NomeEsquerda + "\nARMADURA " + (Armadura ? "EQUIPADA" : "NENHUMA") + "\nBOTAS " + (Botas ? "EQUIPADAS" : "COMUNS") + (hands.MachadoLiberado ? "\n6  MAÇA-CHAVE DO SÓ-NÃO" : "")
     : "ESPADA " + (Espada ? "ESTORNO 40" : "COMUM 25");
   }
   if(boss&&boss.Ativo&&!boss.Corpo.Morto&&!game.Terminou){game.painelInimigo.SetActive(true);game.inimigoNome.text="LORDE SÓ-NÃO"+(boss.Fase2?" — FÚRIA":"");float life=(float)boss.Corpo.Vida/boss.Corpo.vidaMaxima;game.inimigoPreenchimento.fillAmount=life;game.inimigoPreenchimento.rectTransform.localScale=new Vector3(life,1,1);}
  }
 }
}

