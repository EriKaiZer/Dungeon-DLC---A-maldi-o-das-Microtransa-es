using UnityEngine;

namespace DungeonDLC.Sorny
{
    public sealed class SornyMerchant : MonoBehaviour
    {
        public SpriteRenderer visual;
        public Sprite[] parado,compra;
        public AudioClip somCompra;
        public string nome="Mercador do Estorno";
        public int ComprasRealizadas{get;private set;}
        public string Fala{get;private set;}="Equipamento bom. Cobrança única.";
        public bool Vendendo=>Time.unscaledTime<compraAte;
        float compraAte,saudacaoAte;
        AudioSource fonte;
        public Sprite QuadroAtual
        {
            get
            {
                if(Vendendo&&compra!=null&&compra.Length>0)
                {float t=1-(compraAte-Time.unscaledTime)/1.15f;return compra[Mathf.Clamp(Mathf.FloorToInt(t*compra.Length),0,compra.Length-1)];}
                if(parado==null||parado.Length==0)return null;
                float fase=Mathf.Repeat(Time.unscaledTime,3);
                int quadro=fase<2.55f?0:fase<2.7f?1:fase<2.82f?2:3;
                return parado[Mathf.Min(quadro,parado.Length-1)];
            }
        }
        void Awake(){fonte=gameObject.AddComponent<AudioSource>();fonte.playOnAwake=false;fonte.spatialBlend=1;fonte.minDistance=2;fonte.maxDistance=10;fonte.dopplerLevel=0;fonte.priority=40;}
        public void Cumprimentar(){Fala="Vai levar ou só conferir o preço?";saudacaoAte=Time.unscaledTime+4;}
        public void AoComprar()
        {
            ComprasRealizadas++;compraAte=Time.unscaledTime+1.15f;
            Fala=ComprasRealizadas%2==0?"Pronto. Esse item é seu de verdade.":"Obrigado! Aqui o recibo vem junto.";
            saudacaoAte=Time.unscaledTime+5;
            if(fonte&&somCompra){fonte.volume=SornySession.Efeitos*.8f;fonte.pitch=1;fonte.PlayOneShot(somCompra);}
        }
        void Update(){if(visual)visual.sprite=QuadroAtual;if(saudacaoAte>0&&Time.unscaledTime>saudacaoAte){Fala="Equipamento bom. Cobrança única.";saudacaoAte=0;}}
        void LateUpdate(){if(!visual||!Camera.main)return;Vector3 d=visual.transform.position-Camera.main.transform.position;d.y=0;if(d.sqrMagnitude>.01f)visual.transform.rotation=Quaternion.LookRotation(d);}
    }
}
