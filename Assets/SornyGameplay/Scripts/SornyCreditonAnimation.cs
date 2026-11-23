using UnityEngine;

namespace DungeonDLC.Sorny
{
    [DefaultExecutionOrder(190)]
    public sealed class SornyCreditonAnimation : MonoBehaviour
    {
        public SornyCrediton boss;
        public Sprite[] parado,caminhada,combo,hit,atordoado,correntes,giro,salto,fase2,morte;
        Vector3 last;
        void Awake(){last=transform.position;}
        void LateUpdate()
        {
            if(!boss||!boss.Corpo||!boss.Corpo.visual)return;
            Sprite[] frames=parado;int index=0;float t=boss.TempoEstado/boss.Ritmo;
            if(boss.Corpo.Morto){frames=morte;index=Mathf.FloorToInt(boss.TempoEstado*7);}
            else switch(boss.Estado)
            {
                case "CobrancaDupla":frames=combo;index=t<.17f?0:t<.34f?1:t<.50f?2:t<.70f?3:t<1.28f?4:t<1.46f?5:t<1.60f?6:7;break;
                case "ExecucaoFiscal":frames=correntes;index=Mathf.FloorToInt(t*8);break;
                case "JurosRotativos":frames=giro;index=Mathf.FloorToInt(t/1.35f*8);break;
                case "QuebraDeBanca":frames=salto;index=Mathf.FloorToInt(t/1.5f*8);break;
                case "CaloteDivino":frames=fase2;index=Mathf.FloorToInt(boss.TempoEstado*7);break;
                case "Atordoado":frames=atordoado;index=Mathf.FloorToInt(boss.TempoEstado*6);break;
                default:
                    if(boss.TempoDesdeHit<4f/12&&boss.Ativo){frames=hit;index=Mathf.FloorToInt(boss.TempoDesdeHit*12);}
                    else if((transform.position-last).sqrMagnitude>.000001f){frames=caminhada;index=Mathf.FloorToInt(Time.time*7)%Mathf.Max(1,caminhada.Length);}
                    else index=Mathf.FloorToInt(Time.time*5)%Mathf.Max(1,parado.Length);
                    break;
            }
            if(frames!=null&&frames.Length>0)boss.Corpo.visual.sprite=frames[Mathf.Clamp(index,0,frames.Length-1)];
            last=transform.position;
        }
    }
}
