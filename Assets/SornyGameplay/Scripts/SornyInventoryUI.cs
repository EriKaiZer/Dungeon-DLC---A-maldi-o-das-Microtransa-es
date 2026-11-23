using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DungeonDLC.Sorny
{
    [DefaultExecutionOrder(500)]
    public sealed class SornyInventoryUI : MonoBehaviour
    {
        public enum Item { Espada, Escudo, Adaga, Maca, Armadura, Botas, Pocao }
        public Sprite iconeEspada,iconeEscudo,iconeAdaga,iconeMaca,iconeArmadura,iconeBotas,iconePocao,iconeMoeda;
        public bool Aberto { get; private set; }
        public bool ModoLoja { get; private set; }
        public Item Selecionado { get; private set; }
        public Canvas Tela => canvas;
        public SornyMerchant Mercador => merchant;

        SornyGameLoop game;
        SornyEconomy economy;
        SornyHandEquipment hands;
        SornyAudioDirector audio;
        SornyMerchant merchant;
        AudioSource fonte;
        Canvas canvas;
        RectTransform root,content;
        Text saldo,vida,resposta,fala;
        Image retrato;
        readonly Dictionary<Canvas,bool> canvases=new Dictionary<Canvas,bool>();
        readonly Dictionary<Item,Button> cards=new Dictionary<Item,Button>();
        bool movimentoAntes,ataqueAntes,esquerdaAntes,audioAntes,ponteiroAntes;
        CursorLockMode cursorAntes;
        readonly Color fundo=new Color(.025f,.037f,.065f,.99f),painel=new Color(.055f,.075f,.11f,.98f),ouro=new Color(.94f,.73f,.31f),texto=new Color(.88f,.85f,.75f),suave=new Color(.56f,.61f,.66f);

        void Awake()
        {
            game=GetComponent<SornyGameLoop>();economy=GetComponent<SornyEconomy>();hands=game.espada.GetComponent<SornyHandEquipment>();audio=GetComponent<SornyAudioDirector>();
            fonte=gameObject.AddComponent<AudioSource>();fonte.playOnAwake=false;fonte.spatialBlend=0;fonte.dopplerLevel=0;fonte.priority=20;
        }
        public bool AbrirInventario()=>Abrir(false,null);
        public bool AbrirLoja(SornyMerchant npc)=>npc&&Abrir(true,npc);
        bool Abrir(bool loja,SornyMerchant npc)
        {
            if(Aberto||!game||game.Terminou||game.jogador.Morto||Time.timeScale<=0||(SornySession.Instancia&&!SornySession.Instancia.Pronta))return false;
            CriarTela();
            movimentoAntes=game.movimento.bloquearControles;ataqueAntes=hands.direita.bloquearAtaque;esquerdaAntes=hands.esquerda.bloquearAtaque;audioAntes=audio&&audio.enabled;
            cursorAntes=Cursor.lockState;ponteiroAntes=Cursor.visible;
            hands.direita.CancelarAtaque();hands.esquerda.CancelarAtaque();hands.Defender(false);
            game.movimento.bloquearControles=true;hands.direita.bloquearAtaque=hands.esquerda.bloquearAtaque=true;
            if(audio)audio.enabled=false;
            canvases.Clear();
            foreach(var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c!=canvas&&c.renderMode!=RenderMode.WorldSpace))
            {canvases[c]=c.enabled;c.enabled=false;}
            merchant=npc;ModoLoja=loja;Aberto=true;Selecionado=Item.Espada;economy.LimparMensagem();
            if(npc)npc.Cumprimentar();Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            canvas.gameObject.SetActive(true);Construir();return true;
        }
        public void Fechar()
        {
            if(!Aberto)return;
            Aberto=false;ModoLoja=false;merchant=null;
            if(canvas)canvas.gameObject.SetActive(false);
            foreach(var pair in canvases)if(pair.Key)pair.Key.enabled=pair.Value;canvases.Clear();
            Time.timeScale=1;
            if(game&&!game.Terminou)
            {
                game.movimento.bloquearControles=movimentoAntes;hands.direita.bloquearAtaque=ataqueAntes;hands.esquerda.bloquearAtaque=esquerdaAntes;
                Cursor.lockState=cursorAntes;Cursor.visible=ponteiroAntes;
            }
            if(audio){audio.SincronizarEstado();audio.enabled=audioAntes;}
            if(SornySession.Instancia)SornySession.Instancia.Save();
        }
        public void TocarCura(){if(fonte&&audio&&audio.cura){fonte.volume=SornySession.Efeitos;fonte.PlayOneShot(audio.cura,.8f);}}

        void CriarTela()
        {
            if(canvas)return;
            if(!EventSystem.current)
            {var events=new GameObject("Eventos_Inventario",typeof(EventSystem),typeof(InputSystemUIInputModule));events.transform.SetParent(transform,false);}
            var go=new GameObject("Inventario_e_Comercio",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(transform,false);
            canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=200;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var dim=new GameObject("Escurecer_Cenario",typeof(RectTransform),typeof(Image));dim.transform.SetParent(go.transform,false);
            var dr=dim.GetComponent<RectTransform>();dr.anchorMin=Vector2.zero;dr.anchorMax=Vector2.one;dr.offsetMin=dr.offsetMax=Vector2.zero;dim.GetComponent<Image>().color=new Color(.005f,.009f,.018f,.87f);
            root=new GameObject("Tela",typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(go.transform,false);root.anchorMin=root.anchorMax=root.pivot=new Vector2(.5f,.5f);root.sizeDelta=new Vector2(1280,720);
            go.SetActive(false);
        }
        RectTransform Rect(string name,Transform parent,float x,float y,float width,float height)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);return r;
        }
        Image Box(string name,Transform parent,float x,float y,float width,float height,Color color)
        {var im=Rect(name,parent,x,y,width,height).gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;return im;}
        Text Label(string name,string value,Transform parent,float x,float y,float width,float height,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var t=Rect(name,parent,x,y,width,height).gameObject.AddComponent<Text>();t.font=Resources.Load<SornyMenuTheme>("SornyMenuTheme").fonte;t.fontSize=size;t.text=value;t.color=color;t.alignment=align;t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;
            var sh=t.gameObject.AddComponent<Shadow>();sh.effectColor=new Color(0,0,0,.7f);sh.effectDistance=new Vector2(1,-1);return t;
        }
        Image Icon(string name,Sprite sprite,Transform parent,float x,float y,float width,float height,bool owned=true)
        {
            var image=Rect(name,parent,x,y,width,height).gameObject.AddComponent<Image>();image.sprite=sprite;image.enabled=sprite;image.preserveAspect=true;image.raycastTarget=false;image.material=hands.materialArmasUI;image.color=owned?Color.white:new Color(.13f,.15f,.18f);return image;
        }
        Button Button(string name,string caption,Transform parent,float x,float y,float width,float height,Action action,bool enabled=true,int font=15)
        {
            var r=Rect(name,parent,x,y,width,height);var image=r.gameObject.AddComponent<Image>();image.color=painel;
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;button.interactable=enabled;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1.55f,1.35f,.85f);colors.pressedColor=new Color(1.8f,1.4f,.65f);colors.disabledColor=new Color(.45f,.45f,.45f);button.colors=colors;
            if(!string.IsNullOrEmpty(caption))Label("Legenda",caption,r,8,0,width-16,height,font,enabled?texto:suave,TextAnchor.MiddleCenter);
            button.onClick.AddListener(()=>action());return button;
        }

        public Sprite Icone(Item item)=>item==Item.Espada?iconeEspada:item==Item.Escudo?iconeEscudo:item==Item.Adaga?iconeAdaga:item==Item.Maca?iconeMaca:item==Item.Armadura?iconeArmadura:item==Item.Botas?iconeBotas:iconePocao;
        public bool Possui(Item item)=>item==Item.Espada?hands.EspadaLiberada:item==Item.Escudo?hands.EscudoLiberado:item==Item.Adaga?hands.AdagaLiberada:item==Item.Maca?hands.MachadoLiberado:item==Item.Armadura?economy.Armadura:item==Item.Botas?economy.Botas:economy.Pocoes>0;
        bool Equipado(Item item)=>item==Item.Espada?hands.maoDireita==SornyHandEquipment.ArmaDireita.Espada:item==Item.Escudo?hands.maoEsquerda==SornyHandEquipment.ArmaEsquerda.Escudo:item==Item.Adaga?hands.maoDireita==SornyHandEquipment.ArmaDireita.Adaga||hands.maoEsquerda==SornyHandEquipment.ArmaEsquerda.Adaga:item==Item.Maca?hands.maoDireita==SornyHandEquipment.ArmaDireita.Machado:item==Item.Armadura?economy.ArmaduraEquipada:item==Item.Botas&&economy.BotasEquipadas;
        string Nome(Item item)=>item==Item.Espada?(economy.Espada?"Espada do Estorno":"Espada comum"):item==Item.Escudo?"Escudo de madeira":item==Item.Adaga?"Adaga do acervo":item==Item.Maca?"Maça-Chave do Só-Não":item==Item.Armadura?"Armadura vitalícia":item==Item.Botas?"Botas sem mensalidade":"Poção de Estorno";
        string Curto(Item item)=>item==Item.Espada?"ESPADA":item==Item.Escudo?"ESCUDO":item==Item.Adaga?"ADAGA":item==Item.Maca?"MAÇA-CHAVE":item==Item.Armadura?"ARMADURA":item==Item.Botas?"BOTAS":"POÇÃO  x"+economy.Pocoes;
        string Descricao(Item item)=>item==Item.Espada?"Dano leve: "+(economy.Espada?40:25)+"\nPesado completo: "+(economy.Espada?76:48)+"\nUma mão.":item==Item.Escudo?"Reduz 60% do dano frontal.\nSegure o botão direito para defender.":item==Item.Adaga?"Dano leve: 15\nAtaques rápidos.\nPode usar em qualquer mão.":item==Item.Maca?"Dano leve: 42\nPesado completo: 80\nGolpe lento e forte.":item==Item.Armadura?"Reduz em 25% o dano recebido.\nPode equipar ou guardar.":item==Item.Botas?"Caminhada 20% mais rápida.\nPode equipar ou guardar.":"Recupera 25 de vida.\nVocê carrega até três.\nH: usar durante a exploração.";

        void Construir()
        {
            if(content){content.gameObject.SetActive(false);Destroy(content.gameObject);}cards.Clear();retrato=null;fala=null;vida=null;
            content=Rect("Conteudo",root,0,0,1280,720);
            Box("Moldura",content,56,46,1168,628,ouro);Box("Fundo",content,59,49,1162,622,fundo);
            Label("Titulo",ModoLoja?"MERCADOR DO ESTORNO":"INVENTÁRIO",content,88,70,800,44,24,ouro);
            Icon("Moeda",iconeMoeda,content,955,73,35,35);saldo=Label("Saldo",game.Moedas.ToString(),content,999,74,136,35,21,ouro,TextAnchor.MiddleRight);
            Button("Fechar","X",content,1161,70,36,36,Fechar);
            Box("Linha",content,88,122,1108,2,ouro);
            if(ModoLoja)ConstruirLoja();else ConstruirInventario();
            resposta=Label("Resposta",economy.Mensagem,content,ModoLoja?90:390,576,ModoLoja?1100:806,28,15,ouro);
            Label("Ajuda",ModoLoja?"ESC: FECHAR  |  COMPRAS SÃO SALVAS NA PARTIDA":"ESC / TAB / I: FECHAR  |  CLIQUE EM UM EQUIPAMENTO",content,90,628,700,25,12,suave);
            Button("Voltar","VOLTAR AO JOGO",content,984,616,212,38,Fechar,true,13);
            if(cards.TryGetValue(Selecionado,out var selected)&&EventSystem.current)EventSystem.current.SetSelectedGameObject(selected.gameObject);
            Ajustar();
        }
        void ConstruirInventario()
        {
            Label("Caloteus","CALOTEUS",content,90,132,260,28,18,ouro);
            var direita=hands.maoDireita==SornyHandEquipment.ArmaDireita.Machado?Item.Maca:hands.maoDireita==SornyHandEquipment.ArmaDireita.Adaga?Item.Adaga:Item.Espada;
            Slot("MÃO DIREITA",direita,hands.maoDireita==SornyHandEquipment.ArmaDireita.Vazia?"VAZIA":Nome(direita),164,hands.maoDireita!=SornyHandEquipment.ArmaDireita.Vazia);
            Slot("MÃO ESQUERDA",hands.maoEsquerda==SornyHandEquipment.ArmaEsquerda.Adaga?Item.Adaga:Item.Escudo,hands.maoEsquerda==SornyHandEquipment.ArmaEsquerda.Vazia?"VAZIA":Nome(hands.maoEsquerda==SornyHandEquipment.ArmaEsquerda.Adaga?Item.Adaga:Item.Escudo),264,hands.maoEsquerda!=SornyHandEquipment.ArmaEsquerda.Vazia);
            Slot("CORPO",Item.Armadura,economy.ArmaduraEquipada?Nome(Item.Armadura):"SEM ARMADURA",364,economy.ArmaduraEquipada);
            Slot("PÉS",Item.Botas,economy.BotasEquipadas?Nome(Item.Botas):"BOTAS COMUNS",464,economy.BotasEquipadas);
            Label("Mochila","EQUIPAMENTOS",content,390,132,500,28,18,ouro);
            foreach(Item item in Enum.GetValues(typeof(Item)))
            {
                int i=(int)item;float x=390+(i%3)*176,y=164+(i/3)*130;bool owned=Possui(item);var captured=item;
                var card=Button("Item_"+item,"",content,x,y,160,120,()=>Selecionar(captured));cards[item]=card;
                if(Selecionado==item)Box("Selecionado",card.transform,0,117,160,3,ouro);
                Icon("Icone",Icone(item),card.transform,44,6,72,66,owned);
                Label("Nome",owned||item==Item.Pocao?Curto(item):"NÃO ENCONTRADO",card.transform,7,76,146,23,12,owned?texto:suave,TextAnchor.MiddleCenter);
                Label("Estado",Equipado(item)?"EQUIPADO":"",card.transform,7,99,146,16,10,ouro,TextAnchor.MiddleCenter);
            }
            Box("Detalhes",content,941,164,255,396,painel);
            bool possui=Possui(Selecionado);
            Icon("Item_Grande",Icone(Selecionado),content,1025,184,88,100,possui);
            Label("Item_Nome",possui||Selecionado==Item.Pocao?Nome(Selecionado):"Equipamento desconhecido",content,958,295,222,53,17,ouro,TextAnchor.MiddleCenter);
            Label("Descricao",possui||Selecionado==Item.Pocao?Descricao(Selecionado):"Explore a fortaleza para encontrar novos equipamentos.",content,958,357,222,107,14,texto);
            if(possui)AcoesItem();
            vida=Label("Vida","VIDA  "+game.jogador.Vida+" / "+game.jogador.vidaMaxima,content,90,568,250,24,14,texto);
            if(merchant)Button("Retornar_Loja","VOLTAR À LOJA",content,744,616,222,38,()=>{ModoLoja=true;Construir();},true,13);
        }
        void Slot(string titulo,Item item,string nome,float y,bool equipado)
        {
            var b=Button("Slot_"+titulo,"",content,90,y,270,88,()=>Selecionar(item));
            if(equipado)Icon("Icone",Icone(item),b.transform,8,7,62,72);
            Label("Categoria",titulo,b.transform,82,8,178,22,12,suave);
            Label("Equipamento",nome,b.transform,82,30,178,48,14,texto);
        }
        void AcoesItem()
        {
            if(Selecionado==Item.Adaga)
            {
                Button("Equipar_Direita",hands.maoDireita==SornyHandEquipment.ArmaDireita.Adaga?"GUARDAR DIREITA":"EQUIPAR DIREITA",content,958,471,222,33,()=>EquiparDireita(Item.Adaga),true,12);
                Button("Equipar_Esquerda",hands.maoEsquerda==SornyHandEquipment.ArmaEsquerda.Adaga?"GUARDAR ESQUERDA":"EQUIPAR ESQUERDA",content,958,512,222,33,()=>EquiparEsquerda(Item.Adaga),true,12);
            }
            else if(Selecionado==Item.Espada||Selecionado==Item.Maca)
                Button("Equipar_Direita",Equipado(Selecionado)?"GUARDAR ARMA":"EQUIPAR DIREITA",content,958,495,222,40,()=>EquiparDireita(Selecionado),true,12);
            else if(Selecionado==Item.Escudo)
                Button("Equipar_Esquerda",Equipado(Selecionado)?"GUARDAR ESCUDO":"EQUIPAR ESQUERDA",content,958,495,222,40,()=>EquiparEsquerda(Item.Escudo),true,12);
            else if(Selecionado==Item.Armadura||Selecionado==Item.Botas)
                Button("Vestir",Equipado(Selecionado)?"GUARDAR":"EQUIPAR",content,958,495,222,40,()=>{if(Selecionado==Item.Armadura)economy.AlternarArmadura();else economy.AlternarBotas();Construir();},true,12);
            else Button("Usar_Pocao",game.jogador.Vida>=game.jogador.vidaMaxima?"VIDA COMPLETA":"USAR POÇÃO",content,958,495,222,40,()=>{economy.UsarPocao();Construir();},game.jogador.Vida<game.jogador.vidaMaxima,12);
        }
        public void Selecionar(Item item){Selecionado=item;if(Aberto&&!ModoLoja)Construir();}
        public bool EquiparDireita(Item item)
        {
            if(!Possui(item)||item!=Item.Espada&&item!=Item.Adaga&&item!=Item.Maca)return false;
            var arma=item==Item.Espada?SornyHandEquipment.ArmaDireita.Espada:item==Item.Adaga?SornyHandEquipment.ArmaDireita.Adaga:SornyHandEquipment.ArmaDireita.Machado;
            bool ok=hands.SelecionarDireita(hands.maoDireita==arma?SornyHandEquipment.ArmaDireita.Vazia:arma);if(Aberto)Construir();return ok;
        }
        public bool EquiparEsquerda(Item item)
        {
            if(!Possui(item)||item!=Item.Escudo&&item!=Item.Adaga)return false;
            var arma=item==Item.Escudo?SornyHandEquipment.ArmaEsquerda.Escudo:SornyHandEquipment.ArmaEsquerda.Adaga;
            bool ok=hands.SelecionarEsquerda(hands.maoEsquerda==arma?SornyHandEquipment.ArmaEsquerda.Vazia:arma);if(Aberto)Construir();return ok;
        }
        void ConstruirLoja()
        {
            Box("Mercador",content,90,157,240,405,painel);
            retrato=Icon("Retrato",merchant?merchant.QuadroAtual:null,content,105,165,210,275);
            fala=Label("Fala",merchant?merchant.Fala:"",content,108,454,204,90,15,texto,TextAnchor.MiddleCenter);
            string[] nomes={"ESPADA DO ESTORNO","ARMADURA VITALÍCIA","BOTAS DE VIAGEM","POÇÃO DE ESTORNO"};
            string[] descricoes={"Melhora sua espada para 40 de dano.","Reduz em 25% o dano recebido.","Caminhada 20% mais rápida.","Recupera 25 de vida. Limite: três."};
            Item[] items={Item.Espada,Item.Armadura,Item.Botas,Item.Pocao};
            for(int i=0;i<4;i++)
            {
                int index=i;float x=354+(i%2)*285,y=157+(i/2)*205;var item=items[i];
                Box("Produto_"+i,content,x,y,267,187,painel);Icon("Icone_Produto_"+i,Icone(item),content,x+12,y+10,64,80);
                Label("Nome_Produto_"+i,nomes[i],content,x+86,y+12,167,40,15,ouro);
                Label("Descricao_Produto_"+i,descricoes[i],content,x+86,y+58,167,58,12,texto);
                bool bought=economy.Owned(i),full=i==3&&economy.Pocoes>=SornyEconomy.MaxPocoes,can=!bought&&!full&&game.Moedas>=SornyEconomy.Precos[i];
                Label("Preco_"+i,SornyEconomy.Precos[i]+" MOEDAS",content,x+15,y+104,237,28,16,ouro,TextAnchor.MiddleCenter);
                Button("Comprar_"+i,bought?"JÁ COMPRADO":full?"BOLSA CHEIA":can?"COMPRAR":"SEM MOEDAS",content,x+15,y+143,237,32,()=>Comprar(index),can,13);
            }
            Box("Seu_Equipamento",content,942,157,254,405,painel);
            Label("Seu_Titulo","SEU EQUIPAMENTO",content,957,172,224,35,16,ouro,TextAnchor.MiddleCenter);
            Label("Resumo",hands.NomeDireita+"\n\n"+hands.NomeEsquerda+"\n\nARMADURA "+(economy.ArmaduraEquipada?"EQUIPADA":"NENHUMA")+"\n\nBOTAS "+(economy.BotasEquipadas?"EQUIPADAS":"COMUNS")+"\n\nPOÇÕES "+economy.Pocoes+" / "+SornyEconomy.MaxPocoes,content,960,222,218,260,14,texto);
            Button("Ver_Inventario","VER INVENTÁRIO",content,959,509,220,34,()=>{ModoLoja=false;Construir();},true,12);
        }
        public bool Comprar(int item)
        {
            if(!Aberto||!ModoLoja||!merchant)return false;
            bool ok=economy.Buy(item);Construir();return ok;
        }
        void Ajustar(){if(root&&canvas){var rect=canvas.GetComponent<RectTransform>();root.localScale=Vector3.one*Mathf.Min(rect.rect.width/1280,rect.rect.height/720);}}
        void Update()
        {
            if(!game)return;var key=Keyboard.current;
            if(Aberto)
            {
                Ajustar();if(saldo)saldo.text=game.Moedas.ToString();if(resposta)resposta.text=economy.Mensagem;
                if(vida)vida.text="VIDA  "+game.jogador.Vida+" / "+game.jogador.vidaMaxima;
                if(retrato&&merchant)retrato.sprite=merchant.QuadroAtual;if(fala&&merchant)fala.text=merchant.Fala;
                if(key!=null&&(key.escapeKey.wasPressedThisFrame||key.tabKey.wasPressedThisFrame||key.iKey.wasPressedThisFrame))Fechar();return;
            }
            if(key==null||Time.timeScale<=0||game.Terminou||game.jogador.Morto)return;
            if(key.tabKey.wasPressedThisFrame||key.iKey.wasPressedThisFrame){AbrirInventario();return;}
            if(key.hKey.wasPressedThisFrame&&!game.movimento.bloquearControles){economy.UsarPocao();game.Notificar(economy.Mensagem,2);}
        }
        void OnDestroy(){if(!Aberto)return;Aberto=false;Time.timeScale=1;foreach(var pair in canvases)if(pair.Key)pair.Key.enabled=pair.Value;}
    }
}
