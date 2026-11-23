using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Linq;
using DungeonDLC.Sorny;

namespace Caloteus.Editor
{
    public static class InstalarEspadaFluida
    {
        [MenuItem("Caloteus/Criar espada fluida na cena")]
        public static void Criar()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play antes de instalar a espada."); return; }
            const string path = "Assets/CaloteusEspada/Espada_Acesso_Antecipado.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError("Copie a pasta CaloteusEspada completa para Assets."); return; }
            var existente = GameObject.Find("Caloteus_ArmaHUD");
            if (existente != null) { Atualizar(); return; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            var canvasGO = new GameObject("Caloteus_ArmaHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            Undo.RegisterCreatedObjectUndo(canvasGO, "Criar espada fluida");
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var go = new GameObject("EspadaVisual", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvasGO.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.64f, 0.28f);
            rt.sizeDelta = new Vector2(520f, 693.3333f);
            rt.anchoredPosition = new Vector2(-205f, 120f);
            var img = go.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            img.preserveAspect = true;
            img.raycastTarget = false;
            go.AddComponent<EspadaFluida>();
            Atualizar();
            Selection.activeGameObject = go;
            EditorSceneManager.MarkSceneDirty(canvasGO.scene);
            Debug.Log("Espada criada. Salve a cena, aperte Play e clique na aba Game. Desative a imagem da espada antiga se houver.");
        }

        [MenuItem("Caloteus/Atualizar espada - posicao e arco de corte")]
        [MenuItem("Caloteus/Ativar espada seguindo o mouse")]
        public static void Atualizar()
        {
            if (Application.isPlaying) { Debug.LogWarning("Saia do Play antes de atualizar."); return; }
            var hud = GameObject.Find("Caloteus_ArmaHUD");
            if (hud == null) { Criar(); return; }
            var espada = hud.transform.Find("EspadaVisual")?.GetComponent<EspadaFluida>();
            if (espada == null) { Debug.LogError("EspadaVisual nao encontrada dentro do HUD."); return; }
            var rt = espada.GetComponent<RectTransform>();
            AjustarEnquadramento(espada);
            var arco = hud.GetComponentInChildren<ArcoCorte>(true);
            if (arco == null)
            {
                var go = new GameObject("ArcoDeCorte", typeof(RectTransform), typeof(CanvasRenderer), typeof(ArcoCorte));
                Undo.RegisterCreatedObjectUndo(go, "Criar arco de corte");
                go.transform.SetParent(hud.transform, false);
                arco = go.GetComponent<ArcoCorte>();
            }
            Undo.RecordObject(arco, "Conectar arco");
            arco.espada = espada;
            arco.raycastTarget = false;
            var ar = arco.rectTransform;
            Undo.RecordObject(ar, "Posicionar arco");
            ar.anchorMin = ar.anchorMax = new Vector2(0.52f, 0.48f);
            ar.pivot = new Vector2(0.5f, 0.5f);
            ar.anchoredPosition = Vector2.zero;
            ar.sizeDelta = new Vector2(800f, 600f);
            ar.SetSiblingIndex(Mathf.Max(0, rt.GetSiblingIndex()));
            EditorUtility.SetDirty(espada);
            EditorUtility.SetDirty(arco);
            Selection.activeGameObject = espada.gameObject;
            EditorSceneManager.MarkSceneDirty(hud.scene);
            Debug.Log("Apoio do braço ajustado na borda inferior e arco conectado. Salve a cena e teste no Play.");
        }

        public static void AjustarEnquadramento(EspadaFluida espada)
        {
            var rect = espada.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Ajustar apoio do braço");
            Undo.RecordObject(espada, "Ajustar movimento da espada");
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            var sprite = espada.GetComponent<Image>().sprite;
            rect.sizeDelta = new Vector2(1000f * sprite.rect.width / sprite.rect.height, 1000f);
            rect.anchoredPosition = new Vector2(24f, -48f);
            rect.localRotation = Quaternion.Euler(0f, 0f, 12f);
            rect.localScale = Vector3.one;
            espada.rastro = false;
            espada.seguirMouse = true;
            espada.alcanceMouse = new Vector2(38f, 16f);
            espada.inclinacaoMouse = 5f;
            espada.retornoMouse = 10f;
            EditorUtility.SetDirty(rect);
            EditorUtility.SetDirty(espada);
        }

        static Sprite[] ImportarQuadros(string nome)
        {
            const string root = "Assets/CaloteusEspada";
            string path = root + "/Art/Armas/" + nome + ".png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Arte ausente: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.isReadable = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var pixels = texture.GetPixels32();
            int width = texture.width / 2;
            int height = texture.height / 2;
            int xMin = width, yMin = height, xMax = -1, yMax = -1;
            for (int frame = 0; frame < 4; frame++)
            {
                int left = frame % 2 * width;
                int bottom = (1 - frame / 2) * height;
                int count = 0;
                for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (pixels[(bottom + y) * texture.width + left + x].a < 180) continue;
                    xMin = Mathf.Min(xMin, x); yMin = Mathf.Min(yMin, y);
                    xMax = Mathf.Max(xMax, x); yMax = Mathf.Max(yMax, y);
                    count++;
                }
                if (count < 100) throw new InvalidOperationException("Quadro vazio em " + nome + " " + frame);
            }
            var frames = new Sprite[4];
            Directory.CreateDirectory(root + "/Sprites/Armas");
            AssetDatabase.Refresh();
            for (int frame = 0; frame < 4; frame++)
            {
                var rect = new Rect(frame % 2 * width + xMin, (1 - frame / 2) * height + yMin, xMax - xMin + 1, yMax - yMin + 1);
                var sprite = Sprite.Create(texture, rect, new Vector2(1, 0), 100, 0, SpriteMeshType.FullRect);
                sprite.name = nome + "_" + frame;
                string assetPath = root + "/Sprites/Armas/" + sprite.name + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (existing)
                {
                    EditorUtility.CopySerialized(sprite, existing);
                    UnityEngine.Object.DestroyImmediate(sprite);
                    frames[frame] = existing;
                }
                else
                {
                    AssetDatabase.CreateAsset(sprite, assetPath);
                    frames[frame] = sprite;
                }
            }
            return frames;
        }

