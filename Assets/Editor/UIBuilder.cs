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
                waitRect.anchorMin = new Vector2(0.1f, 0.1f);
                waitRect.anchorMax = new Vector2(0.9f, 0.9f);
                waitRect.sizeDelta = Vector2.zero;
                qWaitingPanelGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                
                var qWaitTextGo = new GameObject("WaitingMessageText", typeof(RectTransform), typeof(TextMeshProUGUI));
                qWaitTextGo.transform.SetParent(qWaitingPanelGo.transform, false);
                var qWaitTextRect = qWaitTextGo.GetComponent<RectTransform>();
                qWaitTextRect.anchorMin = Vector2.zero;
                qWaitTextRect.anchorMax = Vector2.one;
                qWaitTextRect.sizeDelta = Vector2.zero;
                var qWaitText = qWaitTextGo.GetComponent<TextMeshProUGUI>();
                qWaitText.text = "待機中...";
                qWaitText.fontSize = 32;
                qWaitText.alignment = TextAlignmentOptions.Center;
                qWaitText.color = Color.white;
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

            TMP_InputField qInputFieldGo = null;
            var qInputFieldTrans = qPanelGo.transform.Find("KanjiInputField");
            if (qInputFieldTrans == null)
            {
                var go = new GameObject("KanjiInputField", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
                go.transform.SetParent(qPanelGo.transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.2f, 0.05f);
                rect.anchorMax = new Vector2(0.6f, 0.15f);
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
            if (qInputFieldGo != null) serializedQ.FindProperty("_kanjiInputField").objectReferenceValue = qInputFieldGo;

            var addBtnGo = qPanelGo.transform.Find("AddKanjiButton")?.GetComponent<Button>();
            if (addBtnGo != null) serializedQ.FindProperty("_addKanjiButton").objectReferenceValue = addBtnGo;
            var completeBtnGo = qPanelGo.transform.Find("CompleteButton")?.GetComponent<Button>();
            if (completeBtnGo != null) serializedQ.FindProperty("_completeButton").objectReferenceValue = completeBtnGo;
            var countTextGo = qPanelGo.transform.Find("KanjiCountText")?.GetComponent<TextMeshProUGUI>();
            if (countTextGo != null) serializedQ.FindProperty("_kanjiCountText").objectReferenceValue = countTextGo;
            var ansTextGo = qPanelGo.transform.Find("ResultPanel/AnswererAnswerText")?.GetComponent<TextMeshProUGUI>();
            if (ansTextGo != null) serializedQ.FindProperty("_answererAnswerText").objectReferenceValue = ansTextGo;

            var suggPanelGo = qPanelGo.transform.Find("SuggestionPanel")?.gameObject;
            if (suggPanelGo != null) serializedQ.FindProperty("_suggestionPanel").objectReferenceValue = suggPanelGo;
            var suggContentRect = qPanelGo.transform.Find("SuggestionPanel/Scroll View/Viewport/Content")?.GetComponent<RectTransform>();
            if (suggContentRect != null) serializedQ.FindProperty("_suggestionContent").objectReferenceValue = suggContentRect;

            var flipper = GameObject.FindObjectOfType<KanjiFlipper>();
            if (flipper != null) serializedQ.FindProperty("_kanjiFlipper").objectReferenceValue = flipper;
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
        var flipperAns = GameObject.FindObjectOfType<KanjiFlipper>();

        var serializedObj = new SerializedObject(answererUI);
        serializedObj.FindProperty("_answererPanel").objectReferenceValue = inputPanelGo;
        serializedObj.FindProperty("_waitingPanel").objectReferenceValue = waitingPanelGo;
        serializedObj.FindProperty("_waitingMessageText").objectReferenceValue = waitText;
        serializedObj.FindProperty("_resultPanel").objectReferenceValue = resultPanelGo;
        serializedObj.FindProperty("_answerInputField").objectReferenceValue = tmpInputFieldComponent;
        serializedObj.FindProperty("_submitAnswerButton").objectReferenceValue = submitBtn;
        serializedObj.FindProperty("_resultText").objectReferenceValue = resText;
        serializedObj.FindProperty("_timerText").objectReferenceValue = timerText;
        serializedObj.FindProperty("_kanjiFlipper").objectReferenceValue = flipperAns;
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

        // 保存
        EditorSceneManager.MarkSceneDirty(canvas.scene);
        EditorSceneManager.SaveScene(canvas.scene);

        return "Success";
    }

    public static string CheckReferences()
    {
        var canvas = GameObject.Find("MainCanvas");
        if (canvas == null) return "Canvas not found";
        var menu = canvas.GetComponent<MainMenuUI>();
        if (menu == null) return "MainMenuUI not found";
        var serialized = new SerializedObject(menu);
        return "_mainMenuPanel: " + (serialized.FindProperty("_mainMenuPanel").objectReferenceValue != null ? serialized.FindProperty("_mainMenuPanel").objectReferenceValue.name : "null") + "\n" +
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
}
#endif
