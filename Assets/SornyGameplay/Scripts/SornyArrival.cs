using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonDLC.Sorny
{
    [DefaultExecutionOrder(-300)]
    public sealed class SornyArrival : MonoBehaviour
    {
        [Serializable]
        public sealed class State
        {
            public bool espada, escudo, adaga, portao, entrou;
            public bool andou, olhou, leve, pesado, bloqueio;
            public bool inventario;
        }

        public bool exploracaoLivre;
        public Transform folhaEsquerda, folhaDireita;
        public SornyPickup espadaInicial, escudoInicial, adagaTesouro;
        public AudioSource somPortao;
        public CrackEnemy sentinelaPonte;
        public SornyTrainingDummy manequim;
        public Vector3 pontoPortao = new Vector3(0, 0, -10);
        public float distanciaAbertura = 5.5f, duracaoAbertura = 2.2f;
        public bool PortaoAberto { get; private set; }
        public bool Entrou { get; private set; }
        public float Abertura { get; private set; }
        public bool Andou { get; private set; }
        public bool Olhou { get; private set; }
        public bool AtaqueLeve { get; private set; }
        public bool AtaquePesado { get; private set; }
        public bool Bloqueou { get; private set; }
        public bool InventarioVisto { get; private set; }
        public string DicaTutorial
        {
            get
            {
                if (exploracaoLivre) return "";
                if (!Andou) return "WASD  /  CAMINHAR PELO CAMINHO";
                if (!Olhou) return "MOVA O MOUSE  /  OLHAR AO REDOR";
                if (!hands.EspadaLiberada) return "PROCURE A ESPADA NO POSTO ILUMINADO À DIREITA";
                if (game.jogador.transform.position.z < -53)
                {
                    if (!AtaqueLeve) return "CLIQUE E SOLTE  /  ACERTE O MANEQUIM COM UM CORTE";
                    if (!AtaquePesado) return "SEGURE O CLIQUE E SOLTE  /  GOLPE PESADO NO MANEQUIM";
                }
                if (!hands.EscudoLiberado) return "RECOLHA O ESCUDO NO ABRIGO ANTES DA PONTE";
                if (!Bloqueou && game.jogador.transform.position.z < -32) return "SEGURE O BOTÃO DIREITO  /  BLOQUEIE O COBRADOR DE FRENTE";
                return PortaoAberto ? "SIGA PELOS PORTÕES ATÉ A MASMORRA" : "ATRAVESSE A PONTE  /  APROXIME-SE DOS PORTÕES";
            }
        }
        public string Objetivo => !hands.EspadaLiberada ? "A CHEGADA  |  ENCONTRE A ESPADA NO POSTO ABANDONADO"
            : !hands.EscudoLiberado ? "A CHEGADA  |  RECOLHA O ESCUDO ANTES DA PONTE"
            : !PortaoAberto ? "A CHEGADA  |  ATRAVESSE A PONTE ATÉ OS PORTÕES"
            : "PORTÕES ABERTOS  |  ENTRE NA FORTALEZA AZUL";

        SornyGameLoop game;
        SornyHandEquipment hands;
        Camera cameraJogo;
        CameraClearFlags fundoOriginal;
        Color corOriginal;
        Vector3 inicioCaminhada;
        float giroInicial, tempoDefesa;

        void Awake()
        {
            game = GetComponent<SornyGameLoop>();
            hands = game.espada.GetComponent<SornyHandEquipment>();
            cameraJogo = Camera.main;
            if (cameraJogo) { fundoOriginal = cameraJogo.clearFlags; corOriginal = cameraJogo.backgroundColor; }
            Reiniciar();
        }

        public void Reiniciar()
        {
            PortaoAberto = Entrou = false; Abertura = 0;
            Andou = Olhou = AtaqueLeve = AtaquePesado = Bloqueou = InventarioVisto = false;
            inicioCaminhada = game.jogador.transform.position;
            giroInicial = game.jogador.transform.eulerAngles.y;
            tempoDefesa = 0;
            hands.RestaurarAcesso(exploracaoLivre, exploracaoLivre, exploracaoLivre);
            AtualizarColetas(); AplicarPortao();
        }

        public bool Coletar(SornyPickup.Tipo tipo)
        {
            if (!game || game.Terminou || game.jogador.Morto || Time.timeScale <= 0) return false;
            if (tipo == SornyPickup.Tipo.EspadaInicial && hands.EspadaLiberada
                || tipo == SornyPickup.Tipo.EscudoInicial && hands.EscudoLiberado
                || tipo == SornyPickup.Tipo.AdagaTesouro && hands.AdagaLiberada) return false;
            if (tipo != SornyPickup.Tipo.EspadaInicial && !hands.EspadaLiberada)
            { game.Notificar("ENCONTRE PRIMEIRO A ESPADA NO POSTO ABANDONADO", 3); return false; }
            if (tipo != SornyPickup.Tipo.EspadaInicial && tipo != SornyPickup.Tipo.EscudoInicial && tipo != SornyPickup.Tipo.AdagaTesouro) return false;
            hands.ReceberInicial(tipo);
            AtualizarColetas();
            game.Notificar(tipo == SornyPickup.Tipo.EspadaInicial ? "ESPADA EQUIPADA"
                : tipo == SornyPickup.Tipo.EscudoInicial ? "ESCUDO EQUIPADO"
                : "ADAGA  |  2: DIREITA  |  4: ESQUERDA", 3);
            return true;
        }

        void AtualizarColetas()
        {
            if (espadaInicial) espadaInicial.RestaurarDisponibilidade(!exploracaoLivre && !hands.EspadaLiberada);
            if (escudoInicial) escudoInicial.RestaurarDisponibilidade(!exploracaoLivre && !hands.EscudoLiberado);
            if (adagaTesouro) adagaTesouro.RestaurarDisponibilidade(!exploracaoLivre && !hands.AdagaLiberada);
        }

        public State Capturar() => new State
        {
            espada = hands.EspadaLiberada, escudo = hands.EscudoLiberado, adaga = hands.AdagaLiberada,
            portao = PortaoAberto, entrou = Entrou,
            andou = Andou, olhou = Olhou, leve = AtaqueLeve, pesado = AtaquePesado, bloqueio = Bloqueou, inventario = InventarioVisto
        };

        public void Restaurar(State estado)
        {
            // A ausência desse campo identifica partidas feitas antes da fase de chegada.
            // Elas já possuíam as três armas e continuam do lugar salvo.
            hands.RestaurarAcesso(exploracaoLivre || estado == null || estado.espada, exploracaoLivre || estado == null || estado.escudo, exploracaoLivre || estado == null || estado.adaga);
            PortaoAberto = estado == null || estado.portao;
            Entrou = estado == null || estado.entrou;
            Andou = estado == null || estado.andou; Olhou = estado == null || estado.olhou;
            AtaqueLeve = estado == null || estado.leve; AtaquePesado = estado == null || estado.pesado; Bloqueou = estado == null || estado.bloqueio;
            InventarioVisto = estado == null || estado.inventario;
            Abertura = PortaoAberto ? 1 : 0;
            AtualizarColetas(); AplicarPortao();
        }

        public void AvancarPortao(float delta)
        {
            if (!game || game.Terminou || game.jogador.Morto || Time.timeScale <= 0 || delta <= 0) return;
            Vector3 posicao = game.jogador.transform.position;
            if (!PortaoAberto && hands.EspadaLiberada && hands.EscudoLiberado && Vector3.ProjectOnPlane(posicao - pontoPortao, Vector3.up).sqrMagnitude <= distanciaAbertura * distanciaAbertura)
            {
                PortaoAberto = true;
                if (somPortao) { somPortao.volume = SornySession.Efeitos * .8f; somPortao.Play(); }
                game.Notificar("A FORTALEZA ABRIU SEUS PORTÕES", 4);
                if (SornySession.Instancia) SornySession.Instancia.Save();
            }
            Abertura = Mathf.MoveTowards(Abertura, PortaoAberto ? 1 : 0, delta / Mathf.Max(.1f, duracaoAbertura));
            AplicarPortao();
            if (!Entrou && posicao.z > -5.5f)
            {
                Entrou = true;
                if (SornySession.Instancia) SornySession.Instancia.Save();
            }
        }

        void AplicarPortao()
        {
            float angulo = Mathf.SmoothStep(0, 1, Abertura) * 100;
            if (folhaEsquerda) folhaEsquerda.localRotation = Quaternion.Euler(0, -angulo, 0);
            if (folhaDireita) folhaDireita.localRotation = Quaternion.Euler(0, angulo, 0);
        }

        void Update()
        {
            AvancarPortao(Time.deltaTime);
            if (game && Time.timeScale > 0 && !game.Terminou)
            {
                if (Vector3.ProjectOnPlane(game.jogador.transform.position - inicioCaminhada, Vector3.up).magnitude > 1.5f) Andou = true;
                if (Mathf.Abs(Mathf.DeltaAngle(giroInicial, game.jogador.transform.eulerAngles.y)) > 15) Olhou = true;
                if (Keyboard.current != null && Keyboard.current.tabKey.isPressed) InventarioVisto = true;
                if (sentinelaPonte) sentinelaPonte.enabled = hands.EscudoLiberado;
                if (hands.Defendendo && sentinelaPonte && sentinelaPonte.Morto) { tempoDefesa += Time.deltaTime; if (tempoDefesa > .6f) Bloqueou = true; }
            }
            if (!cameraJogo || !game) return;
            bool fora = game.jogador.transform.position.z < -6;
            cameraJogo.clearFlags = fora ? CameraClearFlags.SolidColor : fundoOriginal;
            cameraJogo.backgroundColor = fora ? new Color(.16f, .23f, .31f) : corOriginal;
        }

        public void RegistrarAcerto(bool pesado)
        {
            if (pesado) AtaquePesado = true; else AtaqueLeve = true;
        }

        public void RegistrarDefesa() { Bloqueou = true; }

        void LateUpdate()
        {
            if (exploracaoLivre || !game || game.Terminou || Time.timeScale <= 0) return;
            if (game.jogador.transform.position.z >= -6)
            {
                if (Entrou && !InventarioVisto && game.jogador.transform.position.z < 3 && string.IsNullOrEmpty(game.dicaTexto.text))
                    game.dicaTexto.text = "TAB  /  ABRIR INVENTÁRIO";
                return;
            }
            game.objetivoTexto.text = "A CHEGADA À FORTALEZA";
            if (string.IsNullOrEmpty(game.dicaTexto.text)) game.dicaTexto.text = DicaTutorial;
        }

        void OnDestroy()
        {
            if (cameraJogo) { cameraJogo.clearFlags = fundoOriginal; cameraJogo.backgroundColor = corOriginal; }
        }
    }
}

