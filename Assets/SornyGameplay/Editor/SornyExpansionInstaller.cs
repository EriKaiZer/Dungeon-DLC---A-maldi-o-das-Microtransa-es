using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DungeonDLC.MegaPack.Editor;

namespace DungeonDLC.Sorny.Editor
{
    [InitializeOnLoad]
    public static class SornyExpansionInstaller
    {
        const string Root = "Assets/SornyGameplay";
        const string ScenePath = Root + "/Scenes/Fortaleza_Azul_Jogavel.unity";
        const string Request = Root + "/Reports/tesouro-masmorra2.txt";
        const string ExpansionName = "Acervo_Subterraneo_Expansao";
        static double next;
        static SornyExpansionInstaller() { EditorApplication.update += Poll; }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < next || EditorApplication.isCompiling) return;
            next = EditorApplication.timeSinceStartup + 2;
            if (!File.Exists(Request) || File.ReadAllText(Request).Trim() != "pending" || SceneManager.GetActiveScene().path != ScenePath) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) { if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); return; }
            try { Apply(); File.WriteAllText(Request, "done"); }
            catch (Exception error) { File.WriteAllText(Request, "error: " + error); Debug.LogException(error); }
        }

        static GameObject Spawn(string id, Transform parent, Vector3 position, float yaw = 0) => DDLCPackBuilder.Spawn(id, parent, position, yaw);
        static Transform Group(string name, Transform parent) { var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform; }

        static Texture2D Texture(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Sprite SaveSprite(Texture2D texture, Rect rect, Vector2 pivot, float ppu, string name)
        {
            var sprite = Sprite.Create(texture, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            string path = Root + "/Sprites/" + name + ".asset";
            var old = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (old) { EditorUtility.CopySerialized(sprite, old); UnityEngine.Object.DestroyImmediate(sprite); return old; }
            AssetDatabase.CreateAsset(sprite, path);
            return sprite;
        }

        static Sprite[] GuardianFrames()
        {
            var texture = Texture(Root + "/Art/Acervo/Guardiao_Acervo_8.png");
            var px = texture.GetPixels32();
            int w = texture.width / 4, h = texture.height / 2;
            var frames = new Sprite[8];
            float ppu = 1;
            for (int i = 0; i < 8; i++)
            {
                int left = i % 4 * w, bottom = (1 - i / 4) * h;
                int minY = h, maxY = -1;
                for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                    if (px[(bottom + y) * texture.width + left + x].a > 180) { minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
                if (maxY < minY) throw new InvalidOperationException("Quadro vazio do guardião: " + i);
                if (i == 0) ppu = (maxY - minY + 1) / 2.15f;
                // All frames keep the same scale and horizontal anchor; feet/corpse rest on the floor.
                frames[i] = SaveSprite(texture, new Rect(left, bottom, w, h), new Vector2(.5f, minY / (float)h), ppu, "Guardiao_Acervo_" + i);
            }
            return frames;
        }

        static void BindAudio(SornyAudioDirector audio)
        {
            const string path = Root + "/Audio/CorteV3/";
            foreach (string file in Directory.GetFiles(path, "*.wav"))
            {
                var importer = (AudioImporter)AssetImporter.GetAtPath(file);
                var sample = importer.defaultSampleSettings;
                sample.loadType = AudioClipLoadType.DecompressOnLoad;
                sample.compressionFormat = AudioCompressionFormat.PCM;
                importer.defaultSampleSettings = sample;
                importer.forceToMono = false;
                importer.SaveAndReimport();
            }
            AudioClip[] Clips(string prefix, int count) => Enumerable.Range(0, count).Select(i => AssetDatabase.LoadAssetAtPath<AudioClip>(path + prefix + i + ".wav") ?? throw new Exception(prefix + i)).ToArray();
            audio.acertosEspada = Clips("Rasgo_Espada_", 4);
            audio.acertosAdaga = Clips("Rasgo_Adaga_", 3);
            audio.acertosMachado = Clips("Machado_Corte_", 3);
            audio.bloqueiosEscudo = Clips("Bloqueio_Madeira_Metal_", 3);
            audio.danosJogador = Clips("Caloteus_Gemido_", 3);
            audio.impacto = audio.acertosEspada[0];
            audio.danoRecebido = audio.danosJogador[0];
            EditorUtility.SetDirty(audio);
        }

        static void Torch(Transform parent, float x, float z, bool green)
        {
            var torch = Spawn("C01_Tocha_Suporte", parent, new Vector3(x, 1.05f, z), x < 0 ? -90 : 90);
            foreach (var light in torch.GetComponentsInChildren<Light>())
            {
                light.color = green ? new Color(.4f, .9f, .65f) : new Color(1, .68f, .3f);
                light.range = 8;
                var flicker = light.GetComponent<DungeonDLC.MegaPack.DDLCTorchLight>();
                if (flicker) flicker.baseIntensity = green ? 3.1f : 2.5f;
            }
        }

        static Transform Room(Transform parent, string name, int width, int depth, float z, bool north, bool green)
        {
            var room = Group(name, parent);
            room.localPosition = new Vector3(0, 0, z);
            var structure = Group("Estrutura", room);
            for (int x = 0; x < width; x++) for (int y = 0; y < depth; y++)
            {
                Vector3 p = new Vector3((x - (width - 1) * .5f) * 4, 0, (y - (depth - 1) * .5f) * 4);
                Spawn(green ? "M_Piso_Musgo_4m" : "M_Piso_4m", structure, p);
                Spawn("M_Teto_4m", structure, p + Vector3.up * 3.25f);
            }
            float hw = width * 2, hd = depth * 2;
            for (int i = 0; i < width; i++)
            {
                float x = (i - (width - 1) * .5f) * 4;
                Spawn(i == width / 2 ? "M_Arco_4m" : green ? "M_Parede_Umida_4m" : "M_Parede_4m", structure, new Vector3(x, 0, -hd));
                Spawn(i == width / 2 && north ? "M_Arco_4m" : green ? "M_Parede_Umida_4m" : "M_Parede_4m", structure, new Vector3(x, 0, hd));
            }
            for (int i = 0; i < depth; i++)
            {
                float pz = (i - (depth - 1) * .5f) * 4;
                Spawn(green ? "M_Parede_Umida_4m" : "M_Parede_4m", structure, new Vector3(-hw, 0, pz), 90);
                Spawn(green ? "M_Parede_Umida_4m" : "M_Parede_4m", structure, new Vector3(hw, 0, pz), 90);
            }
            var decor = Group("Decoracao", room);
            Torch(decor, -hw + .22f, -2, green);
            Torch(decor, hw - .22f, 2, green);
            if (width >= 3)
            {
                Torch(decor, -hw + .22f, 3.7f, green);
                Torch(decor, hw - .22f, -3.7f, green);
            }
            return room;
        }

        static void Corridor(Transform parent, string name, float z, bool green)
        {
            var corridor = Group(name, parent);
            corridor.localPosition = new Vector3(0, 0, z);
            Spawn(green ? "M_Piso_Musgo_4m" : "M_Piso_4m", corridor, Vector3.zero);
            Spawn("M_Teto_4m", corridor, Vector3.up * 3.25f);
            Spawn(green ? "M_Parede_Umida_4m" : "M_Parede_4m", corridor, new Vector3(-2, 0, 0), 90);
            Spawn(green ? "M_Parede_Umida_4m" : "M_Parede_4m", corridor, new Vector3(2, 0, 0), 90);
        }

        static void Sign(Transform parent, string name, string words, Vector3 local, float yaw = 0)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            go.transform.localScale = Vector3.one * .0035f;
            go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 200);
            var label = new GameObject("Texto", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(go.transform, false);
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = label.GetComponent<Text>();
            text.font = Resources.Load<SornyMenuTheme>("SornyMenuTheme").fonte;
            text.fontSize = 27; text.alignment = TextAnchor.MiddleCenter; text.color = new Color(1, .82f, .38f); text.raycastTarget = false; text.text = words;
            var shadow = label.AddComponent<Shadow>(); shadow.effectDistance = Vector2.one * 2; shadow.effectColor = Color.black;
        }

        static Transform Gate(Transform parent, string name, float z)
        {
            var gate = Spawn("D04_Portao_Definitivo", parent, new Vector3(0, 0, z));
            gate.name = name;
            var rs = gate.GetComponentsInChildren<Renderer>();
            Bounds bounds = rs[0].bounds;
            foreach (var renderer in rs) bounds.Encapsulate(renderer.bounds);
            gate.transform.localScale = new Vector3(3.8f / bounds.size.x, 3.55f / bounds.size.y, 1);
            bounds = rs[0].bounds; foreach (var renderer in rs) bounds.Encapsulate(renderer.bounds);
            gate.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, z - bounds.center.z);
            var barrier = gate.AddComponent<BoxCollider>();
            barrier.center = gate.transform.InverseTransformPoint(new Vector3(0, 1.8f, z));
            barrier.size = new Vector3(3.85f / gate.transform.lossyScale.x, 3.6f / gate.transform.lossyScale.y, .4f / gate.transform.lossyScale.z);
            return gate.transform;
        }

        static void Guardian(Transform parent, SornyGameLoop game, Sprite[] frames, string name, Vector3 position, bool heavy)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false); go.transform.position = position;
            var body = go.AddComponent<CharacterController>();
            body.height = heavy ? 2.05f : 1.85f; body.center = Vector3.up * (body.height * .5f + .04f); body.radius = heavy ? .36f : .3f; body.skinWidth = .025f; body.stepOffset = .2f;
            var visualGo = new GameObject("Sprite_Animado"); visualGo.transform.SetParent(go.transform, false);
            var renderer = visualGo.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0]; renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Crack_Pixel.mat"); renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (heavy) visualGo.transform.localScale = Vector3.one * 1.12f;
            var enemy = go.AddComponent<CrackEnemy>();
            enemy.nome = heavy ? "Carrasco do Acervo" : "Guardião do Acervo";
            enemy.nivel = 2; enemy.vidaMaxima = heavy ? 125 : 90; enemy.dano = heavy ? 29 : 24; enemy.velocidade = heavy ? 1.65f : 2.2f; enemy.percepcao = 10;
            enemy.intervaloAtaque = heavy ? 1.05f : .75f; enemy.quadrosCaminhada = 6;
            enemy.corBase = heavy ? new Color(.95f, .77f, .68f) : Color.white;
            enemy.caminhada = frames.Take(2).ToArray(); enemy.ataque = frames.Skip(2).Take(2).ToArray(); enemy.ferido = frames.Skip(4).Take(2).ToArray(); enemy.morte = frames.Skip(6).Take(2).ToArray();
            enemy.visual = renderer; enemy.jogador = game.jogador; enemy.navegacao = game.GetComponent<SornyNavGrid>();
        }

        static void Treasure(SornyGameLoop game)
        {
            var room = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Single(t => t.name == "Sala_04_Cofre_Deluxe");
            foreach (Transform child in room) if (child.name.StartsWith("Decoracao_Seed_")) child.gameObject.SetActive(false);
            var decor = Group("Tesouro_Organizado", room);
            Torch(decor, -5.78f, -3.6f, false); Torch(decor, 5.78f, -3.6f, false);
            Torch(decor, -5.78f, 3.6f, false); Torch(decor, 5.78f, 3.6f, false);
            Sign(decor, "Sala_do_Tesouro", "COFRE DELUXE\nRECOMPENSA DE EXPLORAÇÃO", new Vector3(0, 2, 5.72f));
            Sign(decor, "Arma_do_Tesouro", "MACHADO ROMPE-TAXAS\nABRA O BAÚ  |  EQUIPAR: 6", new Vector3(-3.7f, 1.8f, 3.9f));
            var chests = game.GetComponent<SornyEconomy>().baus;
            Vector3[] places = { new Vector3(-3.7f, .02f, 50), new Vector3(3.7f, .02f, 50), new Vector3(-3.7f, .02f, 46), new Vector3(3.7f, .02f, 46) };
            for (int i = 0; i < chests.Length; i++)
            {
                Undo.RecordObject(chests[i].transform, "Organizar tesouros");
                chests[i].transform.SetPositionAndRotation(places[i], Quaternion.identity);
                chests[i].contemMachado = chests[i].id == "bau_1";
                EditorUtility.SetDirty(chests[i]);
            }
            var guard = UnityEngine.Object.FindObjectsByType<CrackEnemy>(FindObjectsSortMode.None).FirstOrDefault(e => !e.GetComponent<SornyBoss>() && e.transform.position.z > 42 && e.transform.position.z < 54);
            if (guard) { Undo.RecordObject(guard.transform, "Liberar sala do tesouro"); guard.transform.position = new Vector3(-2.7f, .04f, 36.6f); }
            var lightGo = new GameObject("Luz_Tesouro"); lightGo.transform.SetParent(decor, false); lightGo.transform.localPosition = new Vector3(-3.7f, 2.3f, 2);
            var light = lightGo.AddComponent<Light>(); light.color = new Color(1, .7f, .22f); light.intensity = 1.8f; light.range = 5;
            var healing = UnityEngine.Object.FindObjectsByType<SornyPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(p => p.tipo == SornyPickup.Tipo.Cura && p.disponivelNoNivel && p.transform.position.z > 45);
            if (healing) { Undo.RecordObject(healing.transform, "Distribuir cura entre masmorras"); healing.transform.position = new Vector3(3.8f, 0, 115); }
        }

        static void World(SornyGameLoop game, Sprite[] frames)
        {
            var originalRoot = GameObject.Find("Fortaleza_Azul_Demonstracao").transform;
            var root = Group(ExpansionName, originalRoot);
            foreach (var throne in originalRoot.GetComponentsInChildren<Transform>(true).Where(t => t.name == "C22_Trono_SoNao" && t.position.z > 65))
            {
                Undo.RecordObject(throne, "Liberar passagem atrás do trono");
                throne.position = new Vector3(-4.1f, 0, 68.5f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(throne);
            }
            foreach (var seal in originalRoot.GetComponentsInChildren<Transform>(true).Where(t => t.name == "D02_Selo_Assinatura" && t.position.z > 69)) seal.gameObject.SetActive(false);
            var oldWalls = originalRoot.GetComponentsInChildren<Transform>(true).Where(t => t.name == "M_Parede_4m" && Mathf.Abs(t.position.x) < .05f && Mathf.Abs(t.position.z - 70) < .05f).ToArray();
            if (oldWalls.Length == 0) throw new Exception("Parede atrás do trono não localizada.");
            foreach (var wall in oldWalls) wall.gameObject.SetActive(false);
            Spawn("M_Arco_4m", root, new Vector3(0, 0, 70));
            Corridor(root, "Passagem_Apos_Chefao", 72, false);
            var archive = Room(root, "Sala_Midia_Fisica", 3, 3, 80, true, false);
            Spawn("C06_Pedestal_Premium", archive, new Vector3(0, 0, 1));
            for (int side = -1; side <= 1; side += 2)
            {
                Spawn("C09_Mesa", archive, new Vector3(side * 3.8f, 0, -1));
                for (int i = 0; i < 3; i++) Spawn("C11_Livro_Remaster", archive, new Vector3(side * 3.8f + (i - 1) * .3f, .91f, -1), i * 17);
            }
            Sign(archive, "Historia_do_Acervo", "SALA DA MÍDIA FÍSICA\nO ÚLTIMO FRAGMENTO ESTÁ NO SUBSOLO", new Vector3(0, 2.3f, 5.7f));
            var fragment = UnityEngine.Object.FindObjectsByType<SornyPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(p => p.tipo == SornyPickup.Tipo.Fragmento);
            fragment.transform.position = new Vector3(0, 1.3f, 81);
            fragment.titulo = "Fragmento da Mídia Física";
            EditorUtility.SetDirty(fragment);
            Corridor(root, "Descida_Acervo", 88, true);
            var hall = Room(root, "Masmorra2_01_Galeria_dos_Guardioes", 5, 3, 96, true, true);
            var crossing = Room(root, "Masmorra2_02_Deposito_Proibido", 3, 3, 112, true, true);
            var end = Room(root, "Masmorra2_03_Reliquario_Fisico", 5, 3, 128, false, true);
            Corridor(root, "Corredor_Galeria_Deposito", 104, true);
            Corridor(root, "Corredor_Deposito_Reliquario", 120, true);
            Sign(hall, "Entrada_Masmorra2", "ACERVO SUBTERRÂNEO\nMASMORRA II", new Vector3(0, 2.4f, 5.72f));
            for (int side = -1; side <= 1; side += 2)
            {
                Spawn("M_Pilar", hall, new Vector3(side * 5.6f, 0, 1));
                Spawn("D05_Sarcofago_Consumidor", hall, new Vector3(side * 8.1f, 0, 2), side * 90);
                Spawn("C18_Corrente", hall, new Vector3(side * 9.5f, .9f, -3.8f));
                Spawn("C09_Mesa", crossing, new Vector3(side * 3.9f, 0, 1));
                Spawn("C11_Livro_Dividas", crossing, new Vector3(side * 3.9f, .91f, 1));
                Spawn("M_Pilar_Azul", end, new Vector3(side * 6.6f, 0, 1));
                Spawn("C08_Estatua_Executivo", end, new Vector3(side * 7, 0, 3.5f));
            }
            Spawn("C06_Pedestal_Premium", end, new Vector3(0, 0, 3));
            Sign(end, "Fim_do_Acervo", "A ÚLTIMA CÓPIA\nDERROTE OS GUARDIÕES E RECUPERE-A", new Vector3(0, 2.3f, 5.7f));
            var relic = new GameObject("Ultima_Midia_Fisica"); relic.transform.SetParent(root, false); relic.transform.position = new Vector3(0, 1.3f, 131);
            var final = relic.AddComponent<SornyPickup>(); final.tipo = SornyPickup.Tipo.MidiaFinal; final.titulo = "Recuperar a Última Mídia Física";
            var token = Spawn("C16_Token_Premium", relic.transform, Vector3.up * .3f); token.transform.localScale = Vector3.one * 1.6f;
            foreach (var collider in token.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            final.visual = token.transform;
            var enemies = Group("Guardioes_do_Acervo", game.transform);
            Vector3[] places = { new Vector3(-3.5f,.04f,94), new Vector3(4,.04f,96.5f), new Vector3(0,.04f,100), new Vector3(-2.5f,.04f,110), new Vector3(2.7f,.04f,114), new Vector3(-3.6f,.04f,127), new Vector3(3.8f,.04f,130) };
            for (int i = 0; i < places.Length; i++) Guardian(enemies, game, frames, "Guardiao_Acervo_" + (i + 1), places[i], i == 2 || i == 6);
            var progression = game.GetComponent<SornyProgression>() ?? game.gameObject.AddComponent<SornyProgression>();
            progression.boss = game.GetComponent<SornyEconomy>().boss;
            progression.portaAcervo = Gate(root, "Porta_Midia_Fisica", 70);
            progression.portaMasmorra2 = Gate(root, "Porta_Acervo_Subterraneo", 86);
            progression.origemAcervo = progression.portaAcervo.localPosition;
            progression.origemMasmorra2 = progression.portaMasmorra2.localPosition;
            EditorUtility.SetDirty(progression);
            Treasure(game);
            var grid = game.GetComponent<SornyNavGrid>(); grid.linhas = 287;
            var arena = UnityEngine.Object.FindFirstObjectByType<SornyBossArena>();
            var gates = new[] { progression.portaAcervo.gameObject, progression.portaMasmorra2.gameObject, arena.grade.gameObject };
            foreach (var gate in gates) gate.SetActive(false);
            try { grid.Bake(); } finally { foreach (var gate in gates) gate.SetActive(true); }
            EditorUtility.SetDirty(grid);
        }

        [MenuItem("Dungeon & DLC/16 - Tesouro, machado e segunda masmorra")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath) throw new InvalidOperationException("Abra Fortaleza_Azul_Jogavel fora do Play.");
            var game = UnityEngine.Object.FindFirstObjectByType<SornyGameLoop>();
            if (!game || !game.espada.GetComponent<SornyHandEquipment>()) throw new Exception("Jogador ou equipamentos ausentes.");
            string backup = Root + "/Reports/BackupExpansao/" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Directory.CreateDirectory(backup); EditorSceneManager.SaveScene(scene, backup + "/Fortaleza_Azul_Jogavel.unity", true);
            var texture = Texture(Root + "/Art/Armas/Machado_Rompe_Taxas.png");
            var hands = game.espada.GetComponent<SornyHandEquipment>();
            hands.machado = new[] { SaveSprite(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(1, 0), 100, "Machado_Rompe_Taxas") };
            EditorUtility.SetDirty(hands);
            BindAudio(game.GetComponent<SornyAudioDirector>());
            var frames = GuardianFrames();
            if (!GameObject.Find(ExpansionName)) World(game, frames);
            var hint = game.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name == "Controles");
            if (hint) { hint.text = "WASD MOVER | ESQ ATACA | DIR DEFENDE | 1 ESPADA / 2 ADAGA / 6 MACHADO | 3 ESCUDO / 4 ADAGA / 5 VAZIA | E USAR"; hint.rectTransform.sizeDelta = new Vector2(1180, 24); hint.fontSize = 12; EditorUtility.SetDirty(hint); }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            File.WriteAllText(Root + "/Reports/EXPANSAO_ACERVO.txt", "Sala 04 convertida em tesouro; mesmos quatro baús e IDs, um entrega o Machado Rompe-Taxas. Tecla 6, 42 dano, contato 0,27s e duração 0,82s.\nNovo áudio: rasgo de lâmina, três gemidos sintetizados do jogador e bloqueio de madeira/metal com tremor.\nChefão libera a chave e a passagem atrás do trono. Fragmento do acervo abre a Masmorra II, com três salas, sete guardiões e objetivo final.\nMesma cena, jogador, HUD, equipamentos e arquivo de salvamento. Grupos novos anexados aos existentes.\nBackup: " + backup);
            Debug.Log("SORNY_EXPANSION_APPLIED_OK");
        }

        public static void BatchApply() { EditorSceneManager.OpenScene(ScenePath); Apply(); }
    }
}
