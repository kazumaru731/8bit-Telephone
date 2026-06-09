using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KanjiFlipGame.Core;
using KanjiFlipGame.Kanji;
using System.Collections.Generic;
using System.Linq;

namespace KanjiFlipGame.UI
{
    public class QuestionerUI : MonoBehaviour
    {
        [Header("UI要素")]
        [SerializeField] private TextMeshProUGUI _topicText;
        [SerializeField] private TMP_InputField _kanjiInputField;
        [SerializeField] private Button _addKanjiButton;
        [SerializeField] private Button _completeButton;
        [SerializeField] private TextMeshProUGUI _kanjiCountText;
        [SerializeField] private GameObject _questionerPanel;
        [SerializeField] private GameObject _waitingPanel;
        [SerializeField] private TextMeshProUGUI _waitingMessageText;
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private TextMeshProUGUI _answererAnswerText;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private TextMeshProUGUI _plannedKanjiText; // 出力予定漢字表示用

        [Header("予測変換")]
        [SerializeField] private GameObject _suggestionPanel;
        [SerializeField] private RectTransform _suggestionContent;
        [SerializeField] private GameObject _suggestionButtonPrefab; // nullの場合は動的に生成
        [SerializeField] private TMPro.TMP_FontAsset _fontAsset;

        [Header("参照")]
        [SerializeField] private KanjiFlipper _kanjiFlipper;
        [SerializeField] private KanjiInputValidator _kanjiInputValidator;

        private List<Button> _activeSuggestionButtons = new List<Button>();
        private bool _sortByStrokeCount = false;
        private string _plannedKanji = ""; // 出力予定の一時保存用

