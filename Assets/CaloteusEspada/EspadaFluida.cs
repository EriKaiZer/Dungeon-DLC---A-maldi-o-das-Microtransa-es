using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Caloteus
{
    // Animacao procedural de uma imagem. Nao aplica dano por conta propria.
    [RequireComponent(typeof(Image))]
    public class EspadaFluida : MonoBehaviour
    {
        [Header("Controle")]
        public bool cliqueEsquerdo = true;
        public bool bloquearAtaque;
        [Range(0.5f, 2f)] public float velocidade = 1f;
        [Range(0f, 1.5f)] public float intensidade = 1f;
        public bool respiracao = true;
        public bool rastro = true;
        [Header("Espada acompanhando o mouse")]
        public bool seguirMouse = true;
        public Vector2 alcanceMouse = new Vector2(38f, 16f);
        [Range(2f, 25f)] public float suavidadeMouse = 12f;
        [Range(0f, 30f)] public float inclinacaoMouse = 5f;
        [Min(1f)] public float retornoMouse = 10f;
        [Header("Equipamento e animação")]
        public Sprite[] quadrosAtaque;
        public EspadaFluida outraMao;
        public bool espelhar;
        public bool manterAntebracoNaBorda;
        public bool golpeCurto;
        public Vector2 baseCorte = new Vector2(.62f, .27f);
        public Vector2 pontaCorte = new Vector2(.94f, .98f);
        [Min(0.1f)] public float duracaoGolpe = 0.62f;
        [Min(0.01f)] public float tempoContato = 0.205f;
        public Vector2 DeslocamentoMouse { get { return deslocamentoMouse; } }
        [Header("Contato: conecte aqui seu sistema de dano")]
        public UnityEvent aoGolpear = new UnityEvent();
        public UnityEvent aoIniciarCorte = new UnityEvent();
        public bool Atacando { get; private set; }
        public float TempoGolpe { get { return tempo; } }
        public float InicioCorte => Mathf.Max(.01f, tempoContato - (golpeCurto ? .045f : .065f));
        public float FimCorte => Mathf.Min(DuracaoAtual, ContatoAtual + (golpeCurto ? .045f : .065f));
        public float BrilhoImpacto => Mathf.Clamp01(brilhoImpacto / .12f);
        public bool EmPausaDeImpacto => pausaImpacto > 0;
        public bool Carregando { get; private set; }
        public bool GolpePesado { get; private set; }
        public bool Ocupada => Atacando || Carregando;
        public float Carga => Mathf.Clamp01(tempoCarga / .85f);
        public float MultiplicadorDano => GolpePesado ? Mathf.Lerp(1.45f, 1.9f, cargaDoGolpe) : 1;
        public float ContatoAtual => tempoContato;
        public float DuracaoAtual => duracaoGolpe * (GolpePesado ? 1.3f : 1);
        float tempoCarga, cargaDoGolpe;

        private RectTransform rt;
        private Vector2 origem;
        private Quaternion rotacao;
        private Vector3 escala;
        private float tempo;
        private bool contato;
        private bool iniciouCorte;
        private float pausaImpacto;
        private float brilhoImpacto;
        private bool inicializado;
        private readonly Image[] ecos = new Image[3];
        private readonly Vector2[] posicoes = new Vector2[3];
        private readonly Quaternion[] rotacoes = new Quaternion[3];
        private readonly Vector3[] escalas = new Vector3[3];
        private float proximoEco;
        private Vector2 ponteiroVirtual;
        private Vector2 deslocamentoMouse;
        private float giroMouse;
        private CursorLockMode ultimoLock;

        void Awake()
        {
            rt = GetComponent<RectTransform>();
            origem = rt.anchoredPosition;
            rotacao = rt.localRotation;
            escala = rt.localScale;
            inicializado = true;
            var fonte = GetComponent<Image>();
            for (int i = 0; i < ecos.Length; i++)
            {
                var go = new GameObject("RastroEspada_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(rt.parent, false);
                go.transform.SetSiblingIndex(rt.GetSiblingIndex());
                var img = go.GetComponent<Image>();
                img.sprite = fonte.sprite;
                img.preserveAspect = true;
                img.raycastTarget = false;
                img.color = new Color(0.8f, 0.88f, 1f, 0f);
                var er = img.rectTransform;
                er.anchorMin = rt.anchorMin;
                er.anchorMax = rt.anchorMax;
                er.pivot = rt.pivot;
                er.sizeDelta = rt.sizeDelta;
                ecos[i] = img;
            }
            LimparRastro();
        }

        void Update()
        {
            if (Time.timeScale <= 0 || !Application.isFocused || bloquearAtaque || (cliqueEsquerdo && Cursor.lockState != CursorLockMode.Locked))
            {
                if (Carregando) CancelarCarga();
                return;
            }
            AtualizarMouse(Time.deltaTime);
            bool pressionou = false, soltou = false;
#if ENABLE_INPUT_SYSTEM
            pressionou = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            soltou = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            pressionou = Input.GetMouseButtonDown(0);
            soltou = Input.GetMouseButtonUp(0);
#endif
            if (cliqueEsquerdo && pressionou) IniciarCarga();
            if (Carregando)
            {
                AtualizarCarga(Time.deltaTime);
                if (soltou) SoltarCarga();
                else return;
            }
            if (!Atacando)
            {
                AplicarPose(Vector2.zero, 0, 1);
                if (respiracao) rt.anchoredPosition += new Vector2(Mathf.Sin(Time.time * 1.8f) * 1.8f, Mathf.Sin(Time.time * 2.4f) * 2.5f);
                return;
            }
            AtualizarAtaque(Time.deltaTime);
        }

        public bool IniciarCarga()
        {
            if (!isActiveAndEnabled || bloquearAtaque || Ocupada || Time.timeScale <= 0 || (outraMao && outraMao.Ocupada)) return false;
            Carregando = true;
            tempoCarga = 0;
            return true;
        }

        public void AtualizarCarga(float delta)
        {
            if (!Carregando || Time.timeScale <= 0) return;
            if (bloquearAtaque) { CancelarCarga(); return; }
            tempoCarga = Mathf.Min(.85f, tempoCarga + Mathf.Max(0, delta));
            float u = Mathf.SmoothStep(0, 1, Carga);
            AplicarPose(new Vector2(.065f, -.025f) * u, -16 * u, Mathf.Lerp(1, .97f, u));
        }

        public void SoltarCarga()
        {
            if (!Carregando) return;
            float carga = Carga;
            Carregando = false;
            ComecarGolpe(carga >= .35f, Mathf.InverseLerp(.35f, 1, carga));
        }

        public void CancelarCarga()
        {
            Carregando = false;
            tempoCarga = 0;
            Restaurar();
        }

        private void AtualizarAtaque(float deltaTime)
        {
            if (Time.timeScale <= 0 || !Atacando) return;
            brilhoImpacto = Mathf.Max(0, brilhoImpacto - deltaTime);
            float espera = Mathf.Min(deltaTime, pausaImpacto);
            pausaImpacto -= espera;
            deltaTime -= espera;
            if (deltaTime <= 0) return;
            float destino = Mathf.Min(DuracaoAtual, tempo + deltaTime * Mathf.Clamp(velocidade, .5f, 2f));
            if (!iniciouCorte && destino >= InicioCorte)
            {
                iniciouCorte = true;
                aoIniciarCorte.Invoke();
            }
            if (!contato && destino >= tempoContato)
            {
                // Mostra a pose de contato antes de chamar dano, som e reação.
                tempo = tempoContato;
                Avaliar(tempo);
                AtualizarQuadro();
                contato = true;
                aoGolpear.Invoke();
                if (!isActiveAndEnabled) return;
                if (pausaImpacto > 0) return;
            }
            tempo = destino;
            Avaliar(tempo);
            AtualizarQuadro();
            AtualizarRastro();
            if (tempo >= DuracaoAtual)
            {
                Atacando = false; Carregando = false; GolpePesado = false; tempoCarga = 0;
                Restaurar();
                LimparRastro();
            }
        }

        public void Atacar() { ComecarGolpe(false, 0); }

        void ComecarGolpe(bool pesado, float carga)
        {
            if (!isActiveAndEnabled || bloquearAtaque || Ocupada || Time.timeScale <= 0) return;
            if (outraMao && outraMao.Ocupada) return;
            GolpePesado = pesado;
            cargaDoGolpe = carga;
            Atacando = true;
            tempo = pesado ? InicioCorte * .82f : 0;
            contato = iniciouCorte = false;
            pausaImpacto = brilhoImpacto = 0;
            proximoEco = 0;
            LimparRastro();
        }

        public void ConfirmarImpacto()
        {
            if (!Atacando || !contato) return;
            pausaImpacto = (golpeCurto ? .035f : .060f) * (GolpePesado ? 1.5f : 1);
            brilhoImpacto = .12f;
        }

        private void Avaliar(float t)
        {
            Vector2 p;
            float a, s;
            CalcularTrajeto(t, out p, out a, out s);
            AplicarPose(p, a, Mathf.Lerp(1f, s, intensidade));
        }

        private void CalcularTrajeto(float t, out Vector2 p, out float a, out float s)
        {
            Vector2 preparo = golpeCurto ? new Vector2(.035f, -.025f) : new Vector2(.065f, -.025f);
            Vector2 contatoPose = golpeCurto ? new Vector2(-.13f, -.04f) : new Vector2(-.31f, -.055f);
            Vector2 fim = golpeCurto ? new Vector2(-.18f, -.075f) : new Vector2(-.46f, -.13f);
            float recolher = golpeCurto ? -10 : -16;
            float corte = golpeCurto ? 18 : 36;
            float final = golpeCurto ? 24 : 54;
            if (t < InicioCorte)
                Trecho(t / InicioCorte, Vector2.zero, preparo, 0, recolher, 1, .97f, out p, out a, out s);
            else if (t < tempoContato)
            {
                float u = Mathf.InverseLerp(InicioCorte, tempoContato, t);
                u *= u;
                p = Vector2.Lerp(preparo, contatoPose, u);
                a = Mathf.Lerp(recolher, corte, u);
                s = Mathf.Lerp(.97f, 1.035f, u);
            }
            else if (t < FimCorte)
                Trecho(Mathf.InverseLerp(tempoContato, FimCorte, t), contatoPose, fim, corte, final, 1.035f, 1, out p, out a, out s);
            else
                Trecho(Mathf.InverseLerp(FimCorte, DuracaoAtual, t), fim, Vector2.zero, final, 0, 1, 1, out p, out a, out s);
        }

        private void AplicarPose(Vector2 movimento, float angulo, float tamanho)
        {
            Vector2 posicao;
            Quaternion giro;
            Vector3 tamanhoFinal;
            CalcularTransformacao(movimento, angulo, tamanho, out posicao, out giro, out tamanhoFinal);
            rt.anchoredPosition = posicao;
            rt.localRotation = giro;
            rt.localScale = tamanhoFinal;
        }

        private void CalcularTransformacao(Vector2 movimento, float angulo, float tamanho,
            out Vector2 posicao, out Quaternion giroFinal, out Vector3 tamanhoFinal)
        {
            float enquadramento = 1f;
            var parent = rt.parent as RectTransform;
            if (parent != null)
            {
                float altura = Mathf.Max(1f, rt.rect.height * escala.y);
                enquadramento = Mathf.Min(1f, parent.rect.height * 0.92f / altura, parent.rect.width * 0.62f / altura);
            }
            posicao = origem + deslocamentoMouse + movimento * rt.rect.width * intensidade;
            if (espelhar) posicao.x = origem.x - deslocamentoMouse.x - movimento.x * rt.rect.width * intensidade;
            // O fim do antebraço permanece abaixo da tela durante o movimento da lâmina.
            posicao.y = Mathf.Min(posicao.y, -16f);
            float giro = Mathf.Clamp(giroMouse + angulo * intensidade, -24f, 60f);
            giroFinal = rotacao * Quaternion.Euler(0f, 0f, espelhar ? -giro : giro);
            tamanhoFinal = escala * tamanho * enquadramento;
            if (manterAntebracoNaBorda && !espelhar && parent != null)
            {
                // Keep the sprite's cropped bottom edge outside the viewport. The slash samples this same transform.
                Vector3 a = (Vector3)posicao + giroFinal * Vector3.Scale(new Vector3(-rt.pivot.x * rt.rect.width,-rt.pivot.y * rt.rect.height,0),tamanhoFinal);
                Vector3 b = (Vector3)posicao + giroFinal * Vector3.Scale(new Vector3((1-rt.pivot.x) * rt.rect.width,-rt.pivot.y * rt.rect.height,0),tamanhoFinal);
                float limite = float.PositiveInfinity;
                if (a.y >= 0) limite = Mathf.Min(limite,a.x);
                if (b.y >= 0) limite = Mathf.Min(limite,b.x);
                if ((a.y < 0) != (b.y < 0)) limite = Mathf.Min(limite,Vector3.Lerp(a,b,-a.y/(b.y-a.y)).x);
                if (!float.IsPositiveInfinity(limite)) posicao.x += Mathf.Max(0,-limite);
            }
        }

        // O efeito do corte amostra a mesma pose usada para desenhar a lâmina.
        public Vector3 PontoDoCorteMundo(float instante, float comprimento)
        {
            Vector2 p, posicao;
            float a, s;
            Quaternion giro;
            Vector3 tamanho;
            CalcularTrajeto(instante, out p, out a, out s);
            CalcularTransformacao(p, a, Mathf.Lerp(1, s, intensidade), out posicao, out giro, out tamanho);
            Vector2 pixel = Vector2.Lerp(baseCorte, pontaCorte, comprimento);
            Vector3 local = new Vector3((pixel.x - rt.pivot.x) * rt.rect.width, (pixel.y - rt.pivot.y) * rt.rect.height, 0);
            Vector3 ancora = rt.localPosition - (Vector3)rt.anchoredPosition;
            return rt.parent.TransformPoint(ancora + (Vector3)posicao + giro * Vector3.Scale(local, tamanho));
        }

        private static void Trecho(float u, Vector2 p0, Vector2 p1, float a0, float a1, float s0, float s1, out Vector2 p, out float a, out float s)
        {
            u = Mathf.Clamp01(u);
            u = u * u * u * (u * (u * 6f - 15f) + 10f);
            p = Vector2.Lerp(p0, p1, u);
            a = Mathf.Lerp(a0, a1, u);
            s = Mathf.Lerp(s0, s1, u);
        }

        private void AtualizarRastro()
        {
            bool ativo = rastro && tempo >= InicioCorte && tempo <= FimCorte;
            if (!ativo) { LimparRastro(); return; }
            if (tempo >= proximoEco)
            {
                for (int i = 2; i > 0; i--)
                {
                    posicoes[i] = posicoes[i - 1];
                    rotacoes[i] = rotacoes[i - 1];
                    escalas[i] = escalas[i - 1];
                }
                posicoes[0] = rt.anchoredPosition;
                rotacoes[0] = rt.localRotation;
                escalas[0] = rt.localScale;
                proximoEco = tempo + 0.018f;
            }
            for (int i = 0; i < ecos.Length; i++)
            {
                var e = ecos[i];
                e.rectTransform.anchoredPosition = posicoes[i];
                e.rectTransform.localRotation = rotacoes[i];
                e.rectTransform.localScale = escalas[i];
                e.color = new Color(0.8f, 0.88f, 1f, 0.09f - i * 0.025f);
            }
        }

        private void LimparRastro()
        {
            for (int i = 0; i < ecos.Length; i++)
            {
                posicoes[i] = rt.anchoredPosition;
                rotacoes[i] = rt.localRotation;
                escalas[i] = rt.localScale;
                if (ecos[i] != null) ecos[i].color = Color.clear;
            }
        }
        private void Restaurar()
        {
            if (!inicializado) return;
            AplicarPose(Vector2.zero, 0f, 1f);
            if (quadrosAtaque != null && quadrosAtaque.Length > 0)
                GetComponent<Image>().sprite = quadrosAtaque[0];
        }

        private void AtualizarQuadro()
        {
            if (quadrosAtaque == null || quadrosAtaque.Length < 4) return;
            int quadro = tempo < tempoContato ? 1 : tempo < DuracaoAtual * .65f ? 2 : 3;
            GetComponent<Image>().sprite = quadrosAtaque[quadro];
        }

        public void Recalibrar()
        {
            if (rt == null) rt = GetComponent<RectTransform>();
            origem = rt.anchoredPosition;
            rotacao = rt.localRotation;
            escala = rt.localScale;
            ponteiroVirtual = deslocamentoMouse = Vector2.zero;
            giroMouse = 0f;
            Atacando = false; Carregando = false; GolpePesado = false; tempoCarga = 0;
            pausaImpacto = brilhoImpacto = 0;
            if (!inicializado) return;
            foreach (var eco in ecos)
            {
                if (eco == null) continue;
                var rect = eco.rectTransform;
                rect.anchorMin = rt.anchorMin;
                rect.anchorMax = rt.anchorMax;
                rect.pivot = rt.pivot;
                rect.sizeDelta = rt.sizeDelta;
                eco.sprite = GetComponent<Image>().sprite;
            }
            Restaurar();
            LimparRastro();
        }

        public void CancelarAtaque()
        {
            Atacando = false; Carregando = false; GolpePesado = false; tempoCarga = 0;
            pausaImpacto = brilhoImpacto = 0;
            Restaurar();
            if (inicializado) LimparRastro();
        }
        void OnDisable()
        {
            Atacando = false; Carregando = false; GolpePesado = false; tempoCarga = 0;
            pausaImpacto = brilhoImpacto = 0;
            Restaurar();
            if (inicializado) LimparRastro();
        }
        void OnDestroy()
        {
            foreach (var e in ecos) if (e != null) Destroy(e.gameObject);
        }

        private void AtualizarMouse(float deltaTime)
        {
            if (Atacando) return; // Conserva o ponto de partida durante o golpe.
            Vector2 delta = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                delta = Mouse.current.delta.ReadValue();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            // Os eixos antigos usam unidades diferentes dos deltas em pixels.
            delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 20f;
#endif
            Vector2 alvo = Vector2.zero;
            if (ultimoLock != Cursor.lockState)
            {
                ponteiroVirtual = Vector2.zero;
                delta = Vector2.zero;
                ultimoLock = Cursor.lockState;
            }
            if (seguirMouse && !bloquearAtaque)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    ponteiroVirtual += new Vector2(delta.x / Mathf.Max(1, Screen.width), delta.y / Mathf.Max(1, Screen.height)) * 8f;
                    ponteiroVirtual = Vector2.ClampMagnitude(ponteiroVirtual, 1f);
                    alvo = ponteiroVirtual;
                }
            }
            ponteiroVirtual = Vector2.Lerp(ponteiroVirtual, Vector2.zero, 1f - Mathf.Exp(-retornoMouse * deltaTime));
            float k = 1f - Mathf.Exp(-suavidadeMouse * deltaTime);
            deslocamentoMouse = Vector2.Lerp(deslocamentoMouse, Vector2.Scale(alvo, alcanceMouse), k);
            giroMouse = Mathf.Lerp(giroMouse, -alvo.x * inclinacaoMouse, k);
        }
    }
}
