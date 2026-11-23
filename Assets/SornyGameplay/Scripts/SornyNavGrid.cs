using System.Collections.Generic;
using UnityEngine;
namespace DungeonDLC.Sorny
{
    public sealed class SornyNavGrid : MonoBehaviour
    {
        public Vector3 origem=new Vector3(-10.5f,0,-6);
        public float passo=.5f;
        public int colunas=43,linhas=153;
        [HideInInspector] public bool[] livres;
        public Vector3 Ponto(int i)=>origem+new Vector3((i%colunas)*passo,.04f,(i/colunas)*passo);
        int Celula(Vector3 p){int x=Mathf.Clamp(Mathf.RoundToInt((p.x-origem.x)/passo),0,colunas-1);int z=Mathf.Clamp(Mathf.RoundToInt((p.z-origem.z)/passo),0,linhas-1);return z*colunas+x;}
        int ProximaLivre(int i)
        {
            if(livres[i])return i;int melhor=-1;float dist=float.MaxValue;var p=Ponto(i);
            for(int j=0;j<livres.Length;j++)if(livres[j]){float d=(Ponto(j)-p).sqrMagnitude;if(d<dist){dist=d;melhor=j;}}
            return melhor;
        }
        public List<Vector3> Caminho(Vector3 de,Vector3 para)
        {
            var result=new List<Vector3>();if(livres==null||livres.Length!=colunas*linhas)return result;
            int start=ProximaLivre(Celula(de)),end=ProximaLivre(Celula(para));if(start<0||end<0)return result;
            var g=new float[livres.Length];var previous=new int[livres.Length];var closed=new bool[livres.Length];
            for(int i=0;i<g.Length;i++){g[i]=float.MaxValue;previous[i]=-1;}g[start]=0;var open=new List<int>{start};
            int[] dx={1,-1,0,0,1,-1,1,-1},dz={0,0,1,-1,1,1,-1,-1};
            while(open.Count>0)
            {
                int best=0;float score=float.MaxValue;
                for(int i=0;i<open.Count;i++){int c=open[i];float f=g[c]+Vector3.Distance(Ponto(c),Ponto(end));if(f<score){score=f;best=i;}}
                int at=open[best];open.RemoveAt(best);if(at==end){for(int c=end;c!=start&&c>=0;c=previous[c])result.Add(Ponto(c));result.Reverse();return result;}
                closed[at]=true;int x=at%colunas,z=at/colunas;
                for(int k=0;k<8;k++)
                {
                    int nx=x+dx[k],nz=z+dz[k];if(nx<0||nx>=colunas||nz<0||nz>=linhas)continue;
                    int next=nz*colunas+nx;if(!livres[next]||closed[next])continue;
                    if(k>=4&&(!livres[z*colunas+nx]||!livres[nz*colunas+x]))continue;
                    float cost=g[at]+(k<4?passo:passo*1.414214f);if(cost>=g[next])continue;
                    g[next]=cost;previous[next]=at;if(!open.Contains(next))open.Add(next);
                }
            }
            return result;
        }
        public void Bake()
        {
            Physics.SyncTransforms();livres=new bool[colunas*linhas];
            for(int i=0;i<livres.Length;i++)
            {
                Vector3 p=Ponto(i);bool floor=false;
                foreach(var hit in Physics.RaycastAll(p+Vector3.up*.5f,Vector3.down,.9f,~0,QueryTriggerInteraction.Ignore))
                    if(!hit.collider.GetComponentInParent<CrackEnemy>()&&!hit.collider.GetComponentInParent<SornyPlayerLife>()&&hit.normal.y>.7f){floor=true;break;}
                if(!floor)continue;bool blocked=false;
                foreach(var c in Physics.OverlapCapsule(p+Vector3.up*.39f,p+Vector3.up*1.45f,.28f,~0,QueryTriggerInteraction.Ignore))
                    if(!c.GetComponentInParent<CrackEnemy>()&&!c.GetComponentInParent<SornyPlayerLife>()){blocked=true;break;}
                livres[i]=!blocked;
            }
        }
    }
}