        void Start()
        {
            if (_addKanjiButton != null)
                _addKanjiButton.onClick.AddListener(OnAddKanjiClicked);

            if (_completeButton != null)
                _completeButton.onClick.AddListener(OnCompleteClicked);

            if (_kanjiInputField != null)
                _kanjiInputField.onValueChanged.AddListener(OnInputFieldValueChanged);

            GameManager.Instance.OnGameStateChanged.AddListener(OnGameStateChanged);
            GameManager.Instance.OnAnswerResult.AddListener(OnAnswerResult);
            GameManager.Instance.OnPlayerRoleChanged.AddListener(OnPlayerRoleChanged);

            // 予測変換コンテンツのレイアウト設定 (縦一列のリスト表示にする)
            if (_suggestionContent != null)
            {
                var layout = _suggestionContent.GetComponent<VerticalLayoutGroup>();
                if (layout == null)
                {
                    layout = _suggestionContent.gameObject.AddComponent<VerticalLayoutGroup>();
                }
                layout.spacing = 4f;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = true;

                var fitter = _suggestionContent.GetComponent<ContentSizeFitter>();
                if (fitter == null)
                {
                    fitter = _suggestionContent.gameObject.AddComponent<ContentSizeFitter>();
                }
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            // 初期表示ですべての漢字を画数順に表示する
            if (_suggestionPanel != null)
            {
                _suggestionPanel.SetActive(true);
            }
            UpdateSuggestions("");

            UpdateUI();
        }

        void Update()
        {
            UpdateKanjiCount();
        }

        public void OnAddKanjiClicked()
        {
            if (_kanjiFlipper == null || _kanjiInputValidator == null)
                return;

            string input = _plannedKanji;
            if (string.IsNullOrEmpty(input) && _kanjiInputField != null)
            {
                input = _kanjiInputField.text;
            }
            if (string.IsNullOrEmpty(input)) return;

            AddTextToFlipper(input);

            _plannedKanji = "";
            if (_plannedKanjiText != null)
            {
                _plannedKanjiText.text = "";
            }
        }

        private void AddTextToFlipper(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            string err;
            if (_kanjiInputValidator.ValidateInput(text, out err))
            {
                foreach (char c in text)
                {
                    if (!_kanjiFlipper.CanAddKanji()) break;
                    _kanjiFlipper.AddKanji(c.ToString());
                }
                _kanjiInputField.text = "";
                // 入力決定後はリストを全漢字表示に戻す
                UpdateSuggestions("");
            }
            else
            {
                Debug.LogWarning(err);
            }
        }

        private void OnInputFieldValueChanged(string value)
        {
            // 空のときも常に更新（空なら全件画数順表示になる）
            UpdateSuggestions(value);
        }

        private void UpdateSuggestions(string input)
        {
            if (KanjiDatabase.Instance == null || _suggestionContent == null) return;

            // 常に画数の昇順で検索する
            var results = KanjiDatabase.Instance.SearchKanji(input, true);

            if (results.Count == 0)
            {
                if (_suggestionPanel != null) _suggestionPanel.SetActive(false);
                return;
            }

            if (_suggestionPanel != null) _suggestionPanel.SetActive(true);

            // 既存のボタンをクリア
            foreach (var btn in _activeSuggestionButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            _activeSuggestionButtons.Clear();

            // 新しいボタンを生成 (スクロールできるように最大100件)
            foreach (var kanji in results.Take(100))
            {
                GameObject btnGo;
                if (_suggestionButtonPrefab != null)
                {
                    btnGo = Instantiate(_suggestionButtonPrefab, _suggestionContent);
                }
                else
                {
                    btnGo = CreateDefaultSuggestionButton(kanji);
                }

                Button btn = btnGo.GetComponent<Button>();
                var kChar = kanji.@char; // クロージャ用
                btn.onClick.AddListener(() => OnSuggestionSelected(kChar));
                _activeSuggestionButtons.Add(btn);
            }
        }

        private GameObject CreateDefaultSuggestionButton(KanjiDatabase.KanjiInfo info)
        {
            GameObject btnGo = new GameObject("SuggBtn", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnGo.transform.SetParent(_suggestionContent, false);
            
            var le = btnGo.GetComponent<LayoutElement>();
            le.preferredHeight = 40f;
            le.minHeight = 40f;

            var img = btnGo.GetComponent<Image>();
            img.color = new Color(0.95f, 0.95f, 0.95f, 1f);

            var btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.normalColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            cb.highlightedColor = new Color(0.85f, 0.9f, 1f, 1f);
            cb.pressedColor = new Color(0.7f, 0.8f, 0.95f, 1f);
            cb.selectedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            btn.colors = cb;
            
            GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);
            
            var txt = txtGo.GetComponent<TextMeshProUGUI>();
            // 「漢字 (画数画)」としてリスト表示を分かりやすくする
            txt.text = $"{info.@char} ({info.strokeCount}画)";
            txt.fontSize = 20;
            txt.color = Color.black;
            txt.alignment = TextAlignmentOptions.Center;
            
            var r = txtGo.GetComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.sizeDelta = Vector2.zero;

            if (_fontAsset != null)
            {
                txt.font = _fontAsset;
            }
            else
            {
                var fallbackFont = _topicText != null ? _topicText.font : null;
                if (fallbackFont == null) fallbackFont = GameObject.FindObjectOfType<TextMeshProUGUI>()?.font;
                if (fallbackFont != null) txt.font = fallbackFont;
            }
            
            return btnGo;
        }

        private void OnSuggestionSelected(string kanji)
        {
            // 決定ボタンを押して出力させるため、候補選択時は一時変数に保持し、出力予定テキストに表示する
            _plannedKanji = kanji;
            if (_plannedKanjiText != null)
            {
                _plannedKanjiText.text = kanji;
            }
        }

        public void OnCompleteClicked()
        {
            if (_kanjiFlipper == null || _kanjiFlipper.CurrentKanjiCount == 0) return;

            // フリップデータをシリアライズ
            var flipData = _kanjiFlipper.GetFlipData();
            string json = JsonUtility.ToJson(flipData);

            // ネットワーク経由で送信（早押しキューに登録）
            GameManager.Instance.RPC_SubmitFlip(GameManager.Instance.Runner.LocalPlayer, json);

            // 送信完了メッセージを表示（タイトル上部に配置されるWaitingPanelを利用）
            ShowWaitingPanel("回答者が回答中...");
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.Questioning)
            {
                if (_kanjiFlipper != null) _kanjiFlipper.ClearAll();
            }
            UpdateUI();
        }

        private void OnPlayerRoleChanged(PlayerRole newRole)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            bool isLocalTest = GameManager.Instance != null && GameManager.Instance.IsLocalTestMode;

            if (isLocalTest)
            {
                // 1人テストプレイ時：出題フェーズのみ出題者画面を表示する
                GameState state = GameManager.Instance.CurrentState;
                if (state != GameState.Questioning)
                {
                    HideAllPanels();
                    return;
                }
            }
            else if (GameManager.Instance.LocalPlayerRole != PlayerRole.Questioner)
            {
                HideAllPanels();
                return;
            }

            GameState currentState = GameManager.Instance.CurrentState;
            switch (currentState)
            {
                case GameState.Questioning:
                    ShowQuestionerPanel();
                    // お題入力状態なので待機表示を消す
                    if (_waitingPanel != null) _waitingPanel.SetActive(false);
                    break;

                case GameState.Answering:
                    // 出題者が回答中を確認できるよう、操作パネルは隠すがフリップ自体や進行状況（回答中...）をタイトル上で表示する
                    if (_questionerPanel != null) _questionerPanel.SetActive(true);
                    
                    // 次のフリップを待機中に作れるようにするため、入力用の操作UIは開いたままにする
                    // 回答者が回答中という表示をタイトルの上で表示するため、待機パネル（WaitingPanel）を上部に小さく配置するか、テキストを変更する
                    ShowWaitingPanel("回答者が回答中...");
                    
                    // タイトルの上で表示するために、WaitingPanelをアクティブにし、かつQuestionerPanelも表示する（フリップキャンバスなどが見えるようにするため）
                    if (_waitingPanel != null)
                    {
                        _waitingPanel.SetActive(true);
                        // 位置の調整をコードまたはヒエラルキーで行うが、ここではアクティブ化とテキスト設定を確実に行う
                        if (_waitingMessageText != null) _waitingMessageText.text = "回答者が回答中...";
                    }
                    break;
                
                case GameState.ShowingResult:
                    // OnAnswerResultで処理
                    break;

                default:
                    HideAllPanels();
                    break;
            }
        }

