using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonDLC.Sorny
{
 public sealed class SornyAudioDirector : MonoBehaviour
 {
  public AudioClip trilha, ambiente, espadaAr, impacto, alerta, ataque, morte, moeda, cura, vitoria, derrota;
  public AudioClip[] passos;
  public AudioClip[] acertosEspada, acertosAdaga, bloqueiosEscudo, cortesEspada, cortesAdaga;
  public AudioClip danoRecebido;
  public AudioClip[] danosJogador,acertosMachado;
  [Range(0, 1)] public float volumeMusica = .13f, volumeEfeitos = .6f, volumeAmbiente = .18f;
  [Range(.5f, 1.5f)] public float ganhoExploracao = 1.2f;
  public const string CreditosAudio = "Músicas: Kevin MacLeod — incompetech.com\nDarkest Child · Heroic Age · Volatile Reaction\nCC BY 4.0 — creativecommons.org/licenses/by/4.0/\nSons: Kenney (CC0) e composição do projeto.";
  public AudioSource FonteMusica => music;
  public float VolumeMusicaAtual => volumeMusica * (1 - destaqueImpacto * .33f);

  AudioSource music, amb, sfx, arPerto, vozJogador;
  readonly AudioSource[] impactosPerto = new AudioSource[3];
  readonly float[] ganhosImpacto = new float[3];
  SornyGameLoop game;
  Vector3 previous;
  float walked, previousTime, destaqueImpacto;
  int life, gold, vozImpacto, ultimaEspada = -1, ultimaAdaga = -1, ultimoBloqueio = -1, ultimoArEspada = -1, ultimoArAdaga = -1;
  int bloqueioAteFrame = -10,ultimoDano=-1,ultimoMachado=-1;
  bool ended, panel;
  Caloteus.EspadaFluida direita, esquerda;
  class Estado { public int vida; public string estado; public AudioSource audio; }
  readonly Dictionary<CrackEnemy, Estado> enemies = new Dictionary<CrackEnemy, Estado>();

  AudioSource Source(GameObject go, bool spatial = false, int priority = 100)
  {
   var source = go.AddComponent<AudioSource>();
   source.playOnAwake = false;
   source.spatialBlend = spatial ? 1 : 0;
   source.dopplerLevel = 0;
   source.priority = priority;
   source.minDistance = 1.5f;
   source.maxDistance = 13;
   source.rolloffMode = AudioRolloffMode.Linear;
   return source;
  }

  void Start()
  {
   game = FindFirstObjectByType<SornyGameLoop>();
   music = Source(gameObject, false, 120);
   music.clip = trilha;
   music.loop = true;
   music.volume = volumeMusica * ganhoExploracao;
   music.Play();
   amb = Source(gameObject, false, 200);
   amb.clip = ambiente;
   amb.loop = true;
   amb.volume = volumeAmbiente;
   amb.Play();
   sfx = Source(gameObject, false, 80);
   arPerto = Source(gameObject, false, 48);
   vozJogador = Source(gameObject, false, 22);
   for (int i = 0; i < impactosPerto.Length; i++) impactosPerto[i] = Source(gameObject, false, 24);
   if (!game) return;
   foreach (var enemy in FindObjectsByType<CrackEnemy>(FindObjectsSortMode.None))
    enemies[enemy] = new Estado { vida = enemy.Vida, estado = enemy.Estado, audio = Source(enemy.gameObject, true, 90) };
   direita = game.espada;
   esquerda = direita.outraMao;
   direita.aoIniciarCorte.AddListener(CortarDireita);
   if (esquerda) esquerda.aoIniciarCorte.AddListener(CortarEsquerda);
   SincronizarEstado();
  }

  public void SincronizarEstado()
  {
   if (!game) return;
   previous = game.jogador.transform.position;
   walked = 0;
   life = game.jogador.Vida;
   gold = game.Moedas;
   ended = game.Terminou;
   foreach (var pair in enemies)
    if (pair.Key) { pair.Value.vida = pair.Key.Vida; pair.Value.estado = pair.Key.Estado; }
  }

  void Play(AudioClip clip, float gain = 1)
  {
   if (!clip || !sfx) return;
   sfx.volume = volumeEfeitos;
   sfx.PlayOneShot(clip, gain);
  }

  static AudioClip Escolher(AudioClip[] clips, ref int anterior, AudioClip fallback)
  {
   if (clips == null || clips.Length == 0) return fallback;
   int index = clips.Length == 1 ? 0 : anterior < 0 ? Random.Range(0, clips.Length) : (anterior + Random.Range(1, clips.Length)) % clips.Length;
   anterior = index;
   return clips[index] ? clips[index] : fallback;
  }

  void CortarDireita() { TocarArmaAr(direita && direita.golpeCurto); }
  void CortarEsquerda() { TocarArmaAr(esquerda && esquerda.golpeCurto); }
  public void TocarGolpeExtra() { TocarArmaAr(false); }

  void TocarArmaAr(bool curta)
  {
   if (!isActiveAndEnabled || Time.timeScale <= 0 || !arPerto) return;
   var clip = curta ? Escolher(cortesAdaga, ref ultimoArAdaga, espadaAr) : Escolher(cortesEspada, ref ultimoArEspada, espadaAr);
   if (!clip) return;
   arPerto.clip = clip;
   arPerto.pitch = Random.Range(.97f, 1.04f);
   arPerto.volume = volumeEfeitos * .7f;
   arPerto.Play();
  }

  void TocarImpactoPerto(AudioClip clip, float gain)
  {
   if (!clip || !impactosPerto[vozImpacto]) return;
   int index = vozImpacto;
   var source = impactosPerto[index];
   source.Stop();
   source.clip = clip;
   source.pitch = Random.Range(.95f, 1.035f);
   ganhosImpacto[index] = gain;
   source.volume = volumeEfeitos * gain;
   source.Play();
   vozImpacto = (vozImpacto + 1) % impactosPerto.Length;
  }

  public void TocarBloqueio()
  {
   if (!isActiveAndEnabled || Time.timeScale <= 0) return;
   TocarImpactoPerto(Escolher(bloqueiosEscudo, ref ultimoBloqueio, impacto), .85f);
   destaqueImpacto = Mathf.Max(destaqueImpacto, .75f);
   bloqueioAteFrame = Time.frameCount + 1;
  }

  public void TocarAcerto(CrackEnemy enemy, bool golpeCurto = false, bool pesadoCarregado = false)
  {
   if (!isActiveAndEnabled || Time.timeScale <= 0 || !enemy || !enemies.TryGetValue(enemy, out var estado)) return;
   var hands = game.espada.GetComponent<SornyHandEquipment>();
   bool pesado = !golpeCurto && hands && hands.maoDireita == SornyHandEquipment.ArmaDireita.Machado;
   var clip = pesado ? Escolher(acertosMachado, ref ultimoMachado, impacto) : golpeCurto ? Escolher(acertosAdaga, ref ultimaAdaga, impacto) : Escolher(acertosEspada, ref ultimaEspada, impacto);
   // A pancada permanece também no golpe fatal; a voz de morte é outra camada.
   int canal=vozImpacto;TocarImpactoPerto(clip, golpeCurto ? .82f : .95f);if(pesadoCarregado&&impactosPerto[canal])impactosPerto[canal].pitch*=.86f;
   destaqueImpacto = Mathf.Max(destaqueImpacto, golpeCurto ? .65f : 1);
   if (enemy.Morto && morte)
   {
    estado.audio.volume = volumeEfeitos;
    estado.audio.PlayOneShot(morte, .55f);
   }
   estado.vida = enemy.Vida;
   estado.estado = enemy.Estado;
  }

  public void TocarDanoJogador(bool bloqueado)
  {
   if (game) life = game.jogador.Vida;
   if (bloqueado || !isActiveAndEnabled || Time.timeScale <= 0 || !vozJogador) return;
   var clip = Escolher(danosJogador, ref ultimoDano, danoRecebido);
   if (!clip) return;
   vozJogador.clip = clip;
   vozJogador.pitch = Random.Range(.96f, 1.04f);
   vozJogador.volume = volumeEfeitos * .85f;
   vozJogador.Play();
  }

  void AtualizarMix(float deltaTime)
  {
   destaqueImpacto = Mathf.MoveTowards(destaqueImpacto, 0, deltaTime / .19f);
   if (music) music.volume = Mathf.Clamp01(VolumeMusicaAtual * ganhoExploracao * (game && game.Terminou ? .4f : 1));
   if (amb) amb.volume = volumeAmbiente;
   if (sfx) sfx.volume = volumeEfeitos;
   if (arPerto) arPerto.volume = volumeEfeitos * .7f;
   if (vozJogador) vozJogador.volume = volumeEfeitos * .85f;
   for (int i = 0; i < impactosPerto.Length; i++)
    if (impactosPerto[i]) impactosPerto[i].volume = volumeEfeitos * ganhosImpacto[i];
   foreach (var state in enemies.Values) if (state.audio) state.audio.volume = volumeEfeitos;
  }

  public void Fechar()
  {
   panel = false;
   Time.timeScale = previousTime;
   if (game && !game.Terminou)
   {
    game.movimento.bloquearControles = false;
    game.espada.bloquearAtaque = false;
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
   }
  }

  void Update()
  {
   if (!game || !music) return;
   AtualizarMix(Time.unscaledDeltaTime);
   if (Keyboard.current != null && Keyboard.current.f10Key.wasPressedThisFrame)
   {
    if (panel) Fechar();
    else
    {
     panel = true;
     previousTime = Time.timeScale;
     Time.timeScale = 0;
     game.movimento.bloquearControles = true;
     game.espada.bloquearAtaque = true;
     Cursor.lockState = CursorLockMode.None;
     Cursor.visible = true;
    }
   }
   if (panel) return;
   Vector3 position = game.jogador.transform.position;
   float distance = Vector3.ProjectOnPlane(position - previous, Vector3.up).magnitude;
   previous = position;
   if (distance < 1) walked += distance;
   if (walked > .9f && !game.Terminou)
   {
    walked = 0;
    if (passos != null && passos.Length > 0) Play(passos[Random.Range(0, passos.Length)], .55f);
   }
   if (game.jogador.Vida < life && Time.frameCount > bloqueioAteFrame) TocarDanoJogador(false);
   else if (game.jogador.Vida > life && !ended) Play(cura, .65f);
   life = game.jogador.Vida;
   if (game.Moedas > gold) Play(moeda, .65f);
   gold = game.Moedas;
   foreach (var pair in enemies)
   {
    var enemy = pair.Key;
    var state = pair.Value;
    if (!enemy) continue;
    if (enemy.Vida < state.vida) TocarAcerto(enemy);
    else if (enemy.Estado != state.estado)
    {
     if (enemy.Estado == "Ataque" && ataque) state.audio.PlayOneShot(ataque, .65f);
     else if (state.estado == "Parado" && enemy.Estado == "Caminhada" && alerta) state.audio.PlayOneShot(alerta, .4f);
    }
    state.vida = enemy.Vida;
    state.estado = enemy.Estado;
   }
   if (game.Terminou && !ended) Play(game.jogador.Morto ? derrota : vitoria, .85f);
   ended = game.Terminou;
  }

  void OnDisable() { if (panel) { Time.timeScale = previousTime; panel = false; } }
  void OnDestroy()
  {
   if (direita) direita.aoIniciarCorte.RemoveListener(CortarDireita);
   if (esquerda) esquerda.aoIniciarCorte.RemoveListener(CortarEsquerda);
  }

  void OnGUI()
  {
   if (!panel) return;
   float width = Mathf.Min(560, Screen.width - 30);
   GUILayout.BeginArea(new Rect((Screen.width - width) / 2, 40, width, Mathf.Min(540, Screen.height - 70)), GUI.skin.box);
   GUILayout.Label("ÁUDIO — FORTALEZA AZUL");
   GUILayout.Space(14);
   GUILayout.Label("Música"); volumeMusica = GUILayout.HorizontalSlider(volumeMusica, 0, 1);
   GUILayout.Label("Efeitos"); volumeEfeitos = GUILayout.HorizontalSlider(volumeEfeitos, 0, 1);
   GUILayout.Label("Ambiente"); volumeAmbiente = GUILayout.HorizontalSlider(volumeAmbiente, 0, 1);
   GUILayout.Space(15);
   GUILayout.Label(CreditosAudio);
   GUILayout.Space(15);
   if (GUILayout.Button("VOLTAR AO JOGO")) Fechar();
   GUILayout.EndArea();
  }
 }
}
