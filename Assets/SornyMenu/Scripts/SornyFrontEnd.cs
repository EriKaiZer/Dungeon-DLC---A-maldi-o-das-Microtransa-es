using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
namespace DungeonDLC.Sorny
{
 public sealed class SornyFrontEnd:MonoBehaviour
 {
  public const string MenuScene="Tela_Inicial",GameScene="Fortaleza_Azul_Jogavel";
  public bool pausa;public SornyEconomy economiaLoja;
  public string Tela{get;private set;}="";
  public bool Carregando{get;private set;}
  public SornyMenuTheme tema;
  RectTransform root,conteudo,ponteiro; RawImage fundo; Image fade; Text legenda;
  AudioSource music,sfx; CanvasGroup page; readonly List<Button> botoes=new List<Button>();
  readonly Color ouro=new Color(1,.77f,.32f),marfim=new Color(.95f,.91f,.76f);
  float tempo; bool pronto; Coroutine intro; GameObject ownEvents;Material atmosfera;
  void Start(){Build();}
  public void Build()
  {
   if(pronto)return;pronto=true;tema=tema?tema:Resources.Load<SornyMenuTheme>("SornyMenuTheme");
   if(!tema){Debug.LogError("Tema do menu ausente.");return;}
   Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
   var canvas=new GameObject("Menu Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.transform.SetParent(transform,false);
   var cv=canvas.GetComponent<Canvas>();cv.renderMode=RenderMode.ScreenSpaceOverlay;cv.sortingOrder=100;
   var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
   var outer=canvas.GetComponent<RectTransform>();root=Rect("Quadro 16x9",outer,0,0,1280,720);root.anchorMin=root.anchorMax=new Vector2(.5f,.5f);root.pivot=new Vector2(.5f,.5f);root.anchoredPosition=Vector2.zero;
   // A fixed 16:9 composition scales to fit and leaves black bars on other aspect ratios.
   var bars=new GameObject("Barras",typeof(RectTransform),typeof(Image));bars.transform.SetParent(outer,false);var br=(RectTransform)bars.transform;br.anchorMin=Vector2.zero;br.anchorMax=Vector2.one;br.offsetMin=br.offsetMax=Vector2.zero;bars.GetComponent<Image>().color=Color.black;bars.GetComponent<Image>().raycastTarget=false;bars.transform.SetAsFirstSibling();
   fundo=Raw("Fortaleza",root,0,0,1280,720,tema.fundo);fundo.raycastTarget=false;if(tema.atmosfera){atmosfera=new Material(tema.atmosfera);fundo.material=atmosfera;}
   if(!EventSystem.current){ownEvents=new GameObject("Eventos do menu",typeof(EventSystem),typeof(InputSystemUIInputModule));ownEvents.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
   sfx=gameObject.AddComponent<AudioSource>();sfx.playOnAwake=false;
   if(!pausa){if(PlayerPrefs.HasKey("Sorny.Fullscreen"))Screen.fullScreen=PlayerPrefs.GetInt("Sorny.Fullscreen")==1;music=gameObject.AddComponent<AudioSource>();music.clip=tema.musica;music.loop=true;music.volume=SornySession.Musica;music.Play();}
   if(economiaLoja)Shop();else Home();
  }
  RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
  {var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);return r;}
  RawImage Raw(string name,Transform parent,float x,float y,float w,float h,Texture tex){var r=Rect(name,parent,x,y,w,h);var raw=r.gameObject.AddComponent<RawImage>();raw.texture=tex;return raw;}
  Image Box(string name,Transform parent,float x,float y,float w,float h,Color c){var r=Rect(name,parent,x,y,w,h);var im=r.gameObject.AddComponent<Image>();im.color=c;im.raycastTarget=false;return im;}
  Text Label(string name,string text,Transform parent,float x,float y,float w,float h,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft)
  {var r=Rect(name,parent,x,y,w,h);var t=r.gameObject.AddComponent<Text>();t.font=tema.fonte;t.fontSize=size;t.text=text;t.color=color;t.alignment=align;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;t.raycastTarget=false;var sh=r.gameObject.AddComponent<Shadow>();sh.effectColor=new Color(0,0,0,.9f);sh.effectDistance=new Vector2(2,-2);return t;}
  void Clear(string tela)
  {
   Tela=tela;botoes.Clear();if(conteudo){conteudo.gameObject.SetActive(false);Destroy(conteudo.gameObject);}conteudo=Rect("Conteudo - "+tela,root,0,0,1280,720);page=conteudo.gameObject.AddComponent<CanvasGroup>();ponteiro=null;
   if(EventSystem.current)EventSystem.current.SetSelectedGameObject(null);
  }
  void Sound(bool confirm=false){if(sfx)sfx.PlayOneShot(confirm?tema.confirmar:tema.selecionar,SornySession.Efeitos*.35f);}
  Button Button(string text,float x,float y,float w,Action action,bool enabled=true,int size=22)
  {
   var r=Rect(text,conteudo,x,y,w,48);var bg=r.gameObject.AddComponent<Image>();bg.color=new Color(.03f,.06f,.12f,.12f);
   var b=r.gameObject.AddComponent<Button>();b.targetGraphic=bg;b.interactable=enabled;var c=b.colors;c.normalColor=Color.white;c.highlightedColor=new Color(1,1,.7f,.7f);c.selectedColor=c.highlightedColor;c.pressedColor=ouro;c.disabledColor=new Color(.4f,.4f,.5f,.5f);b.colors=c;
   Label("Texto",text,r,12,0,w-24,48,size,enabled?marfim:new Color(.4f,.4f,.46f));
   b.onClick.AddListener(()=>{if(Carregando)return;Sound(true);action();});
   var trigger=r.gameObject.AddComponent<EventTrigger>();var entry=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};entry.callback.AddListener(_=>{if(b.interactable)EventSystem.current.SetSelectedGameObject(b.gameObject);});trigger.triggers.Add(entry);
   botoes.Add(b);return b;
  }
  void First(){foreach(var b in botoes)if(b.interactable){if(EventSystem.current)EventSystem.current.SetSelectedGameObject(b.gameObject);break;}}
  void Sword()
  {
   ponteiro=Rect("Espadinha",conteudo,55,265,62,30);
   Box("Lamina",ponteiro,20,12,38,7,marfim);Box("Ponta",ponteiro,58,14,5,3,marfim);Box("Guarda",ponteiro,14,6,5,20,ouro);Box("Cabo",ponteiro,2,13,12,6,new Color(.5f,.27f,.1f));
  }
  public void Home()
  {
   Clear("Menu");fundo.texture=tema.fundo;fundo.material=atmosfera;fundo.color=Color.white;fundo.rectTransform.anchoredPosition=Vector2.zero;fundo.rectTransform.sizeDelta=new Vector2(1280,720);
   if(tema.titulo){var logo=Box("Titulo",conteudo,48,28,620,214,Color.white);logo.sprite=tema.titulo;logo.preserveAspect=true;}else Label("Titulo","DUNGEON & DLC",conteudo,52,50,700,100,40,ouro);
   Label("Subtitulo","A Maldição das Microtransações",conteudo,60,239,720,32,16,marfim);
   if(pausa)
   {
    Button("VOLTAR AO JOGO",110,303,480,()=>SornySession.Instancia.Retomar());
    Button("OPÇÕES",110,363,480,Options);Button("SALVAR E IR AO MENU",110,423,620,()=>SornySession.Instancia.VoltarMenu(),true,18);
   }
   else
   {
    Button("NOVO JOGO",110,303,450,()=>{if(SornySession.HasSave)ConfirmNew();else BeginNew();});
    Button("CONTINUAR",110,363,450,()=>LoadGame(true),SornySession.HasSave);
    Button("OPÇÕES",110,423,450,Options);Button("CRÉDITOS",110,483,450,Credits);Button("SAIR",110,543,450,Quit);
   }
   Label("Rodape","Essa parte ainda é grátis.",conteudo,250,658,820,30,14,marfim,TextAnchor.MiddleCenter);
   Label("Ajuda","↑ ↓ / W S  ESCOLHER    ENTER  CONFIRMAR",conteudo,60,698,1000,18,10,new Color(.65f,.69f,.75f));Sword();First();
  }
  void Panel(string title){Clear(title);Box("Painel",conteudo,60,50,1160,620,new Color(.015f,.027f,.065f,.96f));Box("Linha",conteudo,90,125,1100,3,ouro);Label("Cabecalho",title,conteudo,95,70,1080,60,24,ouro);}
  void ConfirmNew(){Panel("NOVO JOGO");Label("Confirmacao","Começar novamente substituirá a partida salva.\n\nQuer entrar na fortaleza desde o início?",conteudo,100,170,1060,150,18,marfim);Button("COMEÇAR NOVAMENTE",110,420,800,BeginNew);Button("VOLTAR",110,490,800,Home);First();}
  public void BeginNew(){SornySession.NewRun();StartIntro();}
  public void StartIntro(){Clear("Abertura");fundo.material=null;intro=StartCoroutine(Intro());}
  IEnumerator Intro()
  {
   var captions=new[]{"FORTALEZA AZUL DAS EXCLUSIVIDADES","Portões fechados. Taxas em aberto.","Caloteus chegou. A cobrança também.","A dungeon espera por você."};
   fade=Box("Transicao",conteudo,0,0,1280,720,Color.black);
   Box("Faixa de legenda",conteudo,0,560,1280,160,new Color(0,0,0,.75f));
   legenda=Label("Legenda","",conteudo,100,580,1080,75,18,marfim,TextAnchor.MiddleCenter);
   Button("PULAR ABERTURA",920,665,330,()=>SkipIntro(),true,12);
   for(int i=0;i<tema.abertura.Length;i++)
   {
    fundo.texture=tema.abertura[i];float duration=7f;
    for(float t=0;t<duration;t+=Time.unscaledDeltaTime)
    {
     float progress=t/duration;fundo.rectTransform.sizeDelta=new Vector2(1280,720)*(1.02f+progress*.035f);fundo.rectTransform.anchoredPosition=new Vector2(-progress*30,-progress*12);
     fade.color=new Color(0,0,0,Mathf.Max(1-t/.7f,(t-(duration-.7f))/.7f));
     legenda.text=captions[Mathf.Min(i,captions.Length-1)].Substring(0,Mathf.Min(captions[Mathf.Min(i,captions.Length-1)].Length,Mathf.FloorToInt(t*27)));yield return null;
    }
   }
   intro=null;LoadGame(false);
  }
  public void SkipIntro(){if(intro!=null){StopCoroutine(intro);intro=null;}LoadGame(false);}
  public void LoadGame(bool saved){if(Carregando)return;Carregando=true;SornySession.LoadSaved=saved;StartCoroutine(Load());}
  IEnumerator Load(){Clear("Carregando");Box("Escuro",conteudo,0,0,1280,720,new Color(0,0,0,.92f));Label("Aguarde","ABRINDO OS PORTÕES...",conteudo,100,310,1080,100,22,ouro,TextAnchor.MiddleCenter);yield return null;Time.timeScale=1;var operation=SceneManager.LoadSceneAsync(GameScene);while(!operation.isDone)yield return null;}
  void SliderRow(string label,float y,float value,float min,float max,Action<float> changed)
  {
   Label(label,label,conteudo,105,y,420,45,16,marfim);var r=Rect("Slider "+label,conteudo,570,y+10,540,26);r.gameObject.AddComponent<Image>().color=Color.clear;var s=r.gameObject.AddComponent<Slider>();
   Box("Trilho",r,0,8,540,8,new Color(.2f,.23f,.3f));var fillArea=Rect("Area preenchimento",r,0,8,540,8);var fill=Box("Volume",fillArea,0,0,0,0,ouro);fill.rectTransform.anchorMin=Vector2.zero;fill.rectTransform.anchorMax=Vector2.one;fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;s.fillRect=fill.rectTransform;
   var area=Rect("Area",r,8,0,524,26);var handle=Box("Cursor",area,0,0,16,26,marfim);handle.raycastTarget=true;handle.rectTransform.anchorMin=handle.rectTransform.anchorMax=new Vector2(0,.5f);handle.rectTransform.pivot=new Vector2(.5f,.5f);s.handleRect=handle.rectTransform;s.targetGraphic=handle;s.minValue=min;s.maxValue=max;s.value=value;s.onValueChanged.AddListener(v=>{changed(v);SornySession.ApplySettings();});
  }
  public void Options()
  {
   Panel("OPÇÕES");SliderRow("MÚSICA",165,SornySession.Musica,0,1,v=>PlayerPrefs.SetFloat("Sorny.Music",v));SliderRow("EFEITOS",225,SornySession.Efeitos,0,1,v=>PlayerPrefs.SetFloat("Sorny.Sfx",v));SliderRow("AMBIENTE",285,SornySession.Ambiente,0,1,v=>PlayerPrefs.SetFloat("Sorny.Amb",v));SliderRow("MOUSE",345,SornySession.Mouse,.01f,.3f,v=>PlayerPrefs.SetFloat("Sorny.Mouse",v));
   Button(Screen.fullScreen?"TELA: CHEIA":"TELA: JANELA",110,420,920,()=>{Screen.fullScreen=!Screen.fullScreen;PlayerPrefs.SetInt("Sorny.Fullscreen",Screen.fullScreen?1:0);PlayerPrefs.Save();StartCoroutine(RefreshOptions());},true,16);
   Label("Info","Os ajustes ficam salvos neste computador.",conteudo,110,495,1050,40,14,marfim);Button("VOLTAR",110,570,500,()=>{PlayerPrefs.Save();Home();});First();
  }
  IEnumerator RefreshOptions(){yield return null;yield return null;Options();}
  public void Credits(){Panel("CRÉDITOS");Label("Creditos","DUNGEON & DLC\nA Maldição das Microtransações\n\nCriação e direção: Erikk\nArtes do menu e abertura: geradas com IA\n\n"+SornyAudioDirector.CreditosAudio+"\n\nFonte: Press Start 2P · CodeMan38\nSIL Open Font License 1.1",conteudo,110,150,1060,400,14,marfim);Button("VOLTAR",110,570,500,Home);First();}
  void Quit(){Panel("SAIR DO JOGO?");Button("SIM, SAIR",110,260,700,()=>{
   #if UNITY_EDITOR
   UnityEditor.EditorApplication.isPlaying=false;
   #else
   Application.Quit();
   #endif
  });Button("VOLTAR",110,330,700,Home);First();}
  public void Shop()
  {
   Panel("LOJA DO ESTORNO");conteudo.Find("Cabecalho").gameObject.SetActive(false);Label("Loja Titulo","LOJA DO ESTORNO",conteudo,105,75,1000,40,20,ouro);var game=FindFirstObjectByType<SornyGameLoop>();Label("Saldo","MOEDAS: "+game.Moedas,conteudo,110,140,1060,40,20,ouro);
   string[] names={"ESPADA DO ESTORNO","ARMADURA VITALÍCIA","BOTAS SEM MENSALIDADE"};string[] desc={"40 de dano por golpe (comum: 25)","Reduz em 25% o dano recebido","Caminhada 20% mais rápida"};
   for(int i=0;i<3;i++){int index=i;float y=198+i*103;Label("Item "+i,names[i],conteudo,110,y,1000,30,18,marfim);Label("Descricao "+i,desc[i],conteudo,110,y+34,700,30,12,marfim);Button(economiaLoja.Owned(i)?"EQUIPADO":"COMPRAR / "+SornyEconomy.Precos[i],850,y+14,330,()=>{economiaLoja.Buy(index);Shop();},!economiaLoja.Owned(i),14);}
   Label("Resposta",economiaLoja.Mensagem,conteudo,110,523,1050,30,14,ouro);Button("VOLTAR À DUNGEON",110,578,900,()=>economiaLoja.CloseShop(),true,18);First();
  }
  GameObject selected;
  void Update()
  {
   if(!pronto||!root)return;var canvas=root.parent.GetComponent<RectTransform>();float fit=Mathf.Min(canvas.rect.width/1280,canvas.rect.height/720);root.localScale=Vector3.one*fit;
   if(music)music.volume=SornySession.Musica;tempo+=Time.unscaledDeltaTime;if(atmosfera)atmosfera.SetFloat("_Phase",tempo);
   if(Tela=="Menu")fundo.color=Color.white*(.96f+.025f*Mathf.Sin(tempo*.8f));
   var keyboard=Keyboard.current;
   if(keyboard!=null&&!Carregando)
   {
    if(Tela=="Abertura"&&(keyboard.escapeKey.wasPressedThisFrame||keyboard.spaceKey.wasPressedThisFrame)){SkipIntro();return;}
    if(keyboard.escapeKey.wasPressedThisFrame){if(economiaLoja)economiaLoja.CloseShop();else if(Tela!="Menu")Home();else if(pausa)SornySession.Instancia.Retomar();}
   }
   var current=EventSystem.current?EventSystem.current.currentSelectedGameObject:null;
   if(current!=selected){if(selected&&current)Sound();selected=current;}
   if(ponteiro&&current){var b=current.GetComponent<Button>();if(b&&botoes.Contains(b)){ponteiro.gameObject.SetActive(true);var rect=b.GetComponent<RectTransform>();ponteiro.anchoredPosition=new Vector2(rect.anchoredPosition.x-65,rect.anchoredPosition.y-9);foreach(var button in botoes){var label=button.GetComponentInChildren<Text>();label.color=!button.interactable?new Color(.4f,.4f,.46f):button==b?ouro:marfim;}}else ponteiro.gameObject.SetActive(false);}
  }
  void OnDestroy(){if(ownEvents)Destroy(ownEvents);if(atmosfera)Destroy(atmosfera);}
 }
}
