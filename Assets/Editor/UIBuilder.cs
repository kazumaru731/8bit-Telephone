#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KanjiFlipGame.UI;
using KanjiFlipGame.Kanji;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

public static class UIBuilder
{
    [MenuItem("Tools/Build and Fix UI")]
    public static string Build()
    {
        var canvas = GameObject.Find("MainCanvas");
        if (canvas == null) return "MainCanvas not found";

        // KanjiElement.prefab のフォント設定自動修正
        var prefabPath = "Assets/Prefabs/KanjiElement.prefab";
        var prefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabGo != null)
        {
            var textMeshPro = prefabGo.GetComponentInChildren<TextMeshProUGUI>(true);
            var tmFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/NotoSansJP_Kanji SDF.asset");
            if (textMeshPro != null && tmFont != null)
            {
                textMeshPro.font = tmFont;
                textMeshPro.color = Color.black; // 文字色を確実に黒に設定（背景と同化するのを防ぐ）
                EditorUtility.SetDirty(textMeshPro);
                EditorUtility.SetDirty(prefabGo);
                PrefabUtility.SavePrefabAsset(prefabGo);
                Debug.Log("Updated KanjiElement prefab font to NotoSansJP_Kanji SDF and color to Black.");
            }
        }

        // GameManagerにNetworkObjectがアタッチされているか確認し、無ければアタッチする
        var gameManagerGo = GameObject.Find("GameManager");
        if (gameManagerGo != null)
        {
            var no = gameManagerGo.GetComponent<Fusion.NetworkObject>();
            if (no == null)
            {
                gameManagerGo.AddComponent<Fusion.NetworkObject>();
                Debug.Log("NetworkObject component added to GameManager.");
            }
        }

        // AutoTester コンポーネントのアタッチ
        var autoTester = canvas.GetComponent<KanjiFlipGame.Core.AutoTester>();
        if (autoTester == null)
        {
            canvas.AddComponent<KanjiFlipGame.Core.AutoTester>();
            Debug.Log("AutoTester component added to MainCanvas.");
        }

        // --- MainMenuUI の移動処理 ---
        var mainMenuPanelGo = canvas.transform.Find("MainMenuPanel")?.gameObject;
        if (mainMenuPanelGo != null)
        {
            var oldMenu = mainMenuPanelGo.GetComponent<MainMenuUI>();
            if (oldMenu != null)
            {
                // すでに Canvas に MainMenuUI があれば削除しておく
                var existingMenu = canvas.GetComponent<MainMenuUI>();
                if (existingMenu != null) GameObject.DestroyImmediate(existingMenu);

                var newMenu = canvas.AddComponent<MainMenuUI>();
                EditorUtility.CopySerialized(oldMenu, newMenu);
                
                // 新しいMainMenuUIの_mainMenuPanelに元のMainMenuPanelを設定する
                var serializedMenu = new SerializedObject(newMenu);
                serializedMenu.FindProperty("_mainMenuPanel").objectReferenceValue = mainMenuPanelGo;
                serializedMenu.ApplyModifiedProperties();

                GameObject.DestroyImmediate(oldMenu);
                Debug.Log("MainMenuUI moved to MainCanvas and _mainMenuPanel assigned.");
            }
        }

