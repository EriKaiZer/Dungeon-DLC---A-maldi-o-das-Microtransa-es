using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DungeonDLC.MegaPack.Editor;

namespace DungeonDLC.Sorny.Editor
{
    [InitializeOnLoad]
    public static class SornyArrivalInstaller
    {
        const string Root = "Assets/SornyGameplay";
        const string ScenePath = Root + "/Scenes/Fortaleza_Azul_Jogavel.unity";
        const string Folder = Root + "/Generated/Chegada";
        const string Request = Root + "/Reports/chegada-fortaleza.txt";
        const string GroupName = "Fase_00_Chegada_a_Fortaleza";
        static double next;
        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static SornyArrivalInstaller() { EditorApplication.update += Poll; }

        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + 2;
            if (!File.Exists(Request) || File.ReadAllText(Request).Trim() != "pending" || SceneManager.GetActiveScene().path != ScenePath) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) { if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); return; }
            try { Apply(); File.WriteAllText(Request, "done"); }
            catch (Exception e) { File.WriteAllText(Request, "error: " + e); Debug.LogException(e); }
        }

        static Transform Group(string name, Transform parent, Vector3 position = default)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.localPosition = position; return go.transform;
        }
        static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(DDLCPackBuilder.Root + "/Materials/" + name + ".mat") ?? throw new Exception("Material ausente: " + name);
        static GameObject Spawn(string id, Transform parent, Vector3 position, float yaw = 0) => DDLCPackBuilder.Spawn(id, parent, position, yaw);

        static Mesh BoxMesh(Vector3 size)
        {
            string id = (size.x.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "_" + size.y.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "_" + size.z.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Replace('.', 'p');
            if (meshes.TryGetValue(id, out Mesh cached)) return cached;
            string path = Folder + "/Caixa_" + id + ".asset";
            cached = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (cached) { meshes[id] = cached; return cached; }
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            Vector3 h = size * .5f;
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float width, float height)
            {
                int n = vertices.Count; vertices.AddRange(new[] { a, b, c, d });
                uv.AddRange(new[] { Vector2.zero, new Vector2(width / 2, 0), new Vector2(width / 2, height / 2), new Vector2(0, height / 2) });
                triangles.AddRange(new[] { n, n + 2, n + 1, n, n + 3, n + 2 });
            }
            Face(new Vector3(-h.x,-h.y,-h.z),new Vector3(h.x,-h.y,-h.z),new Vector3(h.x,h.y,-h.z),new Vector3(-h.x,h.y,-h.z),size.x,size.y);
            Face(new Vector3(h.x,-h.y,h.z),new Vector3(-h.x,-h.y,h.z),new Vector3(-h.x,h.y,h.z),new Vector3(h.x,h.y,h.z),size.x,size.y);
            Face(new Vector3(h.x,-h.y,-h.z),new Vector3(h.x,-h.y,h.z),new Vector3(h.x,h.y,h.z),new Vector3(h.x,h.y,-h.z),size.z,size.y);
            Face(new Vector3(-h.x,-h.y,h.z),new Vector3(-h.x,-h.y,-h.z),new Vector3(-h.x,h.y,-h.z),new Vector3(-h.x,h.y,h.z),size.z,size.y);
            Face(new Vector3(-h.x,h.y,-h.z),new Vector3(h.x,h.y,-h.z),new Vector3(h.x,h.y,h.z),new Vector3(-h.x,h.y,h.z),size.x,size.z);
            Face(new Vector3(-h.x,-h.y,h.z),new Vector3(h.x,-h.y,h.z),new Vector3(h.x,-h.y,-h.z),new Vector3(-h.x,-h.y,-h.z),size.x,size.z);
            var mesh = new Mesh { name = "Chegada_Caixa_Textura_Em_Escala" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path); meshes[id] = mesh; return mesh;
        }

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool solid = true)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            go.GetComponent<MeshFilter>().sharedMesh = BoxMesh(size);
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (solid) go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        static Material Tint(string name, Material source, Color color)
        {
            string path = Folder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(source); material.name = name; material.color = color; AssetDatabase.CreateAsset(material, path); }
            return material;
        }

        static Material SolidMaterial(string name, Color color)
        {
            var material=Tint(name,Mat("Iron"),color);
            material.mainTexture=null;if(material.HasProperty("_BaseMap"))material.SetTexture("_BaseMap",null);
            material.color=color;if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.18f);
            EditorUtility.SetDirty(material);return material;
        }

        static Mesh BladeMesh(bool dagger)
        {
            string path=Folder+(dagger?"/Lamina_Adaga.asset":"/Lamina_Espada.asset");
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh)return mesh;
            float length=dagger?.54f:1,half=dagger?.065f:.09f;
            var verts=new List<Vector3>();var triangles=new List<int>();
            Vector3[] ring={new Vector3(-half,.36f,0),new Vector3(0,.36f,-.03f),new Vector3(half,.36f,0),new Vector3(0,.36f,.03f)};
            verts.AddRange(ring);verts.AddRange(ring.Select(p=>new Vector3(p.x*.78f,.36f+length-.18f,p.z*.8f)));verts.Add(new Vector3(0,.36f+length,0));
            for(int i=0;i<4;i++){int n=(i+1)%4;triangles.AddRange(new[]{i,i+4,n,n,i+4,n+4,i+4,8,n+4});}
            mesh=new Mesh{name=dagger?"Adaga_Aco":"Espada_Aco"};mesh.SetVertices(verts);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }

        static AudioClip GateSound()
        {
            string path=Folder+"/Portao_Madeira.wav";
            if(!File.Exists(path))
            {
                const int rate=22050;int count=(int)(rate*2.5f);var random=new System.Random(1701);float low=0;
                using(var writer=new BinaryWriter(File.Create(path)))
                {
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                    writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
                    for(int i=0;i<count;i++)
                    {
                        float t=(float)i/rate;low=Mathf.Lerp(low,(float)(random.NextDouble()*2-1),.055f);
                        float env=Mathf.Clamp01(t/.08f)*Mathf.Clamp01((2.5f-t)/.6f);
                        float creak=Mathf.Sin(t*(450+55*Mathf.Sin(t*9)))*.09f*Mathf.Pow(.5f+.5f*Mathf.Sin(t*17),3);
                        float drag=low*.8f+Mathf.Sin(t*155)*.07f+creak;
                        float thud=Mathf.Exp(-t*18)*Mathf.Sin(t*230)*.4f;
                        writer.Write((short)(Mathf.Clamp(drag*env+thud,-.95f,.95f)*short.MaxValue));
                    }
                }
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static void Light(Transform parent, string name, Vector3 position, Color color, float range, float intensity)
        {
            var go = new GameObject(name, typeof(Light)); go.transform.SetParent(parent, false); go.transform.localPosition = position;
            var light = go.GetComponent<Light>(); light.type = LightType.Point; light.color = color; light.range = range; light.intensity = intensity; light.shadows = LightShadows.None;
        }

        static void Sign(Transform parent, string name, string words, Vector3 position, float yaw = 0)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas)); go.transform.SetParent(parent, false);
            go.transform.localPosition = position; go.transform.localRotation = Quaternion.Euler(0, yaw, 0); go.transform.localScale = Vector3.one * .003f;
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(640, 180);
            var panel = new GameObject("Fundo", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(go.transform, false);
            var r = panel.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(.025f,.04f,.065f,.96f); panel.GetComponent<Image>().raycastTarget = false;
            var label = new GameObject("Texto", typeof(RectTransform), typeof(Text)); label.transform.SetParent(go.transform, false);
            r = label.GetComponent<RectTransform>(); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(18,10); r.offsetMax = new Vector2(-18,-10);
            var text = label.GetComponent<Text>(); text.font = Resources.Load<SornyMenuTheme>("SornyMenuTheme").fonte;
            text.fontSize = 24; text.alignment = TextAnchor.MiddleCenter; text.color = new Color(1,.83f,.5f); text.text = words; text.raycastTarget = false;
        }

        static void WeaponModel(Transform parent, bool dagger)
        {
            var blade=new GameObject("Lamina",typeof(MeshFilter),typeof(MeshRenderer));blade.transform.SetParent(parent,false);blade.GetComponent<MeshFilter>().sharedMesh=BladeMesh(dagger);blade.GetComponent<MeshRenderer>().sharedMaterial=SolidMaterial("Aco_Equipamentos",new Color(.7f,.76f,.82f));
            var gold=SolidMaterial("Ouro_Equipamentos",new Color(.7f,.44f,.08f));
            Box("Guarda",parent,new Vector3(0,.34f,0),new Vector3(.46f,.07f,.1f),gold,false);
            Box("Cabo",parent,new Vector3(0,.18f,0),new Vector3(.075f,.25f,.075f),Mat("Wood"),false);
            Box("Pomo",parent,new Vector3(0,.025f,0),new Vector3(.12f,.1f,.1f),gold,false);
            var wrap=SolidMaterial("Tecido_Roxo",new Color(.36f,.12f,.3f));
            for(int i=0;i<3;i++)Box("Faixa_"+i,parent,new Vector3(0,.1f+i*.075f,0),new Vector3(.085f,.035f,.085f),wrap,false);
        }

        static SornyPickup WeaponPickup(Transform parent, string name, SornyPickup.Tipo type, Vector3 position)
        {
            var root = Group(name,parent,position);
            var pickup = root.gameObject.AddComponent<SornyPickup>(); pickup.tipo = type; pickup.flutuar = pickup.girar = false;
            var trigger = root.gameObject.AddComponent<SphereCollider>(); trigger.isTrigger = true; trigger.radius = .45f; trigger.center = Vector3.up * .3f;
            var visual = Group("Arma_no_Suporte",root); pickup.visual = visual;
            if (type != SornyPickup.Tipo.EscudoInicial) WeaponModel(visual, type == SornyPickup.Tipo.AdagaTesouro);
            else
            {
                var plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder); plate.name = "Escudo_Madeira"; plate.transform.SetParent(visual,false);
                plate.transform.localPosition = Vector3.up * .34f; plate.transform.localRotation = Quaternion.Euler(90,0,0); plate.transform.localScale = new Vector3(.8f,.045f,.8f);
                UnityEngine.Object.DestroyImmediate(plate.GetComponent<Collider>()); plate.GetComponent<Renderer>().sharedMaterial = Mat("Wood");
                var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder); rim.name = "Borda_Ferro"; rim.transform.SetParent(visual,false);
                rim.transform.localPosition = new Vector3(0,.34f,.025f); rim.transform.localRotation = Quaternion.Euler(90,0,0); rim.transform.localScale = new Vector3(.9f,.035f,.9f);
                UnityEngine.Object.DestroyImmediate(rim.GetComponent<Collider>()); rim.GetComponent<Renderer>().sharedMaterial = Mat("Iron");
                for(int i=-1;i<=1;i++) Box("Reforco_"+i,visual,new Vector3(0,.34f+i*.24f,-.055f),new Vector3(.68f,.045f,.025f),Mat("Iron"),false);
            }
            return pickup;
        }

        static void BuildPath(Transform root, SornyArrival arrival)
        {
            var ground = Group("01_Caminho_e_Postos",root);
            for(int z=-84;z<=-44;z+=4) for(int x=-4;x<=4;x+=4)
                Spawn(x==0?"M_Piso_4m":"M_Piso_Musgo_4m",ground,new Vector3(x,0,z));
            Box("Limite_de_Rochas_Sul",ground,new Vector3(0,2,-86.3f),new Vector3(16,4,.7f),Mat("Damp"));
            for(int side=-1;side<=1;side+=2)
                for(int i=0;i<11;i++)
                {
                    float height=4.3f+(i%3)*.9f;
                    var rock=Box("Encosta_"+side+"_"+i,ground,new Vector3(side*(7.8f+(i%2)*.5f),height*.5f,-84+i*4),new Vector3(4,height,4.3f),Mat("Damp"));
                    rock.transform.localRotation=Quaternion.Euler(0,side*(i%3)*4,0);
                    if(i%3==0) Spawn("B01_Musgo",ground,new Vector3(side*5.75f,.05f,-84+i*4),side<0?-90:90);
                }
            var post=Group("Posto_Abandonado",ground,new Vector3(3.3f,0,-66));
            Spawn("C09_Mesa",post,Vector3.zero);
            Box("Fundo_do_Posto",post,new Vector3(0,1.6f,.7f),new Vector3(3.4f,3.2f,.35f),Mat("Stone"));
            Box("Pilar_E",post,new Vector3(-1.65f,1.5f,-.1f),new Vector3(.28f,3,.28f),Mat("Wood"));
            Box("Pilar_D",post,new Vector3(1.65f,1.5f,-.1f),new Vector3(.28f,3,.28f),Mat("Wood"));
            Box("Cobertura",post,new Vector3(0,3,.15f),new Vector3(3.8f,.3f,2.2f),Mat("Wood"));
            arrival.espadaInicial=WeaponPickup(root,"Primeira_Espada",SornyPickup.Tipo.EspadaInicial,new Vector3(3.3f,.92f,-66.5f));
            Sign(post,"Aviso_Espada","POSTO DE GUARDA",new Vector3(0,2.65f,.47f));
            Light(post,"Luz_do_Posto",new Vector3(0,2,-1.4f),new Color(1,.7f,.4f),6,3);
            var shield=Group("Abrigo_Antes_da_Ponte",ground,new Vector3(-3.4f,0,-45));
            Box("Parede_Abrigo",shield,new Vector3(0,1.5f,.6f),new Vector3(3.4f,3,.45f),Mat("Stone"));
            Box("Suporte",shield,new Vector3(0,.85f,.1f),new Vector3(.16f,1.7f,.16f),Mat("Wood"));
            arrival.escudoInicial=WeaponPickup(root,"Primeiro_Escudo",SornyPickup.Tipo.EscudoInicial,new Vector3(-3.4f,.9f,-45.15f));
            Sign(shield,"Aviso_Escudo","ABRIGO DA PONTE",new Vector3(0,2.3f,.33f));
            Light(shield,"Luz_do_Abrigo",new Vector3(0,2,-1.3f),new Color(1,.7f,.4f),6,3);
            for(int i=0;i<3;i++) Light(root,"Luz_Externa_"+i,new Vector3(0,6,-77+i*22),new Color(.48f,.65f,1),29,8);
        }

        static void BuildBridge(Transform root)
        {
            var bridge=Group("02_Ponte_de_Pedra",root);
            for(int z=-40;z<=-12;z+=4) Spawn("M_Piso_4m",bridge,new Vector3(0,0,z));
            for(int side=-1;side<=1;side+=2)
            {
                Box("Parapeito_"+side,bridge,new Vector3(side*2.18f,.65f,-26),new Vector3(.38f,1.3f,32),Mat("Stone"));
                for(int z=-42;z<=-10;z+=4)
                    Box("Marco_"+side+"_"+z,bridge,new Vector3(side*2.18f,.82f,z),new Vector3(.65f,1.64f,.65f),Mat("Stone"));
            }
            for(int z=-38;z<=-14;z+=8) Box("Apoio_da_Ponte_"+z,bridge,new Vector3(0,-2,z),new Vector3(3.6f,4,1.3f),Mat("Damp"));
            var water=Tint("Agua_Escura",Mat("Blue"),new Color(.1f,.28f,.33f));
            Box("Fosso",bridge,new Vector3(0,-3.8f,-26),new Vector3(46,.2f,31),water,false);
            for(int side=-1;side<=1;side+=2)
                Box("Margem_do_Fosso_"+side,bridge,new Vector3(side*23,-1.4f,-26),new Vector3(3,5,35),Mat("Damp"));
        }

        static void BuildTutorial(Transform root, SornyGameLoop game, SornyArrival arrival)
        {
            var target=Group("Manequim_de_Treino",root,new Vector3(-2.8f,0,-58));
            var dummy=target.gameObject.AddComponent<SornyTrainingDummy>();
            var body=target.gameObject.AddComponent<BoxCollider>();body.center=new Vector3(0,1,0);body.size=new Vector3(.9f,2,.7f);
            Box("Base",target,new Vector3(0,.07f,0),new Vector3(.9f,.14f,.8f),Mat("Wood"),false);
            var visual=Group("Corpo_Oscilante",target);
            Box("Haste",visual,new Vector3(0,.8f,0),new Vector3(.12f,1.6f,.12f),Mat("Wood"),false);
            Box("Saco_de_Treino",visual,new Vector3(0,1.25f,0),new Vector3(.6f,.75f,.4f),Mat("Rope"),false);
            Box("Bracos",visual,new Vector3(0,1.45f,0),new Vector3(1.25f,.12f,.12f),Mat("Wood"),false);
            var head=GameObject.CreatePrimitive(PrimitiveType.Sphere);head.name="Cabeca_de_Palha";head.transform.SetParent(visual,false);head.transform.localPosition=new Vector3(0,1.9f,0);head.transform.localScale=Vector3.one*.38f;
            UnityEngine.Object.DestroyImmediate(head.GetComponent<Collider>());head.GetComponent<Renderer>().sharedMaterial=Mat("Rope");
            dummy.corpo=visual;dummy.impacto=game.GetComponent<SornyAudioDirector>().bloqueiosEscudo.FirstOrDefault();arrival.manequim=dummy;
            Sign(target,"Aviso_Treino","MANEQUIM DE TREINO",new Vector3(0,2.55f,.1f));
            Light(target,"Luz_Treino",new Vector3(0,2,-1.4f),new Color(1,.72f,.42f),6,3);
            var template=UnityEngine.Object.FindObjectsByType<CrackEnemy>(FindObjectsSortMode.None).First(e=>e.nivel==1&&!e.GetComponent<SornyBoss>());
            var guard=UnityEngine.Object.Instantiate(template.gameObject,root);guard.name="Cobrador_da_Ponte_Tutorial";guard.transform.SetPositionAndRotation(new Vector3(0,.04f,-38.7f),Quaternion.identity);
            var enemy=guard.GetComponent<CrackEnemy>();enemy.nome="Cobrador da Ponte";enemy.nivel=0;enemy.vidaMaxima=45;enemy.dano=8;enemy.velocidade=1.4f;enemy.percepcao=6;enemy.intervaloAtaque=1.35f;enemy.enabled=false;arrival.sentinelaPonte=enemy;
        }

        static void BuildCastle(Transform root, SornyArrival arrival)
        {
            var castle=Group("03_Fachada_e_Portoes",root);
            for(int side=-1;side<=1;side+=2)
            {
                Box("Muralha_"+side,castle,new Vector3(side*8.1f,3.5f,-10),new Vector3(11.8f,7,1.4f),Mat("Stone"));
                Box("Torre_"+side,castle,new Vector3(side*10,5,-8.5f),new Vector3(5,10,5),Mat("Stone"));
                Box("Coroamento_"+side,castle,new Vector3(side*10,10,-8.5f),new Vector3(5.6f,.5f,5.6f),Mat("Stone"));
                for(int j=0;j<4;j++)
                    Box("Ameia_"+side+"_"+j,castle,new Vector3(side*10-2.1f+j*1.4f,10.75f,-11.05f),new Vector3(.7f,1.4f,.6f),Mat("Stone"));
                for(int j=0;j<5;j++) Box("Muralha_Ameia_"+side+"_"+j,castle,new Vector3(side*(3.1f+j*1.3f),7.5f,-10),new Vector3(.65f,1,1.4f),Mat("Stone"));
                Spawn("B04_Bandeira_Sorny",castle,new Vector3(side*4.1f,4.2f,-10.77f));
                Spawn("C01_Tocha_Suporte",castle,new Vector3(side*2.85f,1.3f,-10.8f));
            }
            Box("Verga_da_Entrada",castle,new Vector3(0,5.3f,-10),new Vector3(4.4f,3.4f,1.4f),Mat("Stone"));
            for(int side=-1;side<=1;side+=2)
            {
                var pivot=Group(side<0?"Portao_E_Pivo":"Portao_D_Pivo",castle,new Vector3(side*2.15f,0,-10));
                var panel=Group("Folha",pivot,new Vector3(-side*1.075f,0,0));
                Box("Madeira",panel,new Vector3(0,1.8f,0),new Vector3(2.14f,3.6f,.18f),Mat("Wood"));
                for(int y=0;y<3;y++) Box("Cinta_"+y,panel,new Vector3(0,.4f+y*1.35f,-.12f),new Vector3(2.14f,.14f,.07f),Mat("Iron"),false);
                Box("Fechadura",panel,new Vector3(-side*.8f,1.6f,-.15f),new Vector3(.15f,.25f,.1f),Mat("Gold"),false);
                if(side<0)arrival.folhaEsquerda=pivot;else arrival.folhaDireita=pivot;
            }
            Spawn("M_Piso_4m",castle,new Vector3(0,0,-8));
            Light(castle,"Luz_da_Entrada",new Vector3(0,4,-12),new Color(1,.66f,.32f),10,5);
            Sign(castle,"Nome_da_Fortaleza","FORTALEZA AZUL\nDAS EXCLUSIVIDADES",new Vector3(0,4.65f,-10.76f));
            var source=castle.gameObject.AddComponent<AudioSource>(); source.playOnAwake=false; source.spatialBlend=1; source.minDistance=4; source.maxDistance=25; source.dopplerLevel=0;
            source.clip=GateSound();
            arrival.somPortao=source;
            CoverEntrance(root);
        }

        static void CoverEntrance(Transform root)
        {
            var castle=root.Find("03_Fachada_e_Portoes");
            var previous=castle.Find("Vestibulo_Coberto");
            if(previous)
            {
                if(!previous.Find("Fecho_Entrada"))Box("Fecho_Entrada",previous,new Vector3(0,3.35f,-6),new Vector3(4.75f,.5f,.4f),Mat("Stone"));
                return;
            }
            var entry=Group("Vestibulo_Coberto",castle);
            for(int side=-1;side<=1;side+=2)
                Box("Parede_Entrada_"+side,entry,new Vector3(side*2.2f,1.8f,-8),new Vector3(.35f,3.6f,4),Mat("Stone"));
            Box("Teto_Entrada",entry,new Vector3(0,3.85f,-8),new Vector3(4.75f,.5f,4.2f),Mat("Stone"));
            Box("Fecho_Entrada",entry,new Vector3(0,3.35f,-6),new Vector3(4.75f,.5f,.4f),Mat("Stone"));
        }

        static void Bake(SornyGameLoop game, SornyArrival arrival)
        {
            var progress=game.GetComponent<SornyProgression>();
            var gates=new List<Transform>{arrival.folhaEsquerda,arrival.folhaDireita,progress.portaAcervo,progress.portaMasmorra2};
            foreach(var arena in UnityEngine.Object.FindObjectsByType<SornyBossArena>(FindObjectsSortMode.None))
                foreach(var field in typeof(SornyBossArena).GetFields())
                    if(field.FieldType==typeof(Transform))gates.Add(field.GetValue(arena) as Transform);
            var colliders=gates.Where(t=>t).SelectMany(t=>t.GetComponentsInChildren<Collider>()).Distinct().Where(c=>c.enabled).ToArray();
            foreach(var c in colliders)c.enabled=false;
            var nav=game.GetComponent<SornyNavGrid>();nav.origem=new Vector3(-10.5f,0,-86);nav.colunas=43;nav.linhas=449;nav.Bake();
            foreach(var c in colliders)c.enabled=true;
            EditorUtility.SetDirty(nav);Physics.SyncTransforms();
        }

        [MenuItem("Dungeon & DLC/18 - Fase inicial: chegada à fortaleza")]
        public static void Apply()
        {
            if(SceneManager.GetActiveScene().path!=ScenePath)throw new Exception("Abra a Fortaleza Azul Jogavel antes de aplicar.");
            Directory.CreateDirectory(Folder);Directory.CreateDirectory(Root+"/Reports");AssetDatabase.Refresh();
            var scene=SceneManager.GetActiveScene();var game=UnityEngine.Object.FindFirstObjectByType<SornyGameLoop>();
            var fortress=scene.GetRootGameObjects().Single(g=>g.name=="Fortaleza_Azul_Demonstracao");
            var existing=fortress.transform.Find(GroupName);
            if(existing&&game.GetComponent<SornyArrival>())
            {
                if(!existing.Find("03_Fachada_e_Portoes/Vestibulo_Coberto/Fecho_Entrada"))
                {
                    string copy=Root+"/Reports/BackupChegada/"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"_Entrada";Directory.CreateDirectory(copy);
                    if(!EditorSceneManager.SaveScene(scene,copy+"/Fortaleza_Azul_Estado_do_Editor.unity",true))throw new Exception("Nao foi possivel preservar a entrada antes de cobrir.");
                    CoverEntrance(existing);Bake(game,game.GetComponent<SornyArrival>());EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
                    Debug.Log("SORNY_ARRIVAL_ENTRANCE_FINISHED_OK");
                }
                Debug.Log("SORNY_ARRIVAL_ALREADY_APPLIED");return;
            }
            string backup=Root+"/Reports/BackupChegada/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);File.Copy(ScenePath,backup+"/Fortaleza_Azul_Arquivo_Anterior.unity",true);
            if(!EditorSceneManager.SaveScene(scene,backup+"/Fortaleza_Azul_Estado_do_Editor.unity",true))throw new Exception("Nao foi possivel preservar a cena aberta antes da alteracao.");
            var root=Group(GroupName,fortress.transform);
            var arrival=game.gameObject.AddComponent<SornyArrival>();
            BuildPath(root,arrival);BuildBridge(root);BuildCastle(root,arrival);BuildTutorial(root,game,arrival);
            var firstRoom=fortress.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Sala_01_Recepcao_Premium");
            foreach(var wall in firstRoom.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="M_Parede_4m"&&Mathf.Abs(t.position.x)<.1f&&Mathf.Abs(t.position.z+6)<.1f).ToArray())wall.gameObject.SetActive(false);
            if(!firstRoom.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="M_Arco_4m"&&Mathf.Abs(t.position.x)<.1f&&Mathf.Abs(t.position.z+6)<.1f&&t.gameObject.activeInHierarchy))Spawn("M_Arco_4m",root,new Vector3(0,0,-6));
            var treasure=Group("Adaga_do_Tesouro",root);
            Spawn("C06_Pedestal_Premium",treasure,new Vector3(-2.3f,0,51.4f));
            arrival.adagaTesouro=WeaponPickup(treasure,"Adaga_Exploracao",SornyPickup.Tipo.AdagaTesouro,new Vector3(-2.3f,.95f,51.4f));
            Sign(treasure,"Aviso_Adaga","ADAGA DO ACERVO\n2: DIREITA  |  4: ESQUERDA",new Vector3(-2.3f,2.3f,52));
            game.jogador.transform.SetPositionAndRotation(new Vector3(0,.06f,-79),Quaternion.identity);
            foreach(var text in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(text.name=="Controles")text.gameObject.SetActive(false);
            Bake(game,arrival);
            EditorUtility.SetDirty(arrival);EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Root+"/Reports/CHEGADA_FORTALEZA.txt","Tutorial inicial integrado à mesma Fortaleza Azul.\nSpawn: 0, .06, -79.\nEspada: 3.3, .92, -66.5. Manequim: -2.8, 0, -58. Escudo: -3.4, .9, -45.15.\nCobrador da ponte: 45 PV e 8 de dano, só desperta após recolher o escudo.\nPonte: -42 a -10. Portões abrem por aproximação em 2,2 segundos.\nUma dica contextual por vez. Equipamentos aparecem ao segurar TAB.\nAdaga na sala de tesouro original.\nJogador, HUD, baús, chefes e cenas anteriores preservados.\nBackup: "+backup+"\n");
            Debug.Log("SORNY_ARRIVAL_APPLIED_OK");
        }

        public static void BatchApply()
        {
            EditorSceneManager.OpenScene(ScenePath);Apply();File.WriteAllText(Request,"done");
        }
    }
}
