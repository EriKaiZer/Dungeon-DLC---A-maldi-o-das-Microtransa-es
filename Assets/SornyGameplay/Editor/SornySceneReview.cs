using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Caloteus;

namespace DungeonDLC.Sorny.Editor
{
    [InitializeOnLoad]
    public static class SornySceneReview
    {
        const string ScenePath = "Assets/SornyGameplay/Scenes/Fortaleza_Azul_Jogavel.unity";
        const string Reports = "Assets/SornyGameplay/Reports";
        const string RequestPath = Reports + "/revisao-inicial.txt";
        static double nextPoll;

        static SornySceneReview()
        {
            EditorApplication.update += ProcessRequest;
        }

        static void ProcessRequest()
        {
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 2;
            if (EditorApplication.isCompiling) return;
            const string balanceRequest = Reports + "/balanceamento-escudo.txt";
            const string audioRequest = Reports + "/audio-combate.txt";
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.isPlaying && SceneManager.GetActiveScene().path == ScenePath
                    && ((File.Exists(balanceRequest) && File.ReadAllText(balanceRequest).Trim() == "pending")
                    || (File.Exists(audioRequest) && File.ReadAllText(audioRequest).Trim() == "pending")))
                    EditorApplication.ExitPlaymode();
                return;
            }
            if (SceneManager.GetActiveScene().path == ScenePath && File.Exists(audioRequest) && File.ReadAllText(audioRequest).Trim() == "pending")
            {
                try
                {
                    AplicarAudioCombate();
                    File.WriteAllText(audioRequest, "done");
                }
                catch (Exception exception)
                {
                    File.WriteAllText(audioRequest, "error: " + exception);
                    Debug.LogException(exception);
                }
            }
            if (SceneManager.GetActiveScene().path == ScenePath && File.Exists(balanceRequest) && File.ReadAllText(balanceRequest).Trim() == "pending")
            {
                try
                {
                    AplicarBalanceamento();
                    File.WriteAllText(balanceRequest, "done");
                }
                catch (Exception exception)
                {
                    File.WriteAllText(balanceRequest, "error: " + exception);
                    Debug.LogException(exception);
                }
            }
            const string impactRequest = Reports + "/impacto-combate.txt";
            if (SceneManager.GetActiveScene().path == ScenePath && File.Exists(impactRequest) && File.ReadAllText(impactRequest).Trim() == "pending")
            {
                try
                {
                    AplicarImpactoCombate();
                    File.WriteAllText(impactRequest, "done");
                }
                catch (Exception exception)
                {
                    File.WriteAllText(impactRequest, "error: " + exception);
                    Debug.LogException(exception);
                }
            }
            const string weaponRequest = Reports + "/armas-inicial.txt";
            if (SceneManager.GetActiveScene().path == ScenePath && File.Exists(weaponRequest) && File.ReadAllText(weaponRequest).Trim() == "pending")
            {
                try
                {
                    Caloteus.Editor.InstalarEspadaFluida.AtualizarArmas();
                    File.WriteAllText(weaponRequest, "done");
                }
                catch (Exception exception)
                {
                    File.WriteAllText(weaponRequest, "error: " + exception);
                    Debug.LogException(exception);
                }
            }
            if (!File.Exists(RequestPath) || File.ReadAllText(RequestPath).Trim() != "pending") return;
            if (SceneManager.GetActiveScene().path != ScenePath) return;