        // --- QuestionerUI の移動処理と参照設定 ---
        var qPanelGo = canvas.transform.Find("QuestionerPanel")?.gameObject;
        if (qPanelGo != null)
        {
            var qUI = canvas.GetComponent<QuestionerUI>();
            
            // もし qPanelGo の方にまだ QuestionerUI が残っていれば移動する
            var oldQ = qPanelGo.GetComponent<QuestionerUI>();
            if (oldQ != null)
            {
                if (qUI != null) GameObject.DestroyImmediate(qUI);
                qUI = canvas.AddComponent<QuestionerUI>();
                EditorUtility.CopySerialized(oldQ, qUI);
                GameObject.DestroyImmediate(oldQ);
                Debug.Log("QuestionerUI moved from QuestionerPanel to MainCanvas.");
            }
            else if (qUI == null)
            {
                qUI = canvas.AddComponent<QuestionerUI>();
                Debug.Log("QuestionerUI added to MainCanvas.");
            }

            // --- QuestionerUI の子パネル（WaitingPanel, ResultPanel）の生成・設定 ---
            var qWaitingPanelGo = qPanelGo.transform.Find("WaitingPanel")?.gameObject;
            if (qWaitingPanelGo == null)
            {
                qWaitingPanelGo = new GameObject("WaitingPanel", typeof(RectTransform), typeof(Image));
                qWaitingPanelGo.transform.SetParent(qPanelGo.transform, false);
                var waitRect = qWaitingPanelGo.GetComponent<RectTransform>();
                // Titleの上に配置するためのアンカー設定 (上部、高さ60px)
                waitRect.anchorMin = new Vector2(0f, 0.93f);
                waitRect.anchorMax = new Vector2(1f, 1.0f);
                waitRect.pivot = new Vector2(0.5f, 1f);
                waitRect.sizeDelta = Vector2.zero;
                waitRect.anchoredPosition = Vector2.zero;
                qWaitingPanelGo.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
                
                var qWaitTextGo = new GameObject("WaitingMessageText", typeof(RectTransform), typeof(TextMeshProUGUI));
                qWaitTextGo.transform.SetParent(qWaitingPanelGo.transform, false);
                var qWaitTextRect = qWaitTextGo.GetComponent<RectTransform>();
                qWaitTextRect.anchorMin = Vector2.zero;
                qWaitTextRect.anchorMax = Vector2.one;
                qWaitTextRect.sizeDelta = Vector2.zero;
                var qWaitText = qWaitTextGo.GetComponent<TextMeshProUGUI>();
                qWaitText.text = "待機中...";
                qWaitText.fontSize = 24;
                qWaitText.alignment = TextAlignmentOptions.Center;
                qWaitText.color = Color.yellow; // 目立たせるために黄色にする
            }
            else
            {
                // 既存のオブジェクトも位置を強制的に補正
                var waitRect = qWaitingPanelGo.GetComponent<RectTransform>();
                waitRect.anchorMin = new Vector2(0f, 0.93f);
                waitRect.anchorMax = new Vector2(1f, 1.0f);
                waitRect.pivot = new Vector2(0.5f, 1f);
                waitRect.sizeDelta = Vector2.zero;
                waitRect.anchoredPosition = Vector2.zero;
                var qWaitText = qWaitingPanelGo.GetComponentInChildren<TextMeshProUGUI>();
                if (qWaitText != null)
                {
                    qWaitText.fontSize = 24;
                    qWaitText.color = Color.yellow;
                }
            }

            var qResultPanelGo = qPanelGo.transform.Find("ResultPanel")?.gameObject;
            if (qResultPanelGo == null)
            {
                qResultPanelGo = new GameObject("ResultPanel", typeof(RectTransform), typeof(Image));
                qResultPanelGo.transform.SetParent(qPanelGo.transform, false);
                var resRect = qResultPanelGo.GetComponent<RectTransform>();
                resRect.anchorMin = new Vector2(0.2f, 0.3f);
                resRect.anchorMax = new Vector2(0.8f, 0.7f);
                resRect.sizeDelta = Vector2.zero;
                qResultPanelGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

                var qResTextGo = new GameObject("ResultText", typeof(RectTransform), typeof(TextMeshProUGUI));
                qResTextGo.transform.SetParent(qResultPanelGo.transform, false);
                var qResTextRect = qResTextGo.GetComponent<RectTransform>();
                qResTextRect.anchorMin = Vector2.zero;
                qResTextRect.anchorMax = Vector2.one;
                qResTextRect.sizeDelta = Vector2.zero;
                var qResText = qResTextGo.GetComponent<TextMeshProUGUI>();
                qResText.text = "結果";
                qResText.fontSize = 40;
                qResText.alignment = TextAlignmentOptions.Center;
                qResText.color = Color.white;
            }

            // シリアライズドプロパティの設定
            var serializedQ = new SerializedObject(qUI);
            serializedQ.FindProperty("_questionerPanel").objectReferenceValue = qPanelGo;
            serializedQ.FindProperty("_waitingPanel").objectReferenceValue = qWaitingPanelGo;
            serializedQ.FindProperty("_waitingMessageText").objectReferenceValue = qWaitingPanelGo.transform.Find("WaitingMessageText")?.GetComponent<TextMeshProUGUI>();
            serializedQ.FindProperty("_resultPanel").objectReferenceValue = qResultPanelGo;
            serializedQ.FindProperty("_resultText").objectReferenceValue = qResultPanelGo.transform.Find("ResultText")?.GetComponent<TextMeshProUGUI>();

            // シーン上の関連コンポーネントを再設定
            TextMeshProUGUI topicTextGo = null;
            var topicTextTrans = qPanelGo.transform.Find("TopicDisplayText");
            if (topicTextTrans == null)
            {
                var go = new GameObject("TopicDisplayText", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(qPanelGo.transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f, 0.85f);
                rect.anchorMax = new Vector2(0.9f, 0.95f);
                rect.sizeDelta = Vector2.zero;
                var txt = go.GetComponent<TextMeshProUGUI>();
                txt.text = "お題:";
                txt.fontSize = 32;
                txt.color = Color.white;
                txt.alignment = TextAlignmentOptions.Center;
                topicTextGo = txt;
                Debug.Log("TopicDisplayText automatically created under QuestionerPanel.");
            }
            else
            {
                topicTextGo = topicTextTrans.GetComponent<TextMeshProUGUI>();
            }
            if (topicTextGo != null) serializedQ.FindProperty("_topicText").objectReferenceValue = topicTextGo;

            // --- OperationPanel の作成と取得 ---
            var opPanelGo = qPanelGo.transform.Find("OperationPanel")?.gameObject;
            if (opPanelGo == null)
            {
                opPanelGo = GameObject.Find("OperationPanel");
                if (opPanelGo == null || opPanelGo.transform.parent != qPanelGo.transform)
                {
                    opPanelGo = new GameObject("OperationPanel", typeof(RectTransform));
                    opPanelGo.transform.SetParent(qPanelGo.transform, false);
                }
            }

            var opRect = opPanelGo.GetComponent<RectTransform>();
            opRect.anchorMin = new Vector2(0.02f, 0.02f);
            opRect.anchorMax = new Vector2(0.98f, 0.28f); // 画面下部26%を操作パネル用にする
            opRect.pivot = new Vector2(0.5f, 0f);
            opRect.sizeDelta = Vector2.zero;
            opRect.anchoredPosition = Vector2.zero;

            var hlg = opPanelGo.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = opPanelGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            TMP_InputField qInputFieldGo = null;
            var qInputFieldTrans = qPanelGo.transform.Find("KanjiInputField");
            if (qInputFieldTrans == null && opPanelGo != null)
            {
                qInputFieldTrans = opPanelGo.transform.Find("KanjiInputField");
            }
            if (qInputFieldTrans == null)
            {
                var go = new GameObject("KanjiInputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
                go.transform.SetParent(qPanelGo.transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.2f, 0.5f);
                rect.anchorMax = new Vector2(0.8f, 0.6f);
                rect.sizeDelta = Vector2.zero;
                go.GetComponent<Image>().color = Color.white;

                var textAreaGo = new GameObject("Text Area", typeof(RectTransform));
                textAreaGo.transform.SetParent(go.transform, false);
                var taRect = textAreaGo.GetComponent<RectTransform>();
                taRect.anchorMin = Vector2.zero;
                taRect.anchorMax = Vector2.one;
                taRect.sizeDelta = new Vector2(-10, -10);

                var textComponentGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textComponentGo.transform.SetParent(textAreaGo.transform, false);
                var tcRect = textComponentGo.GetComponent<RectTransform>();
                tcRect.anchorMin = Vector2.zero;
                tcRect.anchorMax = Vector2.one;
                tcRect.sizeDelta = Vector2.zero;
                var tcText = textComponentGo.GetComponent<TextMeshProUGUI>();
                tcText.color = Color.black;
                tcText.fontSize = 24;

                var placeholderComponentGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
                placeholderComponentGo.transform.SetParent(textAreaGo.transform, false);
                var pcRect = placeholderComponentGo.GetComponent<RectTransform>();
                pcRect.anchorMin = Vector2.zero;
                pcRect.anchorMax = Vector2.one;
                pcRect.sizeDelta = Vector2.zero;
                var pcText = placeholderComponentGo.GetComponent<TextMeshProUGUI>();
                pcText.text = "漢字を入力...";
                pcText.color = Color.gray;
                pcText.fontSize = 24;
                pcText.fontStyle = FontStyles.Italic;

                var tmpInputField = go.GetComponent<TMP_InputField>();
                tmpInputField.textViewport = taRect;
                tmpInputField.textComponent = tcText;
                tmpInputField.placeholder = pcText;
                
                qInputFieldGo = tmpInputField;
                Debug.Log("KanjiInputField automatically created under QuestionerPanel.");
            }
            else
            {
                qInputFieldGo = qInputFieldTrans.GetComponent<TMP_InputField>();
            }
            // Ensure the input field is active and visible
            var inputFieldObj = qInputFieldGo != null ? qInputFieldGo.gameObject : null;
            if (inputFieldObj != null)
            {
                inputFieldObj.SetActive(true);
                inputFieldObj.transform.SetAsLastSibling();
            }
            if (qInputFieldGo != null) serializedQ.FindProperty("_kanjiInputField").objectReferenceValue = qInputFieldGo;

            System.Func<string, GameObject> findUIElement = (name) =>
            {
                var t = qPanelGo.transform.Find(name);
                if (t != null) return t.gameObject;
                t = opPanelGo.transform.Find(name);
                if (t != null) return t.gameObject;
                return null;
            };

            // 各UIパーツの取得
            var addBtnGoObj = findUIElement("AddKanjiButton");
            var addBtnGo = addBtnGoObj?.GetComponent<Button>();
            var completeBtnGoObj = findUIElement("CompleteButton");
            var completeBtnGo = completeBtnGoObj?.GetComponent<Button>();
            var plannedBoxGo = findUIElement("PlannedKanjiBox");
            var suggPanelGo = findUIElement("SuggestionPanel");
            if (suggPanelGo == null)
            {
                suggPanelGo = new GameObject("SuggestionPanel", typeof(RectTransform), typeof(Image));
                suggPanelGo.transform.SetParent(opPanelGo.transform, false);
                suggPanelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.4f); // 半透明の背景
            }

            var newScrollViewTrans = suggPanelGo.transform.Find("Scroll View");
            GameObject scrollViewGo;
            if (newScrollViewTrans == null)
            {
                // Scroll View
                scrollViewGo = new GameObject("Scroll View", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
                scrollViewGo.transform.SetParent(suggPanelGo.transform, false);
                
                // 背景画像を設定
                var svImage = scrollViewGo.GetComponent<Image>();
                svImage.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
            }
            else
            {
                scrollViewGo = newScrollViewTrans.gameObject;
                var svImage = scrollViewGo.GetComponent<Image>();
                if (svImage == null) svImage = scrollViewGo.AddComponent<Image>();
                svImage.color = new Color(0.1f, 0.1f, 0.1f, 0.6f);
            }

            var viewportTrans = scrollViewGo.transform.Find("Viewport");
            GameObject viewportGo;
            if (viewportTrans == null)
            {
                // Viewport
                viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
                viewportGo.transform.SetParent(scrollViewGo.transform, false);
                var vpRect = viewportGo.GetComponent<RectTransform>();
                vpRect.anchorMin = Vector2.zero;
                vpRect.anchorMax = Vector2.one;
                vpRect.sizeDelta = Vector2.zero;
                viewportGo.GetComponent<Mask>().showMaskGraphic = false;
            }
            else
            {
                viewportGo = viewportTrans.gameObject;
            }

            var contentTrans = viewportGo.transform.Find("Content");
            GameObject contentGo;
            if (contentTrans == null)
            {
                // Content
                contentGo = new GameObject("Content", typeof(RectTransform));
                contentGo.transform.SetParent(viewportGo.transform, false);
                var contentRect = contentGo.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.sizeDelta = new Vector2(0f, 0f);
            }
            else
            {
                contentGo = contentTrans.gameObject;
            }

            // ScrollRect の設定
            var scrollRect = scrollViewGo.GetComponent<ScrollRect>();
            if (scrollRect == null) scrollRect = scrollViewGo.AddComponent<ScrollRect>();
            scrollRect.content = contentGo.GetComponent<RectTransform>();
            scrollRect.viewport = viewportGo.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;

            // --- 上部UI要素の再配置（被り防止） ---
            var flipAreaGo = qPanelGo.transform.Find("FlipArea")?.gameObject;
            if (flipAreaGo != null)
            {
                var rect = flipAreaGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.02f, 0.32f);
                rect.anchorMax = new Vector2(0.98f, 0.85f);
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }

            var flipCanvasGo = qPanelGo.transform.Find("FlipCanvas")?.gameObject;
            if (flipCanvasGo != null)
            {
                var rect = flipCanvasGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.02f, 0.32f);
                rect.anchorMax = new Vector2(0.98f, 0.85f);
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }

            topicTextTrans = qPanelGo.transform.Find("TopicDisplayText");
            if (topicTextTrans != null)
            {
                var rect = topicTextTrans.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.1f, 0.86f);
                rect.anchorMax = new Vector2(0.9f, 0.98f);
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }

