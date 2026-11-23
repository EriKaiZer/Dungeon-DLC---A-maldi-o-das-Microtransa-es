using Caloteus;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DungeonDLC.Sorny
{
    [DefaultExecutionOrder(50)]
    public sealed class SornyHandEquipment : MonoBehaviour
    {
        public enum ArmaDireita { Espada, Adaga, Machado, Vazia }
        public enum ArmaEsquerda { Escudo, Adaga, Vazia }

        public EspadaFluida direita;
        public EspadaFluida esquerda;
        public Sprite[] espada;
        public Sprite[] adaga;
        public Sprite[] escudo;
        public Sprite[] machado;
        public Sprite maoEmCamada;
        public Sprite espadaEmpunhada;
        public bool usarEspadaReferencia;
        public Material materialArmasUI;
        public bool MachadoLiberado { get; private set; }
        public bool EspadaLiberada { get; private set; } = true;
        public bool EscudoLiberado { get; private set; } = true;
        public bool AdagaLiberada { get; private set; } = true;
        public ArmaDireita maoDireita;
        public ArmaEsquerda maoEsquerda;

        SornyGameLoop game;
        SornyAudioDirector audio;
        bool espadaMelhorada;
        bool defesaSolicitada;
        float impactoAte;
        float levantamento;

        public float LevantamentoEscudo => levantamento;

        public bool Defendendo => defesaSolicitada && maoEsquerda == ArmaEsquerda.Escudo
            && PodeAgir && !direita.Ocupada && !esquerda.Ocupada;

        bool PodeAgir => game && !game.Terminou && !game.jogador.Morto
            && Time.timeScale > 0 && !direita.bloquearAtaque;

        public string NomeDireita => maoDireita == ArmaDireita.Vazia ? "DESARMADO" : maoDireita == ArmaDireita.Machado ? "MAÇA DO SÓ-NÃO 42" : maoDireita == ArmaDireita.Adaga ? "ADAGA 15" : espadaMelhorada ? "ESPADA ESTORNO 40" : "ESPADA COMUM 25";
        public string NomeEsquerda => maoEsquerda == ArmaEsquerda.Escudo ? "ESCUDO" : maoEsquerda == ArmaEsquerda.Adaga ? "ADAGA 15" : "VAZIA";

        void Awake()
        {
            game = FindFirstObjectByType<SornyGameLoop>();
            if (game)
            {
                audio = game.GetComponent<SornyAudioDirector>();
                game.jogador.equipamentos = this;
            }
        }

        Image indicadorCarga, preenchimentoCarga;
        Text legendaCarga;
        void Start()
        {
            AplicarEquipamentos();
            var canvas = game.vidaTexto.GetComponentInParent<Canvas>();
            var go = new GameObject("Carga_Ataque_Pesado",typeof(RectTransform),typeof(Image));
            go.transform.SetParent(canvas.transform,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(0,-40);rect.sizeDelta=new Vector2(112,7);
            indicadorCarga=go.GetComponent<Image>();indicadorCarga.color=new Color(.04f,.03f,.02f,.85f);indicadorCarga.raycastTarget=false;
            var fill=new GameObject("Preenchimento",typeof(RectTransform),typeof(Image));fill.transform.SetParent(go.transform,false);
            var fr=fill.GetComponent<RectTransform>();fr.anchorMin=Vector2.zero;fr.anchorMax=Vector2.one;fr.offsetMin=Vector2.one;fr.offsetMax=-Vector2.one;
            preenchimentoCarga=fill.GetComponent<Image>();preenchimentoCarga.type=Image.Type.Filled;preenchimentoCarga.fillMethod=Image.FillMethod.Horizontal;preenchimentoCarga.raycastTarget=false;
            var label=new GameObject("Soltar_para_atacar",typeof(RectTransform),typeof(Text));label.transform.SetParent(go.transform,false);
            var lr=label.GetComponent<RectTransform>();lr.anchorMin=lr.anchorMax=lr.pivot=new Vector2(.5f,.5f);lr.anchoredPosition=new Vector2(0,-19);lr.sizeDelta=new Vector2(260,20);
            legendaCarga=label.GetComponent<Text>();legendaCarga.font=Resources.Load<SornyMenuTheme>("SornyMenuTheme").fonte;legendaCarga.fontSize=12;legendaCarga.alignment=TextAnchor.MiddleCenter;legendaCarga.raycastTarget=false;
            go.SetActive(false);
        }

        public void AtualizarMelhoria(bool melhorada)
        {
            espadaMelhorada = melhorada;
            AplicarEquipamentos();
        }

        public bool SelecionarDireita(ArmaDireita arma)
        {
            if (direita.Ocupada || esquerda.Ocupada || !System.Enum.IsDefined(typeof(ArmaDireita), arma)) return false;
            if ((arma == ArmaDireita.Espada && !EspadaLiberada) || (arma == ArmaDireita.Adaga && !AdagaLiberada)) return false;
            if (arma == ArmaDireita.Machado && (!MachadoLiberado || machado == null || machado.Length == 0)) { if (game) game.Notificar("DERROTE SÓ-NÃO E RECOLHA SUA MAÇA-CHAVE", 2); return false; }
            maoDireita = arma;
            AplicarEquipamentos();
            SalvarEscolha();
            return true;
        }

        public bool SelecionarEsquerda(ArmaEsquerda arma)
        {
            if (direita.Ocupada || esquerda.Ocupada || !System.Enum.IsDefined(typeof(ArmaEsquerda), arma)) return false;
            if ((arma == ArmaEsquerda.Escudo && !EscudoLiberado) || (arma == ArmaEsquerda.Adaga && !AdagaLiberada)) return false;
            maoEsquerda = arma;
            AplicarEquipamentos();
            SalvarEscolha();
            return true;
        }

        public void RestaurarEscolhas(int armaDireita, int armaEsquerda)
        {
            maoDireita = armaDireita == 3 ? ArmaDireita.Vazia : armaDireita == 2 && MachadoLiberado ? ArmaDireita.Machado : armaDireita == 1 && AdagaLiberada ? ArmaDireita.Adaga : EspadaLiberada ? ArmaDireita.Espada : ArmaDireita.Vazia;
            maoEsquerda = armaEsquerda == 0 && EscudoLiberado ? ArmaEsquerda.Escudo : armaEsquerda == 1 && AdagaLiberada ? ArmaEsquerda.Adaga : ArmaEsquerda.Vazia;
            AplicarEquipamentos();
        }

        public void RestaurarMachado(bool liberado)
        {
            MachadoLiberado = liberado;
            if (!liberado && maoDireita == ArmaDireita.Machado) maoDireita = EspadaLiberada ? ArmaDireita.Espada : ArmaDireita.Vazia;
            AplicarEquipamentos();
        }

        public void RestaurarAcesso(bool possuiEspada, bool possuiEscudo, bool possuiAdaga)
        {
            EspadaLiberada = possuiEspada; EscudoLiberado = possuiEscudo; AdagaLiberada = possuiAdaga;
            if (maoDireita == ArmaDireita.Espada && !EspadaLiberada || maoDireita == ArmaDireita.Adaga && !AdagaLiberada)
                maoDireita = EspadaLiberada ? ArmaDireita.Espada : ArmaDireita.Vazia;
            if (maoEsquerda == ArmaEsquerda.Escudo && !EscudoLiberado || maoEsquerda == ArmaEsquerda.Adaga && !AdagaLiberada)
                maoEsquerda = ArmaEsquerda.Vazia;
            if (game) AplicarEquipamentos();
        }

        public void ReceberInicial(SornyPickup.Tipo tipo)
        {
            direita.CancelarAtaque(); esquerda.CancelarAtaque();
            if (tipo == SornyPickup.Tipo.EspadaInicial) { EspadaLiberada = true; maoDireita = ArmaDireita.Espada; }
            if (tipo == SornyPickup.Tipo.EscudoInicial) { EscudoLiberado = true; maoEsquerda = ArmaEsquerda.Escudo; }
            if (tipo == SornyPickup.Tipo.AdagaTesouro) { AdagaLiberada = true; maoDireita = ArmaDireita.Adaga; }
            AplicarEquipamentos();
            entradaEquipamentoAte = Time.time + .45f;
        }

        float entradaEquipamentoAte;

        public void ReceberMachado()
        {
            MachadoLiberado = true;
            direita.CancelarAtaque();
            esquerda.CancelarAtaque();
            maoDireita = ArmaDireita.Machado;
            AplicarEquipamentos();
        }

        void SalvarEscolha()
        {
            if (SornySession.Instancia) SornySession.Instancia.Save();
        }

        void AplicarEquipamentos()
        {
            if (!direita || !esquerda || espada == null || espada.Length == 0 || adaga == null || adaga.Length < 4) return;
            defesaSolicitada = false;
            levantamento = 0;
            impactoAte = 0;
            bool usandoEspada = maoDireita == ArmaDireita.Espada;
            bool usandoMachado = maoDireita == ArmaDireita.Machado;
            bool vazia = maoDireita == ArmaDireita.Vazia;
            direita.enabled = !vazia;
            direita.GetComponent<SornySwordDamage>().enabled = !vazia;
            direita.quadrosAtaque = usandoMachado ? machado : usandoEspada || vazia ? espada : adaga;
            if (usandoEspada && usarEspadaReferencia && espadaEmpunhada) direita.quadrosAtaque = new[] { espadaEmpunhada, espadaEmpunhada, espadaEmpunhada, espadaEmpunhada };
            direita.GetComponent<Image>().sprite = direita.quadrosAtaque[0];
            direita.GetComponent<Image>().color = usandoEspada && espadaMelhorada && !usarEspadaReferencia ? new Color(.65f, .83f, 1) : Color.white;
            direita.duracaoGolpe = usandoMachado ? .82f : usandoEspada ? .62f : .4f;
            direita.tempoContato = usandoMachado ? .27f : usandoEspada ? .205f : .13f;
            direita.golpeCurto = !usandoEspada && !usandoMachado;
            direita.baseCorte = usandoMachado ? new Vector2(.50f,.47f) : new Vector2(.599f,.368f);
            direita.pontaCorte = usandoMachado ? new Vector2(.43f,.91f) : new Vector2(.599f,usandoEspada ? .881f : .626f);
            direita.velocidade = 1;
            var danoDireita = direita.GetComponent<SornySwordDamage>();
            danoDireita.dano = usandoMachado ? 42 : usandoEspada ? espadaMelhorada ? 40 : 25 : 15;
            danoDireita.alcance = usandoMachado ? 2.3f : usandoEspada ? 2.6f : 1.9f;
            direita.manterAntebracoNaBorda=usandoEspada && usarEspadaReferencia;
            ConfigurarRect(direita, false, usandoMachado ? 970f : 1000f);
            ConfigurarCamadas(direita,!usandoMachado && !(usandoEspada && usarEspadaReferencia));
            if(usandoEspada && usarEspadaReferencia) { var pose=direita.GetComponent<RectTransform>();pose.pivot=new Vector2(.8f,-.05f);pose.anchoredPosition=new Vector2(100,-16);pose.localRotation=Quaternion.Euler(0,0,40);direita.baseCorte=new Vector2(.50f,.48f);direita.pontaCorte=new Vector2(.835f,.98f); }
            direita.Recalibrar();
            if (vazia)
            {
                direita.GetComponent<Image>().enabled = false;
                foreach (string nome in new[] { "Arma_Eixo_Reto", "Pegada_Alinhada" })
                { var child = direita.transform.Find(nome); if (child) child.gameObject.SetActive(false); }
            }

            esquerda.CancelarAtaque();
            esquerda.gameObject.SetActive(maoEsquerda != ArmaEsquerda.Vazia);
            esquerda.enabled = maoEsquerda == ArmaEsquerda.Adaga;
            var imagemEsquerda = esquerda.GetComponent<Image>();
            imagemEsquerda.color = Color.white;
            if (maoEsquerda == ArmaEsquerda.Adaga)
            {
                esquerda.quadrosAtaque = adaga;
                imagemEsquerda.sprite = adaga[0];
                esquerda.duracaoGolpe = .4f;
                esquerda.tempoContato = .13f;
                esquerda.golpeCurto = true;
                esquerda.baseCorte = new Vector2(.599f,.368f); esquerda.pontaCorte = new Vector2(.599f,.626f);
                esquerda.velocidade = 1;
                ConfigurarRect(esquerda, true, 900f);
                ConfigurarCamadas(esquerda,true);
                esquerda.Recalibrar();
            }
            else if (maoEsquerda == ArmaEsquerda.Escudo && escudo != null && escudo.Length > 0)
            {
                ConfigurarCamadas(esquerda,false);
                imagemEsquerda.sprite = escudo[0];
                var rect = esquerda.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
                rect.anchoredPosition = new Vector2(-140, -250);
                rect.sizeDelta = new Vector2(1080 * escudo[0].rect.width / escudo[0].rect.height, 1080);
                rect.localScale = Vector3.one;
                rect.localRotation = Quaternion.Euler(0, 0, 8);
            }
            var danoEsquerda = esquerda.GetComponent<SornySwordDamage>();
            danoEsquerda.dano = 15;
            danoEsquerda.alcance = 1.9f;
            foreach (var arco in direita.GetComponentInParent<Canvas>().GetComponentsInChildren<ArcoCorte>())
                arco.enabled = true;
        }

        void ConfigurarCamadas(EspadaFluida animacao,bool usar)
        {
            var source=animacao.GetComponent<Image>();
            var weapon=animacao.transform.Find("Arma_Eixo_Reto");
            var hand=animacao.transform.Find("Pegada_Alinhada");
            if(!usar||!maoEmCamada)
            {
                source.enabled=true;source.material=animacao==direita?materialArmasUI:null;
                if(weapon)weapon.gameObject.SetActive(false);if(hand)hand.gameObject.SetActive(false);return;
            }
            var rect=animacao.GetComponent<RectTransform>();
            rect.sizeDelta=new Vector2(rect.sizeDelta.y*.75f,rect.sizeDelta.y);
            // Rotate around the forearm exit so the cropped end stays below the screen during the swing.
            rect.pivot=new Vector2(1,0);
            rect.anchoredPosition=new Vector2(animacao.espelhar ? -40 : 40,-16);
            rect.localRotation=Quaternion.Euler(0,0,animacao.espelhar ? -18 : 18);
            if(!weapon){var go=new GameObject("Arma_Eixo_Reto",typeof(RectTransform),typeof(Image));go.transform.SetParent(animacao.transform,false);weapon=go.transform;}
            if(!hand){var go=new GameObject("Pegada_Alinhada",typeof(RectTransform),typeof(Image));go.transform.SetParent(animacao.transform,false);hand=go.transform;}
            foreach(var child in new[]{weapon,hand}){var r=child.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.localRotation=Quaternion.identity;r.localScale=Vector3.one;child.gameObject.SetActive(true);var image=child.GetComponent<Image>();image.material=materialArmasUI;image.raycastTarget=false;image.preserveAspect=false;}
            // Blade, guard, grip and pommel stay in ONE rigid sprite. Only the hand overlays it.
            // The source grip is 255 px long; matching its scale to the closed fist avoids a dangling handle.
            var wr=weapon.GetComponent<RectTransform>();
            wr.sizeDelta=rect.sizeDelta*.8f;
            wr.anchoredPosition=new Vector2(rect.sizeDelta.x*.199f,rect.sizeDelta.y*.105f);
            var hr=hand.GetComponent<RectTransform>();hr.sizeDelta=rect.sizeDelta;hr.anchoredPosition=Vector2.zero;
            weapon.GetComponent<Image>().sprite=source.sprite;weapon.GetComponent<Image>().color=source.color;
            hand.GetComponent<Image>().sprite=maoEmCamada;hand.GetComponent<Image>().color=Color.white;hand.SetAsLastSibling();
            source.enabled=false;
        }

        static void ConfigurarRect(EspadaFluida animacao, bool invertida, float altura)
        {
            var rect = animacao.GetComponent<RectTransform>();
            var sprite = animacao.GetComponent<Image>().sprite;
            rect.anchorMin = rect.anchorMax = new Vector2(invertida ? 0 : 1, 0);
            rect.pivot = new Vector2(1, 0);
            rect.anchoredPosition = new Vector2(invertida ? -24 : 24, -48);
            rect.sizeDelta = new Vector2(altura * sprite.rect.width / sprite.rect.height, altura);
            rect.localRotation = Quaternion.identity;
            rect.localScale = new Vector3(invertida ? -1 : 1, 1, 1);
            animacao.espelhar = invertida;
            animacao.rastro = false;
        }

        public void Defender(bool solicitado)
        {
            defesaSolicitada = solicitado;
        }

        public bool TentarBloquear(Vector3 origem)
        {
            if (!Defendendo || !Camera.main) return false;
            Vector3 direcao = Vector3.ProjectOnPlane(origem - game.jogador.transform.position, Vector3.up);
            Vector3 frente = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
            if (direcao.sqrMagnitude < .001f || Vector3.Dot(frente, direcao.normalized) < .5f) return false;
            impactoAte = Time.time + .22f;
            if (audio) audio.TocarBloqueio();
            var chegada = game.GetComponent<SornyArrival>(); if (chegada) chegada.RegistrarDefesa();
            game.Notificar("DEFESA", .7f);
            return true;
        }

        public bool AtacarEsquerda()
        {
            if (!PodeAgir || maoEsquerda != ArmaEsquerda.Adaga || direita.Ocupada || esquerda.Ocupada) return false;
            esquerda.bloquearAtaque = false;
            esquerda.Atacar();
            return esquerda.Atacando;
        }

        void Update()
        {
            if (!direita || !esquerda) return;
            esquerda.bloquearAtaque = direita.bloquearAtaque;
            if (!PodeAgir || !Application.isFocused || Cursor.lockState != CursorLockMode.Locked)
            {
                Defender(false);
                if (game && game.Terminou)
                {
                    direita.CancelarAtaque();
                    esquerda.CancelarAtaque();
                }
                return;
            }
            var teclado = Keyboard.current;
            if (teclado != null)
            {
                if (teclado.digit1Key.wasPressedThisFrame) SelecionarDireita(ArmaDireita.Espada);
                if (teclado.digit2Key.wasPressedThisFrame) SelecionarDireita(ArmaDireita.Adaga);
                if (teclado.digit3Key.wasPressedThisFrame) SelecionarEsquerda(ArmaEsquerda.Escudo);
                if (teclado.digit4Key.wasPressedThisFrame) SelecionarEsquerda(ArmaEsquerda.Adaga);
                if (teclado.digit5Key.wasPressedThisFrame) SelecionarEsquerda(ArmaEsquerda.Vazia);
                if (teclado.digit6Key.wasPressedThisFrame) SelecionarDireita(ArmaDireita.Machado);
            }
            var mouse = Mouse.current;
            Defender(mouse != null && mouse.rightButton.isPressed);
            if (mouse != null && mouse.rightButton.wasPressedThisFrame) AtacarEsquerda();
        }

        void LateUpdate()
        {
            AtualizarPoseEscudo(Time.deltaTime);
            float entrada = Mathf.Clamp01((entradaEquipamentoAte - Time.time) / .45f);
            if (entrada > 0)
            {
                Vector2 baixar = Vector2.down * (190 * entrada * entrada);
                if (direita.enabled) direita.GetComponent<RectTransform>().anchoredPosition += baixar;
                if (esquerda.gameObject.activeSelf) esquerda.GetComponent<RectTransform>().anchoredPosition += baixar;
            }
            if(indicadorCarga){indicadorCarga.gameObject.SetActive(direita.Carregando);preenchimentoCarga.fillAmount=direita.Carga;preenchimentoCarga.color=direita.Carga>=.35f?new Color(1,.75f,.2f):Color.white;legendaCarga.color=preenchimentoCarga.color;legendaCarga.text=direita.Carga>=.35f?"SOLTE: ATAQUE PESADO":"SEGURE PARA CARREGAR";}
        }

        float ajusteTremor() => Time.timeScale > 0 ? 1 : 0;

        void AtualizarPoseEscudo(float deltaTime)
        {
            if (maoEsquerda != ArmaEsquerda.Escudo || !esquerda || escudo == null || escudo.Length == 0) return;
            levantamento = Mathf.MoveTowards(levantamento, Defendendo ? 1 : 0, deltaTime / .16f);
            float levantar = Mathf.SmoothStep(0, 1, levantamento);
            float impacto = Mathf.Clamp01((impactoAte - Time.time) / .22f);
            float faseImpacto = (.22f - Mathf.Clamp(impactoAte - Time.time, 0, .22f)) * 95;
            Vector2 tremor = new Vector2(Mathf.Sin(faseImpacto) * 14, Mathf.Sin(faseImpacto * 1.31f) * 7) * (impacto * impacto * ajusteTremor());
            var rect = esquerda.GetComponent<RectTransform>();
            var parent = rect.parent as RectTransform;
            float ajuste = parent ? Mathf.Min(1, parent.rect.height * Mathf.Lerp(.84f, .98f, levantar) / rect.sizeDelta.y,
                parent.rect.width * Mathf.Lerp(.48f, .60f, levantar) / rect.sizeDelta.x) : 1;
            float largura = rect.sizeDelta.x * ajuste;
            float altura = rect.sizeDelta.y * ajuste;
            float larguraTela = parent ? parent.rect.width : 1920;
            Vector2 repouso = new Vector2(-140, -250) * ajuste;
            // Fecha a guarda diante do tórax. A margem inferior oculta o início do braço.
            Vector2 guarda = new Vector2((larguraTela - largura) * .5f, -altura * .28f);
            Vector2 movimento = Vector2.Scale(direita.DeslocamentoMouse, new Vector2(.10f, .04f)) * ajuste;
            Vector2 apoio = Vector2.Lerp(repouso, guarda, levantar) + movimento + new Vector2(-18, -10) * (impacto * ajuste) + tremor * ajuste;
            apoio.y = Mathf.Min(apoio.y, -16 * ajuste);
            rect.anchoredPosition = apoio;
            rect.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(8, 0, levantar) + impacto * (3 + Mathf.Sin(faseImpacto * .8f) * 3));
            rect.localScale = Vector3.one * ajuste;
            esquerda.GetComponent<Image>().sprite = escudo[0];
        }
    }
}