            try
            {
                Apply();
                File.WriteAllText(RequestPath, "done");
            }
            catch (Exception exception)
            {
                File.WriteAllText(RequestPath, "error: " + exception);
                Debug.LogException(exception);
            }
        }

        static string ObjectPath(Transform target)
        {
            string path = target.name;
            while (target.parent)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }
            return path;
        }

        [MenuItem("Dungeon & DLC/15 - Atualizar trilhas e impacto sonoro")]
        public static void AplicarAudioCombate()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath)
                throw new InvalidOperationException("Abra Fortaleza_Azul_Jogavel e saia do Play.");
            const string root = "Assets/SornyGameplay/Audio/CombateV2/";
            foreach (string path in Directory.GetFiles(root, "*.wav"))
            {
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (!importer) throw new InvalidOperationException("Áudio não importado: " + path);
                bool music = path.Contains("Exploracao_") || path.Contains("Chefe_Volatile_");
                var settings = importer.defaultSampleSettings;
                settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
                settings.quality = music ? .8f : 1;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = false;
                importer.loadInBackground = music;
                importer.SaveAndReimport();
            }
            var game = UnityEngine.Object.FindFirstObjectByType<SornyGameLoop>();
            var audio = game ? game.GetComponent<SornyAudioDirector>() : null;
            var arena = UnityEngine.Object.FindFirstObjectByType<SornyBossArena>();
            if (!audio || !arena) throw new InvalidOperationException("Áudio ou arena ausentes.");
            AudioClip Clip(string name)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(root + name + ".wav");
                if (!clip) throw new InvalidOperationException("Clipe ausente: " + name);
                return clip;
            }
            AudioClip[] Variacoes(string prefix, int count) => Enumerable.Range(0, count).Select(i => Clip(prefix + i)).ToArray();
            string[] before = Hierarchy(scene);
            string backup = Reports + "/BackupAudioCombate/" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Directory.CreateDirectory(backup);
            EditorSceneManager.SaveScene(scene, backup + "/Fortaleza_Azul_Jogavel.unity", true);
            Undo.RecordObjects(new UnityEngine.Object[] { audio, arena }, "Atualizar músicas e impactos");
            audio.trilha = Clip("Exploracao_Heroic_Age");
            audio.ganhoExploracao = 1.2f;
            audio.acertosEspada = Variacoes("Espada_Acerto_", 4);
            audio.acertosAdaga = Variacoes("Adaga_Acerto_", 3);
            audio.bloqueiosEscudo = Variacoes("Escudo_Bloqueio_", 3);
            audio.cortesEspada = Variacoes("Espada_Ar_", 2);
            audio.cortesAdaga = Variacoes("Adaga_Ar_", 2);
            audio.danoRecebido = Clip("Jogador_Dano");
            audio.impacto = audio.acertosEspada[0];
            audio.espadaAr = audio.cortesEspada[0];
            arena.batalha = Clip("Chefe_Volatile_Reaction");
            arena.entradaChefe = Clip("Chefe_Entrada");
            arena.ganhoBatalha = 1.35f;
            if (!before.SequenceEqual(Hierarchy(scene))) throw new InvalidOperationException("A hierarquia foi alterada.");
            EditorUtility.SetDirty(audio);
            EditorUtility.SetDirty(arena);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Reports + "/AUDIO_COMBATE.txt", "Exploração: Heroic Age — Kevin MacLeod.\nChefe: Volatile Reaction — Kevin MacLeod, com entrada de impacto e transição de 0,7s.\nMenu mantém Darkest Child. Créditos no menu e F10.\n14 variações de combate, dano recebido e entrada do chefe. Impacto também nos golpes fatais.\nA música abre espaço por um instante nos acertos, sem alterar os volumes salvos.\nParada da arma: espada 60 ms, adaga 35 ms. Impulso curto da câmera, sem alterar a mira.\nSem criar ou remover objetos na hierarquia.\nBackup: " + backup);
            Debug.Log("SORNY_AUDIO_COMBAT_APPLIED_OK");
        }

        public static void BatchAudioCombate()
        {
            EditorSceneManager.OpenScene(ScenePath);
            AplicarAudioCombate();
        }

        [MenuItem("Dungeon & DLC/14 - Ajustar guarda, curas e dificuldade")]
        public static void AplicarBalanceamento()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath)
                throw new InvalidOperationException("Abra Fortaleza_Azul_Jogavel e saia do Play.");
            var game = UnityEngine.Object.FindFirstObjectByType<SornyGameLoop>();
            var hands = game ? game.espada.GetComponent<SornyHandEquipment>() : null;
            var chest = UnityEngine.Object.FindObjectsByType<SornyChest>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(c => c.id == "bau_1");
            var curas = UnityEngine.Object.FindObjectsByType<SornyPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(p => p.tipo == SornyPickup.Tipo.Cura).ToArray();
            var boss = UnityEngine.Object.FindFirstObjectByType<SornyBoss>();
            if (!game || !hands || !hands.esquerda || curas.Length < 2 || !boss)
                throw new InvalidOperationException("Jogador, duas mãos, curas ou chefe ausentes.");
            string[] before = Hierarchy(scene);
            string backup = Reports + "/BackupBalanceamento/" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Directory.CreateDirectory(backup);
            EditorSceneManager.SaveScene(scene, backup + "/Fortaleza_Azul_Jogavel.unity", true);
            const string enemyPrefabPath = "Assets/SornyGameplay/Prefabs/Crack_Bandido_Culto.prefab";
            if (File.Exists(enemyPrefabPath)) File.Copy(enemyPrefabPath, backup + "/Crack_Bandido_Culto.prefab.txt", true);

            // O baú interativo ocupa o lugar do baú decorativo na sala do cofre.
            foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name == "C07_Bau_Deluxe" && !t.GetComponentInParent<SornyChest>()
                    && Vector3.Distance(t.position, new Vector3(-4, 0, 50)) < 1.5f)
                {
                    Undo.RecordObject(t.gameObject, "Substituir baú decorativo");
                    t.gameObject.SetActive(false);
                }
            Undo.RecordObject(chest.transform, "Mover recompensa para o cofre");
            chest.transform.SetPositionAndRotation(new Vector3(-3.9f, .02f, 50.2f), Quaternion.Euler(0, -90, 0));
            var meio = curas.OrderBy(p => Mathf.Abs(p.transform.position.z - 29)).First();
            var final = curas.Where(p => p != meio).OrderBy(p => Mathf.Abs(p.transform.position.z - 51)).First();
            foreach (var cura in curas)
            {
                Undo.RecordObjects(new UnityEngine.Object[] { cura, cura.gameObject }, "Distribuir curas do nível");
                cura.disponivelNoNivel = cura == meio || cura == final;
                cura.valor = 25;
                cura.RestaurarDisponibilidade(cura.disponivelNoNivel);
                EditorUtility.SetDirty(cura);
            }
            foreach (var enemy in UnityEngine.Object.FindObjectsByType<CrackEnemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Undo.RecordObject(enemy, "Ajustar dano dos inimigos");
                enemy.dano = enemy.GetComponent<SornyBoss>() ? 28 : 18;
                EditorUtility.SetDirty(enemy);
            }
            Undo.RecordObject(boss, "Ajustar dano do chefe");
            boss.danoNormal = 28;
            boss.danoFuria = 34;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(enemyPrefabPath);
            if (prefab && prefab.GetComponent<CrackEnemy>())
            {
                var enemy = prefab.GetComponent<CrackEnemy>();
                Undo.RecordObject(enemy, "Ajustar inimigo reutilizável");
                enemy.dano = 18;
                EditorUtility.SetDirty(enemy);
                PrefabUtility.SavePrefabAsset(prefab);
            }
            var shieldRect = hands.esquerda.GetComponent<RectTransform>();
            Undo.RecordObject(shieldRect, "Apoiar guarda diante do tórax");
            shieldRect.anchoredPosition = new Vector2(-140, -250);
            var grid = game.GetComponent<SornyNavGrid>();
            var arena = UnityEngine.Object.FindFirstObjectByType<SornyBossArena>();
            bool gateActive = arena && arena.grade && arena.grade.gameObject.activeSelf;
            try
            {
                if (arena && arena.grade) arena.grade.gameObject.SetActive(false);
                Physics.SyncTransforms();
                if (grid) grid.Bake();
            }
            finally
            {
                if (arena && arena.grade) arena.grade.gameObject.SetActive(gateActive);
            }
            if (!before.SequenceEqual(Hierarchy(scene))) throw new InvalidOperationException("A hierarquia foi alterada.");
            EditorUtility.SetDirty(boss);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Reports + "/BALANCEAMENTO_ESCUDO.txt", "Guarda centralizada e mais baixa, diante do tórax.\nBaú bau_1 movido da recepção para a sala do cofre, sem alterar ID ou recompensa.\nDuas curas de 25 PV, perto de z=29 e z=51. Entrada e arena sem cura.\nDano comum: 18 (era 12). Chefe: 28 / 34 (era 20 / 25).\nBloqueio frontal reduz 60%: golpe comum causa 8 com escudo, 6 com escudo e armadura.\nSalvamentos antigos e reinício respeitam a distribuição nova de curas.\nMesmos objetos, nomes e pais na Hierarchy.\nBackup: " + backup);
            Debug.Log("SORNY_BALANCE_APPLIED_OK");
        }

        public static void BatchBalanceamento()
        {
            EditorSceneManager.OpenScene(ScenePath);
            AplicarBalanceamento();
        }

        [MenuItem("Dungeon & DLC/13 - Atualizar impacto dos golpes e cobertura do escudo")]
        public static void AplicarImpactoCombate()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath)
                throw new InvalidOperationException("Abra Fortaleza_Azul_Jogavel e saia do Play.");
            var game = UnityEngine.Object.FindFirstObjectByType<SornyGameLoop>();
            var hands = game ? game.espada.GetComponent<SornyHandEquipment>() : null;
            if (!hands || !hands.esquerda || hands.escudo.Length == 0)
                throw new InvalidOperationException("As duas mãos e o escudo aprovado precisam estar instalados.");
            string[] before = Hierarchy(scene);
            string backup = Reports + "/BackupImpactoCombate/" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Directory.CreateDirectory(backup);
            EditorSceneManager.SaveScene(scene, backup + "/Fortaleza_Azul_Jogavel.unity", true);
            Undo.RecordObjects(new UnityEngine.Object[] { hands.direita, hands.esquerda, game.movimento }, "Ajustar impacto do combate");
            hands.direita.duracaoGolpe = .62f;
            hands.direita.tempoContato = .205f;
            hands.direita.golpeCurto = false;
            hands.direita.rastro = false;
            hands.esquerda.golpeCurto = true;
            hands.esquerda.rastro = false;
            game.movimento.forcaImpacto = 1;
            var rect = hands.esquerda.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Aumentar cobertura do escudo");
            rect.sizeDelta = new Vector2(1080 * hands.escudo[0].rect.width / hands.escudo[0].rect.height, 1080);
            rect.anchoredPosition = new Vector2(-140, -230);
            foreach (var arco in hands.direita.GetComponentInParent<Canvas>().GetComponentsInChildren<ArcoCorte>(true))
            {
                Undo.RecordObject(arco, "Conectar corte sincronizado");
                arco.espada = hands.direita;
                arco.enabled = true;
                EditorUtility.SetDirty(arco);
            }
            if (!before.SequenceEqual(Hierarchy(scene))) throw new InvalidOperationException("A hierarquia foi alterada.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Reports + "/IMPACTO_COMBATE.txt", "Golpe: preparação, corte acelerado e recuperação.\nLâmina, rastro, contato e som seguem o mesmo tempo.\nAcerto: parada local da arma de 45 ms (espada) ou 28 ms (adaga), reação do inimigo, som imediato e impulso curto da câmera.\nEscudo: altura de referência 1080; cobertura até 98% da altura e limite de 60% da largura ao defender.\nNenhum objeto criado, removido ou reordenado na hierarquia.\nBackup: " + backup);
            Debug.Log("SORNY_COMBAT_IMPACT_APPLIED_OK");
        }

        public static void BatchImpactoCombate()
        {
            EditorSceneManager.OpenScene(ScenePath);
            AplicarImpactoCombate();
        }

        static string[] Hierarchy(Scene scene)
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Select(ObjectPath)
                .OrderBy(path => path)
                .ToArray();
        }

        [MenuItem("Dungeon & DLC/12 - Revisar baus e enquadramento do braco")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath)
                throw new InvalidOperationException("Abra Fortaleza_Azul_Jogavel e saia do Play.");

            var game = UnityEngine.Object.FindFirstObjectByType<SornyGameLoop>();
            var sword = UnityEngine.Object.FindFirstObjectByType<EspadaFluida>();
            var chests = UnityEngine.Object.FindObjectsByType<SornyChest>(FindObjectsSortMode.None);
            string[] ids = { "bau_1", "bau_2", "bau_3", "bau_4" };
            if (!game || !sword || ids.Any(id => chests.Count(chest => chest.id == id) != 1))
                throw new InvalidOperationException("Jogador, espada ou os quatro baús originais não foram identificados.");

            string[] before = Hierarchy(scene);
            string backup = Reports + "/BackupRevisaoInicial/" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Directory.CreateDirectory(backup);
            EditorSceneManager.SaveScene(scene, backup + "/Fortaleza_Azul_Jogavel.unity", true);
            var report = new StringBuilder();
            report.AppendLine("REVISÃO INICIAL DA FORTALEZA AZUL");
            report.AppendLine("Backup da cena em " + backup);

            Vector3[] positions =
            {
                new Vector3(3.5f, .02f, -1f),
                new Vector3(4.4f, .02f, 14f),
                new Vector3(-4.3f, .02f, 30f),
                new Vector3(4.4f, .02f, 47.3f)
            };
            float[] rotations = { 90f, 90f, -90f, 90f };
            for (int i = 0; i < ids.Length; i++)
            {
                var chest = chests.Single(item => item.id == ids[i]);
                report.AppendLine(ids[i] + " de " + chest.transform.position + " para " + positions[i]);
                Undo.RecordObject(chest.transform, "Liberar espaço ao redor do baú");
                chest.transform.SetPositionAndRotation(positions[i], Quaternion.Euler(0f, rotations[i], 0f));
                EditorUtility.SetDirty(chest.transform);
            }

            Caloteus.Editor.InstalarEspadaFluida.AjustarEnquadramento(sword);
            Physics.SyncTransforms();
            var grid = game.GetComponent<SornyNavGrid>();
            var arena = UnityEngine.Object.FindFirstObjectByType<SornyBossArena>();
            // The moving gate must not become a permanent wall in the navigation grid.
            bool gateActive = arena && arena.grade && arena.grade.gameObject.activeSelf;
            try
            {
                if (arena && arena.grade) arena.grade.gameObject.SetActive(false);
                if (grid) grid.Bake();
            }
            finally
            {
                if (arena && arena.grade) arena.grade.gameObject.SetActive(gateActive);
            }

            if (!before.SequenceEqual(Hierarchy(scene)))
                throw new InvalidOperationException("A revisão alterou a organização dos objetos. Restaure o backup antes de salvar.");

            report.AppendLine("Mesmos objetos e mesmos caminhos na Hierarchy, incluindo baús e seus identificadores.");
            report.AppendLine("Braço com apoio na borda inferior, deslocamento menor e retorno suave após o mouse parar.");
            report.AppendLine("Golpe mantém o contato em 0,205 segundo e duração de 0,62 segundo.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText(Reports + "/REVISAO_INICIAL.txt", report.ToString());
            Debug.Log("FORTALEZA_REVIEW_APPLIED_OK");
        }

        public static void BatchApply()
        {
            EditorSceneManager.OpenScene(ScenePath);
            Apply();
        }
    }
}