            var countTextGoObj = qPanelGo.transform.Find("KanjiCountText")?.gameObject;
            if (countTextGoObj != null)
            {
                var rect = countTextGoObj.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.8f, 0.28f);
                rect.anchorMax = new Vector2(0.98f, 0.32f);
                rect.pivot = new Vector2(1f, 0.5f);
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;
            }

            // 出力予定ボックス（PlannedKanjiBox）の作成と取得
            if (plannedBoxGo == null)
            {
                plannedBoxGo = new GameObject("PlannedKanjiBox", typeof(RectTransform), typeof(Image));
                plannedBoxGo.transform.SetParent(opPanelGo.transform, false);

                var img = plannedBoxGo.GetComponent<Image>();
                img.color = new Color(0.95f, 0.95f, 0.95f, 1f);

                var border = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                border.transform.SetParent(plannedBoxGo.transform, false);
                var txt = border.GetComponent<TextMeshProUGUI>();
                txt.text = "";
                txt.fontSize = 28;
                txt.color = Color.black;
                txt.alignment = TextAlignmentOptions.Center;

                var txtRect = border.GetComponent<RectTransform>();
                txtRect.anchorMin = Vector2.zero;
                txtRect.anchorMax = Vector2.one;
                txtRect.sizeDelta = Vector2.zero;

                var boxFontAsset = GameObject.Find("TitleText")?.GetComponent<TextMeshProUGUI>()?.font;
                if (boxFontAsset != null) txt.font = boxFontAsset;
            }

            var plannedText = plannedBoxGo.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            if (plannedText != null)
            {
                serializedQ.FindProperty("_plannedKanjiText").objectReferenceValue = plannedText;
            }

            // 各操作パーツを OperationPanel の下に移動し LayoutElement を設定
            System.Action<GameObject, float, float> setupLayoutElement = (go, width, height) =>
            {
                if (go == null) return;
                go.transform.SetParent(opPanelGo.transform, false);
                var le = go.GetComponent<LayoutElement>();
                if (le == null) le = go.AddComponent<LayoutElement>();
                le.preferredWidth = width;
                le.minWidth = width;
                le.preferredHeight = height;
                le.minHeight = height;
            };

            // 横並びの幅を指定してレイアウト適用
            setupLayoutElement(suggPanelGo, 260f, 60f); // 漢字リスト
            setupLayoutElement(qInputFieldGo?.gameObject, 180f, 50f); // テキストボックス
            setupLayoutElement(plannedBoxGo, 80f, 50f); // 出力予定ボックス
            setupLayoutElement(addBtnGoObj, 120f, 50f); // 決定ボタン
            setupLayoutElement(completeBtnGoObj, 120f, 50f); // 完成ボタン

            // 横並び順を並び順と一致させる
            if (suggPanelGo != null) suggPanelGo.transform.SetAsLastSibling();
            if (qInputFieldGo != null) qInputFieldGo.transform.SetAsLastSibling();
            if (plannedBoxGo != null) plannedBoxGo.transform.SetAsLastSibling();
            if (addBtnGoObj != null) addBtnGoObj.transform.SetAsLastSibling();
            if (completeBtnGoObj != null) completeBtnGoObj.transform.SetAsLastSibling();

            // 漢字リスト内のスクロールビュー領域のストレッチ設定
            if (suggPanelGo != null)
            {
                var scrollViewTrans = suggPanelGo.transform.Find("Scroll View");
                if (scrollViewTrans != null)
                {
                    var svRect = scrollViewTrans.GetComponent<RectTransform>();
                    svRect.anchorMin = Vector2.zero;
                    svRect.anchorMax = Vector2.one;
                    svRect.sizeDelta = Vector2.zero;
                    svRect.anchoredPosition = Vector2.zero;
                }
            }

            if (addBtnGo != null) serializedQ.FindProperty("_addKanjiButton").objectReferenceValue = addBtnGo;
            if (completeBtnGo != null) serializedQ.FindProperty("_completeButton").objectReferenceValue = completeBtnGo;

            var countTextGo = qPanelGo.transform.Find("KanjiCountText")?.GetComponent<TextMeshProUGUI>();
            if (countTextGo != null) serializedQ.FindProperty("_kanjiCountText").objectReferenceValue = countTextGo;
            var ansTextGo = qPanelGo.transform.Find("ResultPanel/AnswererAnswerText")?.GetComponent<TextMeshProUGUI>();
            if (ansTextGo != null) serializedQ.FindProperty("_answererAnswerText").objectReferenceValue = ansTextGo;

            if (suggPanelGo != null) serializedQ.FindProperty("_suggestionPanel").objectReferenceValue = suggPanelGo;
            var suggContentRect = suggPanelGo?.transform.Find("Scroll View/Viewport/Content")?.GetComponent<RectTransform>();
            if (suggContentRect != null) serializedQ.FindProperty("_suggestionContent").objectReferenceValue = suggContentRect;

            var tmFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/NotoSansJP_Kanji SDF.asset");
            if (tmFont != null)
            {
                serializedQ.FindProperty("_fontAsset").objectReferenceValue = tmFont;
            }

            var flipperGo = qPanelGo.transform.Find("FlipCanvas")?.gameObject;
            var flipper = flipperGo?.GetComponent<KanjiFlipper>();
            if (flipper != null)
            {
                serializedQ.FindProperty("_kanjiFlipper").objectReferenceValue = flipper;

                // KanjiFlipper に _kanjiElementPrefab を自動設定
                var serializedFlipper = new SerializedObject(flipper);
                var prefabProp = serializedFlipper.FindProperty("_kanjiElementPrefab");
                if (prefabProp != null && prefabProp.objectReferenceValue == null)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/KanjiElement.prefab");
                    if (prefab != null)
                    {
                        // Ensure RectTransform exists on prefab root
                        var rt = prefab.GetComponent<RectTransform>();
                        if (rt == null)
                        {
                            rt = prefab.AddComponent<RectTransform>();
                            rt.anchorMin = new Vector2(0.5f, 0.5f);
                            rt.anchorMax = new Vector2(0.5f, 0.5f);
                            rt.pivot = new Vector2(0.5f, 0.5f);
                            rt.sizeDelta = new Vector2(100, 100);
                            Debug.Log("Added missing RectTransform to KanjiElement prefab during UIBuilder fix.");
                        }
                        prefabProp.objectReferenceValue = prefab;
                        serializedFlipper.ApplyModifiedProperties();
                        Debug.Log("KanjiElement prefab automatically assigned to KanjiFlipper.");
                    }
                    else
                    {
                        Debug.LogError("Failed to automatically load Assets/Prefabs/KanjiElement.prefab!");
                    }
                }
            }
            var validator = GameObject.FindObjectOfType<KanjiInputValidator>();
            if (validator != null) serializedQ.FindProperty("_kanjiInputValidator").objectReferenceValue = validator;

            serializedQ.ApplyModifiedProperties();

            // 日本語フォントアセットの設定
            var qFontAsset = GameObject.Find("TitleText")?.GetComponent<TextMeshProUGUI>()?.font;
            if (qFontAsset != null)
            {
                var qWaitText = qWaitingPanelGo.transform.Find("WaitingMessageText")?.GetComponent<TextMeshProUGUI>();
                if (qWaitText != null) qWaitText.font = qFontAsset;
                var qResText = qResultPanelGo.transform.Find("ResultText")?.GetComponent<TextMeshProUGUI>();
                if (qResText != null) qResText.font = qFontAsset;
                if (topicTextGo != null) topicTextGo.font = qFontAsset;
                if (qInputFieldGo != null)
                {
                    if (qInputFieldGo.textComponent != null) qInputFieldGo.textComponent.font = qFontAsset;
                    var placeholderText = qInputFieldGo.placeholder as TextMeshProUGUI;
                    if (placeholderText != null) placeholderText.font = qFontAsset;
                }
            }

            // 子パネルを非アクティブにする
            qWaitingPanelGo.SetActive(false);
            qResultPanelGo.SetActive(false);
            
            Debug.Log("QuestionerUI references and sub panels initialized.");
        }

        // --- AnswererUI の構築・移動処理 ---
        var answererUI = canvas.GetComponent<AnswererUI>();
        if (answererUI == null)
        {
            answererUI = canvas.AddComponent<AnswererUI>();
            Debug.Log("AnswererUI added to MainCanvas.");
        }

        var answererPanelGo = canvas.transform.Find("AnswererPanel")?.gameObject;
        if (answererPanelGo == null)
        {
            answererPanelGo = new GameObject("AnswererPanel", typeof(RectTransform));
            answererPanelGo.transform.SetParent(canvas.transform, false);
            var panelRect = answererPanelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
        }

        // 既存 of AnswererPanel から古い AnswererUI を削除
        var oldA = answererPanelGo.GetComponent<AnswererUI>();
        if (oldA != null) GameObject.DestroyImmediate(oldA);

        // 待機パネル (WaitingPanel) の生成・取得
        var waitingPanelGo = answererPanelGo.transform.Find("WaitingPanel")?.gameObject;
        if (waitingPanelGo == null)
        {
            waitingPanelGo = new GameObject("WaitingPanel", typeof(RectTransform), typeof(Image));
            waitingPanelGo.transform.SetParent(answererPanelGo.transform, false);
            var waitRect = waitingPanelGo.GetComponent<RectTransform>();
            waitRect.anchorMin = new Vector2(0.1f, 0.1f);
            waitRect.anchorMax = new Vector2(0.9f, 0.9f);
            waitRect.sizeDelta = Vector2.zero;
            waitingPanelGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        }

        var waitTextGo = waitingPanelGo.transform.Find("WaitingMessageText")?.gameObject;
        if (waitTextGo == null)
        {
            waitTextGo = new GameObject("WaitingMessageText", typeof(RectTransform), typeof(TextMeshProUGUI));
            waitTextGo.transform.SetParent(waitingPanelGo.transform, false);
            var waitTextRect = waitTextGo.GetComponent<RectTransform>();
            waitTextRect.anchorMin = Vector2.zero;
            waitTextRect.anchorMax = Vector2.one;
            waitTextRect.sizeDelta = Vector2.zero;
        }
        var waitText = waitTextGo.GetComponent<TextMeshProUGUI>();
        waitText.text = "待機中...";
        waitText.fontSize = 32;
        waitText.alignment = TextAlignmentOptions.Center;
        waitText.color = Color.white;

        // 結果パネル (ResultPanel) の生成・取得
        var resultPanelGo = answererPanelGo.transform.Find("ResultPanel")?.gameObject;
        if (resultPanelGo == null)
        {
            resultPanelGo = new GameObject("ResultPanel", typeof(RectTransform), typeof(Image));
            resultPanelGo.transform.SetParent(answererPanelGo.transform, false);
            var resRect = resultPanelGo.GetComponent<RectTransform>();
            resRect.anchorMin = new Vector2(0.2f, 0.3f);
            resRect.anchorMax = new Vector2(0.8f, 0.7f);
            resRect.sizeDelta = Vector2.zero;
            resultPanelGo.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        }

        var resTextGo = resultPanelGo.transform.Find("ResultText")?.gameObject;
        if (resTextGo == null)
        {
            resTextGo = new GameObject("ResultText", typeof(RectTransform), typeof(TextMeshProUGUI));
            resTextGo.transform.SetParent(resultPanelGo.transform, false);
            var resTextRect = resTextGo.GetComponent<RectTransform>();
            resTextRect.anchorMin = Vector2.zero;
            resTextRect.anchorMax = Vector2.one;
            resTextRect.sizeDelta = Vector2.zero;
        }
        var resText = resTextGo.GetComponent<TextMeshProUGUI>();
        resText.text = "結果";
        resText.fontSize = 40;
        resText.alignment = TextAlignmentOptions.Center;
        resText.color = Color.white;

        // 回答入力画面用のサブパネル (AnswerInputPanel) の生成・取得
        var inputPanelGo = answererPanelGo.transform.Find("AnswerInputPanel")?.gameObject;
        if (inputPanelGo == null)
        {
            inputPanelGo = new GameObject("AnswerInputPanel", typeof(RectTransform));
            inputPanelGo.transform.SetParent(answererPanelGo.transform, false);
            var inputPanelRect = inputPanelGo.GetComponent<RectTransform>();
            inputPanelRect.anchorMin = Vector2.zero;
            inputPanelRect.anchorMax = Vector2.one;
            inputPanelRect.sizeDelta = Vector2.zero;
        }

        // 回答者用の FlipArea (ホワイトボード背景) の作成・取得
        var ansFlipAreaGo = inputPanelGo.transform.Find("FlipArea")?.gameObject;
        if (ansFlipAreaGo == null)
        {
            ansFlipAreaGo = new GameObject("FlipArea", typeof(RectTransform), typeof(Image));
            ansFlipAreaGo.transform.SetParent(inputPanelGo.transform, false);
            var rect = ansFlipAreaGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.02f, 0.32f);
            rect.anchorMax = new Vector2(0.98f, 0.85f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            ansFlipAreaGo.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f, 1f); // 明るいグレー
        }

        // 回答者用の FlipCanvas の作成・取得
        var ansFlipCanvasGo = inputPanelGo.transform.Find("FlipCanvas")?.gameObject;
        if (ansFlipCanvasGo == null)
        {
            ansFlipCanvasGo = new GameObject("FlipCanvas", typeof(RectTransform), typeof(KanjiFlipper));
            ansFlipCanvasGo.transform.SetParent(inputPanelGo.transform, false);
            var rect = ansFlipCanvasGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.02f, 0.32f);
            rect.anchorMax = new Vector2(0.98f, 0.85f);
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        var ansFlipper = ansFlipCanvasGo.GetComponent<KanjiFlipper>();
        // _kanjiElementPrefab と _flipCanvas を設定
        var serializedAnsFlipper = new SerializedObject(ansFlipper);
        var prefabPropAns = serializedAnsFlipper.FindProperty("_kanjiElementPrefab");
        if (prefabPropAns != null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/KanjiElement.prefab");
            prefabPropAns.objectReferenceValue = prefab;
        }
        var flipCanvasPropAns = serializedAnsFlipper.FindProperty("_flipCanvas");
        if (flipCanvasPropAns != null)
        {
            flipCanvasPropAns.objectReferenceValue = ansFlipCanvasGo.GetComponent<RectTransform>();
        }
        serializedAnsFlipper.ApplyModifiedProperties();

        // タイマーテキスト
        var timerTextGo = inputPanelGo.transform.Find("TimerText")?.gameObject;
        if (timerTextGo == null)
        {
            timerTextGo = new GameObject("TimerText", typeof(RectTransform), typeof(TextMeshProUGUI));
            timerTextGo.transform.SetParent(inputPanelGo.transform, false);
            var timerRect = timerTextGo.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.1f, 0.8f);
            timerRect.anchorMax = new Vector2(0.9f, 0.9f);
            timerRect.sizeDelta = Vector2.zero;
        }
        var timerText = timerTextGo.GetComponent<TextMeshProUGUI>();
        timerText.text = "残り時間: --秒";
        timerText.fontSize = 28;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = Color.white;

        // 入力フィールド
        var inputFieldGo = inputPanelGo.transform.Find("AnswerInputField")?.gameObject;
        if (inputFieldGo == null)
        {
            inputFieldGo = new GameObject("AnswerInputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
            inputFieldGo.transform.SetParent(inputPanelGo.transform, false);
            var infRect = inputFieldGo.GetComponent<RectTransform>();
            infRect.anchorMin = new Vector2(0.2f, 0.2f);
            infRect.anchorMax = new Vector2(0.6f, 0.3f);
            infRect.sizeDelta = Vector2.zero;
            inputFieldGo.GetComponent<Image>().color = Color.white;

            var textAreaGo = new GameObject("Text Area", typeof(RectTransform));
            textAreaGo.transform.SetParent(inputFieldGo.transform, false);
            var taRect = textAreaGo.GetComponent<RectTransform>();
            taRect.anchorMin = Vector2.zero;
            taRect.anchorMax = Vector2.one;
            taRect.sizeDelta = new Vector2(-10, -10);

            var textComponentGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textComponentGo.transform.SetParent(textAreaGo.transform, false);
            var tcRect = textComponentGo.GetComponent<RectTransform>();
            tcRect.anchorMin = Vector2.zero;
            tcRect.anchorMax = Vector2.one;
            tcRect.sizeDelta = Vector2.zero;
            var tcText = textComponentGo.GetComponent<TextMeshProUGUI>();
            tcText.color = Color.black;
            tcText.fontSize = 24;

            var placeholderComponentGo = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
            placeholderComponentGo.transform.SetParent(textAreaGo.transform, false);
            var pcRect = placeholderComponentGo.GetComponent<RectTransform>();
            pcRect.anchorMin = Vector2.zero;
            pcRect.anchorMax = Vector2.one;
            pcRect.sizeDelta = Vector2.zero;
            var pcText = placeholderComponentGo.GetComponent<TextMeshProUGUI>();
            pcText.text = "答えを入力...";
            pcText.color = Color.gray;
            pcText.fontSize = 24;
            pcText.fontStyle = FontStyles.Italic;

            var tmpInputField = inputFieldGo.GetComponent<TMP_InputField>();
            tmpInputField.textViewport = taRect;
            tmpInputField.textComponent = tcText;
            tmpInputField.placeholder = pcText;
        }
        var tmpInputFieldComponent = inputFieldGo.GetComponent<TMP_InputField>();

        // 送信ボタン
        var submitBtnGo = inputPanelGo.transform.Find("SubmitButton")?.gameObject;
        if (submitBtnGo == null)
        {
            submitBtnGo = new GameObject("SubmitButton", typeof(RectTransform), typeof(Image), typeof(Button));
            submitBtnGo.transform.SetParent(inputPanelGo.transform, false);
            var btnRect = submitBtnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.65f, 0.2f);
            btnRect.anchorMax = new Vector2(0.8f, 0.3f);
            btnRect.sizeDelta = Vector2.zero;
            submitBtnGo.GetComponent<Image>().color = new Color(0.2f, 0.6f, 0.2f);

            var btnTextGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGo.transform.SetParent(submitBtnGo.transform, false);
            var btRect = btnTextGo.GetComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;
            var btText = btnTextGo.GetComponent<TextMeshProUGUI>();
            btText.text = "送信";
            btText.fontSize = 24;
            btText.color = Color.white;
            btText.alignment = TextAlignmentOptions.Center;
        }
        var submitBtn = submitBtnGo.GetComponent<Button>();

        // 参照の設定
        var serializedObj = new SerializedObject(answererUI);
        serializedObj.FindProperty("_answererPanel").objectReferenceValue = inputPanelGo;
        serializedObj.FindProperty("_waitingPanel").objectReferenceValue = waitingPanelGo;
        serializedObj.FindProperty("_waitingMessageText").objectReferenceValue = waitText;
        serializedObj.FindProperty("_resultPanel").objectReferenceValue = resultPanelGo;
        serializedObj.FindProperty("_answerInputField").objectReferenceValue = tmpInputFieldComponent;
        serializedObj.FindProperty("_submitAnswerButton").objectReferenceValue = submitBtn;
        serializedObj.FindProperty("_resultText").objectReferenceValue = resText;
        serializedObj.FindProperty("_timerText").objectReferenceValue = timerText;
        serializedObj.FindProperty("_kanjiFlipper").objectReferenceValue = ansFlipper;
        serializedObj.ApplyModifiedProperties();

        // 日本語フォントアセットの設定
        var fontAsset = GameObject.Find("TitleText")?.GetComponent<TextMeshProUGUI>()?.font;
        if (fontAsset != null) {
            waitText.font = fontAsset;
            resText.font = fontAsset;
            timerText.font = fontAsset;
            var tc = inputFieldGo.GetComponent<TMP_InputField>()?.textComponent;
            if (tc != null) tc.font = fontAsset;
            var pc = inputFieldGo.GetComponent<TMP_InputField>()?.placeholder as TextMeshProUGUI;
            if (pc != null) pc.font = fontAsset;
            var bt = submitBtnGo.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            if (bt != null) bt.font = fontAsset;
        }

        // 親パネルはアクティブにし、子パネルを初期状態で非アクティブにする
        answererPanelGo.SetActive(true);
        waitingPanelGo.SetActive(false);
        resultPanelGo.SetActive(false);
        inputPanelGo.SetActive(false);

        // MainCanvasの直下にあるResultPanel（もし存在すれば）を初期非アクティブにする
        var globalResultPanel = canvas.transform.Find("ResultPanel")?.gameObject;
        if (globalResultPanel != null)
        {
            globalResultPanel.SetActive(false);

            // Ensure PlayerCountText exists under RoomWaitingPanel and assign it
            var waitingPanel = canvas.transform.Find("RoomWaitingPanel")?.gameObject;
            if (waitingPanel != null)
            {
                var countText = waitingPanel.transform.Find("PlayerCountText")?.GetComponent<TMPro.TextMeshProUGUI>();
                if (countText == null)
                {
                    var go = new GameObject("PlayerCountText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                    go.transform.SetParent(waitingPanel.transform, false);
                    var rect = go.GetComponent<RectTransform>();
                    rect.anchorMin = new Vector2(0.1f, 0.8f);
                    rect.anchorMax = new Vector2(0.9f, 0.9f);
                    rect.sizeDelta = Vector2.zero;
                    var tmp = go.GetComponent<TMPro.TextMeshProUGUI>();
                    tmp.text = "参加人数: 0 人";
                    tmp.fontSize = 28;
                    tmp.alignment = TMPro.TextAlignmentOptions.Center;
                    tmp.color = Color.white;
                    countText = tmp;
                }
                var menu = canvas.GetComponent<KanjiFlipGame.UI.MainMenuUI>();
                if (menu != null)
                {
                    var serializedMenu = new SerializedObject(menu);
                    serializedMenu.FindProperty("_playerCountText").objectReferenceValue = countText;
                    serializedMenu.ApplyModifiedProperties();
                }
            }
            Debug.Log("Global ResultPanel set to inactive.");
        }

        // 保存
        EditorSceneManager.MarkSceneDirty(canvas.scene);
        EditorSceneManager.SaveScene(canvas.scene);

        return "Success";
    }

    public static string CheckReferences()
    {
        var canvas = GameObject.Find("MainCanvas");
        if (canvas == null) return "Canvas not found";

        var globalResult = canvas.transform.Find("ResultPanel")?.gameObject;
        string globalResultStatus = globalResult != null ? ("ResultPanel Active: " + globalResult.activeSelf) : "ResultPanel not found";

        var menu = canvas.GetComponent<MainMenuUI>();
        if (menu == null) return globalResultStatus + "\nMainMenuUI not found";
        var serialized = new SerializedObject(menu);
        return globalResultStatus + "\n" +
               "_mainMenuPanel: " + (serialized.FindProperty("_mainMenuPanel").objectReferenceValue != null ? serialized.FindProperty("_mainMenuPanel").objectReferenceValue.name : "null") + "\n" +
               "_selectionPanel: " + (serialized.FindProperty("_selectionPanel").objectReferenceValue != null ? serialized.FindProperty("_selectionPanel").objectReferenceValue.name : "null") + "\n" +
               "_friendMatchModePanel: " + (serialized.FindProperty("_friendMatchModePanel").objectReferenceValue != null ? serialized.FindProperty("_friendMatchModePanel").objectReferenceValue.name : "null") + "\n" +
               "_friendMatchInputPanel: " + (serialized.FindProperty("_friendMatchInputPanel").objectReferenceValue != null ? serialized.FindProperty("_friendMatchInputPanel").objectReferenceValue.name : "null") + "\n" +
               "_topicInputPanel: " + (serialized.FindProperty("_topicInputPanel").objectReferenceValue != null ? serialized.FindProperty("_topicInputPanel").objectReferenceValue.name : "null") + "\n" +
               "_roomWaitingPanel: " + (serialized.FindProperty("_roomWaitingPanel").objectReferenceValue != null ? serialized.FindProperty("_roomWaitingPanel").objectReferenceValue.name : "null");
    }

    public static string BuildGame()
    {
        var scenes = UnityEditor.EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
        string buildPath = "Builds/Windows/8bit-Telephone.exe";
        UnityEditor.BuildPlayerOptions options = new UnityEditor.BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = UnityEditor.BuildTarget.StandaloneWindows64,
            options = UnityEditor.BuildOptions.None
        };
        var report = UnityEditor.BuildPipeline.BuildPlayer(options);
        return report.summary.result.ToString();
    }

    public static string StartRandomMatch()
    {
        var canvas = GameObject.Find("MainCanvas");
        if (canvas == null) return "Canvas not found";
        var menu = canvas.GetComponent<MainMenuUI>();
        if (menu == null) return "MainMenuUI not found";
        var serialized = new SerializedObject(menu);
        var btn = serialized.FindProperty("_randomMatchButton").objectReferenceValue as UnityEngine.UI.Button;
        if (btn == null) return "Button not found";
        btn.onClick.Invoke();
        return "Success";
    }

    public static string CheckLauncher()
    {
        var launcher = GameObject.FindObjectOfType<KanjiFlipGame.Network.NetworkLauncher>();
        if (launcher == null) return "Launcher GameObject not found in scene";
        return "Launcher found. Instance: " + (KanjiFlipGame.Network.NetworkLauncher.Instance != null ? "not null" : "null") + ", isActive: " + launcher.gameObject.activeInHierarchy;
    }

    public static string CheckEditorState()
    {
        return "isPlaying: " + UnityEditor.EditorApplication.isPlaying + ", isPaused: " + UnityEditor.EditorApplication.isPaused;
    }

    public static string CheckGameState()
    {
        var gm = KanjiFlipGame.Core.GameManager.Instance;
        if (gm == null) return "GameManager.Instance is NULL";
        var launcher = KanjiFlipGame.Network.NetworkLauncher.Instance;
        string runnerInfo = "null";
        if (launcher != null && launcher.Runner != null)
        {
            runnerInfo = "connected, LocalPlayer=" + launcher.Runner.LocalPlayer.ToString();
        }
        string objInfo = gm.Object != null ? ("IsValid=" + gm.Object.IsValid) : "Object=null";
        return "GM: " + gm.name + ", IsSpawnedCompleted=" + gm.IsSpawnedCompleted + ", " + objInfo + ", Runner=" + runnerInfo + ", State=" + gm.CurrentState;
    }

    public static string CheckBuildInfo()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Build Scenes ===");
        foreach (var s in UnityEditor.EditorBuildSettings.scenes)
        {
            sb.AppendLine((s.enabled ? "[ON]" : "[OFF]") + " " + s.path);
        }
        return sb.ToString();
    }

    public static string CheckFusionConfig()
    {
        return "Fusion config check skipped to avoid compile error";
    }

    public static string CheckRunnerStatus()
    {
        var runner = KanjiFlipGame.Network.NetworkLauncher.Instance?.Runner;
        if (runner == null) return "Runner is null";
        string result = "Players count: " + System.Linq.Enumerable.Count(runner.ActivePlayers) + "\n";
        foreach (var p in runner.ActivePlayers) {
            bool ready = KanjiFlipGame.Core.GameManager.Instance.IsPlayerReady(p);
            bool consent = KanjiFlipGame.Core.GameManager.Instance.IsPlayerConsented(p);
            result += "Player " + p.PlayerId + ": Ready=" + ready + ", Consented=" + consent + "\n";
        }
        return result;
    }

    public static string GetSessionName()
    {
        var launcher = KanjiFlipGame.Network.NetworkLauncher.Instance;
        if (launcher != null && launcher.Runner != null)
        {
            return "SessionName: " + launcher.Runner.SessionInfo.Name;
        }
        return "No runner";
    }

    public static string DumpQuestionerPanel()
    {
        var canvas = GameObject.Find("MainCanvas");
        if (canvas == null) return "MainCanvas not found";
        var qPanelGo = canvas.transform.Find("QuestionerPanel")?.gameObject;
        if (qPanelGo == null) return "QuestionerPanel not found";

        var sb = new System.Text.StringBuilder();
        DumpTransform(qPanelGo.transform, sb, "");
        return sb.ToString();
    }

    public static void DumpQuestionerPanelCLI()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity/GameScene.unity");
        string dump = DumpQuestionerPanel();
        System.IO.File.WriteAllText("dump_hierarchy.txt", dump);
    }

    public static void DumpRootObjectsCLI()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity/GameScene.unity");
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        var sb = new System.Text.StringBuilder();
        foreach (var r in roots)
        {
            DumpTransform(r.transform, sb, "");
        }
        System.IO.File.WriteAllText("dump_roots.txt", sb.ToString());
    }

    public static void BuildAndBuildGameCLI()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity/GameScene.unity");
        string buildResult = Build();
        Debug.Log("UI Build Result: " + buildResult);
        
        string gameBuildResult = BuildGame();
        Debug.Log("Game Build Result: " + gameBuildResult);
    }

    public static string BuildGameWebGL()
    {
        var scenes = UnityEditor.EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
        string buildPath = "Builds/WebGL";
        
        if (System.IO.Directory.Exists(buildPath))
        {
            try { System.IO.Directory.Delete(buildPath, true); } catch {}
        }
        System.IO.Directory.CreateDirectory(buildPath);

        UnityEditor.BuildPlayerOptions options = new UnityEditor.BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = UnityEditor.BuildTarget.WebGL,
            options = UnityEditor.BuildOptions.None
        };
        
        UnityEditor.EditorUserBuildSettings.SwitchActiveBuildTarget(UnityEditor.BuildTargetGroup.WebGL, UnityEditor.BuildTarget.WebGL);
        
        var report = UnityEditor.BuildPipeline.BuildPlayer(options);
        return report.summary.result.ToString();
    }

    public static void BuildGameWebGLCLI()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity/GameScene.unity");
        string buildResult = Build();
        Debug.Log("UI Build Result: " + buildResult);
        
        string webglBuildResult = BuildGameWebGL();
        Debug.Log("WebGL Build Result: " + webglBuildResult);
    }

    public static string BuildGameiOS()
    {
        var scenes = UnityEditor.EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
        string buildPath = "Builds/iOS";
        
        if (System.IO.Directory.Exists(buildPath))
        {
            try { System.IO.Directory.Delete(buildPath, true); } catch {}
        }
        System.IO.Directory.CreateDirectory(buildPath);

        UnityEditor.BuildPlayerOptions options = new UnityEditor.BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = buildPath,
            target = UnityEditor.BuildTarget.iOS,
            options = UnityEditor.BuildOptions.None
        };
        
        UnityEditor.EditorUserBuildSettings.SwitchActiveBuildTarget(UnityEditor.BuildTargetGroup.iOS, UnityEditor.BuildTarget.iOS);
        
        var report = UnityEditor.BuildPipeline.BuildPlayer(options);
        return report.summary.result.ToString();
    }

    public static void BuildGameiOSCLI()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity/GameScene.unity");
        string buildResult = Build();
        Debug.Log("UI Build Result: " + buildResult);
        
        string iosBuildResult = BuildGameiOS();
        Debug.Log("iOS Build Result: " + iosBuildResult);
    }

    private static void DumpTransform(Transform t, System.Text.StringBuilder sb, string indent)
    {
        sb.AppendLine(indent + t.name + " (" + t.gameObject.activeInHierarchy + ", activeSelf=" + t.gameObject.activeSelf + ")");
        for (int i = 0; i < t.childCount; i++)
        {
            DumpTransform(t.GetChild(i), sb, indent + "  ");
        }
    }

    public static string CheckKanjiFlipper()
    {
        var flippers = GameObject.FindObjectsOfType<KanjiFlipper>();
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Found " + flippers.Length + " KanjiFlipper(s):");
        foreach (var f in flippers)
        {
            var flipCanvas = f.GetType().GetField("_flipCanvas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(f) as RectTransform;
            sb.AppendLine("- GameObject: " + f.gameObject.name + ", active: " + f.gameObject.activeInHierarchy + ", _flipCanvas: " + (flipCanvas != null ? flipCanvas.name : "null"));
        }
        return sb.ToString();
    }

    public static void DumpPrefabCLI()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/KanjiElement.prefab");
        if (prefab == null)
        {
            Debug.LogError("Prefab not found!");
            return;
        }
        var sb = new System.Text.StringBuilder();
        DumpTransform(prefab.transform, sb, "");
        var text = prefab.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
        {
            sb.AppendLine("=== TextMeshProUGUI ===");
            sb.AppendLine("text: " + text.text);
            sb.AppendLine("fontSize: " + text.fontSize);
            sb.AppendLine("color: " + text.color);
            sb.AppendLine("font: " + (text.font != null ? text.font.name : "null"));
            sb.AppendLine("alignment: " + text.alignment);
            var rt = text.GetComponent<RectTransform>();
            sb.AppendLine("RectTransform sizeDelta: " + rt.sizeDelta);
            sb.AppendLine("RectTransform localScale: " + rt.localScale);
        }
        System.IO.File.WriteAllText("prefab_dump.txt", sb.ToString());
    }

    public static void CheckKanjiFlipperCLI()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity/GameScene.unity");
        string result = CheckKanjiFlipper();
        System.IO.File.WriteAllText("flipper_check.txt", result);
    }
}
#endif
