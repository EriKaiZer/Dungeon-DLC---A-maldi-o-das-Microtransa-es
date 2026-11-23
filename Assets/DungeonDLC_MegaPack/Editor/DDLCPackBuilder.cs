using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

namespace DungeonDLC.MegaPack.Editor
{
    public static class DDLCPackBuilder
    {
        public const string Root = "Assets/DungeonDLC_MegaPack";
        static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
        static readonly string[] surfaceNames = { "Stone", "Damp", "Floor", "MossFloor", "Ceiling", "Wood", "Iron" };
        static readonly string[] textureNames = { "A01_Pedra_Azul", "A02_Pedra_Umida", "A03_Piso_Desgastado", "A04_Piso_Musgo", "A05_Teto_Pedra", "A06_Madeira_Antiga", "A07_Ferro_Oxidado" };
        static readonly Dictionary<string, Color> colors = new Dictionary<string, Color> {
            {"Gold", new Color(.58f,.40f,.14f)}, {"Blue",new Color(.055f,.12f,.25f)},
            {"Bone",new Color(.65f,.61f,.44f)}, {"Paper",new Color(.63f,.55f,.36f)},
            {"Wax",new Color(.68f,.58f,.37f)}, {"Glow",new Color(.1f,.55f,.85f)},
            {"Flame",new Color(1,.36f,.055f)}, {"Rope",new Color(.39f,.31f,.18f)} };
        public static void EnsureFolders()
        {
            foreach (string dir in new[] {"Materials", "Prefabs/Models", "Prefabs/Decals", "Prefabs/Signs", "Prefabs/Enemies", "Prefabs/Rooms", "Scenes", "Generated"})
                Directory.CreateDirectory(Root + "/" + dir);
            AssetDatabase.Refresh();
        }
        static Shader LitShader()
        {
            bool urp = GraphicsSettings.currentRenderPipeline != null;
            return Shader.Find(urp ? "Universal Render Pipeline/Lit" : "Standard") ?? Shader.Find("Standard");
        }
        static Material MakeMaterial(string name, Texture texture, Color color, bool alpha = false, bool unlit = false)
        {
            string p = Root + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            Shader shader = unlit ? (Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent")) : LitShader();
            if (!m) { m = new Material(shader); AssetDatabase.CreateAsset(m, p); }
            m.shader = shader;
            m.mainTexture = texture; m.color = color;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", texture);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .03f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", .03f);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0);
            if (alpha)
            {
                if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1);
                if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", .25f);
                if (m.HasProperty("_Mode")) m.SetFloat("_Mode", 1);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetOverrideTag("RenderType", "TransparentCutout");
                m.renderQueue = 2450;
            }
            if (name == "Glow" || name == "Flame")
            {
                m.EnableKeyword("_EMISSION");
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * 1.8f);
            }
            EditorUtility.SetDirty(m);
            materials[name] = m;
            return m;
        }
        public static void BuildMaterials()
        {
            EnsureFolders(); materials.Clear();
            for (int i = 0; i < surfaceNames.Length; i++)
                MakeMaterial(surfaceNames[i], AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Textures/" + textureNames[i] + ".png"), Color.white);
            foreach (var c in colors)
            {
                Texture texture=null;Color tint=c.Value;
                if(c.Key=="Gold"){texture=materials["Iron"].mainTexture;tint=new Color(2.3f,1.7f,.65f);}
                if(c.Key=="Blue"){texture=materials["Stone"].mainTexture;tint=new Color(.18f,.30f,.53f);}
                MakeMaterial(c.Key,texture,tint);
            }
            foreach (string folder in new[] {"Decals", "Signs", "Enemies"})
                if (Directory.Exists(Root + "/Art/" + folder))
                    foreach (string file in Directory.GetFiles(Root + "/Art/" + folder, "*.png"))
                        MakeMaterial(Path.GetFileNameWithoutExtension(file), AssetDatabase.LoadAssetAtPath<Texture2D>(file.Replace('\\','/')), Color.white, folder != "Signs", folder == "Signs" || file.Contains("V01_"));
        }
        static string CleanMaterialName(string name)
        {
            name = name.Replace(" (Instance)", "");
            if (materials.ContainsKey(name)) return name;
            foreach (string key in materials.Keys.OrderByDescending(k => k.Length))
                if (name.EndsWith(key, StringComparison.Ordinal)) return key;
            return "Stone";
        }
        static void MapMaterials(GameObject go)
        {
            foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>())
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => materials[CleanMaterialName(m ? m.name : "Stone")]).ToArray();
        }
        static Mesh QuadMesh()
        {
            string p = Root + "/Generated/Quad.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(p);
            if (mesh) return mesh;
            mesh = new Mesh { name = "DDLC_Quad_Facing_MinusZ" };
            mesh.vertices = new[] { new Vector3(-.5f,0,0), new Vector3(.5f,0,0), new Vector3(.5f,1,0), new Vector3(-.5f,1,0) };
            mesh.uv = new[] { new Vector2(0,0), new Vector2(1,0), new Vector2(1,1), new Vector2(0,1) };
            mesh.triangles = new[] {0,2,1,0,3,2}; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, p); return mesh;
        }
        static GameObject ImageObject(string name, float height, bool billboard)
        {
            var tex = materials[name].mainTexture;
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = QuadMesh();
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = materials[name];
            r.shadowCastingMode = ShadowCastingMode.Off;
            go.transform.localScale = new Vector3(height * tex.width / tex.height, height, 1);
            if (billboard) go.AddComponent<DDLCBillboard>();
            return go;
        }
        public static void BuildPrefabs()
        {
            foreach (string file in Directory.GetFiles(Root + "/Models", "*.obj"))
            {
                string id = Path.GetFileNameWithoutExtension(file);
                var source = AssetDatabase.LoadAssetAtPath<GameObject>(file.Replace('\\','/'));
                if (!source) throw new Exception("Modelo não importado: " + file);
                var go = UnityEngine.Object.Instantiate(source); go.name = id;
                MapMaterials(go);
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
                    if (mf.sharedMesh && mf.sharedMesh.vertexCount > 0)
                        mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                if (id == "C01_Tocha_Suporte")
                {
                    var flame = ImageObject("V01_Chama", .7f, true);
                    flame.transform.SetParent(go.transform, false); flame.transform.localPosition = new Vector3(0,.94f,-.35f);
                    var lightGo = new GameObject("Luz_Tocha"); lightGo.transform.SetParent(go.transform,false);
                    lightGo.transform.localPosition = new Vector3(0,1.18f,-.55f);
                    var l = lightGo.AddComponent<Light>(); l.type=LightType.Point; l.color=new Color(1,.52f,.2f); l.range=6; l.intensity=2.2f; l.shadows=LightShadows.None;
                    lightGo.AddComponent<DDLCTorchLight>();
                }
                PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/Models/"+id+".prefab"); UnityEngine.Object.DestroyImmediate(go);
            }
            foreach (string folder in new[] {"Decals", "Signs", "Enemies"})
                foreach (string file in Directory.GetFiles(Root + "/Art/" + folder, "*.png"))
                {
                    string id = Path.GetFileNameWithoutExtension(file);
                    float h = folder == "Enemies" ? (id.StartsWith("E04") ? 1.15f : 2.1f) : folder == "Signs" ? .8f : id.Contains("B04") ? 2.3f : 1.2f;
                    var image = ImageObject(id,h,folder=="Enemies" || id.StartsWith("V01"));
                    var go = new GameObject(id); image.transform.SetParent(go.transform,false);
                    if (folder=="Signs")
                    {
                        var board=Spawn("C20_Placa_Base",go.transform,Vector3.zero);
                        image.transform.localPosition=new Vector3(0,.05f,-.062f);
                        image.transform.localScale=new Vector3(1.6f,.8f,1);
                    }
                    PrefabUtility.SaveAsPrefabAsset(go,Root+"/Prefabs/"+folder+"/"+id+".prefab"); UnityEngine.Object.DestroyImmediate(go);
                }
        }
        public static GameObject Spawn(string id, Transform parent, Vector3 position, float yaw=0)
        {
            string p = Root+"/Prefabs/Models/"+id+".prefab";
            if (!File.Exists(p)) foreach (string dir in new[] {"Signs","Decals","Enemies","Rooms"})
            { string q=Root+"/Prefabs/"+dir+"/"+id+".prefab"; if (File.Exists(q)) {p=q;break;} }
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(p);
            if (!asset) throw new Exception("Prefab ausente: "+id);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,yaw,0);
            return go;
        }
        static void Socket(Transform parent,string name,Vector3 p,float yaw=0)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localRotation=Quaternion.Euler(0,yaw,0);}
        public static GameObject Room(string name,int width,int depth,int theme,int seed,bool north=true,bool south=true)
        {
            var root=new GameObject(name); var geo=new GameObject("Estrutura").transform;geo.SetParent(root.transform,false);
            // Width/depth are odd counts of 4m cells, so the middle cell is a doorway.
            float halfW=width*2,halfD=depth*2;
            for(int x=0;x<width;x++)for(int z=0;z<depth;z++)
            {
                Vector3 p=new Vector3((x-(width-1)*.5f)*4,0,(z-(depth-1)*.5f)*4);
                Spawn(theme==2?"M_Piso_Musgo_4m":"M_Piso_4m",geo,p);
                Spawn("M_Teto_4m",geo,p+Vector3.up*3.25f);
            }
            for(int x=0;x<width;x++)
            {
                float px=(x-(width-1)*.5f)*4;bool middle=x==width/2;
                Spawn(middle&&south?"M_Arco_4m":"M_Parede_4m",geo,new Vector3(px,0,-halfD));
                Spawn(middle&&north?"M_Arco_4m":"M_Parede_4m",geo,new Vector3(px,0,halfD));
            }
            for(int z=0;z<depth;z++)
            {
                float pz=(z-(depth-1)*.5f)*4;
                Spawn("M_Parede_4m",geo,new Vector3(-halfW,0,pz),90);
                Spawn("M_Parede_4m",geo,new Vector3(halfW,0,pz),90);
            }
            var decor=new GameObject("Decoracao_Seed_"+seed).transform;decor.SetParent(root.transform,false);
            Decorate(decor,width*4,depth*4,theme,seed);
            Socket(root.transform,"Socket_Sul",new Vector3(0,0,-halfD),180);
            Socket(root.transform,"Socket_Norte",new Vector3(0,0,halfD));
            Socket(root.transform,"Spawn_Player",new Vector3(0,.05f,-halfD+2));
            Socket(root.transform,"Spawn_Inimigo_01",new Vector3(1,0,0));
            Socket(root.transform,"Spawn_Inimigo_02",new Vector3(-1,0,2));
            return root;
        }
        public static void Decorate(Transform parent,float width,float depth,int theme,int seed)
        {
            var random=new System.Random(seed);float hw=width/2,hd=depth/2;
            if(width<8)
            {
                Spawn("C01_Tocha_Suporte",parent,new Vector3(-hw+.22f,1.05f,0),-90);
                Spawn("C01_Tocha_Suporte",parent,new Vector3(hw-.22f,1.05f,0),90);
                return;
            }
            // Two side bands leave a central passage at least 3m wide.
            for(int side=-1;side<=1;side+=2)
            {
                float yaw=side<0?-90:90;
                for(int i=0;i<Mathf.Max(1,(int)(depth/6));i++)
                {
                    float z=-hd+3+i*6;
                    Spawn("C01_Tocha_Suporte",parent,new Vector3(side*(hw-.22f),1.05f,z),yaw);
                }
                Spawn("B04_Bandeira_Sorny",parent,new Vector3(side*(hw-.20f),.45f,0),yaw);
                for(int i=0;i<2;i++)
                {
                    string id=new[]{"C02_Barril","C03_Caixa","C04_Caixa_Cordas","C05_Escombros"}[random.Next(4)];
                    Spawn(id,parent,new Vector3(side*(hw-1.25f),0,-hd+2+i*(depth-4)),(float)random.Next(360));
                }
            }
            int[] signs={1,4,7,10,13};
            Spawn("P"+signs[Mathf.Clamp(theme,0,4)].ToString("00"),parent,new Vector3(hw-.21f,1.3f,-2),90);
            if(theme==0){Spawn("C09_Mesa",parent,new Vector3(-hw+2,0,0));Spawn("C11_Livro_Dividas",parent,new Vector3(-hw+2,.89f,0));Spawn("C10_Cadeira",parent,new Vector3(-hw+2,0,1));}
            if(theme==1){for(int side=-1;side<=1;side+=2)Spawn("M_Mureta",parent,new Vector3(side*(hw-2.8f),0,1));Spawn("C08_Estatua_Executivo",parent,new Vector3(-hw+2,0,hd-2));}
            if(theme==2){Spawn("D06_Espinhos",parent,new Vector3(-hw+2,0,0));Spawn("C21_Cavalete_Manutencao",parent,new Vector3(hw-2,0,1),-35);Spawn("M_Ponte_Quebrada",parent,new Vector3(hw-2,0,hd-3),90);}
            if(theme==3){Spawn("C07_Bau_Deluxe",parent,new Vector3(-hw+2,0,2),20);Spawn("C06_Pedestal_Premium",parent,new Vector3(hw-2,0,2));Spawn("C17_Cristal_Licenca",parent,new Vector3(hw-2,1.31f,2));Spawn("D05_Sarcofago_Consumidor",parent,new Vector3(-hw+2,0,-2));}
            if(theme==4){Spawn("D01_Altar_Exclusividade",parent,new Vector3(0,0,hd-5));Spawn("C22_Trono_SoNao",parent,new Vector3(0,0,hd-1.2f));for(int side=-1;side<=1;side+=2){Spawn("M_Pilar_Azul",parent,new Vector3(side*(hw-3),0,2));Spawn("C08_Estatua_Executivo",parent,new Vector3(side*(hw-3),0,hd-3));}Spawn("D02_Selo_Assinatura",parent,new Vector3(0,1.4f,hd-.21f));}
        }
        [MenuItem("Dungeon & DLC/1 - Preparar materiais e prefabs")]
        public static void Prepare()
        {
            BuildMaterials();BuildPrefabs();
            string[] ids={"Sala_01_Recepcao_Premium","Sala_02_Combate_Exclusivo","Sala_03_Patch_Inacabado","Sala_04_Cofre_Deluxe","Sala_05_Trono_SoNao"};
            for(int i=0;i<5;i++)
            {
                int cells=i==4?5:3;
                var room=Room(ids[i],cells,3,i,1400+i,i!=4,i!=0);
                PrefabUtility.SaveAsPrefabAsset(room,Root+"/Prefabs/Rooms/"+ids[i]+".prefab");UnityEngine.Object.DestroyImmediate(room);
            }
            AssetDatabase.SaveAssets(); Debug.Log("DDLC: materiais, prefabs e cinco salas preparados.");
        }
        [MenuItem("Dungeon & DLC/2 - Criar cena demonstracao")]
        public static void DemoMenu()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            CreateDemo();
        }
        public static void CreateDemo()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var root=new GameObject("Fortaleza_Azul_Demonstracao");
            string[] ids={"Sala_01_Recepcao_Premium","Sala_02_Combate_Exclusivo","Sala_03_Patch_Inacabado","Sala_04_Cofre_Deluxe","Sala_05_Trono_SoNao"};
            for(int i=0;i<5;i++)Spawn(ids[i],root.transform,new Vector3(0,0,i*16));
            for(int i=0;i<4;i++)
            {
                var corridor=Room("Corredor_"+(i+1),1,1,0,1800+i);
                corridor.transform.SetParent(root.transform,false);corridor.transform.localPosition=new Vector3(0,0,8+i*16);
            }
            var enemies=new GameObject("Sprites_Visuais_Sem_Combate").transform;enemies.SetParent(root.transform,false);
            Spawn("E01_Guarda_Premium",enemies,new Vector3(-2,0,18));
            Spawn("E02_Cobrador_Assinaturas",enemies,new Vector3(3,0,33));
            Spawn("E04_Mimico_Deluxe",enemies,new Vector3(-2.7f,0,49));
            Spawn("E03_Cavaleiro_Definitivo",enemies,new Vector3(-3,0,65));
            var camGo=new GameObject("Camera_Previa_Sem_Controlador");camGo.tag="MainCamera";
            var cam=camGo.AddComponent<Camera>();cam.nearClipPlane=.05f;cam.farClipPlane=140;cam.fieldOfView=75;
            camGo.transform.position=new Vector3(0,1.7f,-3.5f);camGo.transform.rotation=Quaternion.Euler(3,0,0);
            camGo.AddComponent<AudioListener>();
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.30f,.34f,.43f);
            RenderSettings.fog=true;RenderSettings.fogColor=new Color(.035f,.045f,.07f);RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.025f;
            var fill=new GameObject("Luz_ambiente_suave").AddComponent<Light>();fill.type=LightType.Directional;fill.color=new Color(.55f,.64f,1);fill.intensity=.30f;fill.transform.rotation=Quaternion.Euler(50,-25,0);
            EditorSceneManager.SaveScene(scene,Root+"/Scenes/Fortaleza_Azul_Demo.unity");
            Selection.activeGameObject=root;
            Debug.Log("DDLC: demo com cinco salas conectadas. Use seu controlador FPS existente no Spawn_Player da primeira sala; a câmera incluída é apenas uma prévia estática.");
        }
        public static void BatchBuild()
        {
            Prepare();CreateDemo();ValidatePack();
            var args=Environment.GetCommandLineArgs();int n=Array.IndexOf(args,"-ddlcOutput");
            string output=n>=0?args[n+1]:Path.GetFullPath("DungeonDLC_MegaPack.unitypackage");
            n=Array.IndexOf(args,"-ddlcPreview");
            if(n>=0)CapturePreviews(args[n+1]);
            AssetDatabase.ExportPackage(Root,output,ExportPackageOptions.Recurse);
            Debug.Log("DDLC_PACKAGE="+output);
        }
        public static void CapturePreviews(string folder)
        {
            Directory.CreateDirectory(folder);var cam=Camera.main;
            var rt=new RenderTexture(960,540,24,RenderTextureFormat.ARGB32);rt.Create();
            var old=RenderTexture.active;var oldPos=cam.transform.position;var oldRot=cam.transform.rotation;
            bool oldAsync=ShaderUtil.allowAsyncCompilation;ShaderUtil.allowAsyncCompilation=false;
            string[] names={"01_Recepcao","02_Combate","03_Patch","04_Cofre","05_Trono"};
            try
            {
                // Render the entry again after every material has been drawn once.
                // This also warms first-frame GPU texture uploads in batch rendering.
                for(int step=0;step<6;step++)
                {
                    int i=step%5;
                    cam.transform.position=new Vector3(3,1.7f,-3.7f+i*16);
                    cam.transform.LookAt(new Vector3(0,1.45f,2+i*16));
                    cam.backgroundColor=new Color(.03f,.04f,.06f);cam.clearFlags=CameraClearFlags.SolidColor;
                    foreach(var b in UnityEngine.Object.FindObjectsByType<DDLCBillboard>(FindObjectsSortMode.None))
                    {
                        Vector3 d=b.transform.position-cam.transform.position;d.y=0;
                        if(d.sqrMagnitude>.001f)b.transform.rotation=Quaternion.LookRotation(d,Vector3.up);
                    }
                    if(GraphicsSettings.currentRenderPipeline)
                    {
                        var request=new RenderPipeline.StandardRequest{destination=rt};
                        RenderPipeline.SubmitRenderRequest(cam,request);
                    }
                    else{cam.targetTexture=rt;cam.Render();cam.targetTexture=null;}
                    RenderTexture.active=rt;var texture=new Texture2D(960,540,TextureFormat.RGB24,false);
                    texture.ReadPixels(new Rect(0,0,960,540),0,0);texture.Apply();
                    File.WriteAllBytes(Path.Combine(folder,names[i]+".png"),texture.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally{cam.transform.SetPositionAndRotation(oldPos,oldRot);RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);ShaderUtil.allowAsyncCompilation=oldAsync;}
            Debug.Log("DDLC_PREVIEWS="+folder);
        }
        [MenuItem("Dungeon & DLC/4 - Conferir pacote")]
        public static void ValidatePack()
        {
            var prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{Root+"/Prefabs"});
            int renderers=0,colliders=0;
            foreach(var guid in prefabs)
            {
                var p=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                foreach(var r in p.GetComponentsInChildren<MeshRenderer>(true))
                {renderers++;if(r.sharedMaterials.Any(m=>!m || !m.shader || m.shader.name.Contains("InternalErrorShader")))throw new Exception("Material inválido em "+p.name);}
                colliders+=p.GetComponentsInChildren<MeshCollider>(true).Length;
            }
            foreach(var texture in textureNames)if(!AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Art/Textures/"+texture+".png"))throw new Exception("Textura ausente "+texture);
            var wall=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Models/M_Parede_4m.prefab");
            var bounds=wall.GetComponentsInChildren<MeshRenderer>()[0].bounds;
            if(Mathf.Abs(bounds.size.x-4)>.05f || Mathf.Abs(bounds.size.y-3)>.05f)throw new Exception("Escala de parede incorreta: "+bounds.size);
            Physics.SyncTransforms();int pathChecks=0;
            if(GameObject.Find("Fortaleza_Azul_Demonstracao"))
            {
                for(float z=-4;z<=60;z+=.5f)
                {
                    if(!Physics.Raycast(new Vector3(0,.5f,z),Vector3.down,1f))throw new Exception("Falta piso em Z="+z);
                    if(Physics.CheckCapsule(new Vector3(0,.5f,z),new Vector3(0,1.45f,z),.3f))throw new Exception("Passagem bloqueada em Z="+z);
                    pathChecks++;
                }
            }
            string report="Unity "+Application.unityVersion+"\nPipeline: "+(GraphicsSettings.currentRenderPipeline?GraphicsSettings.currentRenderPipeline.name:"Built-in")+"\nPrefabs: "+prefabs.Length+"\nRenderers conferidos: "+renderers+"\nColliders: "+colliders+"\nVerificações de piso e passagem: "+pathChecks+"\nEscala: parede 4 x 3m confirmada.\nNenhum material ausente.\n";
            File.WriteAllText(Root+"/Generated/VALIDACAO_UNITY.txt",report);Debug.Log(report);
        }
    }
    public sealed class DDLCRoomWindow : EditorWindow
    {
        int width=3,depth=3,theme=0,seed=2026;
        [MenuItem("Dungeon & DLC/3 - Criar sala modular")]
        public static void Open(){GetWindow<DDLCRoomWindow>("Sala Dungeon & DLC");}
        void OnGUI()
        {
            EditorGUILayout.LabelField("Fortaleza Azul",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Cada célula mede 4m. O centro fica livre para circulação. Gere primeiro os materiais e prefabs no menu 1.",MessageType.Info);
            width=EditorGUILayout.IntSlider("Largura (células)",width,1,7);if(width%2==0)width++;
            depth=EditorGUILayout.IntSlider("Profundidade (células)",depth,1,7);if(depth%2==0)depth++;
            theme=EditorGUILayout.Popup("Tema",theme,new[]{"Recepção Premium","Combate Exclusivo","Patch Inacabado","Cofre Deluxe","Trono Só-Não"});
            seed=EditorGUILayout.IntField("Semente",seed);
            if(GUILayout.Button("Criar sala na cena atual"))
            {var go=DDLCPackBuilder.Room("Sala_Modular_"+seed,width,depth,theme,seed);Undo.RegisterCreatedObjectUndo(go,"Criar sala DDLC");Selection.activeGameObject=go;EditorSceneManager.MarkSceneDirty(go.scene);}
        }
    }
}