        static Sprite ImportarEscudoAprovado()
        {
            const string path = "Assets/CaloteusEspada/Art/Armas/Escudo_Verso_Aprovado.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Escudo aprovado ausente: " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        [MenuItem("Caloteus/Atualizar armas e duas maos")]
        public static void AtualizarArmas()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Saia do Play para atualizar as armas.");
            var game = UnityEngine.Object.FindFirstObjectByType<SornyGameLoop>();
            if (!game || game.gameObject.scene.path != "Assets/SornyGameplay/Scenes/Fortaleza_Azul_Jogavel.unity")
                throw new InvalidOperationException("Abra Fortaleza_Azul_Jogavel.");
            string backup = "Assets/SornyGameplay/Reports/BackupArmas/" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
            Directory.CreateDirectory(backup);
            EditorSceneManager.SaveScene(game.gameObject.scene, backup + "/Fortaleza_Azul_Jogavel.unity", true);
            var originalSword = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CaloteusEspada/Espada_Acesso_Antecipado.png");
            if (!originalSword) throw new InvalidOperationException("A espada original não foi encontrada.");
            var swordFrames = new[] { originalSword };
            var daggerFrames = ImportarQuadros("Adaga");
            var shieldFrames = new[] { ImportarEscudoAprovado() };
            var right = game.espada;
            var hud = right.transform.parent;
            var leftTransform = hud.Find("MaoEsquerda");
            if (!leftTransform)
            {
                var go = new GameObject("MaoEsquerda", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, "Adicionar mão esquerda ao HUD existente");
                go.transform.SetParent(hud, false);
                leftTransform = go.transform;
            }
            var leftImage = leftTransform.GetComponent<Image>();
            leftImage.sprite = shieldFrames[0];
            leftImage.preserveAspect = true;
            leftImage.raycastTarget = false;
            var left = leftTransform.GetComponent<EspadaFluida>();
            if (!left) left = leftTransform.gameObject.AddComponent<EspadaFluida>();
            left.cliqueEsquerdo = false;
            left.rastro = false;
            left.espelhar = true;
            left.quadrosAtaque = daggerFrames;
            left.outraMao = right;
            left.enabled = false;
            var leftRect = (RectTransform)leftTransform;
            leftRect.anchorMin = leftRect.anchorMax = leftRect.pivot = Vector2.zero;
            leftRect.anchoredPosition = new Vector2(-125, -210);
            leftRect.sizeDelta = new Vector2(760f * shieldFrames[0].rect.width / shieldFrames[0].rect.height, 760f);
            leftRect.localScale = Vector3.one;
            leftRect.localRotation = Quaternion.Euler(0, 0, 8);
            var leftDamage = left.GetComponent<SornySwordDamage>();
            if (!leftDamage) leftDamage = left.gameObject.AddComponent<SornySwordDamage>();
            leftDamage.dano = 15;
            leftDamage.alcance = 1.9f;
            right.GetComponent<Image>().sprite = swordFrames[0];
            right.quadrosAtaque = swordFrames;
            right.outraMao = left;
            right.duracaoGolpe = .62f;
            right.tempoContato = .205f;
            AjustarEnquadramento(right);
            var hands = right.GetComponent<SornyHandEquipment>();
            if (!hands) hands = right.gameObject.AddComponent<SornyHandEquipment>();
            hands.direita = right;
            hands.esquerda = left;
            hands.espada = swordFrames;
            hands.adaga = daggerFrames;
            hands.escudo = shieldFrames;
            game.jogador.equipamentos = hands;
            var hint = game.GetComponentsInChildren<Text>(true).FirstOrDefault(text => text.name == "Controles");
            if (hint)
            {
                hint.text = "ESQ ATACA | DIR DEFENDE/ATACA | 1/2 MAO DIR | 3/4/5 MAO ESQ | E USAR | ESC PAUSA";
                hint.fontSize = 14;
            }
            EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
            EditorSceneManager.SaveScene(game.gameObject.scene);
            AssetDatabase.SaveAssets();
            File.WriteAllText("Assets/SornyGameplay/Reports/ARMAS_INICIAIS.txt", "ATIVO: espada original, escudo com o verso aprovado e adaga de quatro quadros.\nEspada e escudo animados por movimento, sem reutilizar suas artes rejeitadas.\nBraço direito ampliado de 800 para 1000 unidades de altura.\nNovo filho no HUD existente: MaoEsquerda.\n1 espada, 2 adaga direita, 3 escudo, 4 adaga esquerda, 5 mão esquerda vazia.\nClique esquerdo: ataque direito. Clique direito: defesa ou ataque esquerdo.\nEspada: 25/40 dano, alcance 2.6, duração 0.62s. Adaga: 15 dano, alcance 1.9, duração 0.40s.\nEscudo reduz 60% do dano frontal antes da armadura; impacto em área do boss exige desvio.\nBackup: " + backup);
            Debug.Log("CALOTEUS_WEAPONS_APPLIED_OK");
        }

        public static void BatchWeapons()
        {
            EditorSceneManager.OpenScene("Assets/SornyGameplay/Scenes/Fortaleza_Azul_Jogavel.unity");
            AtualizarArmas();
        }
    }
}
