using UnityEngine;
namespace DungeonDLC.Sorny
{
 public sealed class SornyMenuTheme:ScriptableObject
 {
  public Texture2D fundo; public Sprite titulo; public Texture2D[] abertura;
  public Font fonte; public AudioClip musica,selecionar,confirmar;public Shader atmosfera;
 }
}