        private void AdjustLayoutForLocalTest()
        {
            if (_questionerPanel == null) return;
            
            var rectTrans = _questionerPanel.GetComponent<RectTransform>();
            if (rectTrans != null)
            {
                // 左半分にアンカーを設定
                rectTrans.anchorMin = new Vector2(0f, 0f);
                rectTrans.anchorMax = new Vector2(0.5f, 1f);
                rectTrans.offsetMin = Vector2.zero;
                rectTrans.offsetMax = Vector2.zero;
            }

            if (_waitingPanel != null && _waitingPanel.transform.parent == _questionerPanel.transform.parent)
            {
                var waitingRect = _waitingPanel.GetComponent<RectTransform>();
                if (waitingRect != null)
                {
                    waitingRect.anchorMin = new Vector2(0f, 0f);
                    waitingRect.anchorMax = new Vector2(0.5f, 1f);
                    waitingRect.offsetMin = Vector2.zero;
                    waitingRect.offsetMax = Vector2.zero;
                }
            }

            if (_resultPanel != null && _resultPanel.transform.parent == _questionerPanel.transform.parent)
            {
                var resultRect = _resultPanel.GetComponent<RectTransform>();
                if (resultRect != null)
                {
                    resultRect.anchorMin = new Vector2(0f, 0f);
                    resultRect.anchorMax = new Vector2(0.5f, 1f);
                    resultRect.offsetMin = Vector2.zero;
                    resultRect.offsetMax = Vector2.zero;
                }
            }
        }

        private void ShowQuestionerPanel()
        {
            if (_questionerPanel != null) _questionerPanel.SetActive(true);
            if (_waitingPanel != null) _waitingPanel.SetActive(false);
            if (_resultPanel != null) _resultPanel.SetActive(false);
            if (_topicText != null) _topicText.text = "お題: " + GameManager.Instance.CurrentTopic;
        }

        private void ShowWaitingPanel(string message)
        {
            // 出題パネルを非表示にせず、フリップが見える状態を保ちつつ、待機パネル（状況表示用）のみをアクティブにする
            if (_waitingPanel != null)
            {
                _waitingPanel.SetActive(true);
                if (_waitingMessageText != null) _waitingMessageText.text = message;
            }
            if (_resultPanel != null) _resultPanel.SetActive(false);
        }

        private void ShowResultPanel(string answererAnswer, bool isCorrect)
        {
            if (_questionerPanel != null) _questionerPanel.SetActive(false);
            if (_waitingPanel != null) _waitingPanel.SetActive(false);
            if (_resultPanel != null)
            {
                _resultPanel.SetActive(true);
                if (_answererAnswerText != null) _answererAnswerText.text = " 回答者の答え: " + answererAnswer;
                if (_resultText != null) _resultText.text = isCorrect ? "○" : "×";
            }
        }

        private void HideAllPanels()
        {
            if (_questionerPanel != null) _questionerPanel.SetActive(false);
            if (_waitingPanel != null && _waitingPanel.name != "RoomWaitingPanel") _waitingPanel.SetActive(false);
            if (_resultPanel != null) _resultPanel.SetActive(false);
        }

        private void UpdateKanjiCount()
        {
            if (_kanjiCountText != null && _kanjiFlipper != null)
            {
                _kanjiCountText.text = _kanjiFlipper.CurrentKanjiCount + " / " + _kanjiFlipper.MaxKanjiCount;
            }
        }

        private void OnAnswerResult(bool isCorrect)
        {
            ShowResultPanel(GameManager.Instance.LastAnswer, isCorrect);
        }

        void OnDestroy()
        {
            if (_addKanjiButton != null) _addKanjiButton.onClick.RemoveListener(OnAddKanjiClicked);
            if (_completeButton != null) _completeButton.onClick.RemoveListener(OnCompleteClicked);
            if (_kanjiInputField != null) _kanjiInputField.onValueChanged.RemoveListener(OnInputFieldValueChanged);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged.RemoveListener(OnGameStateChanged);
                GameManager.Instance.OnAnswerResult.RemoveListener(OnAnswerResult);
                GameManager.Instance.OnPlayerRoleChanged.RemoveListener(OnPlayerRoleChanged);
            }
        }
    }
}
