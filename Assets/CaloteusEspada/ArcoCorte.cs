using UnityEngine;
using UnityEngine.UI;

namespace Caloteus
{
    // O rastro amostra as poses reais da arma; não tem um trajeto independente.
    public class ArcoCorte : MaskableGraphic
    {
        public EspadaFluida espada;
        private EspadaFluida ativa;
        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }
        void LateUpdate()
        {
            ativa = espada;
            if (espada && !espada.Atacando && espada.outraMao
                && (espada.outraMao.Atacando || espada.outraMao.BrilhoImpacto > 0)) ativa = espada.outraMao;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (!ativa) return;
            if (ativa.Atacando && !ativa.golpeCurto)
            {
                float fim = Mathf.Min(ativa.TempoGolpe, ativa.FimCorte);
                float inicio = Mathf.Max(ativa.InicioCorte, fim - .055f);
                float apagar = 1 - Mathf.InverseLerp(ativa.FimCorte, ativa.FimCorte + .065f, ativa.TempoGolpe);
                if (fim > inicio && apagar > 0)
                {
                    Faixa(vh, inicio, fim, .58f, .99f, new Color(.70f, .84f, 1, .12f * apagar));
                    Faixa(vh, inicio, fim, .88f, 1, new Color(1, .96f, .80f, .30f * apagar));
                }
            }
            if (ativa.BrilhoImpacto <= 0 || !canvas) return;
            var root = canvas.rootCanvas.transform as RectTransform;
            Vector2 centro = rectTransform.InverseTransformPoint(root.TransformPoint(root.rect.center));
            var cor = new Color(1, .91f, .60f, ativa.BrilhoImpacto);
            for (int i = 0; i < 4; i++)
            {
                float a = (45 + i * 90) * Mathf.Deg2Rad;
                var direcao = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Linha(vh, centro + direcao * 9, centro + direcao * 18, 2.5f, cor);
            }
        }
        private void Faixa(VertexHelper vh, float inicio, float fim, float perto, float longe, Color cor)
        {
            const int segmentos = 10;
            int primeiro = vh.currentVertCount;
            for (int i = 0; i <= segmentos; i++)
            {
                float u = i / (float)segmentos;
                float tempo = Mathf.Lerp(inicio, fim, u);
                Color tom = cor;
                tom.a *= u * u;
                Vector2 a = rectTransform.InverseTransformPoint(ativa.PontoDoCorteMundo(tempo, perto));
                Vector2 b = rectTransform.InverseTransformPoint(ativa.PontoDoCorteMundo(tempo, longe));
                vh.AddVert(a, tom, Vector2.zero);
                vh.AddVert(b, tom, Vector2.zero);
                if (i == 0) continue;
                int k = primeiro + i * 2;
                vh.AddTriangle(k - 2, k, k - 1);
                vh.AddTriangle(k - 1, k, k + 1);
            }
        }

        static void Linha(VertexHelper vh, Vector2 a, Vector2 b, float largura, Color cor)
        {
            Vector2 d = b - a;
            Vector2 n = new Vector2(-d.y, d.x).normalized * largura * .5f;
            int k = vh.currentVertCount;
            vh.AddVert(a - n, cor, Vector2.zero);
            vh.AddVert(a + n, cor, Vector2.zero);
            vh.AddVert(b + n, cor, Vector2.zero);
            vh.AddVert(b - n, cor, Vector2.zero);
            vh.AddTriangle(k, k + 1, k + 2);
            vh.AddTriangle(k, k + 2, k + 3);
        }
    }
}
